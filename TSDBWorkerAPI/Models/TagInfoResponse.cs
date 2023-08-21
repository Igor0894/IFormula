using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSDBWorkerAPI.Models
{
    public class TagInfoResponse
    {
        public string name { get; set; }
        public DateTimeOffset createDate { get; set; }
        public DateTimeOffset updateDate { get; set; }
        public GroupPermission[] groupPermissions { get; set; }
    }
    internal class GroupPermission
    {
        public int groupId { get; set; }
        public string[] permissions { get; set; }
    }
}
