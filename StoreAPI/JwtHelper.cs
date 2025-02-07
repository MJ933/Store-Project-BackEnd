using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public static class JwtHelper
{
    public static string GenerateToken(string userId, string? email, string? phoneNumber, IConfiguration configuration, string? role = null)
    {
        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!)
        );

        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),     // User ID
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // Unique token ID
        };

        // Add email claim if provided
        if (!string.IsNullOrEmpty(email))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, email));
        }

        // Add phone number claim if provided
        if (!string.IsNullOrEmpty(phoneNumber))
        {
            claims.Add(new Claim("phone_number", phoneNumber)); // Custom claim for phone number
        }

        // Add role claim if provided
        if (!string.IsNullOrEmpty(role))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var expireMinutes = Convert.ToDouble(configuration["Jwt:ExpireMinutes"]);
        var expirationTime = DateTime.UtcNow.AddMinutes(expireMinutes);
        Console.WriteLine($"Token expiration time: {expirationTime}");

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: expirationTime,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}