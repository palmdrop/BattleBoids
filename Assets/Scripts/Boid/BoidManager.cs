using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using UnityEngine;
using Random = System.Random;

public class BoidManager : MonoBehaviour
{
    public static BoidManager SharedInstance;
    private List<Player> players;

    private readonly List<Boid> _boids = new List<Boid>();

    private List<HealthBar_V2> _healthBarScripts;
    
    // Random number generator
    private readonly Random _random = new Random();

    // Data structure for efficiently looking up neighbouring boids
    private BoidGrid _grid;

    //Needed for burst rays
    private static Unity.Physics.Systems.BuildPhysicsWorld _bpw;

    private bool firstRun = true;

    // Start is called before the first frame update
    void Start()
    {
        players = GetComponentInParent<GameManager>().GetPlayers();
        _bpw = Unity.Entities.World.DefaultGameObjectInjectionWorld.GetExistingSystem<Unity.Physics.Systems.BuildPhysicsWorld>();
    }

    private void Awake()
    {
        SharedInstance = this;
    }

    // Fetches the boids from the respective players and places them in the boids list
    public void AddPlayerBoids()
    {
        // Clear all boids and add them to the list
        _boids.Clear();

        foreach (Player p in players)
        {
            foreach (Boid b in p.GetFlock()) {
                b.StartBoid();
                _boids.Add(b);
            }
            p.FlockUpdate = false;
        }
    }

    public List<Boid> GetBoids()
    {
        return _boids;
    }

    public Unity.Physics.Systems.BuildPhysicsWorld GetPhysicsWorld()
    {
        return _bpw;
    }

    public BoidGrid GetGrid()
    {
        return _grid;
    }

    // Clear dead boids
    private void ClearDeadBoids()
    {
        _boids.RemoveAll(b => b.IsDead());
    }

    void FixedUpdate()
    {
        //So the grid does not have to be populated more than once
        if (!firstRun)
            _grid.Dispose();
        
        // Remove all dead boids
        ClearDeadBoids();
        // Allocate arrays for all the data required to calculate the boid behaviors
        NativeArray<Boid.BoidInfo> boidInfos = new NativeArray<Boid.BoidInfo>(_boids.Count, Allocator.TempJob); // In data
        // Get all the boid info from the boids
        for (int i = 0; i < _boids.Count; i++)
        {
            boidInfos[i] = _boids[i].GetInfo();
        }

        // A force is calculated for each boid and then later applied 
        NativeArray<float3> forces = new NativeArray<float3>(_boids.Count, Allocator.TempJob); // Out data
        // Information about the entire flocks is gathered
        NativeArray<Player.FlockInfo> flockInfos = new NativeArray<Player.FlockInfo>(players.Count, Allocator.TempJob); // Out and in data

        // Construct and populate the grid to do efficient neighbour queries
        _grid = new BoidGrid();
        _grid.Populate(_boids);

        // Allocate a struct job for calculating flock info
        FlockStructJob flockJob = new FlockStructJob()
        {
            boids = boidInfos,
            flockInfos = flockInfos
        };
        
        // Schedule job 
        JobHandle jobHandle = flockJob.Schedule();
        jobHandle.Complete();

        // Allocate a struct job for calculating boid forces and attack target
        NativeArray<float> randomFloats = new NativeArray<float>(_boids.Count * 2, Allocator.TempJob);
        for (int i = 0; i < randomFloats.Length; i++)
        {
            randomFloats[i] = (float)_random.NextDouble();
        }
        NativeArray<int> targetIndices = new NativeArray<int>(_boids.Count, Allocator.TempJob);
        NativeArray<int> friendlyTargetIndices = new NativeArray<int>(_boids.Count, Allocator.TempJob);
        NativeArray<Boid.ClassInfo> DisposableBoidClassInfos = new NativeArray<Boid.ClassInfo>(ClassInfos.infos, Allocator.TempJob);
        NativeArray<bool> falling = new NativeArray<bool>(_boids.Count, Allocator.TempJob);

        BoidStructJob boidJob = new BoidStructJob
        {
            random = randomFloats,
            flocks = flockInfos,
            boids = boidInfos,
            forces = forces,
            targetIndices = targetIndices,
            friendlyTargetIndices = friendlyTargetIndices,
            grid = _grid,
            cw = _bpw.PhysicsWorld.CollisionWorld,
            boidClassInfos = DisposableBoidClassInfos,
            falling = falling
        };

        // Schedule job
        jobHandle = boidJob.Schedule(_boids.Count, _boids.Count / 10);
        jobHandle.Complete();

        // Update player data with new flock info
        for (int i = 0; i < flockInfos.Length; i++)
        {
            players[i].SetFlockInfo(flockInfos[i]);
        }
        
        // Update boids with forces and target
        for (int i = 0; i < _boids.Count; i++)
        {
            _boids[i].SetFalling(falling[i]);
            _boids[i].UpdateBoid(forces[i]);
            _boids[i].SetTarget(targetIndices[i] != -1 ? _boids[targetIndices[i]] : null);
            _boids[i].SetFriendlyTarget(friendlyTargetIndices[i] != -1 ? _boids[friendlyTargetIndices[i]] : null);
        }

        // Dispose of all data
        randomFloats.Dispose();
        boidInfos.Dispose();
        forces.Dispose();
        flockInfos.Dispose();
        targetIndices.Dispose();
        friendlyTargetIndices.Dispose();
        DisposableBoidClassInfos.Dispose();
        falling.Dispose();
        //_grid.Dispose();

        firstRun = false;
    }

