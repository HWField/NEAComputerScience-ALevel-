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
            foreach (var h in houses) h.RemainingCapacity = h.Capacity;

            var cumulative = new List<(ResidentialBuilding house, int cumulative)>();
            int totalCap = 0;
            foreach (var h in houses)
            {
                totalCap += h.Capacity;
                cumulative.Add((h, totalCap));
            }

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
            foreach (var b in buildings) b.RemainingCapacity = b.Capacity;

            // Build KD-trees by concrete building type
            var kdTrees = buildings
                .GroupBy(b => b.GetType())
                .ToDictionary(
                    g => g.Key,
                    g => new KDTree<Building>(g.Select(b => (b.Latitude, b.Longitude, b)))
                );

            Parallel.ForEach(agents, agent =>
            {
                var workplaceProp = agent.GetType().GetProperty("Workplace");
                if (workplaceProp == null) return;

                Type targetType = workplaceProp.PropertyType;
                if (!kdTrees.TryGetValue(targetType, out var tree)) return;

                int k = shortlistSize;
                while (workplaceProp.GetValue(agent) == null && k <= buildings.Count)
                {
                    var shortlist = tree.KNearest(agent.House.Latitude, agent.House.Longitude, k);

                    // Compute weights
                    var weights = new List<double>();
                    double totalW = 0;
                    foreach (var b in shortlist)
                    {
                        double dist = GeoUtils.Distance(agent.House.Latitude, agent.House.Longitude, b.Latitude, b.Longitude);
                        double w = (b.RemainingCapacity + 1) / Math.Pow(dist + 1, beta);
                        weights.Add(w);
                        totalW += w;
                    }

                    if (totalW <= 0)
                    {
                        k += spilloverIncrement;
                        continue;
                    }

                    // Sample
                    double r = rng.NextDouble() * totalW;
                    double cum = 0;
                    for (int i = 0; i < shortlist.Count; i++)
                    {
                        cum += weights[i];
                        if (r <= cum)
                        {
                            var chosen = shortlist[i];
                            lock (chosen)
                            {
                                if (chosen.RemainingCapacity > 0)
                                {
                                    chosen.RemainingCapacity--;
                                    // Dynamically set agent.Workplace with the correct typed building
                                    workplaceProp.SetValue(agent, chosen);
                                }
                            }
                            break;
                        }
                    }

                    if (workplaceProp.GetValue(agent) == null)
                    {
                        k += spilloverIncrement; // keep expanding if all chosen full
                    }
                }
            });
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

        // // Additional types from OSM
        // Industrial,     // OSM: building=industrial, warehouse
        // Government,     // OSM: amenity=townhall, government offices
        // Religious,      // OSM: amenity=place_of_worship
        // Transport       // OSM: amenity=bus_station, railway=station
    }
}
