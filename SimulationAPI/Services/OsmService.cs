using System.Net.Http;
using System.Text.Json;
using MySimulationAPI.Models;

namespace MySimulationAPI.Services
{
    public class OsmService
    {
        private readonly HttpClient _http = new();

        private static readonly string[] LeisureAmenities = new[]
        {
            "park", "cinema", "theatre", "cafe", "pub", "bar", "sports_centre", "swimming_pool", "library"
        };

        private static readonly string[] MedicalAmenities = new[]
        {
            "hospital", "clinic", "doctors", "pharmacy"
        };

        private static readonly Random _rnd = new();

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

            var response = await _http.GetAsync(
                "https://overpass-api.de/api/interpreter?data=" + Uri.EscapeDataString(query)
            );

            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var elements = doc.RootElement.GetProperty("elements");

            var locations = new List<OSMBuildingPreset>();

            foreach (var el in elements.EnumerateArray())
            {
                string type = "Other";

                if (el.TryGetProperty("tags", out var tags))
                {
                    string? building = tags.TryGetProperty("building", out var b) ? b.GetString() : null;
                    string? amenity = tags.TryGetProperty("amenity", out var a) ? a.GetString() : null;

                    // Map building types
                    if (building != null)
                    {
                        type = building switch
                        {
                            "residential" => "Home",
                            "house" => "Home",
                            "apartments" => "Home",
                            "office" => "Work",
                            "commercial" => "Work",
                            "retail" => "Shop",
                            "industrial" => "Work",
                            "farm" => "Work",
                            "warehouse" => "Work",
                            _ => "Other" // will handle below
                        };
                    }

                    // Map amenity types
                    if (amenity != null)
                    {
                        if (amenity == "school") type = "School";
                        else if (amenity == "restaurant" || amenity == "cafe" || amenity == "pub" || amenity == "bar") type = "Leisure";
                        else if (MedicalAmenities.Contains(amenity)) type = "Medical";
                        else if (LeisureAmenities.Contains(amenity)) type = "Leisure";
                        else if (type == "Other") type = "Work"; // fallback
                    }
                }

                // Randomly assign “Other” types as Home or Work if still Other
                if (type == "Other")
                {
                    type = _rnd.NextDouble() < 0.8 ? "Home" : "Work";
                }

                double? elementLat = null;
                double? elementLon = null;

                if (el.TryGetProperty("lat", out var latEl) &&
                    el.TryGetProperty("lon", out var lonEl))
                {
                    elementLat = latEl.GetDouble();
                    elementLon = lonEl.GetDouble();
                }
                else if (el.TryGetProperty("center", out var center) &&
                         center.TryGetProperty("lat", out var cLat) &&
                         center.TryGetProperty("lon", out var cLon))
                {
                    elementLat = cLat.GetDouble();
                    elementLon = cLon.GetDouble();
                }

                if (elementLat.HasValue && elementLon.HasValue)
                {
                    locations.Add(new OSMBuildingPreset
                    {
                        OSMId = el.GetProperty("id").GetRawText(),
                        Type = type,
                        Latitude = elementLat.Value,
                        Longitude = elementLon.Value
                    });
                }
            }

            return locations;
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

            var response = await _http.GetAsync(
                "https://overpass-api.de/api/interpreter?data=" + Uri.EscapeDataString(query)
            );

            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var elements = doc.RootElement.GetProperty("elements");

            var paths = new List<OSMPathPreset>();

            foreach (var el in elements.EnumerateArray())
            {
                if (!el.TryGetProperty("geometry", out var geometry))
                    continue;

                var pathType = "Road";
                var isOneWay = false;

                if (el.TryGetProperty("tags", out var tags))
                {
                    // Determine path type
                    if (tags.TryGetProperty("highway", out var highway))
                    {
                        string highwayType = highway.GetString() ?? "unclassified";
                        pathType = highwayType switch
                        {
                            "footway" or "path" or "pedestrian" => "Footpath",
                            "cycleway" => "Cycleway",
                            "motorway" or "trunk" or "primary" or "secondary" => "MainRoad",
                            _ => "Road" // residential, tertiary, service, etc.
                        };
                    }
                    else if (tags.TryGetProperty("footway", out _))
                    {
                        pathType = "Footpath";
                    }
                    else if (tags.TryGetProperty("cycleway", out _))
                    {
                        pathType = "Cycleway";
                    }

                    // Check if path is one-way
                    if (tags.TryGetProperty("oneway", out var oneway))
                    {
                        string onewayValue = oneway.GetString() ?? "no";
                        isOneWay = onewayValue == "yes" || onewayValue == "1" || onewayValue == "true";
                    }
                }

                var nodes = new List<OSMPathNodePreset>();

                foreach (var point in geometry.EnumerateArray())
                {
                    if (point.TryGetProperty("lat", out var nodeLat) &&
                        point.TryGetProperty("lon", out var nodeLon))
                    {
                        nodes.Add(new OSMPathNodePreset
                        {
                            Latitude = nodeLat.GetDouble(),
                            Longitude = nodeLon.GetDouble()
                        });
                    }
                }

                if (nodes.Count >= 2) // Only add paths with at least 2 nodes
                {
                    paths.Add(new OSMPathPreset
                    {
                        OSMId = el.GetProperty("id").GetRawText(),
                        Type = pathType,
                        IsOneWay = isOneWay,
                        Nodes = nodes
                    });
                }
            }

            return paths;
        }


        // Combined method to fetch both buildings and paths in one call
        public async Task<(List<OSMBuildingPreset> Buildings, List<OSMPathPreset> Paths)> FetchMapDataAsync(double lat, double lon, double radiusKm)
        {
            var buildingsTask = FetchLocationsAsync(lat, lon, radiusKm);
            var pathsTask = FetchPathsAsync(lat, lon, radiusKm);

            await Task.WhenAll(buildingsTask, pathsTask);

            return (buildingsTask.Result, pathsTask.Result);
        }
    }
}