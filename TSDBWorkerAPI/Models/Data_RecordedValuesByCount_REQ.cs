namespace TSDBWorkerAPI.Models
{
    public class Data_RecordedValuesByCount_REQ
#nullable disable
    {
        public Data_RecordedValuesByCount_REQ(IList<string> tagNames, DateTime startTime, int requestedCount, Direction direction, BoundaryType boundaryType, FilterType filterType = FilterType.All)
        {
            Request = new request();
            Request.TagNames = tagNames;
            Request.StartTime = startTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            Request.RequestedCount = requestedCount;
            Request.Direction = direction.ToString();
            Request.BoundaryType = boundaryType.ToString();
            Request.FilterType = filterType.ToString();
        }
        public request Request { get; set; }
        public class request
        {
            public IList<string> TagNames { get; set; }
            public string StartTime { get; set; }
            public int RequestedCount { get; set; }
            public string Direction { get; set; }
            public string BoundaryType { get; set; }
            public string FilterType { get; set; }
        }
    }
    public class Data_RecordedValuesByCount_RESPONSE_Long
    {
        public Tag[] tags { get; set; }
        public Errors errors { get; set; }
        public class Errors
        {
        }
    }
    public class Data_RecordedValuesByCount_RESPONSE_String
    {
        public Tag[] tags { get; set; }
        public Errors errors { get; set; }
        public class Errors
        {
        }
    }
    public class Data_GetByList_REQ
    {
        public Searchparams[] SearchParamses { get; set; }
        public Data_GetByList_REQ(string Tag, DateTime StartDate, DateTime EndDate)
        {
            SearchParamses = new Searchparams[1] { new Searchparams() };
            SearchParamses[0].Tag = Tag;
            SearchParamses[0].StartDateUTC = StartDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            SearchParamses[0].EndDateUTC = EndDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }

        public class Searchparams
        {
            public string Tag { get; set; }
            public string StartDateUTC { get; set; }
            public string EndDateUTC { get; set; }
        }
    }
    public class Data_GetByList_RESPONSE
    {
        public Tag[] tags { get; set; }
        public Errors errors { get; set; }
        public class Errors
        {
            public string[] EndDateUTC { get; set; }
            public string[] StartDateUTC { get; set; }
        }
    }

    public class Error
    {
        public Errors errors { get; set; }
        public string type { get; set; }
        public string title { get; set; }
        public int status { get; set; }
        public string traceId { get; set; }
        public class Errors
        {
            public string[] EndDateUTC { get; set; }
            public string[] StartDateUTC { get; set; }
        }
    }
    public enum Direction
    {
        Forward,
        Reverse
    }
    public enum BoundaryType
    {
        Inside,
        Outside,
        Interp,
        Auto
    }
    public enum FilterType
    {
        All,
        Good,
        Bad
    }
}
