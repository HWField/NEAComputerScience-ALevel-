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
            "park", "cinema", "theatre", "cafe", "pub", "bar", "sports_centre", "swimming_pool",
            "library", "nightclub", "casino", "community_centre", "arts_centre"
        };

        private static readonly HashSet<string> MedicalAmenities = new()
        {
            "hospital", "clinic", "doctors", "pharmacy", "dentist", "veterinary"
        };

        private static readonly HashSet<string> EducationAmenities = new()
        {
            "school", "kindergarten", "university", "college", "language_school", "music_school"
        };

        private static readonly HashSet<string> FoodServiceAmenities = new()
        {
            "restaurant", "cafe", "pub", "bar", "fast_food", "food_court", "ice_cream"
        };

        private static readonly HashSet<string> ResidentialBuildings = new()
        {
            "residential", "house", "apartments", "detached", "semidetached_house",
            "terrace", "dormitory", "bungalow", "static_caravan", "houseboat"
        };

        private static readonly HashSet<string> WorkBuildings = new()
        {
            "office", "industrial", "warehouse", "factory", "manufacture"
        };

        private static readonly HashSet<string> RetailShops = new()
        {
            "supermarket", "mall", "department_store", "convenience", "clothes",
            "electronics", "furniture", "hardware", "books", "jewelry", "shoes"
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
                string type = ClassifyBuildingType(el.Tags);

                double? elementLat = el.Lat ?? el.Center?.Lat;
                double? elementLon = el.Lon ?? el.Center?.Lon;

                if (elementLat.HasValue && elementLon.HasValue && el.Id.HasValue)
                {
                    // Calculate capacity based on building type and OSM tags
                    int capacity = BuildingCapacityCalculator.CalculateCapacity(type, el.Tags);

                    // Extract building name if available
                    string buildingName = el.Tags?.Name ?? $"{type} {el.Id.Value}";

                    locations.Add(new OSMBuildingPreset
                    {
                        OSMId = el.Id.Value.ToString(),
                        Name = buildingName,
                        Type = type,
                        Latitude = elementLat.Value,
                        Longitude = elementLon.Value,
                        Capacity = capacity
                    });
                }
            });

            return locations.ToList();
        }

        private static string ClassifyBuildingType(Tags? tags)
        {
            if (tags == null) return "Home";

            if (!string.IsNullOrEmpty(tags.Amenity))
            {
                string amenity = tags.Amenity.ToLower();

                if (EducationAmenities.Contains(amenity))
                    return "School";

                if (MedicalAmenities.Contains(amenity))
                    return "Medical";

                if (FoodServiceAmenities.Contains(amenity))
                    return "Leisure";

                if (LeisureAmenities.Contains(amenity))
                    return "Leisure";

                if (amenity == "parking")
                    return "Work";
            }

            if (!string.IsNullOrEmpty(tags.Healthcare))
                return "Medical";

            if (!string.IsNullOrEmpty(tags.Shop))
            {
                string shop = tags.Shop.ToLower();
                return RetailShops.Contains(shop) || shop != "no" ? "Retail" : "Leisure";
            }

            if (!string.IsNullOrEmpty(tags.Office) && tags.Office.ToLower() != "no")
                return "Work";

            if (!string.IsNullOrEmpty(tags.Tourism))
            {
                string tourism = tags.Tourism.ToLower();
                if (tourism == "hotel" || tourism == "hostel" || tourism == "motel")
                    return "Leisure"; // Hotels count as leisure/temporary residence
                if (tourism == "museum" || tourism == "gallery" || tourism == "attraction")
                    return "Leisure";
            }

            if (!string.IsNullOrEmpty(tags.Leisure))
            {
                return "Leisure";
            }

            if (!string.IsNullOrEmpty(tags.Building))
            {
                string building = tags.Building.ToLower();

                if (ResidentialBuildings.Contains(building))
                    return "Home";

                if (WorkBuildings.Contains(building))
                    return "Work";

                if (building == "commercial")
                {
                    if (!string.IsNullOrEmpty(tags.Shop))
                        return "Retail";
                    return "Work"; // Default commercial to work
                }

                if (building == "retail")
                    return "Retail";

                if (building == "farm" || building == "barn" || building == "cowshed")
                    return "Work";

                if (building == "hospital")
                    return "Medical";

                if (building == "school" || building == "university" || building == "college")
                    return "School";
            }

            if (!string.IsNullOrEmpty(tags.Landuse))
            {
                string landuse = tags.Landuse.ToLower();
                if (landuse == "residential")
                    return "Home";
                if (landuse == "commercial" || landuse == "retail")
                    return "Retail";
                if (landuse == "industrial")
                    return "Work";
            }

            if (!string.IsNullOrEmpty(tags.BuildingUse))
            {
                string use = tags.BuildingUse.ToLower();
                if (use.Contains("residential"))
                    return "Home";
                if (use.Contains("commercial") || use.Contains("retail"))
                    return "Retail";
                if (use.Contains("office") || use.Contains("industrial"))
                    return "Work";
            }

            // if it has an address, likely residential
            if (!string.IsNullOrEmpty(tags.AddrStreet) && !string.IsNullOrEmpty(tags.AddrHousenumber))
            {
                return _rnd.NextDouble() < 0.7 ? "Home" : "Work";
            }

            // fallback random assignment
            return _rnd.NextDouble() < 0.75 ? "Home" : "Work";
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

        // Capacity-related tags
        [JsonPropertyName("building:levels")]
        public string? BuildingLevels { get; set; }

        [JsonPropertyName("building:flats")]
        public string? BuildingFlats { get; set; }

        [JsonPropertyName("capacity")]
        public string? Capacity { get; set; }

        [JsonPropertyName("beds")]
        public string? Beds { get; set; }

        [JsonPropertyName("rooms")]
        public string? Rooms { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("building:type")]
        public string? BuildingType { get; set; }

        [JsonPropertyName("shop")]
        public string? Shop { get; set; }

        [JsonPropertyName("office")]
        public string? Office { get; set; }

        [JsonPropertyName("tourism")]
        public string? Tourism { get; set; }

        [JsonPropertyName("healthcare")]
        public string? Healthcare { get; set; }

        [JsonPropertyName("emergency")]
        public string? Emergency { get; set; }

        [JsonPropertyName("leisure")]
        public string? Leisure { get; set; }

        [JsonPropertyName("landuse")]
        public string? Landuse { get; set; }

        [JsonPropertyName("building:use")]
        public string? BuildingUse { get; set; }

        [JsonPropertyName("addr:street")]
        public string? AddrStreet { get; set; }

        [JsonPropertyName("addr:housenumber")]
        public string? AddrHousenumber { get; set; }
    }

    public class GeometryPoint
    {
        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lon")]
        public double Lon { get; set; }
    }
}