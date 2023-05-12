using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace CityInfo.API.Controllers
{
    [Route("api/authentication")]
    public class AuthenticationController : ControllerBase
    {
        public class AuthenticationRequestBody
        {
            public string UserName { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        private class CityInfoAPIUser
        {
            public int UserId { get; set; }
            public string UserName { get; set; } = string.Empty;
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string? City { get; set; }

            public CityInfoAPIUser(int userId, string userName, string firstName, string lastName, string city)
            {
                UserId = userId;
                UserName = userName;
                FirstName = firstName;
                LastName = lastName;
                City = city;
            }

            public CityInfoAPIUser(int userId, string userName)
            {
                UserId = userId;
                UserName = userName;
            }
        }

        private readonly IConfiguration config;
        public AuthenticationController(IConfiguration config){
            this.config = config 
            ?? throw new ArgumentNullException(nameof(config));
        }

        [HttpPost("authenticate")]
        public ActionResult<string> Authenticate(AuthenticationRequestBody auth)
        {
            // Step 1) validate credentials (always passes here)
            var user = ValidateCredentials(auth.UserName, auth.Password);
            if (user == null) return Unauthorized();

            // Step 2) Create Token 
            string? keygenSecret = this.config["Authentication:KeygenSecret"];
            if (keygenSecret == null)
                    return StatusCode(500, "Could not authenticate due to server error");

            var securityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(keygenSecret));
            var signingCredentials = new SigningCredentials(
                securityKey, SecurityAlgorithms.HmacSha256);
            
            var claimsForToken = new List<Claim>();
            claimsForToken.Add(new Claim("sub", user.UserId.ToString()));
            claimsForToken.Add(new Claim("first_name", user.FirstName??""));
            claimsForToken.Add(new Claim("last_name", user.LastName??""));
            claimsForToken.Add(new Claim("city", user.City??""));

            var jwtSecurityToken = new JwtSecurityToken(
                this.config["Authentication:Issuer"],
                this.config["Authenticaon:Audience"],
                claimsForToken,
                // first datetime says when token starts to be valid, second sets timeout
                DateTime.UtcNow,
                DateTime.UtcNow.AddHours(1),
                signingCredentials  
            );

            var tokenOut = new JwtSecurityTokenHandler()
                            .WriteToken(jwtSecurityToken);

            // not right now tokens are unencrypted and relies on https, also the
            // username and pw are passed plaintext to the request url, thats pretty bad
            // exhibit a: https://localhost:7054/api/authentication/authenticate?UserName=a&Password=a
            // so the useful part in this code is the token generation, do not copy this
            return Ok(tokenOut);
                    
        }

        // This code stops at generating tokens, but usually you'd check from a user db instead
        // of an in-memory object. Also, we'll just assume the credentials are valid.
        private CityInfoAPIUser ValidateCredentials(string userName, string password)
        {
            return new CityInfoAPIUser(1, userName??"");
        }
    }
}