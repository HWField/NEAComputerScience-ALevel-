namespace MySimulationAPI.Models
{
    public abstract class Building
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Capacity { get; set; }
        public int RemainingCapacity { get; set; }
        // All buildings belong to a MapArea
        public int MapAreaId { get; set; }
        public MapArea? MapArea { get; set; }
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

    public class LeisureBuilding : Building
    {

    }

}