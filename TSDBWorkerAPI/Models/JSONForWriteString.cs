namespace TSDBWorkerAPI.Models
{
    public class JSONForWriteString
#nullable disable
    {
        public List<sTag> Tags = new List<sTag>();
        public JSONForWriteString(JSONForRead J)
        {
            foreach (rTag tag in J.Tags)
            {
                sTag t = new sTag();
                t.Name = tag.Name;
                t.DataPoints = new List<sDataPoint>();
                foreach (rDataPoint dp in tag.DataPoints)
                {
                    sDataPoint d = new sDataPoint();
                    d.ValueString = dp.ValueString;
                    d.TimeStamp = dp.TimeStamp;
                    d.QualityMark = new sQualityMark(dp.QualityMark.Value);
                    t.DataPoints.Add(d);
                }
                Tags.Add(t);
            }

        }
        public JSONForWriteString(string TagName, List<sDataPoint> dataPoints)
        {
            Tags = new List<sTag>() { new sTag() { Name = TagName, DataPoints = dataPoints } };
        }
    }

    public class sDataPoint
    {
        public string ValueString { get; set; }
        public DateTime TimeStamp { get; set; }
        public sQualityMark QualityMark { get; set; }
    }

    public class sQualityMark
    {
        public string Value { get; set; }
        public sQualityMark(string value)
        {
            Value = value;
        }
    }



    public class sTag
    {
        public string Name { get; set; }
        public List<sDataPoint> DataPoints { get; set; }
    }
}
