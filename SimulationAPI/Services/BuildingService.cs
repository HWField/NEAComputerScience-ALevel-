using System;
using System.Collections.Generic;
using System.Linq;
using MySimulationAPI.Models;

namespace MySimulationAPI.Services
{
    public class BuildingService
    {
        private readonly RoutingService _routingService;

        public BuildingService(RoutingService routingService)
        {
            _routingService = routingService;
        }

        public Building? GetBuildingFromTypeAndTravelTime(BuildingType buildingType, double Latitude, double Longitude, int maxTravelTimeMinutes)
        {
            throw new NotImplementedException("Not Implemented yet");
        }

        public List<Building>? FindBuildingsByType(BuildingType buildingType, double centerLat, double centerLon, double radiusMeters)
        {
            throw new NotImplementedException("Not Implemented yet");
        }

        public Building? FindNearestBuildingByType(BuildingType buildingType, double lat, double lon, double maxRadiusMeters = 5000)
        {
            // Find nearest building of type
            throw new NotImplementedException("Not Implemented yet");
        }



        public static class GeoUtils
        {
            public static double Distance(double lat1, double lon1, double lat2, double lon2)
            {
                double dx = lat1 - lat2;
                double dy = lon1 - lon2;
                return Math.Sqrt(dx * dx + dy * dy);
            }
        }

        // ---------------------------------------------------------
        // KD-Tree for 2D nearest neighbour search
        // ---------------------------------------------------------
        public class KDTree<T>
        {
            private class Node
            {
                public double[] Point;
                public T Value;
                public Node Left, Right;
                public int Depth;
            }

            private Node root;

            public KDTree(IEnumerable<(double x, double y, T value)> items)
            {
                var list = items.ToList();
                root = Build(list, depth: 0);
            }

            private Node Build(List<(double x, double y, T value)> points, int depth)
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
                Search(root, new[] { x, y }, k, result);
                return result.OrderBy(r => r.dist).Take(k).Select(r => r.value).ToList();
            }

            private void Search(Node node, double[] target, int k, List<(double dist, T value)> result)
            {
                if (node == null) return;

                double d = Math.Sqrt(
                    Math.Pow(node.Point[0] - target[0], 2) +
                    Math.Pow(node.Point[1] - target[1], 2)
                );
                result.Add((d, node.Value));

                int axis = node.Depth % 2;
                Node next = target[axis] < node.Point[axis] ? node.Left : node.Right;
                Node other = target[axis] < node.Point[axis] ? node.Right : node.Left;

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

        public static void AssignHousing(List<Agent> agents, List<ResidentialBuilding> houses, Random rng)
        {
            Console.WriteLine($"Assigning housing to {agents.Count} agents across {houses.Count} houses");
            foreach (var h in houses) h.RemainingCapacity = h.Capacity;

            var cumulative = new List<(ResidentialBuilding house, int cumulative)>();
            int totalCap = 0;
            foreach (var h in houses)
            {
                totalCap += h.Capacity;
                cumulative.Add((h, totalCap));
            }
            Console.WriteLine($"Total housing capacity: {totalCap}");
            foreach (var agent in agents)
            {
                while (true)
                {
                    int r = rng.Next(totalCap);
                    int idx = cumulative.BinarySearch((null, r),
                        Comparer<(ResidentialBuilding house, int cumulative)>.Create((a, b) => a.cumulative.CompareTo(b.cumulative)));
                    if (idx < 0) idx = ~idx;

                    var house = cumulative[idx].house;
                    if (house.RemainingCapacity > 0)
                    {
                        house.RemainingCapacity--;
                        agent.House = house;
                        break;
                    }
                }
            }
        }

        public static void AssignWorkplaces(
    List<Agent> agents,
    List<Building> buildings,
    double beta,
    int shortlistSize,
    int spilloverIncrement,
    Random rng)
        {
            // Reset building capacities
            foreach (var b in buildings) b.RemainingCapacity = b.Capacity;

            // Dictionary to track and prevent duplicate workplace IDs
            var assignedWorkplaces = new Dictionary<Agent, int>();

            // Build KD-trees by building type
            var buildingTypeToKdTree = new Dictionary<Type, KDTree<Building>>();

            var residentialBuildings = buildings.OfType<ResidentialBuilding>().ToList();
            var officeBuildings = buildings.OfType<OfficeBuilding>().ToList();
            var educationBuildings = buildings.OfType<EducationBuilding>().ToList();
            var medicalBuildings = buildings.OfType<MedicalBuilding>().ToList();
            var retailBuildings = buildings.OfType<RetailBuilding>().ToList();
            var leisureBuildings = buildings.OfType<LeisureBuilding>().ToList();

            if (officeBuildings.Any())
                buildingTypeToKdTree[typeof(OfficeBuilding)] =
                    new KDTree<Building>(officeBuildings.Select(b => (b.Latitude, b.Longitude, (Building)b)));

            if (educationBuildings.Any())
                buildingTypeToKdTree[typeof(EducationBuilding)] =
                    new KDTree<Building>(educationBuildings.Select(b => (b.Latitude, b.Longitude, (Building)b)));

            if (medicalBuildings.Any())
                buildingTypeToKdTree[typeof(MedicalBuilding)] =
                    new KDTree<Building>(medicalBuildings.Select(b => (b.Latitude, b.Longitude, (Building)b)));

            if (retailBuildings.Any())
                buildingTypeToKdTree[typeof(RetailBuilding)] =
                    new KDTree<Building>(retailBuildings.Select(b => (b.Latitude, b.Longitude, (Building)b)));

            if (leisureBuildings.Any())
                buildingTypeToKdTree[typeof(LeisureBuilding)] =
                    new KDTree<Building>(leisureBuildings.Select(b => (b.Latitude, b.Longitude, (Building)b)));

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
