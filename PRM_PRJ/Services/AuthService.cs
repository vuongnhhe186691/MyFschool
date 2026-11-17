using Microsoft.EntityFrameworkCore;
using PRM_PRJ.DTO;
using PRM_PRJ.DTO.Request;
using PRM_PRJ.DTO.Response;
using PRM_PRJ.Models;
using PRM_PRJ.Services.Email;
using PRM_PRJ.Services.inf;

namespace PRM_PRJ.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwtService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly ILoginOtpService _loginOtpService;
        private readonly IEmailService _emailService;

        public AuthService(
            AppDbContext context,
            JwtService jwtService,
            IRefreshTokenService refreshTokenService,
            ILoginOtpService loginOtpService,
            IEmailService emailService)
        {
            _context = context;
            _jwtService = jwtService;
            _refreshTokenService = refreshTokenService;
            _loginOtpService = loginOtpService;
            _emailService = emailService;
        }

        public async Task<ApiResponse<AuthResponse>> Login(LoginRequest request)
        {
            var normalizedPhone = request.PhoneNumber?.Trim() ?? string.Empty;

            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(x => x.PhoneNumber == normalizedPhone);

            if (user == null)
            {
                return new ApiResponse<AuthResponse>
                {
                    Code = 401,
                    Status = "Không tìm thấy tài khoản",
                    Data = null
                };
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return new ApiResponse<AuthResponse>
                {
                    Code = 401,
                    Status = "Sai mật khẩu",
                    Data = null
                };
            }

            if (!user.IsActive)
            {
                return new ApiResponse<AuthResponse>
                {
                    Code = 403,
                    Status = "Tài khoản đã bị khóa",
                    Data = null
                };
            }

            var roles = user.UserRoles.Select(x => x.Role.Name).ToList();

            if (user.IsTwoFactorEnabled)
            {
                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    return new ApiResponse<AuthResponse>
                    {
                        Code = 400,
                        Status = "Tài khoản chưa có email để nhận OTP",
                        Data = null
                    };
                }

                _loginOtpService.StoreVerifiedPassword(normalizedPhone, request.Password);
                var otp = _loginOtpService.GenerateAndStoreOtp(normalizedPhone);
                await _emailService.SendLoginOtpAsync(user.Email, otp);

                return new ApiResponse<AuthResponse>
                {
                    Code = 200,
                    Status = "Đã gửi OTP đăng nhập về email",
                    Data = new AuthResponse
                    {
                        Username = user.Username,
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber ?? string.Empty,
                        Roles = roles,
                        RequiresTwoFactor = true
                    }
                };
            }

            var accessToken = _jwtService.GenerateAccessToken(user, roles);
            var refreshToken = await _refreshTokenService.GenerateAsync(user);

            return new ApiResponse<AuthResponse>
            {
                Code = 200,
                Status = "Đăng nhập thành công",
                Data = new AuthResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    Username = user.Username,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber ?? string.Empty,
                    Roles = roles,
                    RequiresTwoFactor = false
                }
            };
        }

        public async Task<ApiResponse<AuthResponse>> VerifyLoginOtp(VerifyLoginOtpRequest request)
        {
            var normalizedPhone = request.PhoneNumber?.Trim() ?? string.Empty;

            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(x => x.PhoneNumber == normalizedPhone);

            if (user == null)
            {
                return new ApiResponse<AuthResponse>
                {
                    Code = 401,
                    Status = "Không tìm thấy tài khoản",
                    Data = null
                };
            }

            if (!_loginOtpService.HasValidVerifiedPassword(normalizedPhone, out var verifiedPassword))
            {
                return new ApiResponse<AuthResponse>
                {
                    Code = 400,
                    Status = "Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại",
                    Data = null
                };
            }

            if (!BCrypt.Net.BCrypt.Verify(verifiedPassword, user.PasswordHash))
            {
                _loginOtpService.Clear(normalizedPhone);
                return new ApiResponse<AuthResponse>
                {
                    Code = 401,
                    Status = "Thông tin đăng nhập không còn hợp lệ",
                    Data = null
                };
            }

            if (!_loginOtpService.VerifyOtp(normalizedPhone, request.OtpCode))
            {
                return new ApiResponse<AuthResponse>
                {
                    Code = 400,
                    Status = "OTP không hợp lệ hoặc đã hết hạn",
                    Data = null
                };
            }

            var roles = user.UserRoles.Select(x => x.Role.Name).ToList();
            var accessToken = _jwtService.GenerateAccessToken(user, roles);
            var refreshToken = await _refreshTokenService.GenerateAsync(user);

            _loginOtpService.Clear(normalizedPhone);

            return new ApiResponse<AuthResponse>
            {
                Code = 200,
                Status = "Đăng nhập thành công",
                Data = new AuthResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    Username = user.Username,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber ?? string.Empty,
                    Roles = roles,
                    RequiresTwoFactor = false
                }
            };
        }

        public async Task<ApiResponse<AuthResponse>> RefreshToken(RefreshTokenRequest request)
        {
            var tokenEntity = await _refreshTokenService.ValidateAsync(request.RefreshToken);
            if (tokenEntity == null)
            {
                return new ApiResponse<AuthResponse>
                {
                    Code = 401,
                    Status = "Refresh token không hợp lệ",
                    Data = null
                };
            }

            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(x => x.Id == tokenEntity.UserId);

            if (user == null)
            {
                return new ApiResponse<AuthResponse>
                {
                    Code = 404,
                    Status = "Không tìm thấy user",
                    Data = null
                };
            }

            var roles = user.UserRoles.Select(x => x.Role.Name).ToList();
            var accessToken = _jwtService.GenerateAccessToken(user, roles);
            var newRefreshToken = await _refreshTokenService.RotateAsync(request.RefreshToken);

            if (string.IsNullOrWhiteSpace(newRefreshToken))
            {
                return new ApiResponse<AuthResponse>
                {
                    Code = 401,
                    Status = "Không thể làm mới refresh token",
                    Data = null
                };
            }

            return new ApiResponse<AuthResponse>
            {
                Code = 200,
                Status = "Success",
                Data = new AuthResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = newRefreshToken,
                    Username = user.Username,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber ?? string.Empty,
                    Roles = roles
                }
            };
        }

        public async Task Logout(string refreshToken)
        {
            await _refreshTokenService.RevokeAsync(refreshToken);
        }
    }
}