using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using MySimulationAPI.Models;
using MySimulationAPI.SimulationConfiguration;

namespace MySimulationAPI.Services
{
    // Basic Node + Edge Structures
    public class Node
    {
        public long Id { get; init; }
        public double Lat { get; init; }
        public double Lon { get; init; }
    }

    public class Edge
    {
        public long ToId { get; init; }
        public double DistanceMeters { get; init; }
        public double SpeedModifier { get; init; } = 1.0;
        public int PathId { get; init; }
    }

    // Spatial Index for Nearest Node
    public class NodeGridIndex
    {
        private readonly double _minLon, _minLat, _cellSize;
        private readonly int _cols, _rows;
        private readonly List<long>[][] _cells;
        private readonly IReadOnlyDictionary<long, Node> _nodes;

        public NodeGridIndex(IReadOnlyDictionary<long, Node> nodes,
                             double minLon, double minLat,
                             double cellSizeDegrees, int cols, int rows)
        {
            _nodes = nodes;
            _minLon = minLon;
            _minLat = minLat;
            _cellSize = cellSizeDegrees;
            _cols = cols;
            _rows = rows;

            _cells = Enumerable.Range(0, _cols)
                .Select(_ => Enumerable.Range(0, _rows).Select(__ => new List<long>()).ToArray())
                .ToArray();

            foreach (var kv in nodes)
            {
                var n = kv.Value;
                var (cx, cy) = ToCell(n.Lon, n.Lat);
                if (cx >= 0 && cx < _cols && cy >= 0 && cy < _rows)
                    _cells[cx][cy].Add(kv.Key);
            }
        }

        private (int cx, int cy) ToCell(double lon, double lat)
        {
            var cx = (int)Math.Floor((lon - _minLon) / _cellSize);
            var cy = (int)Math.Floor((lat - _minLat) / _cellSize);
            return (cx, cy);
        }

        public long FindNearestNodeId(double lat, double lon, double radiusMeters = 1000)
        {
            var (cx, cy) = ToCell(lon, lat);
            var bestId = -1L;
            double bestSq = double.MaxValue;
            var radiusCells = Math.Max(1, (int)Math.Ceiling((radiusMeters / 111000.0) / _cellSize));

            for (int dx = -radiusCells; dx <= radiusCells; dx++)
                for (int dy = -radiusCells; dy <= radiusCells; dy++)
                {
                    int x = cx + dx;
                    int y = cy + dy;
                    if (x < 0 || x >= _cols || y < 0 || y >= _rows) continue;
                    foreach (var nid in _cells[x][y])
                    {
                        var n = _nodes[nid];
                        var d2 = HaversineSquaredMeters(lat, lon, n.Lat, n.Lon);
                        if (d2 < bestSq)
                        {
                            bestSq = d2;
                            bestId = nid;
                        }
                    }
                }

            return bestId;
        }

        private static double HaversineSquaredMeters(double lat1, double lon1, double lat2, double lon2)
        {
            var d = HaversineMeters(lat1, lon1, lat2, lon2);
            return d * d;
        }

        private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000.0;
            var dLat = ToRad(lat2 - lat1);
            var dLon = ToRad(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double ToRad(double deg) => deg * Math.PI / 180.0;
    }

    // Routing Service
    public class RoutingService
    {
        private readonly SimulationContext _context;
        private readonly Dictionary<long, Node> _nodes;
        private readonly Dictionary<long, List<Edge>> _graph;
        private readonly NodeGridIndex _grid;
        private readonly ConcurrentDictionary<(long, long), List<long>> _routeCache = new();

        public RoutingService(SimulationContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            var paths = _context.Paths ?? throw new InvalidOperationException("Simulation has no map area.");
            if (paths == null || paths.Count == 0)
                throw new InvalidOperationException("Map area has no path data.");

            // Build graph
            _nodes = BuildNodeDictionary(paths);
            _graph = BuildGraph(paths, _nodes);
            _grid = BuildGrid(_nodes);
        }

