using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using backend.Models.User;
using backend.UOW;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
// using CloudFlare.Client;
// using CloudFlare.Client.Api.Authentication;
using Newtonsoft.Json.Linq;
using RestSharp;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace backend.Services.CloudFlareCdnServices;

public class CloudFlareCdnService : ICloudFlareCdnService
{
    // private readonly CloudFlareClient _client;
    private readonly ILogger<CloudFlareCdnService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IUnitOfWork _unitOfWork;
    private static readonly AmazonS3Client AmazonS3Client;
    // private readonly string? _apiKey;
    // private readonly string? _storeId;
    // private readonly string? _accountId;
    private static readonly string? S3CdnServiceUrl = Environment.GetEnvironmentVariable("CDN_S3_OPERATIONS");
    private static readonly string? S3AccessKeyId = Environment.GetEnvironmentVariable("CDN_ACCESS_KEY_ID");
    private static readonly string? S3SecretKeyId = Environment.GetEnvironmentVariable("CDN_SECRET_KEY_ID");
    // private const string AccountEmail = "misustefan212@gmail.com";
    private const string DirectoryPrefix = "images/";
    private const string BucketName = "images";
    
    
    

    public CloudFlareCdnService(ILogger<CloudFlareCdnService> logger, IConfiguration configuration, IUnitOfWork unitOfWork)
    {
        // _storeId = Environment.GetEnvironmentVariable("CDN_STORE_ID");
        // _apiKey = Environment.GetEnvironmentVariable("CDN_API_KEY");
        // _accountId = Environment.GetEnvironmentVariable("CDN_ACCOUNT_ID");
        // var auth = new ApiTokenAuthentication(_apiKey);
        // _client = new CloudFlareClient(auth);
        _logger = logger;
        _configuration = configuration;
        _unitOfWork = unitOfWork;
    }

    static CloudFlareCdnService()
    {
        Console.WriteLine($"{S3CdnServiceUrl}");
        AmazonS3Client = new AmazonS3Client(
            $"{S3AccessKeyId}",
            $"{S3SecretKeyId}",
            new AmazonS3Config
            {
                ServiceURL = S3CdnServiceUrl,
            });
    }
    
    // private async Task<bool> DirectoryExists(string key)
    // {
    //     try
    //     {
    //         var listObjectRequest = new ListObjectsV2Request
    //         {
    //             BucketName = BucketName,
    //         };
    //
    //         var response = await AmazonS3Client.ListObjectsV2Async(listObjectRequest);
    //
    //         // The correct check is to see if the response contains any objects
    //         // that exactly match the key.
    //         if (response.S3Objects.Any(obj => obj.Key == key))
    //         {
    //             _logger.LogInformation($"Object in '{BucketName}' with key '{key}' exists.");
    //             return true;
    //         }
    //         else
    //         {
    //             _logger.LogWarning($"Object in '{BucketName}' with key '{key}' does NOT exist.");
    //             return false;
    //         }
    //     }
    //     catch (AmazonS3Exception e)
    //     {
    //         _logger.LogError(e, $"Error checking for key '{key}' in bucket '{BucketName}'");
    //         return false; 
    //     }
    // }

    public async Task<string> GetCdnSecret(string secretName)
    {
        try
        {
            var client = new RestClient();
            var request = new RestRequest
            {
                Method = Method.Get,
                Resource = "https://dry-dust-934d.misustefan212.workers.dev/"
            };

            var requestResponse = await client.ExecuteAsync(request);
            if (!requestResponse.IsSuccessful)
            {
                _logger.LogError("Failed to fetch secret: " + requestResponse.StatusCode);
                return "";
            }

            var jsonObject = JObject.Parse(requestResponse.Content!);

            var getSecretValue = jsonObject[secretName]?.ToString();

            if (getSecretValue is null)
            {
                _logger.LogError($"Secret not found with name {secretName}");
                return "";
            }

            return getSecretValue;

        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error getting CDN secret");
            return "";
        }
    }

    public static async Task<string> GetCdnSecret(string secretName,bool dummy)
    {
        try
        {
            var client = new RestClient();
            var request = new RestRequest
            {
                Method = Method.Get,
                Resource = "https://dry-dust-934d.misustefan212.workers.dev/"
            };

            var requestResponse = await client.ExecuteAsync(request);
            if (!requestResponse.IsSuccessful)
            {
                Console.WriteLine("***********************");
                Console.WriteLine("***********************");
                Console.WriteLine("***********************");
                Console.WriteLine("Failed to fetch secret: " + requestResponse.StatusCode);
                return "";
            }

            var jsonObject = JObject.Parse(requestResponse.Content!);

            var getSecretValue = jsonObject[secretName]?.ToString();

            if (getSecretValue is not null)
                return getSecretValue;
            
            Console.WriteLine("***********************");
            Console.WriteLine("***********************");
            Console.WriteLine("***********************");
            Console.WriteLine("Secret is null" + requestResponse.StatusCode);
            return "";

        }
        catch (Exception e)
        {
            Console.WriteLine("***********************");
            Console.WriteLine("***********************");
            Console.WriteLine("***********************");
            Console.WriteLine("Error getting CDN secret");
            Console.WriteLine(e);
            return "";
        }
    }

