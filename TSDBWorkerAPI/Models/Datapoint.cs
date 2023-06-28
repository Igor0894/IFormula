using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSDBWorkerAPI.Models
{
    public class Datapoint
#nullable disable
    {
        public float valueDouble { get; set; }
        public float valueLong { get; set; }
        public float valueFloat { get; set; }
        public string valueString { get; set; }
        public DateTime timeStamp { get; set; }
        public string annotation { get; set; }
        public Qualitymark qualityMark { get; set; }
    }
}
