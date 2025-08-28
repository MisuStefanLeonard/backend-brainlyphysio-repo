using System.Text;
using backend.Models.DTO;
using backend.Models.DTO.Admin.Auth;
using backend.Models.DTO.AnnounceDto;
using backend.Models.DTO.LocationDto;
using backend.Models.DTO.QualityDto;
using backend.Services.AccountService;
using backend.Services.AnnounceService;
using backend.Services.CloudFlareCdnServices;
using backend.Services.LocationService;
using backend.Services.QualityService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace backend.Controllers;

[EnableCors("AllowFrontEnd")]
[Route("api/admin")]
[ApiController]
public class AdminController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly ILocationService _locationService;
    private readonly IQualityService  _qualityService;
    private readonly ICloudFlareCdnService _cloudflareCdnService;
    private readonly IAnnounceService _announceService;

    public AdminController(IAccountService accountService, ILocationService locationService, IQualityService qualityService, ICloudFlareCdnService cloudflareCdnService, IAnnounceService announceService)
    {
        _accountService = accountService;
        _locationService = locationService;
        _qualityService = qualityService;
        _cloudflareCdnService = cloudflareCdnService;
        _announceService = announceService;
    }
    
    /*
     * ACCOUNTS API ROUTES - START
     */
    
    [HttpGet("login/{redirect}")]
    [Authorize]
    public async Task<IActionResult> GetAdminLogIn([FromRoute] string redirect)
    {
        if (redirect != "redirect")
        {
            return Ok("Authorized");
        }
        else
        {
            var isAdminLoggedIn = Request.Cookies.TryGetValue("adminLoggedIn", out var adminLoggedInString) && adminLoggedInString == "1";
            var getHeaderSecret = await _cloudflareCdnService.GetCdnSecret("admin_header");
            var decodedString = "";
            if (Request.Cookies.TryGetValue("ASP_NET_ADMIN_SESSION", out var encodedString))
            {
                decodedString = Encoding.UTF8.GetString(Convert.FromBase64String(encodedString));
            }

            if (isAdminLoggedIn && decodedString != "" && decodedString == getHeaderSecret)
            {
                return NoContent(); // redirect to dashboard
            }
            
            return Ok("Please authorize yourself");
            
        }
    }

    [HttpPost("login")]
    [Authorize]
    public async Task<IActionResult> LogInToDashBoard([FromBody] AdminLogInDto adminLogInDto)
    {
        var result = await _accountService.AdminLogIn(adminLogInDto.Key);

        if (result != 1) return Unauthorized("Bad credentials");
        var cookieOptions = new CookieOptions()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(30)
        };
        
        var getSecretHeader = await _cloudflareCdnService.GetCdnSecret("admin_header");
        var bytesSecret = Encoding.UTF8.GetBytes(getSecretHeader);
        Response.Cookies.Append("ASP_NET_ADMIN_SESSION", Convert.ToBase64String(bytesSecret) , cookieOptions);
        Response.Cookies.Append("adminLoggedIn" , "1" , cookieOptions);
        return Ok("Succesfully logged in into admin dashboard");

    }
    
    [HttpGet]
    [Authorize]
    [Route("accounts")]
    public async Task<IActionResult> GetAccounts()
    {
        var getAccounts = await _accountService.GetAllAccounts();
        return Ok(getAccounts);
    }

    [HttpPost]
    [Authorize]
    [Route("account/create")]
    public async Task<IActionResult> CreateAccount([FromForm] IFormFile? image,[FromForm] string accountString)
    {
        var account = JsonConvert.DeserializeObject<AccountCrud>(accountString);
        var createAccountResponse = await _accountService.CreateAccount(account!,image);

        return createAccountResponse.Message switch
        {
            "Account created successfully" => Ok(createAccountResponse),
            "E-mail already used" => BadRequest("E-mail already used"),
            _ => StatusCode(500, "General error occured")
        };
    }
    
    [HttpPut]
    [Authorize]
    [Route("account/modify")]
    public async Task<IActionResult> ModifyAccount([FromForm] IFormFile? image,[FromForm]string accountString)
    {
        var account = JsonConvert.DeserializeObject<AccountCrud>(accountString);
        var modifyAccountResponse = await _accountService.ModifyAccount(account!,image);
        
        return modifyAccountResponse switch
        {
            1 => Ok("Successfully modified account"),
            -1 => BadRequest("Account does not exist"),
            _ => StatusCode(500, "General error occured")
        };
    }
    
    [HttpDelete]
    [Authorize]
    [Route("account/delete/{idAccount:exists:int}")]
    public async Task<IActionResult> DeleteAccount([FromRoute]int idAccount)
    {
        var deleteAccountResponse = await _accountService.DeleteAccount(idAccount);
        
        return deleteAccountResponse switch
        {
            1 => Ok("Successfully deleted account"),
            -1 => BadRequest("Account that you want to delete does not exist"),
            _ => StatusCode(500, "General error occured")
        };
    }
    
    /*
     * ACCOUNTS API ROUTES - END
     */
    
    // --------------------------------------------------------
    // --------------------------------------------------------
    // --------------------------------------------------------
    
    /*
     * LOCATIONS API ROUTES - START
     */
    
    
    [HttpGet]
    [Authorize]
    [Route("locations")]
    public async Task<IActionResult> GetAvailableLocations()
    {
       var locationResponse = await _locationService.GetAllLocations();
       return Ok(locationResponse);
    }
    
    [HttpPost]
    [Authorize]
    [Route("location/create")]
    public async Task<IActionResult> CreateLocation([FromBody] LocationCrud location)
    {
        var createLocationResponse = await _locationService.CreateLocation(location);

        return createLocationResponse.Message switch
        {
            "Successfully created location from admin interface" => Ok(createLocationResponse),
            "Location already exists" => BadRequest("Location already exists"),
            _ => StatusCode(500, "General error occured")
        };
    }
    
    [HttpPut]
    [Authorize]
    [Route("location/modify")]
    public async Task<IActionResult> ModifyLocation([FromBody] LocationCrud location)
    {
        var modifyLocationResponse = await _locationService.ModifyLocation(location);
        
        return modifyLocationResponse switch
        {
            1 => Ok("Successfully modified location"),
            -1 => BadRequest("Location does not exist"),
            _ => StatusCode(500, "General error occured")
        };
    }
    
    
    [HttpDelete]
    [Authorize]
    [Route("location/delete/{idLocation:int}")]
    public async Task<IActionResult> DeleteLocation([FromRoute]int idLocation)
    {
        var deleteLocationResponse = await _locationService.DeleteLocation(idLocation);
        
        return deleteLocationResponse switch
        {
            1 => Ok("Successfully deleted location"),
            -1 => BadRequest("Location that you want to delete does not exist"),
            _ => StatusCode(500, "General error occured")
        };
    }
    
    /*
     * LOCATIONS API ROUTES - END
     */
    
    // --------------------------------------------------------
    // --------------------------------------------------------
    // --------------------------------------------------------
    
    
    /*
     * QUALITIES API ROUTES - START 
     */
    
    
    [HttpGet]
    [Authorize]
    [Route("qualities")]
    public async Task<IActionResult> GetAvailableQualities()
    {
        var qualitiesResponse = await _qualityService.GetAllQualities();
        return Ok(qualitiesResponse);
    }
    
    [HttpPost]
    [Authorize]
    [Route("quality/create")]
    public async Task<IActionResult> CreateQuality([FromBody] QualityCrud quality)
    {
        var createQualityResponse = await  _qualityService.CreateQuality(quality);

        return createQualityResponse.Message switch
        {
            "Successfully created quality" => Ok(createQualityResponse),
            "Quality already exists" => BadRequest("Quality already exists"),
            _ => StatusCode(500, "General error occured")
        };
    }
    
    [HttpPut]
    [Authorize]
    [Route("quality/modify")]
    public async Task<IActionResult> ModifyQuality([FromBody] QualityCrud quality)
    {
        var modifyQualityResponse = await _qualityService.ModifyQuality(quality);
        
        return modifyQualityResponse switch
        {
            1 => Ok("Successfully modified quality"),
            -1 => BadRequest("Quality does not exist"),
            _ => StatusCode(500, "General error occured")
        };
    }
    
    
    [HttpDelete]
    [Authorize]
    [Route("quality/delete/{idQuality:int}")]
    public async Task<IActionResult> DeleteQuality([FromRoute]int idQuality)
    {
        var deleteQualityResponse = await _qualityService.DeleteQuality(idQuality);
        
        return deleteQualityResponse switch
        {
            1 => Ok("Successfully deleted quality"),
            -1 => BadRequest("Quality that you want to delete does not exist"),
            _ => StatusCode(500, "General error occured")
        };
    }
    
    /*
     * QUALITIES API ROUTES - END
     */
    
    // --------------------------------------------------------
    // --------------------------------------------------------
    // --------------------------------------------------------
    
    /*
     * ANNOUNCE API ROUTES - START
     */

    [HttpGet]
    [Authorize]
    [Route("announces")]
    public async Task<IActionResult> GetAllAnnounces()
    {
        var result = await _announceService.GetAllAnnounces();
        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    [Route("announce/create")]
    public async Task<IActionResult> CreateAnnounce([FromForm] string announce, [FromForm] IFormFile image)
    {
        var announceDto = JsonConvert.DeserializeObject<AnnounceCrud>(announce);
        var result = await _announceService.CreateAnnounce(announceDto!,image);

        return result switch
        {
            1 => Ok("Successfully created announce."),
            -1 => BadRequest("An announce with this title already exists."),
            _ => StatusCode(500, "An error occurred while creating the announce.")
        };
    }

    [HttpPut]
    [Authorize]
    [Route("announce/modify")]
    public async Task<IActionResult> ModifyAnnounce([FromForm] string announce, [FromForm] IFormFile? image)
    {
        var announceDto = JsonConvert.DeserializeObject<AnnounceCrud>(announce);
        var result = await _announceService.ModifyAnnounce(announceDto!,image);
        
        return result switch
        {
            1 => Ok("Successfully modified announce."),
            -1 => NotFound("The announce you are trying to modify does not exist."),
            _ => StatusCode(500, "An error occurred while modifying the announce.")
        };
    }
    
    [HttpDelete]
    [Authorize]
    [Route("announce/delete/{idAnnounce:int}")]
    public async Task<IActionResult> DeleteAnnounce([FromRoute] int idAnnounce)
    {
        var result = await _announceService.DeleteAnnounce(idAnnounce);

        return result switch
        {
            1 => Ok("Successfully deleted announce."),
            -1 => NotFound("The announce you are trying to delete does not exist."),
            _ => StatusCode(500, "An error occurred while deleting the announce.")
        };
    }

    [HttpGet]
    [Authorize]
    [Route("announce/{idAnnounce:int}")]
    public async Task<IActionResult> GetAnnounceById([FromRoute] int idAnnounce)
    {
        var result = await _announceService.GetAnnounceById(idAnnounce);

        if (result is null)
        {
            return NotFound("The announce you are trying to get does not exist.");
        }

        return Ok(result);
    }
    
    /*
     * ANNOUNCE API ROUTES - END
     */

}