        // Graph Construction
        private static Dictionary<long, Node> BuildNodeDictionary(ICollection<Models.Path> paths)
        {
            var dict = new Dictionary<long, Node>();
            foreach (var path in paths)
            {
                foreach (var pathNode in path.Nodes)
                {
                    long nodeKey = pathNode.OSMNodeId;
                    if (!dict.ContainsKey(nodeKey))
                    {
                        dict[nodeKey] = new Node
                        {
                            Id = nodeKey,
                            Lat = pathNode.Lat,
                            Lon = pathNode.Lon
                        };
                    }
                }
            }
            return dict;
        }
        private static Dictionary<long, List<Edge>> BuildGraph(ICollection<Models.Path> paths, Dictionary<long, Node> nodes)
        {
            var graph = new Dictionary<long, List<Edge>>();
            foreach (var node in nodes.Values)
                graph[node.Id] = new List<Edge>();

            foreach (var path in paths)
            {
                var nodeList = path.Nodes.OrderBy(n => n.Sequence).ToList();
                for (int i = 0; i < nodeList.Count - 1; i++)
                {
                    var n1 = nodeList[i];
                    var n2 = nodeList[i + 1];

                    // Use OSMNodeId to look up nodes in the graph 
                    long n1Key = n1.OSMNodeId;
                    long n2Key = n2.OSMNodeId;

                    var distance = HaversineMeters(n1.Lat, n1.Lon, n2.Lat, n2.Lon);

                    graph[n1Key].Add(new Edge
                    {
                        ToId = n2Key,
                        DistanceMeters = distance,
                        SpeedModifier = path.SpeedModifier,
                        PathId = path.Id
                    });

                    if (!path.IsOneWay)
                    {
                        graph[n2Key].Add(new Edge
                        {
                            ToId = n1Key,
                            DistanceMeters = distance,
                            SpeedModifier = path.SpeedModifier,
                            PathId = path.Id
                        });
                    }
                }
            }
            var disconnected = nodes.Keys.Where(id => !graph.ContainsKey(id) || graph[id].Count == 0).Count();



            return graph;
        }

        private static NodeGridIndex BuildGrid(Dictionary<long, Node> nodes)
        {
            double minLat = nodes.Values.Min(n => n.Lat);
            double minLon = nodes.Values.Min(n => n.Lon);
            double maxLat = nodes.Values.Max(n => n.Lat);
            double maxLon = nodes.Values.Max(n => n.Lon);

            double cellSizeDeg = 0.002; // roughly 200m cells
            int cols = Math.Max(1, (int)((maxLon - minLon) / cellSizeDeg) + 1);
            int rows = Math.Max(1, (int)((maxLat - minLat) / cellSizeDeg) + 1);

            return new NodeGridIndex(nodes, minLon, minLat, cellSizeDeg, cols, rows);
        }

        // Public Route Computation
        public (List<(double Lat, double Lon)>? path, int travelTime) ComputeRoute(
            double startLat, double startLon,
            double endLat, double endLon,
            double avgSpeedMph = 3.0)
        {
            long startId = _grid.FindNearestNodeId(startLat, startLon);
            long endId = _grid.FindNearestNodeId(endLat, endLon);

            if (startId == -1L || endId == -1L)
            {
                return CreateDirectPath(startLat, startLon, endLat, endLon, avgSpeedMph);
            }

            // Handle case where start and end are the same node (or very close)
            if (startId == endId)
            {
                var node = _nodes[startId];
                var path = new List<(double Lat, double Lon)> { (node.Lat, node.Lon) };
                return (path, 0); // No travel time needed
            }

            var pathNodeIds = FindPathAStar(startId, endId);
            if (pathNodeIds == null || pathNodeIds.Count < 2)
            {
                // Graph is disconnected - these nodes cannot reach each other
                // Use direct path as fallback (agent will "teleport" but at realistic speed)
                return CreateDirectPath(startLat, startLon, endLat, endLon, avgSpeedMph);
            }

            // Convert node IDs to coordinate list
            var pathCoords = new List<(double Lat, double Lon)>(pathNodeIds.Count);
            for (int i = 0; i < pathNodeIds.Count; i++)
            {
                var node = _nodes[pathNodeIds[i]];
                pathCoords.Add((node.Lat, node.Lon));
            }

            // Compute total route distance
            double totalDistance = 0;
            for (int i = 0; i < pathCoords.Count - 1; i++)
            {
                totalDistance += HaversineMeters(
                    pathCoords[i].Lat, pathCoords[i].Lon,
                    pathCoords[i + 1].Lat, pathCoords[i + 1].Lon);
            }

            // Convert to travel time (seconds)
            var speedMps = avgSpeedMph * 0.44704;
            var travelTime = (int)Math.Round(totalDistance / speedMps);

            return (pathCoords, travelTime);
        }

