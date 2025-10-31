using static MySimulationAPI.Services.OsmService;

namespace MySimulationAPI.Services
{
    public class BuildingCapacityCalculator
    {
        /// <summary>
        /// Calculate building capacity based on OSM tags and building type.
        /// </summary>
        public static int CalculateCapacity(string buildingType, Tags? tags)
        {
            // Check if OSM has an explicit capacity tag
            if (tags != null && !string.IsNullOrEmpty(tags.Capacity))
            {
                if (int.TryParse(tags.Capacity, out int explicitCapacity) && explicitCapacity > 0)
                {
                    return explicitCapacity;
                }
            }

            return buildingType switch
            {
                "Home" => CalculateResidentialCapacity(tags),
                "School" => CalculateEducationCapacity(tags),
                "Medical" => CalculateMedicalCapacity(tags),
                "Work" => CalculateWorkCapacity(tags),
                "Retail" => CalculateRetailCapacity(tags),
                "Leisure" => CalculateLeisureCapacity(tags),
                _ => GetDefaultCapacity(buildingType)
            };
        }

        private static int CalculateResidentialCapacity(Tags? tags)
        {
            if (tags == null) return 4; // Default house capacity

            // Check for explicit flats count
            if (!string.IsNullOrEmpty(tags.BuildingFlats))
            {
                if (int.TryParse(tags.BuildingFlats, out int flats) && flats > 0)
                {
                    // Average 2.5 people per flat
                    return (int)Math.Ceiling(flats * 2.5);
                }
            }

            // Check for rooms
            if (!string.IsNullOrEmpty(tags.Rooms))
            {
                if (int.TryParse(tags.Rooms, out int rooms) && rooms > 0)
                {
                    // Estimate capacity based on rooms (bedrooms)
                    return Math.Max(rooms, 2);
                }
            }

            // Estimate based on building levels
            int levels = GetBuildingLevels(tags);

            // Determine building type
            string? buildingSubType = tags.Building?.ToLower() ?? tags.BuildingType?.ToLower();

            if (buildingSubType == "apartments" || buildingSubType == "flats")
            {
                // Apartments: estimate 4-6 units per floor
                int unitsPerFloor = 5;
                int totalUnits = levels * unitsPerFloor;
                return (int)Math.Ceiling(totalUnits * 2.5); // 2.5 people per unit
            }
            else if (buildingSubType == "house" || buildingSubType == "detached" ||
                     buildingSubType == "semidetached_house" || buildingSubType == "terrace")
            {
                // Houses: 2-6 people depending on size
                return levels > 2 ? 6 : 4;
            }
            else if (buildingSubType == "dormitory" || buildingSubType == "residential")
            {
                // Dormitory: estimate 20-30 per floor
                return levels * 25;
            }

            // Default residential (unknown type)
            return levels > 3 ? 12 : 4;
        }

        private static int CalculateEducationCapacity(Tags? tags)
        {
            if (tags == null) return 200; // Default school capacity

            // Check for explicit capacity
            if (!string.IsNullOrEmpty(tags.Capacity))
            {
                if (int.TryParse(tags.Capacity, out int capacity) && capacity > 0)
                {
                    return capacity;
                }
            }

            int levels = GetBuildingLevels(tags);

            // Check amenity type for more specific estimation
            string? amenity = tags.Amenity?.ToLower();
            if (amenity == "kindergarten")
            {
                return levels * 30; // ~30 children per floor
            }
            else if (amenity == "university" || amenity == "college")
            {
                return levels * 200; // Large lecture halls and classrooms
            }

            // Default school: estimate classrooms
            // Assume 6-8 classrooms per floor, ~25 students per classroom
            int classroomsPerFloor = 7;
            int studentsPerClassroom = 25;
            return levels * classroomsPerFloor * studentsPerClassroom;
        }

