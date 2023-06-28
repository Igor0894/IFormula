namespace TSDBWorkerAPI.Models
{
    public class JSONForRead
#nullable disable
    {
        public List<rTag> Tags { get; set; }
        public rErrors Errors { get; set; }
    }
    public class rDataPoint
    {
        public string ValueString { get; set; }
        public object ValueLong { get; set; }
        public string ValueDouble { get; set; }
        public DateTime TimeStamp { get; set; }
        public object Annotation { get; set; }
        public rQualityMark QualityMark { get; set; }
        public object AddedDateTime { get; set; }
        public object DigitalSetId { get; set; }
        public object ValueFloat { get; set; }
        public object ValueDateTime { get; set; }
    }

    public class rErrors
    {
    }

    public class rQualityMark
    {
        public string Value { get; set; }
        public int StateNumber { get; set; }
        public bool IsBadQuality { get; set; }
    }



    public class rTag
    {
        public string Name { get; set; }
        public List<rDataPoint> DataPoints { get; set; }
    }

}
