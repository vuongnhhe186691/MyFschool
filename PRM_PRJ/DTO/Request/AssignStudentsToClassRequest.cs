using System.ComponentModel.DataAnnotations;

namespace PRM_PRJ.DTO.Request
{
    public class AssignStudentsToClassRequest
    {
        [Required]
        public int TargetClassId { get; set; }

        [Required]
        public List<Guid> StudentIds { get; set; } = new();
    }
}