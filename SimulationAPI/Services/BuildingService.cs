using System;
using System.Collections.Generic;
using System.Linq;
using MySimulationAPI.Models;
using MySimulationAPI.SimulationConfiguration;

namespace MySimulationAPI.Services
{
    public class BuildingService
    {
        private readonly SimulationContext _context;
        private readonly RoutingService _routingService;

        // Cached buildings and KD-trees
        private readonly List<Building> _buildings;
        private readonly Dictionary<Type, KDTree<Building>> _kdTrees;

        // Cached building lists by type for efficient random selection
        private readonly Dictionary<Type, List<Building>> _buildingsByType;

        public BuildingService(SimulationContext context)
        {
            _context = context;
            _routingService = context.RoutingService;
            _buildings = context.Buildings.ToList();

            _kdTrees = BuildKdTrees(_buildings);
            _buildingsByType = new Dictionary<Type, List<Building>>
            {
                [typeof(ResidentialBuilding)] = _buildings.OfType<ResidentialBuilding>().Cast<Building>().ToList(),
                [typeof(OfficeBuilding)] = _buildings.OfType<OfficeBuilding>().Cast<Building>().ToList(),
                [typeof(EducationBuilding)] = _buildings.OfType<EducationBuilding>().Cast<Building>().ToList(),
                [typeof(MedicalBuilding)] = _buildings.OfType<MedicalBuilding>().Cast<Building>().ToList(),
                [typeof(RetailBuilding)] = _buildings.OfType<RetailBuilding>().Cast<Building>().ToList(),
                [typeof(LeisureBuilding)] = _buildings.OfType<LeisureBuilding>().Cast<Building>().ToList()
            };
        }

        private static Dictionary<Type, KDTree<Building>> BuildKdTrees(List<Building> buildings)
        {
            var dict = new Dictionary<Type, KDTree<Building>>();

            void AddTree<T>(IEnumerable<T> typedBuildings) where T : Building
            {
                var list = typedBuildings.ToList();
                if (list.Any())
                    dict[typeof(T)] = new KDTree<Building>(list.Select(b => (b.Latitude, b.Longitude, (Building)b)));
            }

            AddTree(buildings.OfType<ResidentialBuilding>());
            AddTree(buildings.OfType<OfficeBuilding>());
            AddTree(buildings.OfType<EducationBuilding>());
            AddTree(buildings.OfType<MedicalBuilding>());
            AddTree(buildings.OfType<RetailBuilding>());
            AddTree(buildings.OfType<LeisureBuilding>());

            return dict;
        }
        public Building? FindNearestBuildingByType<T>(double lat, double lon, int k = 5, double maxRadiusMeters = 5000)
           where T : Building
        {
            if (!_kdTrees.TryGetValue(typeof(T), out var tree))
                return null;

            var results = tree.KNearest(lat, lon, k);
            return results.FirstOrDefault();
        }

        public T? GetRandomBuilding<T>(Random rng) where T : Building
        {
            if (!_buildingsByType.TryGetValue(typeof(T), out var list) || list.Count == 0)
                return null;
            return (T)list[rng.Next(list.Count)];
        }




        public static class GeoUtils
        {

            // Calculate approximate distance using Haversine formula (in meters)

            public static double Distance(double lat1, double lon1, double lat2, double lon2)
            {
                const double R = 6371000; // Earth's radius in meters
                double dLat = ToRadians(lat2 - lat1);
                double dLon = ToRadians(lon2 - lon1);

                double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                          Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                          Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

                double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
                return R * c;
            }

            private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
        }

        // ---------------------------------------------------------
        // KD-Tree for 2D nearest neighbour search
        // ---------------------------------------------------------
        public class KDTree<T>
        {
            private class Node
            {
                public double[] Point = null!;
                public T Value = default!;
                public Node? Left, Right;
                public int Depth;
            }

            private Node? root;

            public KDTree(IEnumerable<(double x, double y, T value)> items)
            {
                var list = items.ToList();
                root = Build(list, depth: 0);
            }

