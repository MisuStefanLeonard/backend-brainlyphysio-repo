using System.Security.Claims;
using backend.Models.User;

namespace backend.Services.CloudFlareCdnServices;

public interface ICloudFlareCdnService
{
    public Task<string> GetCdnSecret(string secretName);
    public Task<int> AddOrUpdateToCdnBucket(MemoryStream imageStream, string fileNameInS3);
    public Task<int> DeleteFromCdnBucket(string keyName);
    public Task<string?> GeneratePresignedUrl(string imageKey);
    Task<string> GenerateJwtAccesToken(Account currentLogIn);
    string RefreshToken();
    Task<Tuple<ClaimsPrincipal? , Account?>>TokenValidation(string token , string refreshToken);
    
}