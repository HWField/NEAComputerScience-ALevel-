using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

namespace MySimulationAPI.Services
{
    // Minimal node + edge DTOs used by the router
    public class Node
    {
        public int Id { get; init; }
        public double Lat { get; init; }
        public double Lon { get; init; }
    }

    public class Edge
    {
        public int ToId { get; init; }
        public double DistanceMeters { get; init; }
        public double SpeedModifier { get; init; } = 1.0; // lower -> slower
        public int PathId { get; init; } // optional
    }

    // Simple spatial grid index for nearest-node queries
    public class NodeGridIndex
    {
        private readonly double _minLon, _minLat, _cellSize; // cellSize in degrees (approx) or meters converted
        private readonly int _cols, _rows;
        private readonly List<int>[][] _cells; // stores node ids
        private readonly IReadOnlyDictionary<int, Node> _nodes;

        // Build using nodes and chosen cell size in degrees (approx). For production, convert meters->degrees.
        public NodeGridIndex(IReadOnlyDictionary<int, Node> nodes, double minLon, double minLat, double cellSizeDegrees, int cols, int rows)
        {
            _nodes = nodes;
            _minLon = minLon;
            _minLat = minLat;
            _cellSize = cellSizeDegrees;
            _cols = cols;
            _rows = rows;

            _cells = Enumerable.Range(0, _cols)
                .Select(_ => Enumerable.Range(0, _rows).Select(__ => new List<int>()).ToArray())
                .ToArray();

            foreach (var kv in nodes)
            {
                var id = kv.Key;
                var n = kv.Value;
                var (cx, cy) = ToCell(n.Lon, n.Lat);
                if (cx >= 0 && cx < _cols && cy >= 0 && cy < _rows)
                    _cells[cx][cy].Add(id);
            }
        }

        private (int cx, int cy) ToCell(double lon, double lat)
        {
            var cx = (int)Math.Floor((lon - _minLon) / _cellSize);
            var cy = (int)Math.Floor((lat - _minLat) / _cellSize);
            return (cx, cy);
        }

        // radiusMeters -> convert to degrees roughly: 1 deg lat ≈ 111,000 m. We'll expand a few cells.
        public int FindNearestNodeId(double lat, double lon, double radiusMeters = 1000)
        {
            var (cx, cy) = ToCell(lon, lat);
            var bestId = -1;
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
            // We return squared distance (meters^2)
            var d = HaversineMeters(lat1, lon1, lat2, lon2);
            return d * d;
        }

        private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371000.0;
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

    public class RoutingService
    {
        // Adjacency list per map area id
        private readonly ConcurrentDictionary<int, Dictionary<int, List<Edge>>> _graphs = new();
        private readonly ConcurrentDictionary<int, IReadOnlyDictionary<int, Node>> _nodesByMap = new();
        private readonly ConcurrentDictionary<int, NodeGridIndex> _gridIndexes = new();

        // route cache keyed by mapId,startId,endId
        private readonly ConcurrentDictionary<string, List<int>> _routeCache = new();

        // Build graph from MapArea (call once when simulation created/loaded)
        // mapId: some map identifier, nodes: list of nodes, edges: list of edges (constructed from Path/PathNode)
        public void BuildGraph(int mapId, IEnumerable<Node> nodes, IEnumerable<(int from, int to, double distanceMeters, double speedModifier, int pathId)> edges,
                               double minLon, double minLat, double cellSizeDegrees = 0.002, int cols = 200, int rows = 200)
        {
            var nodeDict = nodes.ToDictionary(n => n.Id);
            _nodesByMap[mapId] = nodeDict;

            var adj = new Dictionary<int, List<Edge>>(nodeDict.Count);
            foreach (var n in nodeDict.Keys) adj[n] = new List<Edge>();

            foreach (var e in edges)
            {
                if (!adj.TryGetValue(e.from, out var list)) continue;
                list.Add(new Edge { ToId = e.to, DistanceMeters = e.distanceMeters, SpeedModifier = e.speedModifier, PathId = e.pathId });
            }

            _graphs[mapId] = adj;

            // build grid index for nearest node lookup
            var grid = new NodeGridIndex(nodeDict, minLon, minLat, cellSizeDegrees, cols, rows);
            _gridIndexes[mapId] = grid;
        }

