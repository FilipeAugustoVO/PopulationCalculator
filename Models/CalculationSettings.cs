namespace PopulationCalculator.Models
{
    public class CalculationSettings
    {
        // Historical Growth Rates
        public double Rate4660 { get; set; }
        public double Rate4670 { get; set; }
        public double Rate5060 { get; set; }
        public double Rate5565 { get; set; }
        public double Rate5070 { get; set; }
        public double Rate6070 { get; set; }
        public double Rate7080 { get; set; }

        // Population Settings
        public double InitialPopulation1945 { get; set; }
        public double ModernPopulation { get; set; }
        public double OtlMaxPopulation { get; set; }

        // Growth Period Settings
        public bool UseCustomPreWarPeriods { get; set; }
        public bool UseCustomPostWarPeriods { get; set; }
        public bool UseCustomGhoulPeriods { get; set; }

        // Custom Periods
        public List<(int years, double rateModifier)> CustomPreWarPeriods { get; set; }
        public List<(int years, double rateModifier, int popChange)> CustomPostWarPeriods { get; set; }
        public List<(int years, double rateModifier, int popChange)> CustomGhoulPeriods { get; set; }

        // Output Settings
        public string CustomOutputFile { get; set; }
        public string DefaultOutputFile { get; set; }
    }
}