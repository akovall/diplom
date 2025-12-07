using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace diplom.models
{
    internal class WorkLog
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public int TaskItemId { get; set; }
        public DateTime GeneratedAt { get; set; }
        public double TotalHours { get; set; }
        public string Description { get; set; }

    }
}