    public async Task<int> AddOrUpdateToCdnBucket(MemoryStream imageStream, string fileNameInS3)
    {
        try
        {
            var request = new PutObjectRequest
            {
                BucketName = BucketName,
                Key = fileNameInS3,
                InputStream = imageStream,
                ContentType = "image/*",
                DisablePayloadSigning = true,
                DisableDefaultChecksumValidation = true
            };
            
            var response = await AmazonS3Client.PutObjectAsync(request);
            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                _logger.LogInformation($"Successfully uploaded {fileNameInS3} to {BucketName}.");
                return 1;
            }

            _logger.LogError($"Could not upload {fileNameInS3} to {BucketName}.");
            return -1;
        }
        catch (AmazonS3Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }


    public async Task<int> DeleteFromCdnBucket(string keyName)
    {
        try
        {

            await AmazonS3Client.DeleteObjectAsync(BucketName, keyName);
            _logger.LogInformation($"Succesfully deleted item with keyname {keyName}");
            return 1;
        }
        catch (AmazonS3Exception e)
        {
            _logger.LogError($"Error when deleting item with Key {keyName} from {BucketName}");
            _logger.LogInformation(e.Message);
            throw;
        }
       
    }

    
    
    public async Task<string?> GeneratePresignedUrl(string imageKey)
    {
        try
        {
            // Construct the CloudFront URL directly
            await Task.Delay(1);
            var expiration = DateTime.UtcNow.AddHours(24);
            
            var presignedUrlRequest = new GetPreSignedUrlRequest
            {
                BucketName = BucketName,
                Key = imageKey,
                Expires = expiration,
                Verb = HttpVerb.GET
            };
            
            var presignedUrl = await AmazonS3Client.GetPreSignedURLAsync(presignedUrlRequest);
            _logger.LogInformation($"Successfully generated CloudFront URL in {BucketName} with file {imageKey}");
            return presignedUrl;
           
        }
        catch (AmazonS3Exception e)
        {
            _logger.LogInformation($"Error when trying to generate CloudFront URL in {BucketName} with file {imageKey}");
            Console.WriteLine(e.Message);
            return null;
        }
    }

    public async Task<string> GenerateJwtAccesToken(Account currentLogIn)
    {
       
        var secretValue = await GetCdnSecret("jwt"); 
       

        if (string.IsNullOrEmpty(secretValue))
        {
            throw new ArgumentNullException(secretValue);
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretValue));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var jwtSettings = _configuration.GetSection("JwtSettings");

        var claims = new List<Claim>
        {
            // new ("username" , currentLogIn.Username!),
            new (JwtRegisteredClaimNames.Email , currentLogIn.Email!),
            new (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new (ClaimTypes.Role , currentLogIn.Role),
            new ("user_id" , currentLogIn.IdAccount.ToString())
        };
        
        var token = new JwtSecurityToken
        (
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string RefreshToken()
    {
        var secureRandomBytes = new byte[128];
        using var randomNumberGenerator = RandomNumberGenerator.Create();
        randomNumberGenerator.GetBytes(secureRandomBytes);
        var refreshToken = Convert.ToBase64String(secureRandomBytes);
        return refreshToken;
    }

    private async Task<TokenValidationParameters> GetValidationParameters()
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secret = await GetCdnSecret("jwt"); 
        var key = Encoding.UTF8.GetBytes(secret);
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero
        };
    }
    
    public async Task<Tuple<ClaimsPrincipal? , Account?>> TokenValidation(string token , string refreshToken)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        try
        {
            if (token == "refresh")
            {
                throw new SecurityTokenExpiredException("Expired token . Issuing a new one.");
            }
            
            var validationParameters = await GetValidationParameters();

            var claimsPrincipal = tokenHandler.ValidateToken(token, validationParameters,
                out  _);

            return new Tuple<ClaimsPrincipal?, Account?>(claimsPrincipal , null); // succesfull
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogInformation("Issuing new acces token..");
            var checkRefreshToken = await _unitOfWork.Repository<RememberUser>()
                .FindQueryable(rm => rm.SessionToken == refreshToken)
                .FirstOrDefaultAsync();

            if (checkRefreshToken == null || checkRefreshToken.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogError("Refresh token expired. Session expired");
                return new Tuple<ClaimsPrincipal?, Account?>(null , null);
            }

            var userId = checkRefreshToken.IdAccount;
            var findUser = await _unitOfWork.Repository<Account>()
                .FindQueryable(c => c.IdAccount == userId)
                .FirstOrDefaultAsync();

            if (findUser == null)
            {
                _logger.LogError("User not found in db!");
                return new Tuple<ClaimsPrincipal?, Account?>(null , null);
            }
            var validateAgain = tokenHandler.ValidateToken(await GenerateJwtAccesToken(findUser),  await GetValidationParameters(),
                out  _);
            return new Tuple<ClaimsPrincipal?, Account?>(validateAgain, findUser); // refresh 
        }
        catch (Exception e)
        {
            Console.WriteLine("Error when validating the token -> Error: " + e.Message);
            return new Tuple<ClaimsPrincipal?, Account?>(null, null); // general error  
        }
    }
}