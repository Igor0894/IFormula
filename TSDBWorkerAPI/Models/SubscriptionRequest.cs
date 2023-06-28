using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TSDBWorkerAPI.Models
{
    internal class SubscriptionRequest
    {
        public SubscriptionRequest(Tag[] tags, TimeSpan timeToLive, int maxCountPointsInBuffer)
        {
            this.tags = tags;
            this.timeToLive = timeToLive;
            this.maxCountPointsInBuffer = maxCountPointsInBuffer;
        }
        public Tag[] tags { get; set; }
        public TimeSpan timeToLive { get; set; }
        public int maxCountPointsInBuffer { get; set; }

        public class Tag
#nullable disable
        {
            public Tag(string tag)
            {
                this.tag = tag;
            }
            public Tag(string tag, DateTime ArchiveDataStartTimestamp)
            {
                this.tag = tag;
                this.ArchiveDataStartTimestamp = ArchiveDataStartTimestamp.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss");
            }
            public string tag { get; set; }
            public string ArchiveDataStartTimestamp { get; set; }
        }
    }
}
