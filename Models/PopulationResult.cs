namespace PopulationCalculator.Models
{
    public class PopulationResult
    {
        public required string StateName { get; set; }
        public int Year { get; set; }
        public double Population { get; set; }
        public required string Description { get; set; }

        public override string ToString()
        {
            return $"{StateName},{Year},{Population:N3},{Description}";
        }
    }
}