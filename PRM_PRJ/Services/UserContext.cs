using PRM_PRJ.Services.inf;
using System.Security.Claims;

namespace PRM_PRJ.Services
{
    public class UserContext : IUserContext
    {
        private readonly IHttpContextAccessor _context;

        public UserContext(IHttpContextAccessor context)
        {
            _context = context;
        }

        private ClaimsPrincipal User
        {
            get
            {
                var user = _context.HttpContext?.User;

                if (user == null || !user.Identity.IsAuthenticated)
                    throw new UnauthorizedAccessException("User is not authenticated");

                return user;
            }
        }

        public Guid GetUserId()
        {
            var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(id))
                throw new UnauthorizedAccessException("UserId claim missing");

            if (!Guid.TryParse(id, out var userId))
                throw new UnauthorizedAccessException("Invalid UserId");

            return userId;
        }

        public string GetUsername()
        {
            return User.FindFirst(ClaimTypes.Name)?.Value
                ?? throw new UnauthorizedAccessException("Username claim missing");
        }

        public string GetRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value
                ?? throw new UnauthorizedAccessException("Role claim missing");
        }
    }
}