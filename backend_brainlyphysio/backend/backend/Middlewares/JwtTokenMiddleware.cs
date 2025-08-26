using System.Security.Claims;
using backend.Services.CloudFlareCdnServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace backend.Middlewares;

public class JwtTokenMiddleware : IMiddleware
{
    private readonly ILogger<JwtTokenMiddleware> _logger;
    private readonly ICloudFlareCdnService _cloudFlareCdnService;

    public JwtTokenMiddleware(ILogger<JwtTokenMiddleware> logger, ICloudFlareCdnService cloudFlareCdnService)
    {
        _logger = logger;
        _cloudFlareCdnService = cloudFlareCdnService;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            var endpoint = context.GetEndpoint();
            _logger.LogInformation("EXECUTING JWT TOKEN MIDDLEWARE");
            var path = context.Request.Path;
            _logger.LogInformation($"PATH IN JWT TOKEN MIDDLEWARE -> {path}");

            if (path.StartsWithSegments("/openapi/v1.json"))
            {
                _logger.LogInformation("JWT TOKEN MIDDLEWARE BYPASSED");
                await next(context);
                return;
            }

            

            // Check if endpoint allows anonymous access
            if (endpoint?.Metadata.GetMetadata<AllowAnonymousAttribute>() != null)
            {
                await next(context);
                return;
            }
        
            if (context.Request.Cookies.TryGetValue("userLoggedIn", out var isLoggedIn) &&
                context.Request.Cookies.TryGetValue("session_tok" , out var refreshToken))
            {
                ClaimsPrincipal? principalUser;
                if (context.Request.Cookies.TryGetValue("JWTToken", out var jwt))
                {
                    var response = await _cloudFlareCdnService.TokenValidation(jwt, refreshToken);
                    principalUser = response.Item1;
                }
                else
                {
                    var response = await _cloudFlareCdnService.TokenValidation("refresh", refreshToken);
                    if (response.Item1 == null)
                    {
                        _logger.LogWarning("Refresh token expired");
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsync("Refresh token expired");
                        return;
                    }
                    principalUser = response.Item1;
                    var cookieOptions = new CookieOptions()
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTime.UtcNow.AddMinutes(15)
                        // Expires = DateTime.UtcNow.AddSeconds(30) // testing

                    };
                    var generateNewJwt = await _cloudFlareCdnService.GenerateJwtAccesToken(response.Item2!);
                    context.Response.Cookies.Append("JWTToken" , generateNewJwt , cookieOptions);
                }

                if (principalUser == null)
                {
                    _logger.LogWarning("Refresh token expired");
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Refresh token expired");
                    return;
                }

                if (isLoggedIn == "1")
                {
                    context.User = principalUser;
                    _logger.LogInformation("User successfully set.");
                   
                }
                else
                {
                    _logger.LogWarning("Invalid JWT Token");
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Invalid JWT Token");
                    return;
                }
            }
            else
            {
                _logger.LogWarning("Missing refresh token / Jwt / userLoggedIn");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Missing refresh token ");
                return;
            }

            await next(context);
        }
        catch (AuthenticationFailureException ex)
        {
            _logger.LogError(ex, "Authentication failure: Access was denied by the resource owner or by the remote server.");
            context.Response.Redirect("http://localhost:3000/home");
        }
    }
}