    // This job calculates information specific for the entire flock, such as 
    // average velocity, average position and entity count
    [BurstCompile]
    private struct FlockStructJob : IJob
    {
        [ReadOnly] public NativeArray<Boid.BoidInfo> boids;
        [WriteOnly] public NativeArray<Player.FlockInfo> flockInfos;

        public void Execute()
        {
            // Temporary array for holding flock info data
            NativeArray<Player.FlockInfo> tempFlockInfos = new NativeArray<Player.FlockInfo>(flockInfos.Length, Allocator.Temp);
            NativeArray<float> minSquaredDistanceToAverage = new NativeArray<float>(flockInfos.Length, Allocator.Temp);
            
            // Iterate over all of the boids in order to calculate flock info data
            for (int i = 0; i < boids.Length; i++)
            {
                Boid.BoidInfo boid = boids[i];
                
                // Translate the flockid to an index in the array
                Player.FlockInfo flockInfo = tempFlockInfos[boid.flockId - 1];
                
                // Contribute to flock data
                flockInfo.avgPos += boid.pos;
                flockInfo.avgVel += boid.vel;
                flockInfo.boidCount++;

                // Save new data in array (necessary since "flockInfo" is a temporary value)
                tempFlockInfos[boid.flockId - 1] = flockInfo;
            }
            
            // Iterate over all the temporary flock info structs and average the results
            for (int i = 0; i < tempFlockInfos.Length; i++)
            {
                Player.FlockInfo flockInfo = tempFlockInfos[i];
                if (flockInfo.boidCount != 0)
                {
                    flockInfo.avgPos /= flockInfo.boidCount;
                    flockInfo.avgVel /= flockInfo.boidCount;
                }

                tempFlockInfos[i] = flockInfo;
                minSquaredDistanceToAverage[i] = float.MaxValue;
            }
            
            // Iterate over all boids again to find the median boid position of each flock
            for (int i = 0; i < boids.Length; i++)
            {
                Boid.BoidInfo boid = boids[i];
                Player.FlockInfo flockInfo = tempFlockInfos[boid.flockId - 1];
                
                // Calculate the squared distance between the boid and the average position of all allied boids
                float distSqToAverage = math.distancesq(boid.pos, flockInfo.avgPos);
                
                // If the calculated value is less than the stored one, update
                if (distSqToAverage < minSquaredDistanceToAverage[boid.flockId - 1])
                {
                    flockInfo.medianPos = boid.pos;
                    minSquaredDistanceToAverage[boid.flockId - 1] = distSqToAverage;
                    tempFlockInfos[boid.flockId - 1] = flockInfo;
                }
            }
            
            // Finally, assign the data to the output array
            for (int i = 0; i < tempFlockInfos.Length; i++)
            {
                flockInfos[i] = tempFlockInfos[i];
            }

            // Dispose of temporary data
            tempFlockInfos.Dispose();
        }
    }

