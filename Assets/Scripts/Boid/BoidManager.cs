using System;
using System.Collections;
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

    private List<Boid> _boids = new List<Boid>();
    
    // Random number generator
    private Random random = new Random();

    // Data structure for efficiently looking up neighbouring boids
    private BoidGrid _grid = new BoidGrid();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Fetches the boids from the respective players and places them in the boids list
    private void AddPlayerBoids()
    {
        _boids.Clear();
        foreach (Player p in players) {
            foreach (GameObject b in p.GetFlock()) {
                _boids.Add(b.GetComponent<Boid>());
            }
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
            grid = _grid,
            flockInfos = flockInfos
        };
        
        // Schedule job 
        JobHandle jobHandle = flockJob.Schedule();
        jobHandle.Complete();

        // Allocate a struct job for calculating boid forces and attack target
        NativeArray<float> randomFloats = new NativeArray<float>(_boids.Count * 2, Allocator.TempJob);
        for (int i = 0; i < randomFloats.Length; i++)
        {
            randomFloats[i] = (float)random.NextDouble();
        }
        NativeArray<bool> enemyInRanges = new NativeArray<bool>(_boids.Count, Allocator.TempJob);
        NativeArray<int> boidIndices = new NativeArray<int>(_boids.Count, Allocator.TempJob);

        BoidStructJob boidJob = new BoidStructJob
        {
            random = randomFloats,
            flocks = flockInfos,
            boids = boidInfos,
            forces = forces,
            enemyInRanges = enemyInRanges,
            boidIndices = boidIndices,
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
            if (enemyInRanges[i]) { // If enemy in attack range
                _boids[i].SetTarget(_boids[boidIndices[i]]);
            } else {
                _boids[i].SetTarget(null);
            }
        }

        // Dispose of all data
        randomFloats.Dispose();
        boidInfos.Dispose();
        forces.Dispose();
        flockInfos.Dispose();
        enemyInRanges.Dispose();
        boidIndices.Dispose();
        _grid.Dispose();
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
    public struct FlockStructJob : IJob
    {
        [ReadOnly] public NativeArray<Boid.BoidInfo> boids;
        [ReadOnly] public BoidGrid grid;
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
    public struct BoidStructJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float> random;
        [ReadOnly] public NativeArray<Player.FlockInfo> flocks;
        [ReadOnly] public NativeArray<Boid.BoidInfo> boids;
        [WriteOnly] public NativeArray<float3> forces;
        [WriteOnly] public NativeArray<bool> enemyInRanges;
        [WriteOnly] public NativeArray<int> boidIndices;
        [ReadOnly] public BoidGrid grid;

        // Translates a squared distance into a normalized distance representation,
        // i.e to a value from 0 to 1
        private float NormalizedDist(float sqrDist, float maxDist)
        {
            return math.sqrt(sqrDist) / maxDist;
        }

        // Calculates the power of a certain behavior. This is a combination of the weight for that behavior,
        // the distance to the target, and the falloff exponent
        private float CalculatePower(float weight, float normalizedDist, float exponent)
        {
            return weight * math.pow(1.0f - normalizedDist, exponent);
        }

        // Calculates a random force based on an array of random values
        private float3 CalculateRandomForce(int index, float strength)
        {
            float angle = math.PI * 2 * random[index * 2];
            float amount = random[index * 2 + 1];
            
            // Ignore they y component, since the boids only move in 2 dimensions
            return new float3(math.cos(angle), 0.0f, math.sin(angle)) * amount * strength;
        }

        private bool BoidIndexInAttackRange(
                float3 vectorFromSelfToEnemy,
                float3 forward,
                float attackDstRange,
                float attackAngleRange) 
        {

            // Calc distance to target
            float dist = math.sqrt(
                        vectorFromSelfToEnemy.x * vectorFromSelfToEnemy.x
                      + vectorFromSelfToEnemy.y * vectorFromSelfToEnemy.y
                      + vectorFromSelfToEnemy.z * vectorFromSelfToEnemy.z);

            // Calc angle to target
            float dp = vectorFromSelfToEnemy.x * forward.x
                     + vectorFromSelfToEnemy.y * forward.y
                     + vectorFromSelfToEnemy.z * forward.z;

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
       
        

        private float3 AlignmentForce(Boid.BoidInfo boid, NativeArray<int> neighbors, NativeArray<float> distances)
        {
            // Average velocity is used to calculate alignment force
            float3 avgVel = float3.zero;

            for (int i = 0; i < neighbors.Length; i++)
            {
                Boid.BoidInfo neighbour = boids[neighbors[i]];

                // No alignment behavior with enemy boids
                if (boid.flockId != neighbour.flockId) continue;
                
                // The distance between the current boid and the neighbour
                float distance = distances[i];
                float normalizedViewDistance = distance / boid.classInfo.viewRadius;
                
                // Contribute to average velocity
                float amount = CalculatePower(boid.classInfo.morale,
                    normalizedViewDistance, boid.classInfo.alignmentExponent);
                
                //TODO possible variation: use heading of neighbouring boids instead of velocity! this way, faster
                //TODO boids do not have more influence (although this might be desirable)
                avgVel += neighbour.vel * amount;
            }
            
            // Calculate alignment force
            Vector3 alignmentForce;
            if (avgVel.Equals(float3.zero)) alignmentForce = float3.zero;
            else alignmentForce = math.normalize(avgVel) * boid.classInfo.alignmentStrength;

            return alignmentForce;
        }

        private float3 CohesionForce(Boid.BoidInfo boid, NativeArray<int> neighbors, NativeArray<float> distances)
        {
            // Average neighbour position 
            float3 avgNeighborPos = float3.zero;
            float avgNeighborPosDivider = 0.0f;

            for (int i = 0; i < neighbors.Length; i++)
            {
                Boid.BoidInfo neighbour = boids[neighbors[i]];
                
                // No cohesion behavior with enemy boids
                if (boid.flockId != neighbour.flockId) continue;
                
                float distance = distances[i];
                float normalizedViewDistance = distance / boid.classInfo.viewRadius;

                // Add to average position for cohesion, weighted using morale
                float amount = CalculatePower(boid.classInfo.morale, normalizedViewDistance,
                    boid.classInfo.cohesionExponent);

                avgNeighborPos += neighbour.pos * amount;
                avgNeighborPosDivider += amount;
            }

            // Calculate cohesion force
            float3 cohesionForce;
            if (avgNeighborPosDivider == 0) cohesionForce = float3.zero;
            else cohesionForce = math.normalize((avgNeighborPos / avgNeighborPosDivider) - boid.pos) * boid.classInfo.cohesionStrength;

            return cohesionForce;
        }
        

        public void Execute(int index)
        {

            // Separation force acting on the boid
            float3 avgSeparation = float3.zero;
            float separationDivider = 0.0f;
            
            // Fear force acting on the boid (a boid fears enemy boids)
            float3 avgFear = float3.zero;
            float avgFearDivider = 0.0f;

            // Init attack info
            bool enemyInRange = false;
            int targetBoidIndex = -1; // index of target boid in _boids
            float sqrDistToClosestEnemyInRange = math.INFINITY;

            // Current boid
            Boid.BoidInfo boid = boids[index];

            NativeArray<int> neighbours = grid.FindBoidsWithinRadius(boid, boid.classInfo.viewRadius);
            
            // Calculate the distances between the current boid and the neighbours once at the start,
            // and send these distances to each behavior function. This avoids having to recalculate the distances for
            // each behavior.
            NativeArray<float> distances = CalculateDistances(boid, neighbours);
            
            // Iterate over all the neighbours
            foreach (int i in neighbours)
            {
                Boid.BoidInfo neighbour = boids[i];

                // Compare the distance between this boid and the neighbour using the
                // square of the distance and radius. This avoids costly square root operations
                // And if close enough, add to average position for separation
                float3 vector = (neighbour.pos - boid.pos);
                float sqrDist = vector.x * vector.x + vector.y * vector.y + vector.z * vector.z;

                // If boid is beyond view radius, ignore
                if (sqrDist > (boid.classInfo.viewRadius * boid.classInfo.viewRadius)) continue;

                // Calculate a normalized distance (a value from 0.0 to 1.0)
                float dist = math.sqrt(sqrDist);
                float normalizedViewDistance = dist / boid.classInfo.viewRadius;

                if (neighbour.flockId == boid.flockId) {
                    // Friendly boid
                    
                    // If within separation radius...
                    if (dist < boid.classInfo.separationRadius)
                    {
                        // Calculate a normalized separation distance, i.e a value from 0 to 1
                        float normalizedSeparationDistance = dist / boid.classInfo.separationRadius;
                        
                        // The power of the separation should be stronger the closer the two boids are to each other,
                        // inversely proportional to the distance (with respect to the separation exponent)
                        float amount = CalculatePower(1,
                            1.0f - normalizedSeparationDistance, -boid.classInfo.separationExponent);
                        
                        // The separation force between the two boids
                        float3 separation = (boid.pos - neighbour.pos) / dist;

                        avgSeparation += separation * amount;
                        separationDivider += amount;
                    }
                } 
                else 
                {
                    // Enemy boid

                    // If the enemy boid is within the fear radius...
                    if (dist < boid.classInfo.fearRadius)
                    {
                        // Normalize distance with respect to fear radius
                        float normalizedFearDistance = dist / boid.classInfo.fearRadius;
                        
                        // Calculate the strength of the fear. This is inversely proportional to some exponent of the normalized distance
                        float amount =
                            CalculatePower(1.0f, 1.0f - normalizedFearDistance, -boid.classInfo.fearExponent);

                        // Fear force between the two boids
                        float3 fear = (boid.pos - neighbour.pos) / dist;

                        avgFear += fear * amount;
                        avgFearDivider += amount;
                    }

                    // If closer than current attack target
                    // TODO: use other factors to determine target as well? for example target weak enemy boid?
                    // TODO: we could use weight calculated using distance, health, and other factors
                    if (sqrDist < sqrDistToClosestEnemyInRange) 
                    {
                        enemyInRange = BoidIndexInAttackRange(vector,
                                boid.forward,
                                boid.classInfo.attackDstRange,
                                boid.classInfo.attackAngleRange);
                        
                        if (enemyInRange) // and in range, update target
                        { 
                            targetBoidIndex = i;
                            sqrDistToClosestEnemyInRange = sqrDist;
                        }
                    }
                }
            }
            
            //TODO do we want to normalize all the forces before scaling with the behavior strength?
            //TODO this will make the behavior force equally strong at all times... Might not be what we want

            neighbours.Dispose();
            distances.Dispose();

            // Calculate separation force
            float3 separationForce;
            if (separationDivider == 0) separationForce = float3.zero;
            else separationForce = (avgSeparation / separationDivider) * boid.classInfo.separationStrength;

            // Calculate aggression force
            Player.FlockInfo enemyFlock = flocks[boid.flockId == 1 ? 1 : 0];
            float3 enemyFlockPos = enemyFlock.avgPos;
            float3 aggressionForce;

            if (enemyFlock.boidCount == 0) aggressionForce = float3.zero;
            else aggressionForce = math.normalize(enemyFlockPos - boid.pos) * boid.classInfo.aggressionStrength;
            
            // Calculate fear force
            // This force is similar to the separation force, but only acts on enemy boids
            float3 fearForce;
            if (avgFearDivider == 0.00) fearForce = float3.zero;
            else fearForce = (avgFear / avgFearDivider) * boid.classInfo.fearStrength;
            
            // Calculate attack force
            // The attack force tries to move the boid towards the target boid
            float3 attackForce;
            if (!enemyInRange) attackForce = float3.zero;
            else attackForce = math.normalize(boids[targetBoidIndex].pos - boid.pos) *
                               CalculatePower(boid.classInfo.attackMovementStrength, NormalizedDist(sqrDistToClosestEnemyInRange, boid.classInfo.attackDstRange), boid.classInfo.attackMovementExponent);

            // Calculate random force
            float3 randomForce = CalculateRandomForce(index, boid.classInfo.randomMovements);

            // Sum all the forces
            float3 desire = 
                              AlignmentForce(boid, neighbours, distances) 
                            + CohesionForce(boid, neighbours, distances)
                            + separationForce
                            + aggressionForce
                            + fearForce
                            + attackForce
                            + randomForce
            ;

            float3 force = desire - boid.vel;

            // Limit the force to max force
            if (math.lengthsq(force) > boid.classInfo.maxForce)
            {
                force = math.normalize(force) * boid.classInfo.maxForce;
            }

            forces[index] = force;

            // Update attack info
            enemyInRanges[index] = enemyInRange;
            boidIndices[index] = targetBoidIndex;
        }
    }
}
