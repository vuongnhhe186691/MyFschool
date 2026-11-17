using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRM_PRJ.DTO;
using PRM_PRJ.DTO.Request;
using PRM_PRJ.DTO.Response;
using PRM_PRJ.Models;
using PRM_PRJ.Services.Email;
using PRM_PRJ.Services.inf;
using System.Security.Claims;

namespace PRM_PRJ.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly AppDbContext _context;
        private readonly IForgotPasswordOtpService _forgotPasswordOtpService;
        private readonly IEmailService _emailService;

        public AuthController(
            IAuthService authService,
            AppDbContext context,
            IForgotPasswordOtpService forgotPasswordOtpService,
            IEmailService emailService)
        {
            _authService = authService;
            _context = context;
            _forgotPasswordOtpService = forgotPasswordOtpService;
            _emailService = emailService;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.Login(request);
            return StatusCode(result.Code, new
            {
                code = result.Code,
                status = result.Status,
                message = result.Status,
                token = result.Data?.AccessToken,
                refreshToken = result.Data?.RefreshToken,
                data = result.Data
            });
        }

        [AllowAnonymous]
        [HttpPost("verify-login-otp")]
        public async Task<IActionResult> VerifyLoginOtp([FromBody] VerifyLoginOtpRequest request)
        {
            var result = await _authService.VerifyLoginOtp(request);
            return StatusCode(result.Code, new
            {
                code = result.Code,
                status = result.Status,
                message = result.Status,
                token = result.Data?.AccessToken,
                refreshToken = result.Data?.RefreshToken,
                data = result.Data
            });
        }

        [AllowAnonymous]
        [HttpPost("forgot-password/request-otp")]
        public async Task<IActionResult> RequestOtp([FromBody] ForgotPasswordOtpRequest request)
        {
            var phoneNumber = request.PhoneNumber?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return BadRequest(new { message = "Số điện thoại không được để trống" });

            var user = await _context.Users.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy tài khoản" });

            if (string.IsNullOrWhiteSpace(user.Email))
                return BadRequest(new { message = "Tài khoản chưa có email để nhận OTP" });

            var otp = _forgotPasswordOtpService.GenerateAndStore(phoneNumber);
            await _emailService.SendOtpAsync(user.Email, otp);

            return Ok(new SimpleMessageResponse
            {
                Message = "Đã gửi OTP về email của tài khoản"
            });
        }

        [AllowAnonymous]
        [HttpPost("forgot-password/reset")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var phoneNumber = request.PhoneNumber?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return BadRequest(new { message = "Số điện thoại không được để trống" });

            var user = await _context.Users.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy tài khoản" });

            if (!_forgotPasswordOtpService.Verify(phoneNumber, request.OtpCode))
                return BadRequest(new { message = "OTP không hợp lệ hoặc đã hết hạn" });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();
            _forgotPasswordOtpService.Remove(phoneNumber);

            return Ok(new { message = "Đặt lại mật khẩu thành công" });
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userIdClaim = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { message = "Token không hợp lệ" });

            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng" });

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                return BadRequest(new { message = "Mật khẩu hiện tại không đúng" });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đổi mật khẩu thành công" });
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<ApiResponse<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
            => await _authService.RefreshToken(request);

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
        {
            await _authService.Logout(request.RefreshToken);
            return Ok(new { message = "Đăng xuất thành công" });
        }
    }
}