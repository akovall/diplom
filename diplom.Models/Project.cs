
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace diplom.Models
{
    public class Project
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsArchived { get; set; } = false;

        [JsonIgnore]
        public List<TaskItem> Tasks { get; set; } = new();
    }
}