using System.ComponentModel.DataAnnotations;

namespace PRM_PRJ.DTO.Request
{
    public class CreateApplicationRequest
    {
        [Required(ErrorMessage = "Tiêu đề không được để trống")]
        [MaxLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nội dung không được để trống")]
        [MaxLength(4000, ErrorMessage = "Nội dung tối đa 4000 ký tự")]
        public string Content { get; set; } = string.Empty;
    }
}