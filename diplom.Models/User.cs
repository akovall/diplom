using diplom.Models.enums;
using System.ComponentModel.DataAnnotations;

namespace diplom.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; } 
        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;
        [MaxLength(100)]
        public string JobTitle { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Employee;
        public bool IsActive { get; set; } = true;
        public List<TaskItem> Tasks { get; set; } = new();
        public List<TimeEntry> TimeEntries { get; set; } = new();
    }
}
