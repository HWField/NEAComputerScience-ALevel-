namespace MySimulationAPI.Models
{
    public class MapAreaPreset
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Radius { get; set; }


        public ICollection<OSMBuildingPreset> Buildings { get; set; } = new List<OSMBuildingPreset>();
        public ICollection<OSMPathPreset> Paths { get; set; } = new List<OSMPathPreset>();
    }

    public class OSMBuildingPreset
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public string? OSMId { get; set; } // OSM unique identifier
        public int? Capacity { get; set; }
        public string Type { get; set; } = null!; // Home, School, Work, Medical, etc.

        public int MapAreaPresetId { get; set; }
        public MapAreaPreset MapAreaPreset { get; set; } = null!;
    }

    public class OSMPathPreset
    {
        public int Id { get; set; }
        public string? OSMId { get; set; }
        public ICollection<OSMPathNodePreset> Nodes { get; set; } = new List<OSMPathNodePreset>();
        public string Type { get; set; } = "Road";
        public double SpeedModifier { get; set; } = 1.0;
        public bool IsOneWay { get; set; } = false;

        public int MapAreaPresetId { get; set; }
        public MapAreaPreset MapAreaPreset { get; set; } = null!;
    }

    public class OSMPathNodePreset
    {
        public int Id { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public int OSMPathPresetId { get; set; }
        public OSMPathPreset OSMPathPreset { get; set; } = null!;
    }
}