    // This job calculates all the forces acting on the boids and the attack target
    [BurstCompile]
    private struct BoidStructJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float> random;
        [ReadOnly] public NativeArray<Player.FlockInfo> flocks;
        [ReadOnly] public NativeArray<Boid.BoidInfo> boids;
        [ReadOnly] public NativeArray<Boid.ClassInfo> boidClassInfos;
        [WriteOnly] public NativeArray<float3> forces;
        [WriteOnly] public NativeArray<int> targetIndices;
        [WriteOnly] public NativeArray<int> friendlyTargetIndices;
        [ReadOnly] public BoidGrid grid;
        [ReadOnly] public Unity.Physics.CollisionWorld cw;
        [WriteOnly] public NativeArray<bool> falling;

        public void Execute(int index)
        {
            // Current boid
            Boid.BoidInfo boid = boids[index];
            Boid.ClassInfo classInfo = boidClassInfos[(int)boid.type];
            NativeArray<int> neighbours = grid.FindBoidsWithinRadius(boid, classInfo.viewRadius);

            // Calculate the distances between the current boid and the neighbours once at the start,
            // and send these distances to each behavior function. This avoids having to recalculate the distances for
            // each behavior.
            NativeArray<float> distances = CalculateDistances(boid, neighbours);

            int targetBoidIndex = -1;
            int friendlyTargetBoidIndex = -1;
            float targetViewDistance = 0.0f;
            if (boid.type == Boid.Type.Healer || boid.type == Boid.Type.Hero)
            {
                friendlyTargetBoidIndex = FindFriendlyTargetIndex(boid, classInfo, neighbours, distances);
            }

            if (classInfo.attackDistRange > classInfo.viewRadius)
            {
                NativeArray<int> tmp = grid.FindBoidsWithinRadius(boid, classInfo.attackDistRange);
                targetBoidIndex = FindEnemyTargetIndex(boid, classInfo, tmp, CalculateDistances(boid, tmp));
            }
            else
                targetBoidIndex = FindEnemyTargetIndex(boid, classInfo, neighbours, distances);

            targetViewDistance = classInfo.attackDistRange;

            float3 desire;

            // If the boid is scared by a nearby scarecrow, only the fear behavior applies
            if (IsScared(boid, classInfo, neighbours))
            {
                desire = FearForce(boid, classInfo, neighbours, distances, true);
            }
            // Otherwise, all behaviors apply
            else
            {
                desire =
                    // Reynolds behaviors
                    AlignmentForce(boid, classInfo, neighbours, distances)
                    + CohesionForce(boid, classInfo, neighbours, distances)
                    + SeparationForce(boid, classInfo, neighbours, distances)
                    
                    // Additional behaviors
                    + (IsConfident(boid, neighbours)
                        // If confidence is high, be aggressive and have normal fear levels
                        ? AggressionForce(boid, classInfo, targetBoidIndex) + 1 * FearForce(boid, classInfo, neighbours, distances, false) 
                        // If confidence is low, search for the ally flock and duplicate fear levels
                        : SearchForce(boid, classInfo)     + 2 * FearForce(boid, classInfo, neighbours, distances, false))
                    
                    + ApproachForce(boid, classInfo, targetBoidIndex, targetViewDistance)
                    + AvoidanceForce(boid, classInfo, neighbours, distances) 
                    + RandomForce(index, classInfo.randomMovements);
            }
            
            if (HeadedForCollisionWithMapBoundary(boid, classInfo))
            {
                desire += AvoidCollisionDir(boid, classInfo) * classInfo.avoidCollisionWeight;
            }

            // Boids will accelerate if they are moving in their desired direction
            float dot = math.dot(desire, boid.vel);
            if (dot > 0.5)
            {
                desire += boid.forward * (dot - 0.5f) * 2.0f * classInfo.accelerationDesire;
            }

            float3 force = desire - boid.vel;

            // Limit the force to max force
            if (math.lengthsq(force) > classInfo.maxForce)
            {
                force = math.normalize(force) * classInfo.maxForce;
            }
            
            float3 hoverForce = HoverForce(boid, classInfo);
            if (hoverForce.y == 0) falling[index] = true;
            force += hoverForce;

            forces[index] = force;

            // Update attack info
            targetIndices[index] = targetBoidIndex;
            friendlyTargetIndices[index] = friendlyTargetBoidIndex;
            
            neighbours.Dispose();
            distances.Dispose();
        }


