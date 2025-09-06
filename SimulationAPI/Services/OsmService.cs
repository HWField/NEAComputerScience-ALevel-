using System.Net.Http;
using System.Text.Json;
using MySimulationAPI.Models;

namespace MySimulationAPI.Services
{
    public class OsmService
    {
        private readonly HttpClient _http = new();

        public async Task<List<Location>> FetchLocationsAsync(double lat, double lon, double radiusKm)
        {
            double radiusMeters = radiusKm * 1000;

            // Overpass QL query: fetch buildings and amenities
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

                    if (building != null)
                    {
                        type = building switch
                        {
                            "residential" => "Home",
                            "office" => "Work",
                            "commercial" => "Work",
                            "retail" => "Shop",
                            _ => "Other"
                        };
                    }
                    else if (amenity != null)
                    {
                        type = amenity switch
                        {
                            "school" => "School",
                            "restaurant" => "Restaurant",
                            _ => "Other"
                        };
                    }
                }

                double? elementLat = null;
                double? elementLon = null;

                // Try "lat" / "lon" first (nodes)
                if (el.TryGetProperty("lat", out var latEl) &&
                    el.TryGetProperty("lon", out var lonEl))
                {
                    elementLat = latEl.GetDouble();
                    elementLon = lonEl.GetDouble();
                }
                // If node doesn't exist (ways/relations), use "center"
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
                        Lon = elementLon.Value,
                    });
                }
            }

            return locations;
        }
    }
}