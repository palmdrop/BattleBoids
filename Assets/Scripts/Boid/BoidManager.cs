using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using UnityEngine;
using Random = System.Random;

public class BoidManager : MonoBehaviour
{
    private List<Player> players;

    private readonly List<Boid> _boids = new List<Boid>();
    
    // Random number generator
    private readonly Random _random = new Random();

    // Data structure for efficiently looking up neighbouring boids
    private BoidGrid _grid;

    //Needed for burst rays
    private static Unity.Physics.Systems.BuildPhysicsWorld _bpw;

    // Start is called before the first frame update
    void Start()
    {
        players = GetComponentInParent<GameManager>().GetPlayers();
        _bpw = Unity.Entities.World.DefaultGameObjectInjectionWorld.GetExistingSystem<Unity.Physics.Systems.BuildPhysicsWorld>();
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

    // Clear dead boids
    private void ClearDeadBoids()
    {
        _boids.RemoveAll(b => b.IsDead());
    }

    // Update is called once per frame
    void Update()
    {
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
        NativeArray<float> morale = new NativeArray<float>(_boids.Count, Allocator.TempJob);

        BoidStructJob boidJob = new BoidStructJob
        {
            random = randomFloats,
            flocks = flockInfos,
            boids = boidInfos,
            forces = forces,
            targetIndices = targetIndices,
            friendlyTargetIndices = friendlyTargetIndices,
            morale = morale,
            grid = _grid,
            cw = _bpw.PhysicsWorld.CollisionWorld
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
            _boids[i].UpdateBoid(forces[i]);
            _boids[i].SetTarget(targetIndices[i] != -1 ? _boids[targetIndices[i]] : null);
            _boids[i].SetFriendlyTarget(friendlyTargetIndices[i] != -1 ? _boids[friendlyTargetIndices[i]] : null);
            _boids[i].SetMorale(morale[i]);
        }

        // Dispose of all data
        randomFloats.Dispose();
        boidInfos.Dispose();
        forces.Dispose();
        flockInfos.Dispose();
        targetIndices.Dispose();
        friendlyTargetIndices.Dispose();
        _grid.Dispose();
        morale.Dispose();
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
            // Also assign the data to the output array
            for (int i = 0; i < tempFlockInfos.Length; i++)
            {
                Player.FlockInfo flockInfo = tempFlockInfos[i];
                if (flockInfo.boidCount != 0)
                {
                    flockInfo.avgPos /= flockInfo.boidCount;
                    flockInfo.avgVel /= flockInfo.boidCount;
                }

                flockInfos[i] = flockInfo;
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
        [WriteOnly] public NativeArray<float3> forces;
        [WriteOnly] public NativeArray<int> targetIndices;
        [WriteOnly] public NativeArray<int> friendlyTargetIndices;
        [WriteOnly] public NativeArray<float> morale;
        [ReadOnly] public BoidGrid grid;
        [ReadOnly] public Unity.Physics.CollisionWorld cw;

        public void Execute(int index)
        {
            // Current boid
            Boid.BoidInfo boid = boids[index];

            NativeArray<int> neighbours = grid.FindBoidsWithinRadius(boid, boid.classInfo.viewRadius);
            
            // Calculate the distances between the current boid and the neighbours once at the start,
            // and send these distances to each behavior function. This avoids having to recalculate the distances for
            // each behavior.
            NativeArray<float> distances = CalculateDistances(boid, neighbours);

            int targetBoidIndex = -1;
            int friendlyTargetBoidIndex = -1;
            float targetViewDistance = 0.0f;
            float friendlyTargetViewDistance = 0.0f;
            if (boid.type == Boid.Type.Healer || boid.type == Boid.Type.Hero)
            {
                friendlyTargetBoidIndex = FindFriendlyTargetIndex(boid, neighbours, distances);
                friendlyTargetViewDistance = boid.classInfo.viewRadius;
            }
            targetBoidIndex = FindEnemyTargetIndex(boid, neighbours, distances);
            targetViewDistance = boid.classInfo.attackDistRange;
            
            // Calculate the confidence of the current boid
            float confidence = CalculateConfidence(boid, neighbours);
            
            // Sum all the forces
            float3 desire =
                // Reynolds behaviors
                AlignmentForce(boid, neighbours, distances)
                + CohesionForce(boid, neighbours, distances)
                + SeparationForce(boid, neighbours, distances)
                
                // Additional behaviors
                + (confidence >= boid.classInfo.confidenceThreshold
                    // If confidence is high, be aggressive and have normal fear levels
                    ? AggressionForce(boid) + 1 * FearForce(boid, neighbours, distances) 
                    // If confidence is low, search for the ally flock and duplicate fear levels
                    : SearchForce(boid)     + 2 * FearForce(boid, neighbours, distances))
                
                // 
                + ApproachForce(boid, targetBoidIndex, targetViewDistance)
                + RandomForce(index, boid.classInfo.randomMovements);

            if (HeadedForCollisionWithMapBoundary(boid))
            {
                desire += AvoidCollisionDir(boid) * boid.classInfo.avoidCollisionWeight;
            }


            float3 force = desire - boid.vel;

            // Limit the force to max force
            if (math.lengthsq(force) > boid.classInfo.maxForce)
            {
                force = math.normalize(force) * boid.classInfo.maxForce;
            }

            force += HoverForce(boid);

            forces[index] = force;

            // Update attack info
            targetIndices[index] = targetBoidIndex;
            friendlyTargetIndices[index] = friendlyTargetBoidIndex;

            morale[index] = boid.moraleDefault;
            
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
        
        private float CalculateConfidence(Boid.BoidInfo boid, NativeArray<int> neighbours)
        {
            // Count the number of neighbouring allies and enemies
            int allyCounter = 0;
            int enemyCounter = 0;

            for (int i = 0; i < neighbours.Length; i++)
            {
                int index = neighbours[i];
                Boid.BoidInfo neighbour = boids[index];
                if (boid.flockId == neighbour.flockId)
                {
                    allyCounter++;
                }
                else
                {
                    enemyCounter++;
                }
            }

            // We need to handle the case of there being no neighbouring enemies separately, to avoid
            // dividing by zero, and because we might want to set the confidence differently in this case,
            // to avoid stalemate situations.
            if (enemyCounter == 0)
            {
                // If the boid is alone in its flock, it has nothing to lose and will be confident anyway
                if(flocks[boid.flockId - 1].boidCount == 1)
                {
                    return 1;
                }

                // Otherwise, set the confidence level to the number of allies around the current boid
                // This will ensure a confidence level of 0.0 for boids that are alone (and there are allies still
                // on the field)
                return allyCounter;
            }
            
            // Otherwise, calculate the confidence...
            // We add one to the ally counter, otherwise the boids will not count themselves
            return (float)(allyCounter + 1) / enemyCounter;
        }

       
        private float3 AlignmentForce(Boid.BoidInfo boid, NativeArray<int> neighbours, NativeArray<float> distances)
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
                float normalizedViewDistance = distance / boid.classInfo.viewRadius;
                
                // Contribute to average velocity
                float amount = CalculatePower(boid.morale,
                    normalizedViewDistance, boid.classInfo.alignmentExponent);
                
                //TODO possible variation: use heading of neighbouring boids instead of velocity! this way, faster
                //TODO boids do not have more influence (although this might be desirable)
                avgVel += neighbour.vel * amount * neighbour.classInfo.gravity;
            }
            
            // Calculate alignment force
            if (avgVel.Equals(float3.zero)) return float3.zero;
            return math.normalize(avgVel) * boid.classInfo.alignmentStrength;
        }

        private float3 CohesionForce(Boid.BoidInfo boid, NativeArray<int> neighbours, NativeArray<float> distances)
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
                float normalizedViewDistance = distance / boid.classInfo.viewRadius;

                // Add to average position for cohesion, weighted using morale
                float amount = CalculatePower(boid.morale, normalizedViewDistance,
                    boid.classInfo.cohesionExponent);

                avgNeighborPos += neighbour.pos * amount * neighbour.classInfo.gravity;
                avgNeighborPosDivider += amount * neighbour.classInfo.gravity;
            }

            // Calculate cohesion force
            if (avgNeighborPosDivider == 0) return float3.zero;
            return  math.normalize((avgNeighborPos / avgNeighborPosDivider) - boid.pos) * boid.classInfo.cohesionStrength;
        }
        