            private Node? Build(List<(double x, double y, T value)> points, int depth)
            {
                if (points.Count == 0) return null;

                int axis = depth % 2;
                points.Sort((a, b) => axis == 0 ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
                int median = points.Count / 2;

                var node = new Node
                {
                    Point = new[] { points[median].x, points[median].y },
                    Value = points[median].value,
                    Depth = depth
                };

                node.Left = Build(points.Take(median).ToList(), depth + 1);
                node.Right = Build(points.Skip(median + 1).ToList(), depth + 1);

                return node;
            }

            public List<T> KNearest(double x, double y, int k)
            {
                if (root == null || k <= 0)
                    return new List<T>();

                // Umin-heap (priority queue) for efficient k-nearest tracking
                var heap = new SortedSet<(double dist, int id, T value)>(
                    Comparer<(double dist, int id, T value)>.Create((a, b) =>
                    {
                        int cmp = a.dist.CompareTo(b.dist);
                        return cmp != 0 ? cmp : a.id.CompareTo(b.id); // Use id as tiebreaker in case of equal distances
                    })
                );

                int nextId = 0;
                SearchOptimized(root, x, y, k, heap, ref nextId);

                return heap.Select(h => h.value).ToList();
            }

            private void SearchOptimized(Node? node, double x, double y, int k,
                                        SortedSet<(double dist, int id, T value)> heap,
                                        ref int nextId)
            {
                if (node == null) return;

                // Calculate squared distance 
                double dx = node.Point[0] - x;
                double dy = node.Point[1] - y;
                double distSq = dx * dx + dy * dy;

                // Add to heap
                heap.Add((Math.Sqrt(distSq), nextId++, node.Value));

                // Keep only k nearest to optimize memory pressure
                if (heap.Count > k)
                {
                    heap.Remove(heap.Max);
                }

                // Determine which subtree to search first
                int axis = node.Depth % 2;
                double diff = (axis == 0 ? x : y) - node.Point[axis];

                Node? nearSubtree = diff < 0 ? node.Left : node.Right;
                Node? farSubtree = diff < 0 ? node.Right : node.Left;

                SearchOptimized(nearSubtree, x, y, k, heap, ref nextId);

                // Only search far subtree if it could contain closer points
                double maxDistSq = heap.Count < k ? double.MaxValue : heap.Max.dist * heap.Max.dist;
                if (diff * diff < maxDistSq)
                {
                    SearchOptimized(farSubtree, x, y, k, heap, ref nextId);
                }
            }
        }


        public void AssignHousing()
        {
            var agents = _context.Agents;
            var houses = _buildings.OfType<ResidentialBuilding>().ToList();
            var rng = _context.Rng;

            Console.WriteLine($"Assigning housing to {agents.Count} agents across {houses.Count} houses");

            // set local capacities
            foreach (var h in houses)
                h.RemainingCapacity = h.Capacity;

            // Calculate total capacity
            long totalCapacity = houses.Sum(h => (long)h.Capacity);
            Console.WriteLine($"Total housing capacity: {totalCapacity}");

            if (totalCapacity < agents.Count)
            {
                throw new InvalidOperationException(
                    $"Insufficient housing capacity: {totalCapacity} slots for {agents.Count} agents");
            }

            var startTime = DateTime.Now;

            // Build cumulative capacity array for O(log n) binary search
            var cumulativeCapacity = new long[houses.Count];
            long cumSum = 0;
            for (int i = 0; i < houses.Count; i++)
            {
                cumSum += houses[i].Capacity;
                cumulativeCapacity[i] = cumSum;
            }

            // Shuffle agents performantly
            var shuffledIndices = Enumerable.Range(0, agents.Count).ToArray();

            // Fisher-Yates shuffle on indices
            for (int i = shuffledIndices.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (shuffledIndices[i], shuffledIndices[j]) = (shuffledIndices[j], shuffledIndices[i]);
            }

            // Assign agents using weighted random selection
            long currentSlot = 0;
            int currentHouseIdx = 0;
            int slotsUsedInCurrentHouse = 0;

            foreach (var agentIdx in shuffledIndices)
            {
                while (currentHouseIdx < houses.Count && slotsUsedInCurrentHouse >= houses[currentHouseIdx].Capacity)
                {
                    currentHouseIdx++;
                    slotsUsedInCurrentHouse = 0;
                }

                if (currentHouseIdx >= houses.Count)
                    break; // Safety check

                var house = houses[currentHouseIdx];
                agents[agentIdx].House = house;
                house.RemainingCapacity--;

                slotsUsedInCurrentHouse++;
                currentSlot++;
            }

            var elapsed = (DateTime.Now - startTime).TotalSeconds;
            Console.WriteLine($"✓ Housing assigned in {elapsed:F2}s ({agents.Count / elapsed:F0} agents/s)");
        }

