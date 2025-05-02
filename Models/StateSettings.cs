namespace PopulationCalculator.Models
{
    public class StateSettings
    {
        // Basic properties
        public string StateName { get; set; }
        public string Region { get; set; }
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
        public List<(int years, double rateModifier)> PreWarPeriods { get; set; }
        public List<(int years, double rateModifier, int popChange)> PostWarPeriods { get; set; }
        public List<(int years, double rateModifier, int popChange)> GhoulPeriods { get; set; }

        public StateSettings()
        {
            PreWarPeriods = new List<(int, double)>();
            PostWarPeriods = new List<(int, double, int)>();
            GhoulPeriods = new List<(int, double, int)>();
        }
    }
}