        // Create a simple direct path when graph routing fails
        private (List<(double Lat, double Lon)> path, int travelTime) CreateDirectPath(
            double startLat, double startLon,
            double endLat, double endLon,
            double avgSpeedMph)
        {
            var path = new List<(double Lat, double Lon)>
            {
                (startLat, startLon),
                (endLat, endLon)
            };

            double distance = HaversineMeters(startLat, startLon, endLat, endLon);
            var speedMps = avgSpeedMph * 0.44704;
            var travelTime = (int)Math.Round(distance / speedMps);

            return (path, travelTime);
        }

        // A* Search Implementation
        private List<long>? FindPathAStar(long startId, long endId)
        {
            var key = (startId, endId);
            if (_routeCache.TryGetValue(key, out var cached))
                return cached;

            if (!_graph.TryGetValue(startId, out var startEdges))
            {
                return null;
            }

            if (!_graph.TryGetValue(endId, out _))
            {
                return null;
            }

            var open = new PriorityQueue<long, double>();
            var gScore = new Dictionary<long, double> { [startId] = 0 };
            var fScore = new Dictionary<long, double> { [startId] = HeuristicMeters(_nodes[startId], _nodes[endId]) };
            var cameFrom = new Dictionary<long, long>();

            open.Enqueue(startId, fScore[startId]);
            int iterations = 0;
            int maxIterations = 10000; // Prevent infinite loops

            while (open.Count > 0 && iterations < maxIterations)
            {
                iterations++;
                var current = open.Dequeue();
                if (current == endId)
                {
                    var path = ReconstructPath(cameFrom, current);
                    _routeCache[key] = path;
                    return path;
                }

                if (!_graph.TryGetValue(current, out var neighbors)) continue;

                foreach (var edge in neighbors)
                {
                    var tentativeG = gScore[current] + edge.DistanceMeters / edge.SpeedModifier;
                    if (!gScore.TryGetValue(edge.ToId, out var existingG) || tentativeG < existingG)
                    {
                        cameFrom[edge.ToId] = current;
                        gScore[edge.ToId] = tentativeG;
                        fScore[edge.ToId] = tentativeG + HeuristicMeters(_nodes[edge.ToId], _nodes[endId]);
                        open.Enqueue(edge.ToId, fScore[edge.ToId]);
                    }
                }
            }

            // Cache the failed result to avoid recomputing
            _routeCache[key] = null!;
            return null;
        }

        private static List<long> ReconstructPath(Dictionary<long, long> cameFrom, long current)
        {
            var path = new List<long> { current };
            while (cameFrom.TryGetValue(current, out var prev))
            {
                current = prev;
                path.Add(current);
            }
            path.Reverse();
            return path;
        }

        // Helpers
        private static double HeuristicMeters(Node a, Node b) =>
            HaversineMeters(a.Lat, a.Lon, b.Lat, b.Lon);

        private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000.0;
            var dLat = ToRad(lat2 - lat1);
            var dLon = ToRad(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double ToRad(double deg) => deg * Math.PI / 180.0;
    }
}