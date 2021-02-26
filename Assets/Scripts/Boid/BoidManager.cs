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

        // Hashmap mapping from one index in the boid array to a list of indices of neighbouring boids
        NativeMultiHashMap<int, int> neighbours = new NativeMultiHashMap<int, int>(10, Allocator.TempJob);

        // Allocate a struct job for calculating flock info
        FlockStructJob flockJob = new FlockStructJob()
        {
            boids = boidInfos,
            grid = _grid,
            flockInfos = flockInfos,
            allNeighbours = neighbours
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
            neighbourArray = neighbours
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
        neighbours.Dispose();
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
        [WriteOnly] public NativeMultiHashMap<int, int> allNeighbours;

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

                // Calculate neighbouring boids using the grid
                NativeArray<int> neighbours = grid.FindBoidsWithinRadius(boid, boid.classInfo.viewRadius);
                foreach (int j in neighbours)
                {
                    allNeighbours.Add(i, j);
                }
                neighbours.Dispose();
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
        [ReadOnly] public NativeMultiHashMap<int, int> neighbourArray;

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
                float attackDstRange,
                float attackAngleRange,
                int boidIndex) {

            float sqrAttackDstRange = attackDstRange * attackDstRange;
            float cosAttackAngleRange = Mathf.Cos(attackAngleRange);
            float sqrCosAttackAngleRange = cosAttackAngleRange * cosAttackAngleRange;

            float sqrZ = vectorFromSelfToEnemy.z * vectorFromSelfToEnemy.z;
            float sqrDst = vectorFromSelfToEnemy.x * vectorFromSelfToEnemy.x
                         + vectorFromSelfToEnemy.y * vectorFromSelfToEnemy.y
                         + sqrZ;
            float sqrCosAngle = sqrZ / sqrDst;

            // Ignores
            if (sqrDst > sqrAttackDstRange) { // If distance too big
                return false;
            }
            if (vectorFromSelfToEnemy.z < 0f) { // if enemy behind
                return false;
            }
            if (sqrCosAngle < sqrCosAttackAngleRange) { // If angle to big
                return false;
            }

            return true;
        }

        public void Execute(int index)
        {
            /*** BOID BEHAVIOR VARIABLES ***/
            // Average velocity is used to calculate alignment force
            float3 avgVel = float3.zero;

            // Average neighbour position used to calculate cohesion
            float3 avgPosCohesion = float3.zero;
            float avgPosCohesionDivider = 0.0f;

            // Average neighbour position used to calculate cohesion
            float3 avgPosSeparation = float3.zero;
            float avgPosSeparationDivider = 0.0f;
            
            // Average position of visible enemy boids
            float3 avgEnemyPos = float3.zero;
            float avgEnemyPosDivider = 0.0f;

            // Position of closest enemy
            float3 targetPos = float3.zero;
            float targetDist = math.INFINITY;

            // Init attack info
            bool enemyInRange = false;
            int boidIndex = -1; // index of target boid in _boids
            float sqrDstToClosestEnemyInRange = Mathf.Infinity;

            Boid.BoidInfo boid = boids[index];

            // Iterate over all the neighbours
            foreach (int i in neighbourArray.GetValuesForKey(index))
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
                float normalizedDistance = NormalizedDist(sqrDist, boid.classInfo.viewRadius);

                if (neighbour.flockId == boid.flockId) {
                    // Friendly boid
                    
                    // Contribute to average velocity
                    float amount = CalculatePower(boid.classInfo.morale,
                        normalizedDistance, boid.classInfo.alignmentExponent);
                    
                    avgVel += neighbour.vel * amount;
                    
                    // Add to average position for cohesion, weighted using morale
                    amount = CalculatePower(boid.classInfo.morale, normalizedDistance,
                        boid.classInfo.cohesionExponent);
                    
                    avgPosCohesion += neighbour.pos * amount;
                    avgPosCohesionDivider += amount;
                    
                    // Add to average position for separation
                    amount = CalculatePower(1,
                        normalizedDistance, boid.classInfo.separationExponent);

                    avgPosSeparation += neighbour.pos * amount;
                    avgPosSeparationDivider += amount;
                    
                } else {
                    // Enemy boid
                    float amount = 1.0f;
                        //CalculatePower(1.0f, normalizedDistance, boid.classInfo.fearExponent);
                        
                    avgEnemyPos += neighbour.pos * amount;
                    avgEnemyPosDivider += amount;
                    
                    // If the enemy boid is closer than the previous enemy boid, update the target and target dist
                    // Resulting target will be the boid closest
                    // TODO: use other factors to determine target as well? for example target weak enemy boid?
                    // TODO: we could use weight calculated using distance, health, and other factors
                    if (sqrDist < targetDist) {
                        targetPos = neighbour.pos;
                        targetDist = sqrDist;
                    }

                    // If closer than current attack target
                    if (sqrDist < sqrDstToClosestEnemyInRange) {
                        enemyInRange = BoidIndexInAttackRange(vector,
                                boid.classInfo.attackDstRange,
                                boid.classInfo.attackAngleRange,
                                i);
                        if (enemyInRange) { // and in range, update target
                            boidIndex = i;
                            sqrDstToClosestEnemyInRange = sqrDist;
                        }
                    }
                }
            }

            

            // Calculate alignment force
            Vector3 alignmentForce;
            if (avgVel.Equals(new float3(0, 0, 0))) alignmentForce = new float3(0, 0, 0);
            else alignmentForce = math.normalize(avgVel) * boid.classInfo.alignmentStrength;

            // Calculate cohesion force
            Vector3 cohesionForce;
            if (avgPosCohesionDivider == 0) cohesionForce = new float3(0, 0, 0);
            else cohesionForce = math.normalize((avgPosCohesion / avgPosCohesionDivider) - boid.pos) * boid.classInfo.cohesionStrength;

            // Calculate separation force
            Vector3 separationForce;
            if (avgPosSeparationDivider == 0) separationForce = new float3(0, 0, 0);
            else separationForce = math.normalize(boid.pos - (avgPosSeparation / avgPosSeparationDivider)) * boid.classInfo.separationStrength;

            // Calculate aggression force
            Player.FlockInfo enemyFlock = flocks[boid.flockId == 1 ? 1 : 0];
            float3 enemyFlockPos = enemyFlock.avgPos;
            Vector3 aggressionForce;
            
            if (enemyFlock.boidCount == 0) aggressionForce = new float3(0, 0, 0);
            else
                aggressionForce = math.normalize(enemyFlockPos - boid.pos) * boid.classInfo.aggressionStrength;
            
            // Normalize distance to enemy target boid
            float normalizedTargetDist = NormalizedDist(targetDist, boid.classInfo.viewRadius);
            
            // Calculate fear force
            // This force is facing away from the weighted center of the nearby enemy boids, but is 
            // scaled using the proximity of the closest (target) enemy boid
            Vector3 fearForce;
            if (avgEnemyPosDivider <= 0.00 || float.IsPositiveInfinity(targetDist)) fearForce = new float3(0, 0, 0);
            else fearForce = math.normalize(boid.pos - (avgEnemyPos / avgEnemyPosDivider)) *
                             CalculatePower(boid.classInfo.fearStrength, normalizedTargetDist, boid.classInfo.fearExponent);
            
            // Calculate attack force
            Vector3 attackForce;
            if (float.IsPositiveInfinity(targetDist)) attackForce = new float3(0, 0, 0);
            else attackForce = math.normalize(targetPos - boid.pos) *
                               CalculatePower(boid.classInfo.attackMovementStrength, normalizedTargetDist, boid.classInfo.attackMovementExponent);

            // Calculate random force
            Vector3 randomForce = CalculateRandomForce(index, boid.classInfo.randomMovements);

            // Sum all the forces
            forces[index] = 
                        alignmentForce 
                            + cohesionForce 
                            + separationForce
                            + aggressionForce
                            + fearForce
                            + attackForce
                            + randomForce
            ;

            // Update attack info
            enemyInRanges[index] = enemyInRange;
            boidIndices[index] = boidIndex;
        }
    }
}
