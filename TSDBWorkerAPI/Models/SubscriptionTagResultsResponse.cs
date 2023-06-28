using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSDBWorkerAPI.Models
{
    internal class SubscriptionTagResultsResponse
#nullable disable
    {
        public Subscriptiontagresult[] subscriptionTagResults { get; set; }
        public class Subscriptiontagresult
        {
            public Tag tag { get; set; }
        }
    }
}
