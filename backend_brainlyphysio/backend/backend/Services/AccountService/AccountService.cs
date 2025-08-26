using System.Security.Authentication;
using backend.Models.DTO;
using backend.Models.DTO.AnnounceDto;
using backend.Models.DTO.LocationDto;
using backend.Models.DTO.QualityDto;
using backend.Models.DTO.UserDto;
using backend.Models.DTO.UserDto.Auth;
using backend.Models.DTO.UserDto.Members;
using backend.Models.User;
using backend.Models.Utils;
using backend.Services.CloudFlareCdnServices;
using backend.Services.Helpers;
using backend.UOW;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace backend.Services.AccountService;

public class AccountService : IAccountService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AccountService> _logger;
    private readonly ICloudFlareCdnService _cloudflareCdnService;

    public AccountService(IUnitOfWork unitOfWork, ILogger<AccountService> logger,
        ICloudFlareCdnService cloudflareCdnService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cloudflareCdnService = cloudflareCdnService;
    }

    public async Task<int> AdminLogIn(string key)
    {
        var secret = await _cloudflareCdnService.GetCdnSecret("admin_login");
        
        if (secret == key)
        {
            _logger.LogInformation("key was equal");
            return 1;
        }
        _logger.LogInformation("key was not equal");
        return 0;
    }

     public async Task<LoginDto?> LoginAccountAsync(LoginDto loginDto )
    {
        IDbContextTransaction? addRefreshTokenTransaction = null;
        try
        {
            addRefreshTokenTransaction = await _unitOfWork.BeginTransactionAsync();
            var conturiRepository = _unitOfWork.Repository<Account>();
            var plainTextPassword = loginDto.ParolaProp;
            var currentUser = await conturiRepository
                .FindQueryable(user => user.Email == loginDto.NumeProp)
                .FirstOrDefaultAsync();

            RememberUser newRefreshToken;
            var rememberUserRepository = _unitOfWork.Repository<RememberUser>();
            var expiringTime = currentUser is not { Role: "Admin" } ? DateTime.UtcNow.AddDays(7) : DateTime.UtcNow.AddDays(30);
            
            
            // if (currentUser is not null && plainTextPassword == null)
            // {
            //     _logger.LogInformation("GOOGLE AUTH");
            //     var tokenForGoogleUser = await _tokenService.GenerateJwtAccesToken(currentUser);
            //     var refreshTokenForGoogleUser = _tokenService.RefreshToken() ;
            //     loginDto.TokenProp = tokenForGoogleUser;
            //     loginDto.RoleProp = currentUser.Rol;
            //     loginDto.RefreshTokenProp = refreshTokenForGoogleUser;
            //     newRefreshToken = new RememberUser
            //     {
            //         IdCont = currentUser.IdCont,
            //         SessionToken = refreshTokenForGoogleUser,
            //         IssuedAt = DateTime.UtcNow,
            //         ExpiresAt = expiringTime
            //     };
            //     await rememberUserRepository.AddAsync(newRefreshToken);
            //     await _unitOfWork.CommitTransactionAsync(addRefreshTokenTransaction);
            //     return loginDto;
            // }
            
           
            if (currentUser is null || !UserHelpers.VerifyCryptedPassword(plainTextPassword!, currentUser.Password!))
            {
                throw new UnauthorizedAccessException("Account does not exist / Wrong password or username");
            }


            if (currentUser == null)
            {
                throw new InvalidCredentialException("Username or password are wrong");
            }

            if (currentUser.IsVerified == false)
            {
                throw new UnauthorizedAccessException("Account not verified");
            }
            
            var findRefreshTokenInDb = await _unitOfWork.Repository<RememberUser>()
                .FindQueryable(token => token.IdAccount == currentUser.IdAccount)
                .FirstOrDefaultAsync();

            if (findRefreshTokenInDb != null)
            {
                var newExpirationTime = currentUser is not {Role: "Admin"} ? DateTime.UtcNow.AddDays(7)
                        : DateTime.UtcNow.AddDays(30);
                findRefreshTokenInDb.IssuedAt = DateTime.UtcNow;
                findRefreshTokenInDb.ExpiresAt = newExpirationTime;
                await rememberUserRepository.UpdateAsync(findRefreshTokenInDb);
                await _unitOfWork.CommitTransactionAsync(addRefreshTokenTransaction);
                var tokenForUser = await _cloudflareCdnService.GenerateJwtAccesToken(currentUser);
                loginDto.TokenProp = tokenForUser;
                loginDto.RoleProp = currentUser.Role;
                loginDto.RefreshTokenProp = findRefreshTokenInDb.SessionToken;
            }
            else
            {
                var tokenForCurrentUser = await _cloudflareCdnService.GenerateJwtAccesToken(currentUser);
                var refreshTokenForCurrentUser = _cloudflareCdnService.RefreshToken() ;
                loginDto.TokenProp = tokenForCurrentUser;
                loginDto.RoleProp = currentUser.Role;
                loginDto.RefreshTokenProp = refreshTokenForCurrentUser;

           
                newRefreshToken = new RememberUser
                {
                    IdAccount = currentUser.IdAccount,
                    SessionToken = refreshTokenForCurrentUser,
                    IssuedAt = DateTime.UtcNow,
                    ExpiresAt = expiringTime
                };

                await rememberUserRepository.AddAsync(newRefreshToken);
                await _unitOfWork.CommitTransactionAsync(addRefreshTokenTransaction);
            }

            return loginDto;
        }
        catch (InvalidCredentialException)
        {
            if (addRefreshTokenTransaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(addRefreshTokenTransaction);
            }
            _logger.LogError("Invalid credentials");
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            if (addRefreshTokenTransaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(addRefreshTokenTransaction);
            }
            _logger.LogError("Account has not been activated ");
            return null;
        }
        catch (Exception e)
        {
            if (addRefreshTokenTransaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(addRefreshTokenTransaction);
            }
            _logger.LogError("General error occured");
            _logger.LogError(e.StackTrace);
            _logger.LogError(e.Message);

            return null;
        }



    }

    public async Task AddAccountAsync(Account newAccount)
    {
        IDbContextTransaction? transaction = null;

        try
        {
            transaction = await _unitOfWork.BeginTransactionAsync();
            var repository = _unitOfWork.Repository<Account>();
            // var getDockerEnv = Environment.GetEnvironmentVariable("DOCKER");
            // var siteUrl = getDockerEnv != "true" ? "http://localhost:3000" : "https://www.texxshop.ro";

            var token = UserHelpers.Token(30,30, newAccount.Email!);
            var hashedPassword = UserHelpers.CryptPassword(newAccount.Password!);

            var accountToCreate = new Account
            {
                Name = newAccount.Name?.Trim(),
                Prename = newAccount.Prename?.Trim(),
                Email = newAccount.Email?.Trim(),
                Description = newAccount.Description?.Trim(),
                PhoneNumber = newAccount.PhoneNumber?.Trim(),
                Password = hashedPassword,
                ImagePath = null,
                ImageHash = null,
                Role = "Membru",
                ConfirmationLinkHour = DateTime.UtcNow,
                ActivationCode = token,
                IsVerified = true,
            };

            await repository.AddAsync(accountToCreate);

            await _unitOfWork.CommitTransactionAsync(transaction);
            
            
            // var url = await _bucketAcces.GenerateUrl("LogoTexx.png" , null);
            // var insertLogo = url != null
            //     ? $"<mj-section>\n" +
            //       $" <mj-column>\n" +
            //       $"   <mj-image width=\"100px\" src=\"{url}\" alt=\"Company Logo\"/>\n" +
            //       $" </mj-column>\n" +
            //       $"</mj-section>"
            //     : "";
            //
            // var mjmlTemplate = $"<mjml>\n" +
            //                    $"  <mj-body>\n  " +
            //                    $"{insertLogo}" +
            //                    $"  <mj-section>\n   " +
            //                    $"   <mj-column>\n      " +
            //                    $"  <mj-text font-size=\"18px\" color=\"#F45E43\" font-family=\"helvetica\" align=\"center\">Confirmare cont / Account confirmation</mj-text>\n       " +
            //                    $" <mj-spacer></mj-spacer>\n " +
            //                    $"     </mj-column>\n" +
            //                    $"      <mj-column background-color=\"#a8a8a8\" border-radius=\"20px\" padding=\"20px\" width=\"100%\">\n " +
            //                    $"        <mj-text font-size=\"22px\" color=\"#F45E43\">\n " +
            //                    $"         RO\n" +
            //                    $"        </mj-text>\n " +
            //                    $"        <mj-text font-size=\"18px\" color=\"#333333\">\n " +
            //                    $"         Acest mail expira intr-o ora!\n" +
            //                    $"        </mj-text>\n " +
            //                    $"       <mj-text font-size=\"18px\" color=\"#333333\">\n  " +
            //                    $"        <strong>Confirmarea de cont nou.</strong>\n" +
            //                    $"        </mj-text>\n        <mj-text font-size=\"18px\" color=\"blue\">\n" +
            //                    $"          In cazul in care nu ati fost dvs. sau nu recunoasteti acest mail, NU dati click pe nimic! Contacti-ne in cel mai scurt timp la <strong >texx@yahoo.com</strong>\n" +
            //                    $"        </mj-text>\n" +
            //                    $"       \t<mj-text font-size=\"18px\" color=\"#333333\">\n" +
            //                    $"          Noul cod de reactivare. Da click pe acest link pentru a-ti activa contul\n" +
            //                    $"         </mj-text>\n" +
            //                    $"          <mj-button color=\"white\" background-color=\"black\">\n" +
            //                    $"           <a href=\"{siteUrl}/user/confirmare/{token}\">CLICK</a>\n" +
            //                    $"        </mj-button>\n" +
            //                    $"         <mj-text font-size=\"22px\" color=\"#F45E43\">\n" +
            //                    $"          EN\n " +
            //                    $"       </mj-text>\n   " +
            //                    $"        <mj-text font-size=\"18px\" color=\"#333333\">\n " +
            //                    $"        This mail expires in 1 hour!\n" +
            //                    $"        </mj-text>\n " +
            //                    $"     <mj-text font-size=\"18px\" color=\"#333333\">\n " +
            //                    $"         <strong>New confirmation request for account</strong>\n  " +
            //                    $"      </mj-text>\n  " +
            //                    $"      <mj-text font-size=\"18px\" color=\"blue\">\n " +
            //                    $"         If you did not request this confirmation , do not click ANYTHING! Contact us as fast as possible at <strong >texx@yahoo.com</strong>\n  " +
            //                    $"      </mj-text>\n " +
            //                    $"      \t<mj-text font-size=\"18px\" color=\"#333333\">\n " +
            //                    $"         New reactivation code. Click on the button\n " +
            //                    $"        </mj-text>\n          <mj-button color=\"white\" background-color=\"black\">\n " +
            //                    $"          <a href=\"{siteUrl}/en/user/confirmare/{token}\">CLICK</a>\n" +
            //                    $"        </mj-button>\n\n" +
            //                    $"      </mj-column>\n" +
            //                    $"    </mj-section>\n" +
            //                    $"  </mj-body>\n" +
            //                    $"</mjml>";

            // var convertToHtml = await _mjmlService.ConvertMjmlToHtml(mjmlTemplate);
            //
            //
            //
            // await _emailService.SendEmailAsync(accountToCreate.Email!,
            //     "Confirmare email / Email Confirmation",
            //    convertToHtml!
            // );
        }
        catch (DbUpdateException e)
        {
            if (transaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(transaction);
            }

            _logger.LogError("Someting happened when creating an account: Error Message:" + e.Message);
            _logger.LogError("Someting happened when creating an account: Stacktrace :" + e.StackTrace);
            throw;
        }

    }

    public async Task<int> LogoutAsync(string refreshToken)
    {
        IDbContextTransaction? deleteRefreshTokenTransaction = null;
        try
        {
            deleteRefreshTokenTransaction = await _unitOfWork.BeginTransactionAsync();
            var getRefreshTokenFromDb = await _unitOfWork.Repository<RememberUser>()
                .FindQueryable(token => token.SessionToken == refreshToken)
                .FirstOrDefaultAsync();

            if (getRefreshTokenFromDb == null)
            {
                return 1; // token already deleted | not present
            }

            await _unitOfWork.Repository<RememberUser>().DeleteAsync(getRefreshTokenFromDb);
            await _unitOfWork.CommitTransactionAsync(deleteRefreshTokenTransaction);
            return 1; // succes delete
        }
        catch (Exception e)
        {
            if (deleteRefreshTokenTransaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(deleteRefreshTokenTransaction);
                _logger.LogError(e.Message);
                _logger.LogError(e.StackTrace);

            }
            return -1;
        }
    }

    public async Task<int> LogoutAsync(int userId)
    {
        IDbContextTransaction? deleteRefreshTokenTransaction = null;

        try
        {
            deleteRefreshTokenTransaction = await _unitOfWork.BeginTransactionAsync();
            var getRefreshTokenFromDbById = await _unitOfWork.Repository<RememberUser>()
                .FindQueryable(token => token.IdAccount == userId)
                .FirstOrDefaultAsync();

            if (getRefreshTokenFromDbById == null)
            {
                _logger.LogWarning("Session token was already deleted from db.  No deletion required.");
                return 1; // token already deleted | not present
            }

            await _unitOfWork.Repository<RememberUser>().DeleteAsync(getRefreshTokenFromDbById);
            await _unitOfWork.CommitTransactionAsync(deleteRefreshTokenTransaction);
            _logger.LogInformation("Succefully deleted session from the db.");
            return 1; // succes delete
        }
        catch (Exception e)
        {
            if (deleteRefreshTokenTransaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(deleteRefreshTokenTransaction);
                _logger.LogError(e.Message);
                _logger.LogError(e.StackTrace);

            }
            return -1;
        }
    }

    public async Task<List<AccountCrud>> GetAllAccounts()
    {
        var getAccounts = await _unitOfWork.Repository<Account>()
            .GetSimpleQueryable()
            .Select(acc => new AccountCrud
            {
                IdAccount = acc.IdAccount,
                Name = acc.Name,
                Prename = acc.Prename,
                Email = acc.Email,
                Description = acc.Description,
                PhoneNumber = acc.PhoneNumber,
                ImagePath = acc.ImagePath,
                MemberLocations = acc.OperationPlaces.
                    Select(op => new LocationCrud
                    {
                        IdLocation = op.Location.IdLocation,
                        City = op.Location.City,
                        County = op.Location.County,
                    }).ToList(),
                MemberQualities = acc.Qualities
                    .Select(op => new QualityCrud
                    {
                        IdQuality = op.Quality.IdQuality,
                        QualityName = op.Quality.QualityName,
                    }).ToList()
            }).ToListAsync();
        
        foreach(var acc in getAccounts)
        {
            if (acc.ImagePath != null)
            {
                acc.PresignedUrl = await _cloudflareCdnService.GeneratePresignedUrl(acc.ImagePath);
            }
        }

        return getAccounts;
    }

    public async Task<List<Member>> GetAllMembers()
    {
        _logger.LogInformation("Fetching member data for displaying on main page...");
        var members =  await _unitOfWork.Repository<Account>()
            .GetSimpleQueryable()
            .Where(member => member.Role != "Admin")
            .OrderByDescending(member => member.Role == "Fondator")
            .Select(member => new Member
            {
                Name = member.Name,
                Prename = member.Prename,
                Email = member.Email,
                Description = member.Description,
                PhoneNumber = member.PhoneNumber,
                ImagePath = member.ImagePath,
                PresignedUrl = null,
                MemberLocations = member.OperationPlaces.
                    Select(op => new LocationCrud
                    {
                        City = op.Location.City,
                        County = op.Location.County,
                    }).ToList(),
                MemberQualities = member.Qualities
                    .Select(op => new QualityCrud
                    {
                        QualityName = op.Quality.QualityName,
                    }).ToList()
            }).AsSplitQuery()
            .ToListAsync();

        foreach (var member in members)
        {
            if (member.ImagePath != null)
            {
                member.PresignedUrl = await _cloudflareCdnService.GeneratePresignedUrl(member.ImagePath);
            }
        }
        
        return members;
    }

    public async Task<CreateAccountResponseDto> CreateAccount(AccountCrud cont,IFormFile? image)
    {
        IDbContextTransaction? createTransaction = null;
        try
        {
            _logger.LogInformation("Starting creating account from admin interface...");
            createTransaction = await _unitOfWork.BeginTransactionAsync();
            var accountRepository = _unitOfWork.Repository<Account>();
            var operationPlacesRepository = _unitOfWork.Repository<OperationPlace>();
            var memberQualityRepository = _unitOfWork.Repository<MemberQuality>();

            var alreadyExists = await accountRepository
                .FindQueryable(acc => acc.Email == cont.Email)
                .FirstOrDefaultAsync() != null;

            if (alreadyExists)
            {
                _logger.LogError("E-mail already used");
                return new CreateAccountResponseDto
                {
                    Id = 0,
                    Message = "E-mail already used",
                    PresignedUrl = null
                };
            }
            
            var presignedUrlToReturn = "";
            
            var newAccount = new Account
            {
                Name = cont.Name?.Trim(),
                Prename = cont.Prename?.Trim(),
                Email = cont.Email?.Trim(),
                Description = cont.Description?.Trim(),
                PhoneNumber = cont.PhoneNumber?.Trim(),
                Password = null,
                Role = "Membru",
                ImagePath = image?.FileName,
                ConfirmationLinkHour = DateTime.UtcNow,
                ActivationCode = UserHelpers.Token(30,70,cont.Email!),
                IsVerified = true,
            };
            
            var idToBeReturned = newAccount.IdAccount;

            if (image != null)
            {
                var imageHash = await UserHelpers.ComputeImageHash(image);
                _logger.LogInformation("Uploading image to Cloudflare CDN...");
                using var memoryStream = new MemoryStream();
                memoryStream.Position = 0;
                await image.CopyToAsync(memoryStream);
                var response = await _cloudflareCdnService.AddOrUpdateToCdnBucket(memoryStream, image.FileName);

                if (response == 1)
                {
                    _logger.LogInformation("Successfully uploaded image to Cloudflare CDN");
                    newAccount.ImageHash = imageHash;
                    presignedUrlToReturn = await _cloudflareCdnService.GeneratePresignedUrl(image.FileName);
                }
                else
                {
                    _logger.LogError("Error uploading image to Cloudflare CDN");
                }
                
              
            }
            
            await accountRepository.AddAsync(newAccount);
            await _unitOfWork.CommitAsync();
            
            _logger.LogInformation("Adding locations to the account...");
            var newMemberLocations =
                cont.MemberLocations
                    .Select(location =>
                        new OperationPlace 
                            { 
                                IdLocation = location.IdLocation, 
                                IdAccount = newAccount.IdAccount, 
                            }).ToList();
            
            await operationPlacesRepository.AddRangeAsync(newMemberLocations);
            _logger.LogInformation("Successfully added location to the account");
            
            _logger.LogInformation("Adding qualities to the account...");
            var newMemberQualities =
                cont.MemberQualities
                    .Select(quality =>
                        new MemberQuality
                        {
                            IdQuality = quality.IdQuality,
                            IdMember = newAccount.IdAccount,
                        }).ToList();
            
            await memberQualityRepository.AddRangeAsync(newMemberQualities);
            _logger.LogInformation("Successfully added qualities to the account");

            await _unitOfWork.CommitTransactionAsync(createTransaction);
            _logger.LogInformation("Successfully created account from admin interface along with locations and qualities.");
            return new CreateAccountResponseDto
            {
                Id = idToBeReturned,
                Message = "Account created successfully",
                PresignedUrl = presignedUrlToReturn
            };
        }
        catch (Exception e)
        {
            if (createTransaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(createTransaction);
            }

            _logger.LogError(e, "Error creating account");
            return new CreateAccountResponseDto
            {
                Id = 0,
                Message = "Error creating account",
                PresignedUrl = null
            };
        }
    }

    public async Task<int> ModifyAccount(AccountCrud cont,IFormFile? image)
    {
        IDbContextTransaction? updateTransaction = null;
        try
        {
            _logger.LogInformation("Starting modifying account data from admin interface...");
            updateTransaction = await  _unitOfWork.BeginTransactionAsync();
            var accountRepository = _unitOfWork.Repository<Account>();
            var operationPlacesRepository= _unitOfWork.Repository<OperationPlace>();
            var memberQualityRepository = _unitOfWork.Repository<MemberQuality>();
            
            var findAccount = await accountRepository
                .FindQueryable(account => account.IdAccount == cont.IdAccount)
                .FirstOrDefaultAsync();

            if (findAccount is null)
            {
                _logger.LogError($"Account with id {cont.IdAccount} not found");
                return -1;
            }
            
            // _mapper.Map(cont, findAccount);
            findAccount.Name = cont.Name?.Trim();
            findAccount.Prename = cont.Prename?.Trim();
            findAccount.Email = cont.Email?.Trim();
            findAccount.Description = cont.Description?.Trim();
            findAccount.PhoneNumber = cont.PhoneNumber?.Trim();
            

            if (image == null)
            {
                // Delete old image if exists
                if (findAccount.ImagePath != null)
                {
                    _logger.LogInformation("Deleting old image from Cloudflare CDN...");
                    var deleteResponse = await _cloudflareCdnService.DeleteFromCdnBucket(findAccount.ImagePath!);
                    if (deleteResponse == 1)
                        _logger.LogInformation("Successfully deleted image from Cloudflare CDN");
                    else
                        _logger.LogError("Error deleting image from Cloudflare CDN");

                    findAccount.ImagePath = null;
                    findAccount.ImageHash = null;
                }
                else
                {
                    _logger.LogInformation("No image to delete. Continuing...");
                }
            }
            else
            {
                var newHash = await UserHelpers.ComputeImageHash(image);
                if (newHash != findAccount.ImageHash)
                {
                    _logger.LogInformation("Uploading new image to Cloudflare CDN...");
                    using var memoryStream = new MemoryStream();
                    await image.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    var uploadResponse = await _cloudflareCdnService.AddOrUpdateToCdnBucket(memoryStream, image.FileName);
                    if (uploadResponse == 1)
                        _logger.LogInformation("Successfully uploaded image");
                    else
                        _logger.LogError("Error uploading image");

                    if (!string.IsNullOrEmpty(findAccount.ImagePath))
                    {
                        _logger.LogInformation("Deleting old image from Cloudflare CDN...");
                        await _cloudflareCdnService.DeleteFromCdnBucket(findAccount.ImagePath!);
                    }

                    findAccount.ImagePath = image.FileName;
                    findAccount.ImageHash = newHash;
                }
                else
                {
                    _logger.LogInformation("New image is identical to the old one. Skipping upload.");
                }
            }


            
            _logger.LogInformation("Modifying the locations of the account...");

            var findCurrentOperationPlaces = await 
                operationPlacesRepository.FindQueryable(op => op.IdAccount == cont.IdAccount)
                .ToListAsync();
            
            var locationsToDelete = new List<OperationPlace>();
            var locationsToAdd = new List<OperationPlace>();
            
            foreach (var operationPlace in findCurrentOperationPlaces)
            {
                var isLocationInDb = cont.MemberLocations
                    .Any(location => location.IdLocation == operationPlace.IdLocation);
                
                if(!isLocationInDb)
                {
                    locationsToDelete.Add(operationPlace);
                }
            }
            

            foreach (var newLoc in cont.MemberLocations)
            {
              var isNewLocationInDb = findCurrentOperationPlaces.Any(op => op.IdLocation == newLoc.IdLocation);

              if (!isNewLocationInDb)
              {
                  locationsToAdd.Add(new OperationPlace
                  {
                      IdLocation = newLoc.IdLocation,
                      IdAccount = findAccount.IdAccount,
                  });
              }
            }

            if (locationsToAdd.Count > 0)
            {
                await operationPlacesRepository.AddRangeAsync(locationsToAdd);
            }

            if (locationsToDelete.Count > 0)
            {
                await operationPlacesRepository.DeleteRangeAsync(locationsToDelete);
            }
            
            _logger.LogInformation("Finished modifying the locations of the account...");
            _logger.LogInformation("Modifying member qualities ...");
            
            
            var findCurrentMemberQualities = await memberQualityRepository
                .FindQueryable(op => op.IdMember == cont.IdAccount)
                .ToListAsync();
            
            var qualitiesToDelete = new List<MemberQuality>();
            var qualitiesToAdd = new List<MemberQuality>();
            
            foreach (var memberQuality in findCurrentMemberQualities)
            {
                var isQualityInDb = cont.MemberQualities
                    .Any(quality => quality.IdQuality == memberQuality.IdQuality);
                
                if(!isQualityInDb)
                {
                    qualitiesToDelete.Add(memberQuality);
                }
            }
            

            foreach (var newQuality in cont.MemberQualities)
            {
                var isNewQualityInDb = findCurrentMemberQualities.Any(op => op.IdQuality == newQuality.IdQuality);

                if (!isNewQualityInDb)
                {
                    qualitiesToAdd.Add(new MemberQuality
                    {
                        IdQuality = newQuality.IdQuality,
                        IdMember = findAccount.IdAccount,
                    });
                }
            }

            if (qualitiesToAdd.Count > 0)
            {
                await memberQualityRepository.AddRangeAsync(qualitiesToAdd);
            }

            if (qualitiesToDelete.Count > 0)
            {
                await memberQualityRepository.DeleteRangeAsync(qualitiesToDelete);
            }

            _logger.LogInformation("Finished modifying member qualities ...");
            
            
            await accountRepository.UpdateAsync(findAccount);
            await _unitOfWork.CommitTransactionAsync(updateTransaction);
            _logger.LogInformation("Successfully modified account from admin interface");
            return 1;
        }
        catch (Exception e)
        {
            if (updateTransaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(updateTransaction);
            }

            _logger.LogError(e, "Error when trying to modify account");
            return -1;
        }
    }

    public async Task<int> DeleteAccount(int idAccount)
    {
        IDbContextTransaction? deleteTransaction = null;
        try
        {
            _logger.LogInformation("Starting deleting account data from admin interface...");
            deleteTransaction = await  _unitOfWork.BeginTransactionAsync();
            var accountRepository = _unitOfWork.Repository<Account>();
            
            var findAccountToDelete = await accountRepository.GetByIdAsync(idAccount);
            
            if (findAccountToDelete is null)
            {
                _logger.LogError($"Account with id {idAccount} not found");
                return -1;
            }

            if (findAccountToDelete.ImagePath != null)
            {
                _logger.LogInformation("Deleting image from Cloudflare CDN...");
                var deleteResponse = await _cloudflareCdnService.DeleteFromCdnBucket(findAccountToDelete.ImagePath!);
                if (deleteResponse == 1)
                    _logger.LogInformation("Successfully deleted image from Cloudflare CDN");
                else
                    _logger.LogError("Error deleting image from Cloudflare CDN");
            }
            
            await accountRepository.DeleteAsync(findAccountToDelete);
            await _unitOfWork.CommitTransactionAsync(deleteTransaction);
            _logger.LogInformation("Successfully deleted account from admin interface");
            return 1;
        }
        catch (Exception e)
        {
            if (deleteTransaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(deleteTransaction);
            }
            
            _logger.LogError(e, "Error when trying to delete account");
            return -1;
        }
    }

    public async Task<Account?> GetAccountByEmailAsync(string email)
    {
        return await _unitOfWork.Repository<Account>().FindQueryable(user => user.Email == email)
            .FirstOrDefaultAsync();
    }
}