        // A* shortest path between node ids
        public List<int>? FindPathAStar(int mapId, int startNodeId, int endNodeId)
        {
            var key = $"{mapId}:{startNodeId}:{endNodeId}";
            if (_routeCache.TryGetValue(key, out var cached)) return new List<int>(cached);

            if (!_graphs.TryGetValue(mapId, out var graph)) return null;
            var nodes = _nodesByMap[mapId];

            var pq = new PriorityQueue<int, double>();
            var gScore = new Dictionary<int, double>();
            var fScore = new Dictionary<int, double>();
            var cameFrom = new Dictionary<int, int>();

            foreach (var n in graph.Keys)
            {
                gScore[n] = double.PositiveInfinity;
                fScore[n] = double.PositiveInfinity;
            }

            gScore[startNodeId] = 0;
            fScore[startNodeId] = HeuristicMeters(nodes[startNodeId], nodes[endNodeId]);

            pq.Enqueue(startNodeId, fScore[startNodeId]);

            while (pq.Count > 0)
            {
                var current = pq.Dequeue();
                if (current == endNodeId)
                {
                    var path = ReconstructPath(cameFrom, current);
                    _routeCache[key] = path; // cache it
                    return path;
                }

                if (!graph.TryGetValue(current, out var neighbors)) continue;

                foreach (var edge in neighbors)
                {
                    var tentativeG = gScore[current] + edge.DistanceMeters / edge.SpeedModifier; // cost
                    if (tentativeG < gScore[edge.ToId])
                    {
                        cameFrom[edge.ToId] = current;
                        gScore[edge.ToId] = tentativeG;
                        fScore[edge.ToId] = tentativeG + HeuristicMeters(nodes[edge.ToId], nodes[endNodeId]);
                        pq.Enqueue(edge.ToId, fScore[edge.ToId]);
                    }
                }
            }

            return null;
        }

        private static double HeuristicMeters(Node a, Node b)
        {
            return HaversineMeters(a.Lat, a.Lon, b.Lat, b.Lon);
        }

        private static List<int> ReconstructPath(Dictionary<int, int> cameFrom, int current)
        {
            var path = new List<int> { current };
            while (cameFrom.TryGetValue(current, out var prev))
            {
                current = prev;
                path.Add(current);
            }
            path.Reverse();
            return path;
        }

        // Given a start and end lat/lon, find nearest nodes then path
        public List<int>? GetRouteForPositions(int mapId, double startLat, double startLon, double endLat, double endLon)
        {
            if (!_gridIndexes.TryGetValue(mapId, out var grid)) return null;
            var nodes = _nodesByMap[mapId];

            var startNodeId = grid.FindNearestNodeId(startLat, startLon, radiusMeters: 500);
            var endNodeId = grid.FindNearestNodeId(endLat, endLon, radiusMeters: 500);

            if (startNodeId == -1 || endNodeId == -1) return null;
            return FindPathAStar(mapId, startNodeId, endNodeId);
        }

        // Convert node path to lat/lon coordinates for movement system
        public List<(double Lat, double Lon)>? ConvertPathToCoordinates(int mapId, List<int> nodePath)
        {
            if (!_nodesByMap.TryGetValue(mapId, out var nodes)) return null;

            return nodePath.Select(nodeId =>
            {
                var node = nodes[nodeId];
                return (node.Lat, node.Lon);
            }).ToList();
        }

        // Main route computation method for agents
        public (List<(double Lat, double Lon)>? path, TimeSpan travelTime) ComputeRoute(
            int mapId, double startLat, double startLon, double endLat, double endLon,
            double avgSpeedMph = 3.0) // walking speed
        {
            var nodePath = GetRouteForPositions(mapId, startLat, startLon, endLat, endLon);
            if (nodePath == null) return (null, TimeSpan.Zero);

            var coordPath = ConvertPathToCoordinates(mapId, nodePath);
            if (coordPath == null) return (null, TimeSpan.Zero);

            // Calculate total distance and travel time
            double totalDistanceMeters = 0;
            for (int i = 0; i < coordPath.Count - 1; i++)
            {
                var (lat1, lon1) = coordPath[i];
                var (lat2, lon2) = coordPath[i + 1];
                totalDistanceMeters += HaversineMeters(lat1, lon1, lat2, lon2);
            }

            var avgSpeedMps = avgSpeedMph * 0.44704; // mph to m/s
            var travelTimeSeconds = totalDistanceMeters / avgSpeedMps;
            var travelTime = TimeSpan.FromSeconds(travelTimeSeconds);

            return (coordPath, travelTime);
        }

        // utilities
        private static double Lerp(double a, double b, double t) => a + (b - a) * t;

        private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371000.0;
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