        // Calculates the power of a certain behavior. This is a combination of the weight for that behavior,
        // the distance to the target, and the falloff exponent
        private static float CalculatePower(float weight, float normalizedDist, float exponent)
        {
            return weight * math.pow(1.0f - normalizedDist, exponent);
        }

        // Calculates a random force based on an array of random values
        private float3 RandomForce(int index, float strength)
        {
            float angle = math.PI * 2 * random[index * 2];
            float amount = random[index * 2 + 1];
            
            // Ignore they y component, since the boids only move in 2 dimensions
            return new float3(math.cos(angle), 0.0f, math.sin(angle)) * amount * strength;
        }

        private static bool BoidIndexInAttackRange(
                float3 vectorFromSelfToEnemy,
                float dist,
                float3 forward,
                float attackDstRange,
                float attackAngleRange) 
        {
            // Calc angle to target
            float dp = math.dot(vectorFromSelfToEnemy, forward);
            float angle = math.acos(dp / dist);

            // Ignores
            if (dist > attackDstRange || angle > attackAngleRange) // If distance or angle is too big
            { 
                return false;
            }
            
            return true;
        }

        
        // Calculates the distances between the current boid and all its neighbours
        private NativeArray<float> CalculateDistances(Boid.BoidInfo boid, NativeArray<int> neighbours)
        {
            NativeArray<float> distances = new NativeArray<float>(neighbours.Length, Allocator.Temp);
            
            // Iterate over all the neighbours to calculate the distances from the current boid
            for (int i = 0; i < neighbours.Length; i++)
            {
                // Get current neighbor
                Boid.BoidInfo neighbour = boids[neighbours[i]];
                
                // Calculate the distance and add to array
                float3 vector = (neighbour.pos - boid.pos);
                float sqrDist = vector.x * vector.x + vector.y * vector.y + vector.z * vector.z;
                distances[i] = math.sqrt(sqrDist);
            }

            return distances;
        }
        
        private bool IsConfident(Boid.BoidInfo boid, NativeArray<int> neighbours)
        {
            // If the boid is the last one left, they will automatically become confident.
            // The last boid has nothing to loose, and might as well be aggressive.
            // This also avoids stalemates
            if (flocks[boid.flockId - 1].boidCount == 1)
            {
                return true;
            }

            // Iterate over all the neighbouring boids and check if at least one of them are friendly
            for (int i = 0; i < neighbours.Length; i++)
            {
                int index = neighbours[i];
                Boid.BoidInfo neighbour = boids[index];
                // If a friendly boid is found, then the current boid grows confident (it is not alone)
                if (boid.flockId == neighbour.flockId)
                {
                    return true;
                }
            }

            // If no ally is found, the boid becomes scared and tries to find its flock
            return false;
        }
        
        private bool IsScared(Boid.BoidInfo boid, Boid.ClassInfo classInfo, NativeArray<int> neighbours)
        {
            if (boid.type == Boid.Type.Scarecrow) return false;
            for (int i = 0; i < neighbours.Length; i++)
            {
                Boid.BoidInfo neighbour = boids[neighbours[i]];
                if (boid.flockId != neighbour.flockId && neighbour.type == Boid.Type.Scarecrow) return true;
            }

            return false;
        }
       
        private float3 AlignmentForce(Boid.BoidInfo boid, Boid.ClassInfo classInfo, NativeArray<int> neighbours, NativeArray<float> distances)
        {
            // Average velocity is used to calculate alignment force
            float3 avgVel = float3.zero;

            for (int i = 0; i < neighbours.Length; i++)
            {
                Boid.BoidInfo neighbour = boids[neighbours[i]];

                // No alignment behavior with enemy boids
                if (boid.flockId != neighbour.flockId) continue;
                
                // The distance between the current boid and the neighbour
                float distance = distances[i];
                float normalizedViewDistance = distance / classInfo.viewRadius;
                
                // Contribute to average velocity
                float amount = CalculatePower(boid.morale,
                    normalizedViewDistance, classInfo.alignmentExponent);
                
                //TODO possible variation: use heading of neighbouring boids instead of velocity! this way, faster
                //TODO boids do not have more influence (although this might be desirable)
                avgVel += neighbour.vel * amount * boidClassInfos[(int)neighbour.type].gravity;
            }
            
            // Calculate alignment force
            if (avgVel.Equals(float3.zero)) return float3.zero;
            return math.normalize(avgVel) * classInfo.alignmentStrength;
        }

