using backend.Models.DTO;
using backend.Models.DTO.AnnounceDto;
using backend.Models.DTO.UserDto;
using backend.Models.DTO.UserDto.Auth;
using backend.Models.DTO.UserDto.Members;
using backend.Models.User;

namespace backend.Services.AccountService;

public interface IAccountService
{
    Task<int> AdminLogIn(string key);
    public Task<LoginDto?> LoginAccountAsync(LoginDto loginDto); // 
    public Task AddAccountAsync(Account newAccount); //
    public Task<int> LogoutAsync(string refreshToken); // 
    public Task<int> LogoutAsync(int userId);
    Task<List<AccountCrud>> GetAllAccounts();
    Task<List<Member>> GetAllMembers(); 
    Task<CreateAccountResponseDto> CreateAccount(AccountCrud cont,IFormFile? image);
    Task<int> ModifyAccount(AccountCrud cont,IFormFile? image);
    Task<int> DeleteAccount(int idAccount);
    public Task<Account?> GetAccountByEmailAsync(string email);
    
    
   
    
}