using backend.Services.AccountService;
using backend.Services.AnnounceService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[EnableCors("AllowFrontEnd")]
[Route("api/user")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly IAnnounceService _announceService;

    public UserController(IAccountService accountService, IAnnounceService announceService)
    {
        _accountService = accountService;
        _announceService = announceService;
    }
    
    [HttpGet]
    [AllowAnonymous]
    [Route("members")]
    public async Task<IActionResult> GetMembers()
    {
        return Ok(await _accountService.GetAllMembers());
    }

    [HttpGet]
    [AllowAnonymous]
    [Route("announces")]
    public async Task<IActionResult> GetAnnounces()
    {
        return Ok(await _announceService.GetAllAnnounces());
    }
    
    [HttpGet]
    [AllowAnonymous]
    [Route("announce/{idAnnounce:int}")]
    public async Task<IActionResult> GetAnnounceById([FromRoute] int idAnnounce)
    {
        return Ok(await _announceService.GetAnnounceById(idAnnounce));
    }
}