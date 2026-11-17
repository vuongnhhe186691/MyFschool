using PRM_PRJ.DTO;
using PRM_PRJ.DTO.Response;
using PRM_PRJ.DTO.Request;

namespace PRM_PRJ.Services.inf
{
    public interface IAuthService
    {
        Task<ApiResponse<AuthResponse>> Login(LoginRequest request);
        Task<ApiResponse<AuthResponse>> VerifyLoginOtp(VerifyLoginOtpRequest request);
        Task<ApiResponse<AuthResponse>> RefreshToken(RefreshTokenRequest request);
        Task Logout(string refreshToken);
    }
}