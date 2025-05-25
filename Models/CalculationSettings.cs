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
        public required List<Period> CustomPreWarPeriods { get; set; }
        public required List<Period> CustomPostWarPeriods { get; set; }
        public required List<Period> CustomGhoulPeriods { get; set; }

        // Output Settings
        public required string CustomOutputFile { get; set; }
        public required string DefaultOutputFile { get; set; }
    }
}