using System.ComponentModel.DataAnnotations;

namespace PRM_PRJ.Models
{
    public class RefreshToken
    {
        [Key]
        public int Id { get; set; }

        public string Token { get; set; }

        public DateTime ExpiryDate { get; set; }

        public bool IsRevoked { get; set; }

        public Guid UserId { get; set; }

        public User User { get; set; }
    }
}