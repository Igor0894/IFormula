using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSDBWorkerAPI.Models
{
    public class DataArcValuesResponse
#nullable disable
    {
        public Tag[] tags { get; set; }
        public Errors errors { get; set; }
        public class Errors
        {
        }
    }
}
