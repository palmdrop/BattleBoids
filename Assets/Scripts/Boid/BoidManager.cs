using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using UnityEngine;
using Random = System.Random;

public class BoidManager : MonoBehaviour
{
    [SerializeField] private List<Player> players = new List<Player>();
    private bool _isBattlePhase;

    private readonly List<Boid> _boids = new List<Boid>();
    
    // Random number generator
    private readonly Random _random = new Random();

    // Data structure for efficiently looking up neighbouring boids
    private BoidGrid _grid;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Fetches the boids from the respective players and places them in the boids list
    private void AddPlayerBoids()
    {
        // Check if there's been an update to the player flocks
        // TODO improve, player/spawnarea should be able to tell boid manager update occured
        bool update = false;
        foreach (Player p in players)
        {
            if (p.FlockUpdate)
            {
                update = true;
                break;
            }
        }

        // If no update, return
        if (!update) return;
        
        // Otherwise, clear all boids and add them once again to the list
        _boids.Clear();
        
        foreach (Player p in players)
        {
            foreach (GameObject b in p.GetFlock()) {
                _boids.Add(b.GetComponent<Boid>());
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
        // When game is started, clear boids and fetch all the new boids from the 
        // corresponding players
        if (_isBattlePhase)
        {
            AddPlayerBoids();
        }

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
        NativeArray<float> morale = new NativeArray<float>(_boids.Count, Allocator.TempJob);

        BoidStructJob boidJob = new BoidStructJob
        {
            random = randomFloats,
            flocks = flockInfos,
            boids = boidInfos,
            forces = forces,
            targetIndices = targetIndices,
            morale = morale,
            grid = _grid
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
            _boids[i].SetMorale(morale[i]);
        }

        // Dispose of all data
        randomFloats.Dispose();
        boidInfos.Dispose();
        forces.Dispose();
        flockInfos.Dispose();
        targetIndices.Dispose();
        _grid.Dispose();
        morale.Dispose();
    }

    public void BeginBattle() {
        _isBattlePhase = true;
    }

    public void StopBattle() {
        _isBattlePhase = false;
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
        [WriteOnly] public NativeArray<float> morale;
        [ReadOnly] public BoidGrid grid;

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
            float targetViewDistance = 0.0f;
            if (boid.type == Boid.Type.Healer) 
            {
                targetBoidIndex = FindBoidToHealIndex(boid, neighbours, distances);
                targetViewDistance = boid.classInfo.viewRadius;
            }
            else
            { 
                targetBoidIndex = FindEnemyTargetIndex(boid, neighbours, distances);
                targetViewDistance = boid.classInfo.attackDistRange;
            }
            
            // Sum all the forces
            float3 desire = 
                            // Reynolds behaviors
                              AlignmentForce(boid, neighbours, distances) 
                            + CohesionForce(boid, neighbours, distances)
                            + SeparationForce(boid, neighbours, distances)
                            // Additional behaviors
                            + AggressionForce(boid)
                            + FearForce(boid, neighbours, distances)
                            + ApproachForce(boid, targetBoidIndex, targetViewDistance)
                            + RandomForce(index, boid.classInfo.randomMovements);

            float3 force = desire - boid.vel;

            // Limit the force to max force
            if (math.lengthsq(force) > boid.classInfo.maxForce)
            {
                force = math.normalize(force) * boid.classInfo.maxForce;
            }

            forces[index] = force;

            // Update attack info
            targetIndices[index] = targetBoidIndex;

            morale[index] = CalculateMorale(boid, neighbours, distances);
            
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
                avgVel += neighbour.vel * amount;
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

                avgNeighborPos += neighbour.pos * amount;
                avgNeighborPosDivider += amount;
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

        private float3 FearForce(Boid.BoidInfo boid, NativeArray<int> neighbours, NativeArray<float> distances)
        {
            // Fear force acting on the boid (a boid fears enemy boids)
            float3 avgFear = float3.zero;
            float avgFearDivider = 0.0f;

            for (int i = 0; i < neighbours.Length; i++)
            {
                Boid.BoidInfo neighbour = boids[neighbours[i]];
                
                // No fear for friendly boids
                if (boid.flockId == neighbour.flockId) continue;
                
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
            return (avgFear / avgFearDivider) * boid.classInfo.fearStrength;
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

        private int FindBoidToHealIndex(Boid.BoidInfo boid, NativeArray<int> neighbours, NativeArray<float> distances)
        {
            int healIndex = -1;
            int lowestAllyHealth = int.MaxValue;

            for (int i = 0; i < neighbours.Length; i++)
            {
                Boid.BoidInfo neighbour = boids[neighbours[i]];
                
                // Enemy boids cannot be healed
                // And do not try to heal boids with max health
                if (boid.flockId != neighbour.flockId 
                    || distances[i] > boid.abilityDistance
                    || neighbour.health == neighbour.maxHealth) continue;

                // Store the neighbour with the lowest health
                if (neighbour.health < lowestAllyHealth)
                {
                    healIndex = neighbours[i];
                    lowestAllyHealth = neighbour.health;
                }
            }

            return healIndex;
        }

        private float CalculateMorale(Boid.BoidInfo boid, NativeArray<int> neighbours, NativeArray<float> distances)
        {
            int boost = 0;
            Boid.BoidInfo neighbour;

            for (int i = 0; i < neighbours.Length; i++) {
                neighbour = boids[neighbours[i]];

                if (neighbour.flockId == boid.flockId           // Same flock
                 && neighbour.type == Boid.Type.Hero            // Is Hero
                 && neighbour.abilityDistance > distances[i]) { // and dist < Hero ability dist
                    boost++;
                } else if (neighbour.flockId != boid.flockId           // Different flock
                        && neighbour.type == Boid.Type.Scarecrow       // Is Scarecrow
                        && neighbour.abilityDistance > distances[i]) { // and dist < Scarecrow ability dist
                    boost--;
                }
            }

            // NOTE
            // moraleBoostStrength is arbitrary and can be changed for balancing reasons
            // if no. Heros == no. Scarecrows, the effect is canceled and modifier is 1
            float moraleModifyStrength = 10f;
            float modifier = math.pow(moraleModifyStrength, boost);
            float morale = boid.moraleDefault * modifier;

            // Prevent negative morale
            return math.max(morale, 0.0f);
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
    }
}