        private float3 CohesionForce(Boid.BoidInfo boid, Boid.ClassInfo classInfo, NativeArray<int> neighbours, NativeArray<float> distances)
        {
            // Average neighbour position 
            float3 avgNeighborPos = float3.zero;
            float avgNeighborPosDivider = 0.0f;

            for (int i = 0; i < neighbours.Length; i++)
            {
                Boid.BoidInfo neighbour = boids[neighbours[i]];
                
                // No cohesion behavior with enemy boids
                if (boid.flockId != neighbour.flockId) continue;
                
                float distance = distances[i];
                float normalizedViewDistance = distance / classInfo.viewRadius;

                // Add to average position for cohesion, weighted using morale
                float amount = CalculatePower(boid.morale, normalizedViewDistance,
                    classInfo.cohesionExponent);

                avgNeighborPos += neighbour.pos * amount * boidClassInfos[(int)neighbour.type].gravity;
                avgNeighborPosDivider += amount * boidClassInfos[(int)neighbour.type].gravity;
            }

            // Calculate cohesion force
            if (avgNeighborPosDivider == 0) return float3.zero;

            avgNeighborPos /= avgNeighborPosDivider;
            
            if (avgNeighborPos.Equals(boid.pos))
            {
                return float3.zero;
            }
            
            return  math.normalize(avgNeighborPos - boid.pos) * classInfo.cohesionStrength;
        }
        
        private float3 SeparationForce(Boid.BoidInfo boid, Boid.ClassInfo classInfo, NativeArray<int> neighbours, NativeArray<float> distances)
        {
            // Separation force acting on the boid
            float3 avgSeparation = float3.zero;
            float separationDivider = 0.0f;

            for (int i = 0; i < neighbours.Length; i++)
            {
                Boid.BoidInfo neighbour = boids[neighbours[i]];
                
                // No separation behavior with enemy boids (we have fear for that)
                if (boid.flockId != neighbour.flockId) continue;
                
                float distance = distances[i];
                
                // Continue if outside separation radius
                if (distance > classInfo.separationRadius) continue;
                
                // Calculate a normalized separation distance, i.e a value from 0 to 1
                float normalizedSeparationDistance = distance / classInfo.separationRadius;
                    
                // The power of the separation should be stronger the closer the two boids are to each other,
                // inversely proportional to the distance (with respect to the separation exponent)
                float amount = CalculatePower(1,
                    1.0f - normalizedSeparationDistance, -classInfo.separationExponent);
                    
                // The separation force between the two boids
                float3 separation = (boid.pos - neighbour.pos) / distance;

                avgSeparation += separation * amount;
                separationDivider += amount;
            }

            // Calculate separation force
            if (separationDivider == 0) return float3.zero;
            return (avgSeparation / separationDivider) * classInfo.separationStrength;
        }

