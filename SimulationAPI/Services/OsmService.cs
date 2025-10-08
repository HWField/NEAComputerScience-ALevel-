using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using MySimulationAPI.Models;
using System.Collections.Concurrent;

namespace MySimulationAPI.Services
{
    public class OsmService
    {
        private readonly HttpClient _http = new();
        private static readonly Random _rnd = new();

        private static readonly HashSet<string> LeisureAmenities = new()
        {
            "park", "cinema", "theatre", "cafe", "pub", "bar", "sports_centre", "swimming_pool", "library"
        };

        private static readonly HashSet<string> MedicalAmenities = new()
        {
            "hospital", "clinic", "doctors", "pharmacy"
        };

        private static readonly Dictionary<string, string> BuildingTypeMap = new()
        {
            {"residential","Home"},
            {"house","Home"},
            {"apartments","Home"},
            {"office","Work"},
            {"commercial","Work"},
            {"retail","Retail"},
            {"industrial","Work"},
            {"farm","Work"},
            {"warehouse","Work"}
        };

        public async Task<List<OSMBuildingPreset>> FetchLocationsAsync(double lat, double lon, double radiusKm)
        {
            double radiusMeters = radiusKm * 1000;
            string query = $@"
[out:json];
(
  node(around:{radiusMeters},{lat},{lon})[building];
  way(around:{radiusMeters},{lat},{lon})[building];
  relation(around:{radiusMeters},{lat},{lon})[building];
  node(around:{radiusMeters},{lat},{lon})[amenity];
  way(around:{radiusMeters},{lat},{lon})[amenity];
  relation(around:{radiusMeters},{lat},{lon})[amenity];
);
out center;
";

            using var response = await _http.GetAsync(
                "https://overpass-api.de/api/interpreter?data=" + Uri.EscapeDataString(query),
                HttpCompletionOption.ResponseHeadersRead
            );
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            var osmResponse = await JsonSerializer.DeserializeAsync<OverpassResponse>(stream);

            var locations = new ConcurrentBag<OSMBuildingPreset>();
            if (osmResponse?.Elements == null) return locations.ToList();
            Console.WriteLine($"Fetched {osmResponse.Elements.Count} location elements from OSM.");
            Parallel.ForEach(osmResponse.Elements, el =>
            {
                string type = "Other";

                if (el.Tags != null)
                {
                    if (!string.IsNullOrEmpty(el.Tags.Building))
                        type = BuildingTypeMap.TryGetValue(el.Tags.Building, out var mapped) ? mapped : "Other";

                    if (!string.IsNullOrEmpty(el.Tags.Amenity))
                    {
                        string amenity = el.Tags.Amenity;
                        if (amenity == "school") type = "School";
                        else if (amenity is "restaurant" or "cafe" or "pub" or "bar")
                            type = "Leisure";
                        else if (MedicalAmenities.Contains(amenity)) type = "Medical";
                        else if (LeisureAmenities.Contains(amenity)) type = "Leisure";
                        else if (type == "Other") type = "Work";
                    }
                }

                if (type == "Other")
                    type = _rnd.NextDouble() < 0.8 ? "Home" : "Work";

                double? elementLat = el.Lat ?? el.Center?.Lat;
                double? elementLon = el.Lon ?? el.Center?.Lon;

                if (elementLat.HasValue && elementLon.HasValue && el.Id.HasValue)
                {
                    locations.Add(new OSMBuildingPreset
                    {
                        OSMId = el.Id.Value.ToString(),
                        Type = type,
                        Latitude = elementLat.Value,
                        Longitude = elementLon.Value
                    });
                }
            });

            return locations.ToList();
        }