        private static int CalculateMedicalCapacity(Tags? tags)
        {
            if (tags == null) return 50; // Default medical facility capacity

            // Check for explicit beds count (most accurate for hospitals)
            if (!string.IsNullOrEmpty(tags.Beds))
            {
                if (int.TryParse(tags.Beds, out int beds) && beds > 0)
                {
                    return beds;
                }
            }

            int levels = GetBuildingLevels(tags);

            // Determine medical facility type
            string? amenity = tags.Amenity?.ToLower();
            string? healthcare = tags.Healthcare?.ToLower();
            string? emergency = tags.Emergency?.ToLower();

            if (amenity == "hospital" || healthcare == "hospital" || emergency == "yes")
            {
                // Large hospital: ~50-100 beds per floor
                return levels * 75;
            }
            else if (amenity == "clinic" || healthcare == "clinic" || amenity == "doctors")
            {
                // Clinic: ~10-20 capacity per floor
                return levels * 15;
            }
            else if (amenity == "pharmacy")
            {
                // Pharmacy: small retail capacity
                return 10;
            }

            // Default medical facility
            return levels * 30;
        }

        private static int CalculateWorkCapacity(Tags? tags)
        {
            if (tags == null) return 50; // Default workplace capacity

            int levels = GetBuildingLevels(tags);

            string? buildingSubType = tags.Building?.ToLower() ?? tags.BuildingType?.ToLower();
            string? office = tags.Office?.ToLower();

            if (buildingSubType == "office" || office != null)
            {
                // Office building: estimate 10-15 people per floor
                // Modern offices: ~10-15 sqm per person, floor ~500-1000 sqm
                return levels * 40;
            }
            else if (buildingSubType == "industrial" || buildingSubType == "warehouse")
            {
                // Industrial/warehouse: fewer people, larger space
                return levels * 20;
            }
            else if (buildingSubType == "commercial" || buildingSubType == "retail")
            {
                // Commercial: varies, use moderate estimate
                return levels * 30;
            }
            else if (buildingSubType == "farm")
            {
                // Farm: small capacity
                return 5;
            }

            // Default work capacity
            return levels * 30;
        }

        private static int CalculateRetailCapacity(Tags? tags)
        {
            if (tags == null) return 30; // Default retail capacity

            int levels = GetBuildingLevels(tags);

            string? shop = tags.Shop?.ToLower();
            string? buildingSubType = tags.Building?.ToLower();

            if (shop == "mall" || shop == "department_store" || buildingSubType == "retail")
            {
                // Large retail: 50-100 people per floor
                return levels * 75;
            }
            else if (shop == "supermarket" || shop == "convenience")
            {
                // Supermarket: moderate capacity
                return levels * 40;
            }
            else if (shop != null)
            {
                // Small shop
                return levels * 20;
            }

            // Default retail
            return levels * 30;
        }

        private static int CalculateLeisureCapacity(Tags? tags)
        {
            if (tags == null) return 50; // Default leisure capacity

            int levels = GetBuildingLevels(tags);

            string? amenity = tags.Amenity?.ToLower();
            string? tourism = tags.Tourism?.ToLower();

            if (amenity == "cinema" || amenity == "theatre")
            {
                // Cinema/Theatre: large seating capacity
                return levels * 150;
            }
            else if (amenity == "restaurant" || amenity == "cafe")
            {
                // Restaurant/Cafe: table capacity
                return levels * 40;
            }
            else if (amenity == "pub" || amenity == "bar")
            {
                // Pub/Bar: standing + seating
                return levels * 60;
            }
            else if (amenity == "library")
            {
                // Library: reading spaces
                return levels * 50;
            }
            else if (amenity == "sports_centre" || amenity == "swimming_pool")
            {
                // Sports facilities: larger capacity
                return levels * 100;
            }
            else if (amenity == "park")
            {
                // Park: outdoor, large capacity
                return 200;
            }
            else if (tourism == "hotel")
            {
                // Hotel: estimate rooms
                int roomsPerFloor = 20;
                return levels * roomsPerFloor * 2; // 2 people per room average
            }

            // Default leisure
            return levels * 50;
        }

        private static int GetBuildingLevels(Tags? tags)
        {
            if (tags == null) return 1;

            if (!string.IsNullOrEmpty(tags.BuildingLevels))
            {
                // Try to parse building:levels
                if (int.TryParse(tags.BuildingLevels, out int levels) && levels > 0)
                {
                    return Math.Min(levels, 100); // Cap at 100 floors for safety
                }
            }

            // Default to 1 floor if not specified
            return 1;
        }

        private static int GetDefaultCapacity(string buildingType)
        {
            return buildingType switch
            {
                "Home" => 4,
                "School" => 200,
                "Medical" => 50,
                "Work" => 50,
                "Retail" => 30,
                "Leisure" => 50,
                _ => 20
            };
        }
    }
}
