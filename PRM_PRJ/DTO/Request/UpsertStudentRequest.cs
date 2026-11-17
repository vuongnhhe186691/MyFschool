using System.ComponentModel.DataAnnotations;

namespace PRM_PRJ.DTO.Request
{
    public class UpsertStudentRequest
    {
        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string DateOfBirth { get; set; } = string.Empty;

        public string? Gender { get; set; }

        [Required]
        public int ClassId { get; set; }

        [Required, MaxLength(20)]
        public string StudentCode { get; set; } = string.Empty;

        public Guid? UserId { get; set; }

        public List<Guid> ParentIds { get; set; } = new();

        public string? AvatarUrl { get; set; }
        public bool IsActive { get; set; } = true;
    }
}