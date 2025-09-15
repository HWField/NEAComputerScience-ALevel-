using System;
using System.Collections.Generic;
using System.Linq;
using MySimulationAPI.Models;

namespace MySimulationAPI.Services
{
    public class BuildingService
    {
        private readonly RoutingService _routingService;

        public BuildingService(RoutingService routingService)
        {
            _routingService = routingService;
        }

        public Building? GetBuildingFromTypeAndTravelTime(BuildingType buildingType, double Latitude, double Longitude, int maxTravelTimeMinutes)
        {
            throw new NotImplementedException("Not Implemented yet");
        }

        public List<Building>? FindBuildingsByType(BuildingType buildingType, double centerLat, double centerLon, double radiusMeters)
        {
            throw new NotImplementedException("Not Implemented yet");
        }

        public Building? FindNearestBuildingByType(BuildingType buildingType, double lat, double lon, double maxRadiusMeters = 5000)
        {
            // Find nearest building of type
            throw new NotImplementedException("Not Implemented yet");
        }
    }
    public enum BuildingType
    {
        Residential,    // ResidentialBuilding - OSM: building=residential, apartments, house
        Office,         // OfficeBuilding - OSM: building=office, commercial
        Education,      // EducationBuilding - OSM: amenity=school, university, college
        Medical,        // MedicalBuilding - OSM: amenity=hospital, clinic, pharmacy
        Retail,         // RetailBuilding - OSM: shop=*, building=retail (stores, supermarkets, shopping centers)
        Hospitality,    // RetailBuilding with HasFood=true - OSM: amenity=restaurant, cafe, bar, pub, fast_food
        Recreation,     // OSM: leisure=sports_centre, fitness_centre, swimming_pool, park, amenity=cinema, theatre, nightclub, casino

        // // Additional types from OSM
        // Industrial,     // OSM: building=industrial, warehouse
        // Government,     // OSM: amenity=townhall, government offices
        // Religious,      // OSM: amenity=place_of_worship
        // Transport       // OSM: amenity=bus_station, railway=station
    }
}