        public void AssignWorkplaces(
            double beta = 2.0,
            int shortlistSize = 50,
            int maxCandidates = 200)
        {
            var agents = _context.Agents;
            var buildings = _context.Buildings;

            Console.WriteLine($"Assigning workplaces to {agents.Count} agents across {buildings.Count} buildings");

            // Reset building capacities
            foreach (var b in buildings)
                b.RemainingCapacity = b.Capacity;

            // Filter agents that need workplace assignment and group by type
            var agentsNeedingWork = agents
                .Where(a => !(a is ElderlyRetiredAgent) && a.House != null)
                .ToList();

            if (agentsNeedingWork.Count == 0)
            {
                Console.WriteLine("No agents need workplace assignment");
                return;
            }

            Console.WriteLine($"Agents needing workplace: {agentsNeedingWork.Count}");

            // Group agents by their building type needs
            var agentsByBuildingType = new Dictionary<Type, List<Agent>>();
            foreach (var agent in agentsNeedingWork)
            {
                Type buildingType = agent switch
                {
                    StudentAgent => typeof(EducationBuilding),
                    MedicalWorkerAgent => typeof(MedicalBuilding),
                    RetailWorkerAgent => typeof(RetailBuilding),
                    ServiceWorkerAgent => typeof(LeisureBuilding),
                    _ => typeof(OfficeBuilding)
                };

                if (!agentsByBuildingType.ContainsKey(buildingType))
                    agentsByBuildingType[buildingType] = new List<Agent>();

                agentsByBuildingType[buildingType].Add(agent);
            }

            var startTime = DateTime.Now;
            int totalAssigned = 0;

            // Process each building type independently to eliminate lock contention
            foreach (var (buildingType, typeAgents) in agentsByBuildingType)
            {
                if (!_kdTrees.TryGetValue(buildingType, out var tree))
                {
                    Console.WriteLine($"No buildings of type {buildingType.Name}, skipping {typeAgents.Count} agents");
                    continue;
                }

                Console.WriteLine($"\nAssigning {typeAgents.Count} agents to {buildingType.Name}...");

                var typeStartTime = DateTime.Now;
                int assigned = AssignWorkplacesForType(typeAgents, tree, beta, shortlistSize, maxCandidates);
                var typeTime = (DateTime.Now - typeStartTime).TotalSeconds;

                totalAssigned += assigned;
                Console.WriteLine($"  ✓ Assigned {assigned}/{typeAgents.Count} in {typeTime:F2}s ({(assigned / typeTime):F0} agents/s)");
            }

            var totalTime = (DateTime.Now - startTime).TotalSeconds;
            Console.WriteLine($"\n✓ Workplace assignment complete in {totalTime:F1}s");
            Console.WriteLine($"  Total assigned: {totalAssigned}/{agentsNeedingWork.Count} agents");
            Console.WriteLine($"  Average rate: {(totalAssigned / totalTime):F0} agents/s");
        }

