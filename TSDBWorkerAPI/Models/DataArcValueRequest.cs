using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSDBWorkerAPI.Models
{
    public class DataArcValueRequest
#nullable disable
    {
        public request Request { get; set; }
        public DataArcValueRequest(Dictionary<string, DateTime[]> tagsWithTimestamps, RetrievalTypeConstants boundaryType)
        {
            Request = new request();
            Request.Data = MyDictionaryToJson(tagsWithTimestamps);
            Request.Mode = (int)boundaryType;
        }
        public DataArcValueRequest(string tagName, DateTime timestamp, RetrievalTypeConstants boundaryType)
        {
            Request = new request();
            Request.Data = $"{{\"{tagName}\":[\"{timestamp.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ")}\"]}}";
            Request.Mode = (int)boundaryType;
        }
        string MyDictionaryToJson(Dictionary<string, DateTime[]> dict)
        {
            var entries = dict.Select(d =>
                string.Format("\"{0}\": [{1}]", d.Key, string.Join(",", d.Value)));
            return "{" + string.Join(",", entries) + "}";
        }

        public class request
        {
            public string Data { get; set; }
            public int Mode { get; set; }
        }
        string MyDictionaryToJson(Dictionary<int, List<int>> dict)
        {
            var entries = dict.Select(d =>
                string.Format("\"{0}\": [{1}]", d.Key, string.Join(",", d.Value)));
            return "{" + string.Join(",", entries) + "}";
        }
    }
    public enum RetrievalTypeConstants
    {
        /// <summary>
        /// The rt at or before
        /// </summary>
        rtAtOrBefore = 1,
        /// <summary>
        /// The rt at or after
        /// </summary>
        rtAtOrAfter = 2,
        /// <summary>
        /// The rt interpolated
        /// </summary>
        rtInterpolated = 3,
        /// <summary>
        /// The rt compressed
        /// </summary>
        rtCompressed = 4,
        /// <summary>
        /// The rt automatic
        /// </summary>
        rtAuto = 5,
        /// <summary>
        /// The rt before
        /// </summary>
        rtBefore = 6,
        /// <summary>
        /// The rt after
        /// </summary>
        rtAfter = 7,
    }
}