        private float3 SeparationForce(Boid.BoidInfo boid, NativeArray<int> neighbours, NativeArray<float> distances)
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
                if (distance > boid.classInfo.separationRadius) continue;
                
                // Calculate a normalized separation distance, i.e a value from 0 to 1
                float normalizedSeparationDistance = distance / boid.classInfo.separationRadius;
                    
                // The power of the separation should be stronger the closer the two boids are to each other,
                // inversely proportional to the distance (with respect to the separation exponent)
                float amount = CalculatePower(1,
                    1.0f - normalizedSeparationDistance, -boid.classInfo.separationExponent);
                    
                // The separation force between the two boids
                float3 separation = (boid.pos - neighbour.pos) / distance;

                avgSeparation += separation * amount;
                separationDivider += amount;
            }

            // Calculate separation force
            if (separationDivider == 0) return float3.zero;
            return (avgSeparation / separationDivider) * boid.classInfo.separationStrength;
        }

        private float3 AggressionForce(Boid.BoidInfo boid)
        {
            // Calculate aggression force
            // TODO this line assumes there's only two flocks and that the ID of the flock corresponds to the index 
            // TODO in the flocks array. Find better solution
            Player.FlockInfo enemyFlock = flocks[boid.flockId == 1 ? 1 : 0];
            
            if (enemyFlock.boidCount == 0) return float3.zero;
            return math.normalize(enemyFlock.avgPos - boid.pos) * boid.classInfo.aggressionStrength;
        }
        
        private float3 SearchForce(Boid.BoidInfo boid)
        {
            Player.FlockInfo flock = flocks[boid.flockId - 1];
            
            if (flock.boidCount <= 1) return float3.zero;
            //TODO do not use aggression strength for search as well?
            return math.normalize(flock.avgPos - boid.pos) * boid.classInfo.searchStrength;
        }

        private float3 FearForce(Boid.BoidInfo boid, NativeArray<int> neighbours, NativeArray<float> distances)
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

                if (neighbour.flockId != boid.flockId           // Different flock
                        && neighbour.type == Boid.Type.Scarecrow       // Is Scarecrow
                        && neighbour.abilityDistance > distances[i]) { // and dist < Scarecrow ability dist
                    fearMultiplier = boid.classInfo.fearMultiplier;
                }
                
                float distance = distances[i];
                
                // Continue if outside fear radius
                if (distance > boid.classInfo.fearRadius) continue;
                
                // Normalize distance with respect to fear radius
                float normalizedFearDistance = distance / boid.classInfo.fearRadius;
                    
                // Calculate the strength of the fear. This is inversely proportional to some exponent of the normalized distance
                float amount =
                    CalculatePower(1.0f, 1.0f - normalizedFearDistance, -boid.classInfo.fearExponent);

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
            return (avgFear / avgFearDivider) * boid.classInfo.fearStrength * fearMultiplier;
        }
        private int FindEnemyTargetIndex(Boid.BoidInfo boid, NativeArray<int> neighbours, NativeArray<float> distances)
        {
            // Init attack info
            int targetIndex = -1; // index of target boid in _boids
            float distToClosestEnemyInRange = boid.classInfo.attackDistRange;

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
                    boid.classInfo.attackDistRange,
                    boid.classInfo.attackAngleRange);
                    
                if (enemyInRange) // and in range, update target
                { 
                    targetIndex = neighbours[i];
                    distToClosestEnemyInRange = distance;
                }
            }

            return targetIndex;
        }

        private int FindFriendlyTargetIndex(Boid.BoidInfo boid, NativeArray<int> neighbours, NativeArray<float> distances)
        {
            int healIndex = -1;
            int lowestAllyHealth = int.MaxValue;

            for (int i = 0; i < neighbours.Length; i++)
            {
                Boid.BoidInfo neighbour = boids[neighbours[i]];
                
                // Enemy boids cannot be healed
                // And do not try to heal boids with max health
                if (boid.flockId != neighbour.flockId 
                    || distances[i] > boid.abilityDistance)
                    continue;
                if (boid.type == Boid.Type.Healer &&
                    neighbour.health == neighbour.maxHealth)
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

        private float3 ApproachForce(Boid.BoidInfo boid, int targetBoidIndex, float targetDistRange)
        {
            // Calculate attack force
            // The attack force tries to move the boid towards the target boid
            if (targetBoidIndex == -1) return float3.zero;

            //TODO find better solution than to recalculate distance here
            //TODO problem is that targetBoidIndex corresponds to index in boid array, not necessarily in distances array
            
            float3 vector = boids[targetBoidIndex].pos - boid.pos;
            float dist = math.length(vector);
            return vector * 
                   CalculatePower(boid.classInfo.approachMovementStrength, 
                       dist / targetDistRange, 
                       boid.classInfo.approachMovementExponent);
        }

        private bool HeadedForCollisionWithMapBoundary(Boid.BoidInfo boid)
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
                    End = boid.pos + dir * boid.collisionAvoidanceDistance,
                    Filter = new Unity.Physics.CollisionFilter
                    {
                        BelongsTo = boid.collisionMask,
                        CollidesWith = boid.collisionMask
                    }
                };

                if (cw.CastRay(ray))   //Cast rays to nearby boundaries
                {
                    return true;
                }
            }
            return false;
        }

        private float3 AvoidCollisionDir(Boid.BoidInfo boid)
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
                    End = boid.pos + dir * boid.collisionAvoidanceDistance,
                    Filter = new Unity.Physics.CollisionFilter
                    {
                        BelongsTo = boid.collisionMask,
                        CollidesWith = boid.collisionMask
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

        private float3 HoverForce(Boid.BoidInfo boid)
        {

            Unity.Physics.RaycastInput ray = new Unity.Physics.RaycastInput
            {
                Start = boid.pos,
                End = boid.pos + new float3(0,-1,0),
                Filter = new Unity.Physics.CollisionFilter
                {
                    BelongsTo = boid.groundMask,
                    CollidesWith = boid.groundMask
                }
            };
            Unity.Physics.RaycastHit hit;
            if (cw.CastRay(ray, out hit))   //Cast rays to nearby boundaries
            {
                float deltaY = boid.classInfo.targetHeight - (math.length(boid.pos - hit.Position));
                float velY = boid.vel.y;

                //Formula to determine whether to hover or fall, uses a PI-regulator with values Ki and Kp
                Vector3 yForce = new Vector3(0, (deltaY > 0 ? (boid.classInfo.hoverKp * deltaY - boid.classInfo.hoverKi * velY) : 0), 0);
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
