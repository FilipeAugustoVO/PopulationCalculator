namespace PopulationCalculator.Models;

public static class PopulationFormulas
{
    public static readonly Dictionary<string, double> PreWarFormulas = new()
    {
        { "0.25% formula", 0.25 / 100 },
        { "0.5% formula", 0.5 / 100 },
        { "0.75% formula", 0.75 / 100 },
        { "1% formula", 1.0 / 100 },
        { "1.10% formula", 1.1 / 100 },
        { "1.25% formula", 1.25 / 100 },
        { "1.5% formula", 1.5 / 100 },
        { "1.75% formula", 1.75 / 100 },
        { "2% formula", 2.0 / 100 }
    };

    public static void AddHistoricalRates(
        double rate4660, double rate4670, double rate5060, 
        double rate5565, double rate5070, double rate6070, 
        double rate7080)
    {
        PreWarFormulas.Add("46-60 formula", rate4660 / 100);
        PreWarFormulas.Add("46-70 formula", rate4670 / 100);
        PreWarFormulas.Add("50-60 formula", rate5060 / 100);
        PreWarFormulas.Add("55-65 formula", rate5565 / 100);
        PreWarFormulas.Add("50-70 formula", rate5070 / 100);
        PreWarFormulas.Add("60-70 formula", rate6070 / 100);
        PreWarFormulas.Add("70-80s formula", rate7080 / 100);
    }

    public static readonly Dictionary<string, double> PostApocFormulas = new()
    {
        { "0.25% formula", 0.0025 },
        { "0.5% formula", 0.005 },
        { "0.75% formula", 0.0075 },
        { "1% formula", 0.01 },
        { "1.10% formula", 0.011 },
        { "1.25% formula", 0.0125 },
        { "1.5% formula", 0.015 },
        { "1.75% formula", 0.0175 },
        { "2% formula", 0.02 },
        { "-12% formula", -0.12 },
        { "-25% formula", -0.25 },
        { "-50% formula", -0.50 },
        { "-75% formula", -0.75 }
    };
}