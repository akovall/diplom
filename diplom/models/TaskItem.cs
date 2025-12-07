using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace diplom.models
{
    internal class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
        public int AssignedToEmployeeId { get; set; }
        public int ProjectId { get; set; }
        public string Status { get; set; }
        public List<TimeEntry> TimeEntries { get; set; }
    }
}
