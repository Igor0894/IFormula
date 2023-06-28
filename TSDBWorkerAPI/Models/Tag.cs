using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSDBWorkerAPI.Models
{
    public class Tag
#nullable disable
    {
        public string name { get; set; }
        public Datapoint[] dataPoints { get; set; }
    }
}
