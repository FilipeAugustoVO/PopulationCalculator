namespace PopulationCalculator.Models
{
    public class PopulationResult
    {
        public string StateName { get; set; } = "";
        public int Year { get; set; }
        public double Population { get; set; }
        public bool IsOffMap { get; set; }  // Whether the state exists on the map

        public override string ToString()
        {
            // Only show population for states that have a physical presence on the map
            if (!IsOffMap)
            {
                return $"{StateName} ({Year}): {Population:N0}";
            }
            
            // For off-map states like ghouls, indicate they are off-map
            return $"{StateName} ({Year}): Off-map";
        }
    }
}