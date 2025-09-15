using System.Collections.Generic;

namespace MySimulationAPI.Models
{
    public abstract class Mitigation
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public abstract void Apply(Simulation sim);
    }

    public class MaskWearing : Mitigation
    {
        public double Efficacy { get; set; }
        public override void Apply(Simulation sim)
        {
            // reduce infection probability
        }
    }

    public class SocialDistancing : Mitigation
    {
        public override void Apply(Simulation sim)
        {
            // modify agent routines or reduce contacts
        }
    }
}