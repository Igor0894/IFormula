namespace TSDBWorkerAPI.Models
{
    public class TSDBValue
#nullable disable
    {
        public TSDBValue(
                      string tagName,
                      DateTime timeUTC,
                      object value,
                      string annotation,
                      Quality quality,
                      string digitalSetValue = null)
        {
            TimestampUTC = timeUTC;
            Annotation = annotation;
            Value = value;
            TagName = tagName;
            Quality = quality;
            DigitalSetValue = digitalSetValue;
        }
        public string TagName;
        public Quality Quality;
        public DateTime TimestampUTC;
        public DateTime Timestamp => TimestampUTC.ToLocalTime();
        public object Value;
        public string Annotation;
        public bool IsGood() => this.Quality == Quality.good || this.Quality == Quality.goodLocalOverride;
        public string DigitalSetValue;
        public override string ToString() => string.Format("{0} {1} {2} {3} {4}", (object)this.Timestamp.ToLocalTime(), this.Value, (object)this.DigitalSetValue, (object)this.Quality, (object)this.Annotation);
    }
}
