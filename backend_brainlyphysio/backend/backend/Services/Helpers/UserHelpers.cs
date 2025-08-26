using System.Security.Cryptography;
using System.Text;

namespace backend.Services.Helpers;

public class UserHelpers
{
    private const string Chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public static string Token(int size, int size2, string userEmail)
    {
        var seed = GenerateSeed(userEmail);
        var generateRandom = new Random(seed);

       
        var builder = new StringBuilder();

        
        for (var i = 0; i < size; i++)
        {
            var index = generateRandom.Next(Chars.Length);
            builder.Append(Chars[index]);
        }

        while (builder.Length < 60)
        {
            var nextNum = generateRandom.Next(size2);
            builder.Append(nextNum);
        }
        
       
        while (builder.Length < 100)
        {
            var index = generateRandom.Next(Chars.Length);
            builder.Append(Chars[index]);
            var nextNum = generateRandom.Next(size2);
            builder.Append(nextNum);
        }

        if (builder.Length > 100)
        {
            builder.Length = 100; 
        }

        return builder.ToString();
    }
    
    private static int GenerateSeed(string userEmail)
    {
        var emailBytes = Encoding.UTF8.GetBytes(userEmail);
        var hashBytes = SHA256.HashData(emailBytes);
        var hashInt = BitConverter.ToInt32(hashBytes, 0);
        return hashInt ^ DateTime.Now.Ticks.GetHashCode();
    }

    public static string CryptPassword(string uncryptedPassword)
    {
        return BCrypt.Net.BCrypt.EnhancedHashPassword(uncryptedPassword, 13);
    }

    public static bool VerifyCryptedPassword(string plainTextPassword, string cryptedPassword)
    {
        return BCrypt.Net.BCrypt.EnhancedVerify(plainTextPassword, cryptedPassword);
    }

    public static async Task<string> ComputeImageHash(IFormFile image)
    {
        using var sha256 = SHA256.Create();
        using var stream = new MemoryStream();
        await image.CopyToAsync(stream);
        stream.Position = 0;
        var hash = await sha256.ComputeHashAsync(stream);
        return Convert.ToBase64String(hash);
    }
    
    
}