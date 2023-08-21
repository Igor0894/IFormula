using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSDBWorkerAPI.Models
{
    internal class SnapshotResponse
    {
        public string tagName { get; set; }
        public int tagId { get; set; }
        public Datapoint dataPoint { get; set; }
    }
}
