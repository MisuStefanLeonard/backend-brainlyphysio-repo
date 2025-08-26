using backend.Models.DTO.UserDto.Auth;
using backend.Models.User;
using backend.Services.AccountService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[EnableCors("AllowFrontEnd")]
[ApiController]
[Route("api/auth")]
public class AuthenticationController : ControllerBase
{
    
    private readonly IAccountService _userService;

    public AuthenticationController(IAccountService userService)
    {
        _userService = userService;
    }

    [HttpPost("getUserByEmail")]
    [AllowAnonymous]
    public async Task<ActionResult<Account>> GetAccountByEmailAsync([FromBody]EmailRequestDto emailRequestDto)
    {
        var account = await _userService.GetAccountByEmailAsync(emailRequestDto.Email);
        if (account == null)
        {
            return NotFound();
        }

        return Ok();
    }

    [HttpPost("inregistrare")]
    [AllowAnonymous]
    public async Task<ActionResult<Account>> AddAccount(Account account)
    {
        await _userService.AddAccountAsync(account);
        return Ok();
    }
    
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<Account>> LoginAccount([FromBody]LoginDto loginDto)
    {
       
        // if (Request.Cookies.TryGetValue("session_tok", out _))
        // {
        //     return StatusCode(515,"You are already logged in!");
        // }
        
        var user = await _userService.LoginAccountAsync(loginDto);
        var isAdmin = user is { RoleProp: "Admin" };
        if (user == null)
        {
            return NotFound("Unautohrized access!");
        }
  

        var cookieOptions = new CookieOptions()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddMinutes(15)
            // Expires = DateTime.UtcNow.AddSeconds(30) // testing

        };
        

        var isLoggedInCookieOptions = new CookieOptions()
        {
            Secure = true, // if not working , remove
            Expires = !isAdmin ? DateTime.UtcNow.AddDays(7) : DateTime.UtcNow.AddDays(30),
            // Expires = DateTime.UtcNow.AddMinutes(1), // testing
            SameSite = SameSiteMode.Strict
        };

        var refreshTokenOptions = new CookieOptions()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = !isAdmin ? DateTime.UtcNow.AddDays(7) : DateTime.UtcNow.AddDays(30)
            // Expires = DateTime.UtcNow.AddMinutes(1) // testing

        };
        
        Response.Cookies.Append("JWTToken", loginDto.TokenProp, cookieOptions);
        Response.Cookies.Append("session_tok" , loginDto.RefreshTokenProp! , refreshTokenOptions);
        Response.Cookies.Append("userLoggedIn" , "1" , isLoggedInCookieOptions);
        Response.Cookies.Append("admin" , isAdmin ? "1" : "0" , isLoggedInCookieOptions);
      

        return Ok();
    }
    
    [HttpGet("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
       
        // var googleGeneratedCookie = Request.Cookies[".AspNetCore.Cookies"];
       
        var isLoggedInCookieOptions = new CookieOptions()
        {
            Expires = DateTime.UtcNow.AddYears(-2),
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
        };

        var userIdClaim = HttpContext.User.Claims.FirstOrDefault(p => p.Type == "user_id");
        var userId = 0;
        if (userIdClaim != null)
        {
            userId = int.Parse(userIdClaim.Value);
        }
        
        if (Request.Cookies.TryGetValue("session_tok", out var refreshToken))
        {
          
            var response = await _userService.LogoutAsync(refreshToken);
            if (response == -1)
            {
                Console.WriteLine("Error when deleting the refresh token from db");
            }
        }
        else
        {
            Console.WriteLine("---------");
            Console.WriteLine($"Session token not found. Trying to get the user id from context");
            Console.WriteLine("---------");

            if (userId != 0)
            {
                var response = await _userService.LogoutAsync(userId);
                if (response == -1)
                {
                    Console.WriteLine("Error when deleting the refresh token from db by userId");
                }
              
            }
            else
            {
                Console.WriteLine("---------");
                Console.WriteLine($"User id not found in claim...");
                Console.WriteLine("---------");
            }
        }
       

        Response.Cookies.Append("JWTToken", "", isLoggedInCookieOptions);
        Response.Cookies.Append("session_tok", "", isLoggedInCookieOptions);
        Response.Cookies.Append("userLoggedIn" , "" , isLoggedInCookieOptions);
        Response.Cookies.Append("admin" , "" , isLoggedInCookieOptions);
        Response.Cookies.Append("adminLoggedIn" , "" , isLoggedInCookieOptions);
        Response.Cookies.Append("ASP_NET_ADMIN_SESSION" , "" , isLoggedInCookieOptions);
        

        // if (googleGeneratedCookie != null)
        // {
        //     Response.Cookies.Append(".AspNetCore.Cookies", "", isLoggedInCookieOptions);
        // }
        
        await Task.Delay(10);

        return Ok("Succesfully logout of the account");
       
    }
}