        public async Task<List<OSMPathPreset>> FetchPathsAsync(double lat, double lon, double radiusKm)
        {
            double radiusMeters = radiusKm * 1000;
            string query = $@"
[out:json];
(
  way(around:{radiusMeters},{lat},{lon})[highway];
  way(around:{radiusMeters},{lat},{lon})[footway];
  way(around:{radiusMeters},{lat},{lon})[path];
  way(around:{radiusMeters},{lat},{lon})[cycleway];
);
out geom;
";

            using var response = await _http.GetAsync(
                "https://overpass-api.de/api/interpreter?data=" + Uri.EscapeDataString(query),
                HttpCompletionOption.ResponseHeadersRead
            );
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            var osmResponse = await JsonSerializer.DeserializeAsync<OverpassResponse>(stream);

            var paths = new ConcurrentBag<OSMPathPreset>();
            if (osmResponse?.Elements == null) return paths.ToList();

            Parallel.ForEach(osmResponse.Elements, el =>
            {
                if (el.Geometry == null || el.Geometry.Count < 2) return;

                string pathType = "Road";
                bool isOneWay = false;

                if (el.Tags != null)
                {
                    if (!string.IsNullOrEmpty(el.Tags.Highway))
                        pathType = el.Tags.Highway switch
                        {
                            "footway" or "path" or "pedestrian" => "Footpath",
                            "cycleway" => "Cycleway",
                            "motorway" or "trunk" or "primary" or "secondary" => "MainRoad",
                            _ => "Road"
                        };
                    else if (!string.IsNullOrEmpty(el.Tags.Footway)) pathType = "Footpath";
                    else if (!string.IsNullOrEmpty(el.Tags.Cycleway)) pathType = "Cycleway";

                    if (!string.IsNullOrEmpty(el.Tags.Oneway))
                        isOneWay = el.Tags.Oneway is "yes" or "1" or "true";
                }

                var nodes = el.Geometry.Select(g => new OSMPathNodePreset
                {
                    Latitude = g.Lat,
                    Longitude = g.Lon
                }).ToList();

                paths.Add(new OSMPathPreset
                {
                    OSMId = el.Id.ToString(),
                    Type = pathType,
                    IsOneWay = isOneWay,
                    Nodes = nodes
                });
            });

            return paths.ToList();
        }

        public async Task<(List<OSMBuildingPreset> Buildings, List<OSMPathPreset> Paths)> FetchMapDataAsync(double lat, double lon, double radiusKm)
        {
            var buildingsTask = FetchLocationsAsync(lat, lon, radiusKm);
            var pathsTask = FetchPathsAsync(lat, lon, radiusKm);

            await Task.WhenAll(buildingsTask, pathsTask);

            return (buildingsTask.Result, pathsTask.Result);
        }
    }

    // ---------------------------
    // DTOs for deserialization
    // ---------------------------
    public class OverpassResponse
    {
        [JsonPropertyName("elements")]
        public List<Element>? Elements { get; set; }
    }

    public class Element
    {
        [JsonPropertyName("id")]
        public long? Id { get; set; }

        [JsonPropertyName("lat")]
        public double? Lat { get; set; }

        [JsonPropertyName("lon")]
        public double? Lon { get; set; }

        [JsonPropertyName("center")]
        public Center? Center { get; set; }

        [JsonPropertyName("tags")]
        public Tags? Tags { get; set; }

        [JsonPropertyName("geometry")]
        public List<GeometryPoint>? Geometry { get; set; }
    }

    public class Center
    {
        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lon")]
        public double Lon { get; set; }
    }

    public class Tags
    {
        [JsonPropertyName("building")]
        public string? Building { get; set; }

        [JsonPropertyName("amenity")]
        public string? Amenity { get; set; }

        [JsonPropertyName("highway")]
        public string? Highway { get; set; }

        [JsonPropertyName("footway")]
        public string? Footway { get; set; }

        [JsonPropertyName("cycleway")]
        public string? Cycleway { get; set; }

        [JsonPropertyName("oneway")]
        public string? Oneway { get; set; }
    }

    public class GeometryPoint
    {
        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lon")]
        public double Lon { get; set; }
    }
}