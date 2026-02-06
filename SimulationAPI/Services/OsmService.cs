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

        // Amenity classifications based on OSM tags to determine building types for capacity estimation and agent assignment.
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
                (._;>;);
                out body;
                ";

            using var response = await _http.GetAsync(
                "https://overpass-api.de/api/interpreter?data=" + Uri.EscapeDataString(query),
                HttpCompletionOption.ResponseHeadersRead
            );
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            var osmResponse = await JsonSerializer.DeserializeAsync<OverpassResponse>(stream);

            if (osmResponse?.Elements == null) return new List<OSMPathPreset>();

            // Separate nodes and ways from the response
            var nodeDict = new Dictionary<long, (double Lat, double Lon)>();
            var ways = new List<Element>();

            foreach (var el in osmResponse.Elements)
            {
                if (el.Type == "node" && el.Id.HasValue && el.Lat.HasValue && el.Lon.HasValue)
                {
                    nodeDict[el.Id.Value] = (el.Lat.Value, el.Lon.Value);
                }
                else if (el.Type == "way")
                {
                    ways.Add(el);
                }
            }

            Console.WriteLine($"[OSM] Fetched {nodeDict.Count} unique nodes and {ways.Count} ways");

            var paths = new ConcurrentBag<OSMPathPreset>();

            Parallel.ForEach(ways, way =>
            {
                if (way.Nodes == null || way.Nodes.Count < 2) return;

                string pathType = "Road";
                bool isOneWay = false;
                double speedModifier = 1.0;

                if (way.Tags != null)
                {
                    if (!string.IsNullOrEmpty(way.Tags.Highway))
                    {
                        pathType = way.Tags.Highway switch
                        {
                            "footway" or "path" or "pedestrian" or "steps" => "Walkway",
                            "cycleway" => "Cycleway",
                            "motorway" or "trunk" => "Motorway",
                            "primary" or "secondary" => "Road",
                            "residential" or "tertiary" or "unclassified" => "Residential",
                            "service" or "track" => "Residential",
                            _ => "Road"
                        };

                        // Set speed modifiers based on road type
                        speedModifier = way.Tags.Highway switch
                        {
                            "motorway" or "trunk" => 2.0,      // Fast roads
                            "primary" or "secondary" => 1.5,    // Main roads
                            "residential" or "tertiary" => 1.0, // Normal roads
                            "service" or "track" => 0.8,        // Slow roads
                            "footway" or "path" or "pedestrian" => 0.6, // Walking paths
                            _ => 1.0
                        };
                    }
                    else if (!string.IsNullOrEmpty(way.Tags.Footway))
                    {
                        pathType = "Walkway";
                        speedModifier = 0.6;
                    }
                    else if (!string.IsNullOrEmpty(way.Tags.Cycleway))
                    {
                        pathType = "Cycleway";
                        speedModifier = 1.2;
                    }

                    if (!string.IsNullOrEmpty(way.Tags.Oneway))
                        isOneWay = way.Tags.Oneway is "yes" or "1" or "true";
                }

                // Build ordered list of nodes with their coordinates
                var pathNodes = new List<OSMPathNodePreset>();
                foreach (var nodeRef in way.Nodes)
                {
                    if (nodeDict.TryGetValue(nodeRef, out var coords))
                    {
                        pathNodes.Add(new OSMPathNodePreset
                        {
                            OSMNodeId = nodeRef,
                            Latitude = coords.Lat,
                            Longitude = coords.Lon
                        });
                    }
                }

                if (pathNodes.Count < 2) return;

                paths.Add(new OSMPathPreset
                {
                    OSMId = way.Id?.ToString() ?? "unknown",
                    Type = pathType,
                    IsOneWay = isOneWay,
                    SpeedModifier = speedModifier,
                    Nodes = pathNodes
                });
            });

            Console.WriteLine($"[OSM] Created {paths.Count} path segments");
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
        [JsonPropertyName("type")]
        public string? Type { get; set; }

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

        [JsonPropertyName("nodes")]
        public List<long>? Nodes { get; set; }
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