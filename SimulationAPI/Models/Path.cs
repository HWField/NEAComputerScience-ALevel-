using System.Collections.Generic;

namespace MySimulationAPI.Models
{
    public class Path
    {
        public int Id { get; set; }

        // Sequence of nodes making up this path
        public List<PathNode> Nodes { get; set; } = new();

        // Road/walkway classification
        public PathType Type { get; set; } = PathType.Road;

        // Speed modifier (e.g. agent moves faster on motorways, slower in residential areas)
        public double SpeedModifier { get; set; } = 1.0;

        // Oneway restrictions
        public bool IsOneWay { get; set; } = false;

        public int MapAreaId { get; set; }
        public MapArea MapArea { get; set; } = null!;
    }

    public class PathNode
    {
        public int Id { get; set; }
        public long OSMNodeId { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
        public int Sequence { get; set; } // Order in the path
        public int PathId { get; set; }
        public Models.Path Path { get; set; } = null!;
        public List<Path> ConnectedPaths { get; set; } = new();
    }

    public enum PathType
    {
        Road,
        Residential,
        Motorway,
        Walkway,
        Cycleway,
        Rail,
        Other
    }

}