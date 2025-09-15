namespace MySimulationAPI.Models
{
    public abstract class Building
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int? Capacity { get; set; }

        // All buildings belong to a MapArea
        public MapArea MapArea { get; set; } = null!;
    }

    public class ResidentialBuilding : Building
    {

    }

    public class OfficeBuilding : Building
    {
        public bool HasCafeteria { get; set; }
    }

    public class EducationBuilding : Building
    {
        public int MaxStudents { get; set; }
    }

    public class MedicalBuilding : Building
    {
        public int NumBeds { get; set; }
        public bool HasICU { get; set; }
    }

    public class RetailBuilding : Building
    {
        public bool HasFood { get; set; }
    }
}