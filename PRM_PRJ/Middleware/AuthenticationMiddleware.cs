using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using PRM_PRJ.Services;

namespace PRM_PRJ.Middleware
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, JwtService jwtService)
        {
            var endpoint = context.GetEndpoint();

            var allowAnonymous = endpoint?.Metadata?.GetMetadata<AllowAnonymousAttribute>() != null;
            var requireAuth = endpoint?.Metadata?.GetMetadata<AuthorizeAttribute>() != null;

            if (allowAnonymous || !requireAuth)
            {
                await _next(context);
                return;
            }

            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    message = "Missing Token"
                }));
                return;
            }

            var token = authHeader.Substring(7);

            try
            {
                var principal = jwtService.ValidateToken(token);
                context.User = principal;
            }
            catch
            {
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    message = "Invalid Token"
                }));
                return;
            }

            await _next(context);
        }
    }
}