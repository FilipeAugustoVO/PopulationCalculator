namespace PopulationCalculator.Models
{
    public class StateSettings
    {
        // Basic properties
        public required string StateName { get; set; }
        public bool IsOffMap { get; set; }
        public double InitialPopulation1945 { get; set; }
        public double ModernPopulation { get; set; }
        public double OtlMaxPopulation { get; set; }

        // Historical growth rates
        public double Rate4660 { get; set; }
        public double Rate4670 { get; set; }
        public double Rate5060 { get; set; }
        public double Rate5565 { get; set; }
        public double Rate5070 { get; set; }
        public double Rate6070 { get; set; }
        public double Rate7080 { get; set; }

        // Period settings
        public bool UseCustomPreWarPeriods { get; set; }
        public bool UseCustomPostWarPeriods { get; set; }
        public bool UseCustomGhoulPeriods { get; set; }
        public List<(int years, double rateModifier)> PreWarPeriods { get; set; } = new();
        public List<(int years, double rateModifier, double popChange)> PostWarPeriods { get; set; } = new();
        public List<(int years, double rateModifier, double popChange)> GhoulPeriods { get; set; } = new();
    }
}