        private float3 AggressionForce(Boid.BoidInfo boid, Boid.ClassInfo classInfo, int targetIndex)
        {
            // If the boid has found a target, disable aggression force (the approach force will then take precedence)
            if (targetIndex != -1) return float3.zero;
            
            // Calculate aggression force
            // TODO this line assumes there's only two flocks and that the ID of the flock corresponds to the index 
            // TODO in the flocks array. Find better solution
            Player.FlockInfo enemyFlock = flocks[boid.flockId == 1 ? 1 : 0];

            // If there are no enemy boids or if the median enemy flock position is equal to that of the current boid,
            // then return a zero vector
            if (enemyFlock.boidCount == 0 || enemyFlock.medianPos.Equals(boid.pos)) return float3.zero;
            
            // Calculate the distance between the current boid and the enemy flock
            float dist = math.distance(boid.pos, enemyFlock.medianPos);
            
            // Calculate expected future location of enemy flock
            float3 expectedEnemyFlockPos = enemyFlock.medianPos;
            {
                // Compare the position of the current boid in relation to the position of the enemy flock
                // If the boid is moving away from the enemy flock, do no prediction, just try to turn
                float3 directionToEnemyFlock = enemyFlock.avgPos - boid.pos;
                float dot = math.dot(boid.vel, directionToEnemyFlock);
                
                if (!float3.zero.Equals(boid.vel) && dot > 0.0)
                { 
                    // Make a crude calculation of the time required to reach the enemy boid
                    float timeToReach = math.min(dist / math.lengthsq(boid.vel), classInfo.aggressionDistanceCap);
                    
                    expectedEnemyFlockPos += enemyFlock.avgVel * timeToReach;
                }
            }

            // If the boid is closer than the aggression distance cap, then apply aggression falloff
            float scale = 1.0f;
            if (dist < classInfo.aggressionDistanceCap)
            {
                scale = math.pow(dist / classInfo.aggressionDistanceCap, classInfo.aggressionFalloff);
            }

            // Scale the aggression based on the difference between the flock sizes. Make sure the scaling is at least 1.0,
            // and not more than the max aggression multiplier
            scale *= math.max(1.0f, math.min(classInfo.maxAggressionMultiplier, (float) flocks[boid.flockId - 1].boidCount / enemyFlock.boidCount));
            
            // Calculate the initial aggression force
            float3 force = math.normalize(expectedEnemyFlockPos - boid.pos) * classInfo.aggressionStrength * scale;
            
            // Finally, if the enemy flock is beside or behind the boid, it will try to break to more efficiently
            // turn in the direction of the enemy flock
            /*float d = math.dot(boid.vel, force);
            if (d < 0.5)
            {
                float breakScale = math.pow((1.0f - (d + 1.0f) / 2.0f), 4.0f);
                force += -boid.vel * breakScale;
            }*/

            return force;
        }
        
        private float3 SearchForce(Boid.BoidInfo boid, Boid.ClassInfo classInfo)
        {
            Player.FlockInfo flock = flocks[boid.flockId - 1];

            if (flock.boidCount <= 1) return float3.zero;

            // Use the median position for search force, unless the current boid
            // is the boid which has the median position. In that case, use the average position.
            float3 searchPosition = flock.medianPos;
            if(searchPosition.Equals(boid.pos)) searchPosition = flock.avgPos;
            
            // If the search position is still equal to the boids position, apply no force
            if (searchPosition.Equals(boid.pos)) return float3.zero;
            
            return math.normalize(searchPosition - boid.pos) * classInfo.searchStrength;
        }

        private float3 FearForce(Boid.BoidInfo boid, Boid.ClassInfo classInfo, NativeArray<int> neighbours, NativeArray<float> distances, bool isScared)
        {
            // Fear force acting on the boid (a boid fears enemy boids)
            float3 avgFear = float3.zero;
            float avgFearDivider = 0.0f;
            float fearMultiplier = 1f;

            for (int i = 0; i < neighbours.Length; i++)
            {
                Boid.BoidInfo neighbour = boids[neighbours[i]];
                
                // No fear for friendly boids
                if (boid.flockId == neighbour.flockId) continue;

                Boid.ClassInfo neighbourClassInfo = boidClassInfos[(int) neighbour.type];
                if (neighbour.type == Boid.Type.Scarecrow       // Is Scarecrow
                    && neighbourClassInfo.abilityDistance > distances[i]) { // and dist < Scarecrow ability dist
                    fearMultiplier = neighbourClassInfo.fearMultiplier;
                }
                
                float distance = distances[i];

                // If the boid is scared, use the view radius, otherwise, use the regular fear radius
                float radius = isScared ? classInfo.viewRadius : classInfo.fearRadius;
                
                // Continue if outside fear radius
                if (distance > radius) continue;
                
                // Normalize distance with respect to fear radius
                float normalizedFearDistance = distance / radius;
                    
                // Calculate the strength of the fear. This is inversely proportional to some exponent of the normalized distance
                float amount =
                    CalculatePower(1.0f, 1.0f - normalizedFearDistance, -classInfo.fearExponent);

                // Fear force between the two boids
                float3 fear = (boid.pos - neighbour.pos) / distance;

                avgFear += fear * amount;
                avgFearDivider += amount;
            }
            
            //TODO do we want to normalize all the forces before scaling with the behavior strength?
            //TODO this will make the behavior force equally strong at all times... Might not be what we want
            // Calculate fear force
            // This force is similar to the separation force, but only acts on enemy boids
            if (avgFearDivider == 0.00) return float3.zero;
            return (avgFear / avgFearDivider) * classInfo.fearStrength * fearMultiplier;
        }
        private int FindEnemyTargetIndex(Boid.BoidInfo boid, Boid.ClassInfo classInfo, NativeArray<int> neighbours, NativeArray<float> distances)
        {
            // Init attack info
            int targetIndex = -1; // index of target boid in _boids
            float distToClosestEnemyInRange = classInfo.attackDistRange;

            for (int i = 0; i < neighbours.Length; i++)
            {
                Boid.BoidInfo neighbour = boids[neighbours[i]];
                
                // Friendly boids cannot be targets
                if (boid.flockId == neighbour.flockId) continue;
                
                float distance = distances[i];
                
                // If the enemy boid is closer than the currently stored one...
                if (distance > distToClosestEnemyInRange) continue;
                
                bool enemyInRange = BoidIndexInAttackRange(neighbour.pos - boid.pos,
                    distance,
                    boid.forward,
                    classInfo.attackDistRange,
                    classInfo.attackAngleRange);
                    
                if (enemyInRange) // and in range, update target
                { 
                    targetIndex = neighbours[i];
                    distToClosestEnemyInRange = distance;
                }
            }

            return targetIndex;
        }

