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

        public async Task<List<Location>> FetchLocationsAsync(double lat, double lon, double radiusKm)
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

            var locations = new List<Location>();

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
                    locations.Add(new Location
                    {
                        OSMId = el.GetProperty("id").GetRawText(),
                        Type = type,
                        Lat = elementLat.Value,
                        Lon = elementLon.Value
                    });
                }
            }

            return locations;
        }
    }
}