        private int AssignWorkplacesForType(
            List<Agent> agents,
            KDTree<Building> tree,
            double beta,
            int shortlistSize,
            int maxCandidates)
        {
            var rng = _context.Rng;

            // Get all buildings of type for fallback assignment
            var allBuildings = _buildingsByType.Values
                .SelectMany(list => list)
                .Where(b => tree.KNearest(b.Latitude, b.Longitude, 1).Contains(b))
                .ToList();

            // Gather candidates for all agents
            var candidateData = new (Agent agent, List<(Building building, double weight)> candidates)[agents.Count];

            Parallel.For(0, agents.Count, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, i =>
            {
                var agent = agents[i];
                var house = agent.House!;

                // Get more candidates than needed to account for capacity issues
                var nearbyBuildings = tree.KNearest(house.Latitude, house.Longitude, maxCandidates);

                if (nearbyBuildings.Count == 0)
                {
                    candidateData[i] = (agent, new List<(Building, double)>());
                    return;
                }

                // Compute weights for all candidates
                var weightedCandidates = new List<(Building building, double weight)>(nearbyBuildings.Count);

                foreach (var building in nearbyBuildings)
                {
                    double latDiff = building.Latitude - house.Latitude;
                    double lonDiff = building.Longitude - house.Longitude;
                    double distSq = latDiff * latDiff + lonDiff * lonDiff;

                    // higher capacity and closer are higher weighted (Adding 1 to avoid division by zero)
                    double weight = (building.Capacity + 1) / Math.Pow(distSq + 0.0001, beta / 2);
                    weightedCandidates.Add((building, weight));
                }

                // Sort by weight descending - most preferred first
                weightedCandidates.Sort((a, b) => b.weight.CompareTo(a.weight));

                candidateData[i] = (agent, weightedCandidates);
            });

            // Assign in priority order (sequential but cache-friendly)
            // Sort agents by their top candidate's weight, prioritizing agents with fewer good options
            var sortedAgentIndices = Enumerable.Range(0, agents.Count)
                .Where(i => candidateData[i].candidates.Count > 0)
                .OrderBy(i =>
                {
                    var candidates = candidateData[i].candidates;
                    // Agents with lower weight variance (fewer good options) get priority
                    if (candidates.Count < 2) return 0;
                    return candidates[0].weight - candidates[Math.Min(shortlistSize - 1, candidates.Count - 1)].weight;
                })
                .ToList();

            int assignedCount = 0;
            var unassignedAgents = new List<Agent>();

            // Assign workplaces sequentially to avoid capacity conflicts
            foreach (var idx in sortedAgentIndices)
            {
                var (agent, candidates) = candidateData[idx];

                if (candidates.Count == 0)
                {
                    unassignedAgents.Add(agent);
                    continue;
                }

                // Try top candidates in order until one with capacity is found
                // Use probabilistic selection from top candidates
                Building? chosen = null;

                // Take top N candidates with remaining capacity
                var availableCandidates = candidates
                    .Where(c => c.building.RemainingCapacity > 0)
                    .Take(shortlistSize)
                    .ToList();

                if (availableCandidates.Count > 0)
                {
                    // Weighted random selection from available candidates
                    double totalWeight = availableCandidates.Sum(c => c.weight);
                    double rand = rng.NextDouble() * totalWeight;
                    double cumulative = 0;

                    foreach (var (building, weight) in availableCandidates)
                    {
                        cumulative += weight;
                        if (rand <= cumulative)
                        {
                            chosen = building;
                            break;
                        }
                    }

                    // Fallback to first available if something goes wrong
                    chosen ??= availableCandidates[0].building;

                    // Assign and update capacity
                    agent.Workplace = chosen;
                    chosen.RemainingCapacity--;
                    assignedCount++;
                }
                else
                {
                    unassignedAgents.Add(agent);
                }
            }

            // Fallback for unassigned agents assign to any building with capacity
            if (unassignedAgents.Count > 0)
            {
                Console.WriteLine($"    ⚠ {unassignedAgents.Count} agents couldn't find nearby capacity, using fallback...");

                // Find all buildings with remaining capacity, sorted by capacity (largest first)
                var availableBuildings = allBuildings
                    .Where(b => b.RemainingCapacity > 0)
                    .OrderByDescending(b => b.RemainingCapacity)
                    .ToList();

                int fallbackAssigned = 0;
                int buildingIdx = 0;

                foreach (var agent in unassignedAgents)
                {
                    // Find next building with capacity
                    while (buildingIdx < availableBuildings.Count &&
                           availableBuildings[buildingIdx].RemainingCapacity <= 0)
                    {
                        buildingIdx++;
                    }

                    if (buildingIdx >= availableBuildings.Count)
                    {
                        break;
                    }

                    var building = availableBuildings[buildingIdx];
                    agent.Workplace = building;
                    building.RemainingCapacity--;
                    fallbackAssigned++;
                    assignedCount++;
                }

                if (fallbackAssigned > 0)
                {
                    Console.WriteLine($"    ✓ Fallback assigned {fallbackAssigned} agents");
                }

                if (fallbackAssigned < unassignedAgents.Count)
                {
                    Console.WriteLine($"    ⚠ {unassignedAgents.Count - fallbackAssigned} agents remain unassigned (insufficient total capacity)");
                }
            }

            return assignedCount;
        }
    }

    public enum BuildingType
    {
        Residential,    // ResidentialBuilding - OSM: building=residential, apartments, house
        Office,         // OfficeBuilding - OSM: building=office, commercial
        Education,      // EducationBuilding - OSM: amenity=school, university, college
        Medical,        // MedicalBuilding - OSM: amenity=hospital, clinic, pharmacy
        Retail,         // RetailBuilding - OSM: shop=*, building=retail (stores, supermarkets, shopping centers)
        Hospitality,    // RetailBuilding with HasFood=true - OSM: amenity=restaurant, cafe, bar, pub, fast_food
        Recreation,     // OSM: leisure=sports_centre, fitness_centre, swimming_pool, park, amenity=cinema, theatre, nightclub, casino

        // // Additional types from OSM (might implement later)
        // Industrial,     // OSM: building=industrial, warehouse
        // Government,     // OSM: amenity=townhall, government offices
        // Religious,      // OSM: amenity=place_of_worship
        // Transport       // OSM: amenity=bus_station, railway=station
    }
}