        private int FindFriendlyTargetIndex(Boid.BoidInfo boid, Boid.ClassInfo classInfo, NativeArray<int> neighbours, NativeArray<float> distances)
        {
            int healIndex = -1;
            int lowestAllyHealth = int.MaxValue;

            for (int i = 0; i < neighbours.Length; i++)
            {
                Boid.BoidInfo neighbour = boids[neighbours[i]];
                
                // Enemy boids cannot be healed
                // And do not try to heal boids with max health
                if (boid.flockId != neighbour.flockId 
                    || distances[i] > classInfo.abilityDistance)
                    continue;
                if (boid.type == Boid.Type.Healer &&
                    neighbour.health == boidClassInfos[(int)neighbour.type].maxHealth)
                    continue;
                if (boid.type == Boid.Type.Hero &&
                    neighbour.isBoosted)
                    continue;

                // Store the neighbour with the lowest health
                if (neighbour.health < lowestAllyHealth)
                {
                    healIndex = neighbours[i];
                    lowestAllyHealth = neighbour.health;
                }
            }

            return healIndex;
        }

        private float3 ApproachForce(Boid.BoidInfo boid, Boid.ClassInfo classInfo, int targetBoidIndex, float targetDistRange)
        {
            // Calculate attack force
            // The attack force tries to move the boid towards the target boid
            if (targetBoidIndex == -1) return float3.zero;

            //TODO find better solution than to recalculate distance here
            //TODO problem is that targetBoidIndex corresponds to index in boid array, not necessarily in distances array
            
            float3 vector = boids[targetBoidIndex].pos - boid.pos;
            float dist = math.length(vector);
            return vector * 
                   CalculatePower(classInfo.approachMovementStrength, 
                       1.0f - dist / targetDistRange,
                       classInfo.approachMovementExponent);
        }

        // Calculates the a force which tries to steer boids away from enemy field of view
        private float3 AvoidanceForce(Boid.BoidInfo boid, Boid.ClassInfo classInfo, NativeArray<int> neighbours,
            NativeArray<float> distances)
        {
            float3 force = float3.zero;

            // Iterate over all the neighbouring boids
            for (int i = 0; i < neighbours.Length; i++)
            {
                Boid.BoidInfo neighbour = boids[neighbours[i]];

                // Ignore if the neighbour is a ally
                if (boid.flockId == neighbour.flockId) continue;

                // Values required to determine if the current boid is in attack range of enemy boid
                float distance = distances[i];
                // The boid will assume the enemy has the same attack distance and attack angle as itself
                float attackDistRange = classInfo.attackDistRange;
                float attackAngleRange = classInfo.attackAngleRange; 

                // Determine if boid is in range of enemy and if the enemy is in range of the boid
                bool selfInRange = BoidIndexInAttackRange(boid.pos - neighbour.pos, distance, neighbour.forward, attackDistRange, attackAngleRange);
                bool enemyInRange = BoidIndexInAttackRange(neighbour.pos - boid.pos, distance, boid.forward, attackDistRange, attackAngleRange);

                // If the boid itself is in range of the enemy, but the enemy is not in range of the boid itself,
                // then we want to turn away from the view of the enemy
                if (selfInRange && !enemyInRange)
                {
                    // Turn away from the enemy boid
                    float3 turnDir = boid.forward - neighbour.forward;
                    if (turnDir.x == 0 && turnDir.y == 0 && turnDir.z == 0) 
                    {
                        //TODO handle this (very unlikely) case
                        //TODO this happens when the two boids have the same forward direction
                        return float3.zero;
                    }

                    // Scale force using avoidance strength
                    force += math.normalize(turnDir) * classInfo.avoidanceStrength;
                }
            }

            return force;
        }

