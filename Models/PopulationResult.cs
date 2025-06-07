namespace PopulationCalculator.Models
{
    public class PopulationResult
    {
        public string StateName { get; set; } = "";
        public string Formula { get; set; } = "";
        public double Population { get; set; }
        public bool IsValid { get; set; }
        public string ValidationStatus { get; set; } = "";
        
        // KEOFF and FYOC stats
        public double KeoffRate { get; set; }
        public double SurvivingPopulation { get; set; }
        public double FyocLossRate { get; set; }
        public double PostFyocPopulation { get; set; }
        
        // Post-war projections
        public List<PostWarProjection> PostWarProjections { get; set; } = new();
        
        // MP Statistics
        public double MpStatsAverage { get; set; }
        public double MpStatsMedian { get; set; }
        
        // Ghoul stats
        public GhoulStats GhoulStats { get; set; } = new();
    }

    public class PostWarProjection
    {
        public string Formula { get; set; } = "";
        public double Population { get; set; }
        public double BaseMp { get; set; }
        public int OnMapMp { get; set; }
        public double OffMapMp { get; set; }
        public double DailyOffMap { get; set; }
    }

    public class GhoulStats
    {
        public double InitialPopulation { get; set; }
        public double FinalPopulation { get; set; }
        public double OffMapMp { get; set; }
        public double DailyOffMap { get; set; }
    }
}