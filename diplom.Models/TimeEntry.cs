using diplom.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.Json.Serialization;

namespace diplom.Models
{
    public class TimeEntry
    {
        [Key]
        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        [MaxLength(200)]
        public string Comment { get; set; } = string.Empty;

        public bool IsManual { get; set; } = false;
        
        public int TaskId { get; set; }
        [ForeignKey("TaskId")]
        [JsonIgnore]
        public TaskItem? Task { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        [JsonIgnore]
        public User? User { get; set; }

        [NotMapped]
        public TimeSpan Duration
        {
            get
            {
                if (EndTime.HasValue)
                    return EndTime.Value - StartTime;

                return DateTime.UtcNow - StartTime; 
            }
        }


    }
}