        private bool HeadedForCollisionWithMapBoundary(Boid.BoidInfo boid, Boid.ClassInfo classInfo)
        {
            float rayCastTheta = 10f;

            for (int i = 0; i < 3; i++) //Send 3 rays. This is to avoid tangentially going too close to an obstacle.
            {
                float angle = ((i + 1) / 2) * rayCastTheta;    // series 0, theta, theta, 2*theta, 2*theta...
                int sign = i % 2 == 0 ? 1 : -1;                 // series 1, -1, 1, -1...

                float3 dir = math.normalize(RotationMatrix_y(angle * sign, boid.vel));

                Unity.Physics.RaycastInput ray = new Unity.Physics.RaycastInput
                {
                    Start = boid.pos,
                    End = boid.pos + dir * classInfo.collisionAvoidanceDistance,
                    Filter = new Unity.Physics.CollisionFilter
                    {
                        BelongsTo = classInfo.collisionMask,
                        CollidesWith = classInfo.collisionMask
                    }
                };

                if (cw.CastRay(ray))   //Cast rays to nearby boundaries
                {
                    return true;
                }
            }
            return false;
        }

        private float3 AvoidCollisionDir(Boid.BoidInfo boid, Boid.ClassInfo classInfo)
        {
            float rayCastTheta = 10f;

            for (int i = 3; i < 300 / rayCastTheta; i++)
            {
                float angle = ((i + 1) / 2) * rayCastTheta;    // series 0, theta, theta, 2*theta, 2*theta...
                int sign = i % 2 == 0 ? 1 : -1;                 // series 1, -1, 1, -1...

                float3 dir = math.normalize(RotationMatrix_y(angle * sign, boid.vel));

                Unity.Physics.RaycastInput ray = new Unity.Physics.RaycastInput
                {
                    Start = boid.pos,
                    End = boid.pos + dir * classInfo.collisionAvoidanceDistance,
                    Filter = new Unity.Physics.CollisionFilter
                    {
                        BelongsTo = classInfo.collisionMask,
                        CollidesWith = classInfo.collisionMask
                    }
                };

                if (!(cw.CastRay(ray)))   //Cast rays to nearby boundaries
                {
                    //Should only affect turn component of velocity. Should not accellerate forwards or backwards.
                    return sign < 0 ? boid.right : -boid.right;
                }
            }
            return new float3(0, 0, 0);
        }

        private float3 HoverForce(Boid.BoidInfo boid, Boid.ClassInfo classInfo)
        {

            Unity.Physics.RaycastInput ray = new Unity.Physics.RaycastInput
            {
                Start = boid.pos,
                End = boid.pos + new float3(0,-1,0),
                Filter = new Unity.Physics.CollisionFilter
                {
                    BelongsTo = classInfo.groundMask,
                    CollidesWith = classInfo.groundMask
                }
            };
            Unity.Physics.RaycastHit hit;
            if (cw.CastRay(ray, out hit))   //Cast rays to nearby boundaries
            {
                float deltaY = classInfo.targetHeight - (math.length(boid.pos - hit.Position));
                float velY = boid.vel.y;

                //Formula to determine whether to hover or fall, uses a PI-regulator with values Ki and Kp
                Vector3 yForce = new Vector3(0, (deltaY > 0 ? (classInfo.hoverKp * deltaY - classInfo.hoverKi * velY) : 0), 0);
                
                return yForce;
            }

            return new float3(0,0,0);
        }

        private float3 RotationMatrix_y(float angle, float3 vector)
        {
            float cos = math.cos(angle * math.PI / 180);
            float sin = math.sin(angle * math.PI / 180);

            return new float3(vector.x * cos - vector.z * sin, 0, vector.x * sin + vector.z * cos);
        }
    }
}
