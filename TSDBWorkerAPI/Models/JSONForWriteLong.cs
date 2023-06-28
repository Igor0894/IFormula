namespace TSDBWorkerAPI.Models
{
    public class JSONForWriteLong
#nullable disable
    {
        public List<iTag> Tags = new List<iTag>();
        public JSONForWriteLong(JSONForRead J)
        {
            foreach (rTag tag in J.Tags)
            {
                iTag t = new iTag();
                t.Name = tag.Name;
                t.DataPoints = new List<iDataPoint>();
                foreach (rDataPoint dp in tag.DataPoints)
                {
                    iDataPoint d = new iDataPoint();
                    d.ValueLong = dp.ValueLong.ToString();
                    d.TimeStamp = dp.TimeStamp;
                    d.QualityMark = new iQualityMark(dp.QualityMark.Value);
                    t.DataPoints.Add(d);
                }
                Tags.Add(t);
            }

        }
        public JSONForWriteLong(string TagName, List<iDataPoint> dataPoints)
        {
            Tags = new List<iTag>() { new iTag() { Name = TagName, DataPoints = dataPoints } };
        }
        public JSONForWriteLong()
        {

        }
    }

    public class iDataPoint
    {
        public string ValueLong { get; set; }
        public DateTime TimeStamp { get; set; }
        public iQualityMark QualityMark { get; set; }
    }

    public class iQualityMark
    {
        public string Value { get; set; }
        public iQualityMark(string value)
        {
            Value = value;
        }
    }



    public class iTag
    {
        public string Name { get; set; }
        public List<iDataPoint> DataPoints { get; set; }
    }
}
