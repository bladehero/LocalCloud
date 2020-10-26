using LocalCloud.Data.Models;
using LocalCloud.Interfaces.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LocalCloud.Services
{
    public class JwtService : IJwtService
    {
        private readonly ApplicationSettings _applicationSettings;
        public JwtService(IOptions<ApplicationSettings> applicationSettings)
        {
            _applicationSettings = applicationSettings.Value;
        }

        public string CreateToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_applicationSettings.SecretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new []
                {
                    new Claim(ClaimTypes.Name, user.Login),
                    new Claim(ClaimTypes.NameIdentifier, user.Author)
                }),
                Expires = DateTime.UtcNow.AddSeconds(_applicationSettings.TokenLifetimeInSeconds),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);
            return tokenString;
        }
    }
}
