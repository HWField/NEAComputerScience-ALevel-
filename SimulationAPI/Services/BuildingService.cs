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

            // Pre-build KD-trees by building type for fast spatial lookup
            _kdTrees = BuildKdTrees(_buildings);

            // Cache building lists by type
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
            /// <summary>
            /// Calculate approximate distance using Haversine formula (in meters)
            /// </summary>
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
                var result = new List<(double dist, T value)>();
                if (root != null)
                    Search(root, new[] { x, y }, k, result);
                return result.OrderBy(r => r.dist).Take(k).Select(r => r.value).ToList();
            }

            private void Search(Node? node, double[] target, int k, List<(double dist, T value)> result)
            {
                if (node == null) return;

                double d = Math.Sqrt(
                    Math.Pow(node.Point[0] - target[0], 2) +
                    Math.Pow(node.Point[1] - target[1], 2)
                );
                result.Add((d, node.Value));

                int axis = node.Depth % 2;
                Node? next = target[axis] < node.Point[axis] ? node.Left : node.Right;
                Node? other = target[axis] < node.Point[axis] ? node.Right : node.Left;

                Search(next, target, k, result);

                if (result.Count < k || Math.Abs(node.Point[axis] - target[axis]) < result.Max(r => r.dist))
                {
                    Search(other, target, k, result);
                }
            }
        }

        // ---------------------------------------------------------
        // Assignment
        // ---------------------------------------------------------

        public void AssignHousing()
        {
            var agents = _context.Agents;
            var houses = _buildings.OfType<ResidentialBuilding>().ToList();
            var rng = _context.Rng;

            Console.WriteLine($"Assigning housing to {agents.Count} agents across {houses.Count} houses");

            // Reset capacities
            foreach (var h in houses) h.RemainingCapacity = h.Capacity;

            // Build list of available slots (much more efficient than random selection)
            var availableSlots = new List<ResidentialBuilding>();
            foreach (var house in houses)
            {
                for (int i = 0; i < house.Capacity; i++)
                {
                    availableSlots.Add(house);
                }
            }

            Console.WriteLine($"Total housing capacity: {availableSlots.Count}");

            if (availableSlots.Count < agents.Count)
            {
                throw new InvalidOperationException(
                    $"Insufficient housing capacity: {availableSlots.Count} slots for {agents.Count} agents");
            }

            // Shuffle the slots for random distribution
            for (int i = availableSlots.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (availableSlots[i], availableSlots[j]) = (availableSlots[j], availableSlots[i]);
            }

            // Assign agents to shuffled slots - O(n) instead of O(n²)!
            for (int i = 0; i < agents.Count; i++)
            {
                var house = availableSlots[i];
                agents[i].House = house;
                house.RemainingCapacity--;
            }
        }

        public void AssignWorkplaces(
            double beta = 2.0,
            int shortlistSize = 10,
            int spilloverIncrement = 10)
        {
            var agents = _context.Agents;
            var buildings = _context.Buildings.ToList();
            var rng = _context.Rng;

            // Reset building capacities
            foreach (var b in buildings)
                b.RemainingCapacity = b.Capacity;

            // Use pre-built KD-trees from constructor
            var buildingTypeToKdTree = _kdTrees;

            // Process sequentially to avoid race conditions
            foreach (var agent in agents)
            {
                // Skip retired agents
                if (agent is ElderlyRetiredAgent) continue;

                // Match agent type to building type
                Type buildingType = agent switch
                {
                    StudentAgent => typeof(EducationBuilding),
                    MedicalWorkerAgent => typeof(MedicalBuilding),
                    RetailWorkerAgent => typeof(RetailBuilding),
                    ServiceWorkerAgent => typeof(LeisureBuilding),
                    _ => typeof(OfficeBuilding) // Default for office workers and others
                };

                // If no buildings of this type, skip
                if (!buildingTypeToKdTree.TryGetValue(buildingType, out var tree))
                    continue;

                if (agent.House == null) continue; // Skip if no home assigned

                int k = shortlistSize;
                bool assigned = false;

                // Keep expanding search until workplace found or no more options
                while (!assigned && k <= buildings.Count)
                {
                    var shortlist = tree.KNearest(agent.House.Latitude, agent.House.Longitude, k);

                    // Compute weights based on distance and capacity
                    var weights = new List<double>();
                    double totalW = 0;

                    foreach (var b in shortlist)
                    {
                        double dist = GeoUtils.Distance(agent.House.Latitude, agent.House.Longitude,
                                                       b.Latitude, b.Longitude);
                        double w = (b.RemainingCapacity + 1) / Math.Pow(dist + 1, beta);
                        weights.Add(w);
                        totalW += w;
                    }

                    if (totalW <= 0)
                    {
                        k += spilloverIncrement;
                        continue;
                    }

                    // Sample from weighted distribution
                    double r = rng.NextDouble() * totalW;
                    double cum = 0;

                    for (int i = 0; i < shortlist.Count; i++)
                    {
                        cum += weights[i];
                        if (r <= cum)
                        {
                            var chosen = shortlist[i];

                            if (chosen.RemainingCapacity > 0)
                            {
                                chosen.RemainingCapacity--;
                                agent.Workplace = chosen;
                                assigned = true;
                            }
                            break;
                        }
                    }

                    if (!assigned)
                        k += spilloverIncrement; // Expand search radius
                }
            }
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
