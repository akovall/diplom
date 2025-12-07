using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Remoting;
using System.Text;
using System.Threading.Tasks;

namespace diplom.models
{
    internal class Employee
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public int RoleId { get; set; }
        public string Position { get; set; }
        public DateTime HireDate { get; set; }
        public List<TimeEntry> TimeEntries { get; set; }

    }
}
