namespace TSDBWorkerAPI.Models
{
    public class SummaryRequest
#nullable disable
    {
        public string TagName { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public int IntervalsCount { get; set; }
        public string SummaryType { get; set; }
        public string CalculationBasis { get; set; }
        public SummaryRequest(string tagName, DateTime startDateLocal, DateTime endDateLocal, int intervalsCount, SummaryType summaryType, CalculationBasis calculationBasis)
        {
            TagName = tagName;
            StartDate = startDateLocal.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            EndDate = endDateLocal.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            IntervalsCount = intervalsCount;
            SummaryType = summaryType.ToString();
            CalculationBasis = calculationBasis.ToString();
        }
    }
    public class SummaryTotalResponse
    {
        public DateTime startIntervalUTC { get; set; }
        public DateTime endIntervalUTC { get; set; }
        public Summarydatа summaryDatа { get; set; }
    }

    public class Summarydatа
    {
        public float total { get; set; }
    }

    public enum SummaryType
    {
        Total,
        Minimum,
        Maximum,
        Average,
        Count,
        Range,
        StandardDeviation,
        PercentGood,
        All
    }
    public enum CalculationBasis
    {
        TimeWeighted,
        EventWeighted,
        TimeWeightedContinuous,
        TimeWeightedDiscrete,
        EventWeightedExcludeMostRecentEvent,
        EventWeightedExcludeEarliestEvent,
        EventWeightedIncludeBothEnds
    }
}
