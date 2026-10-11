using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CodeInspector.Leads.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace CodeInspector.Leads.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(IConfiguration configuration) : ControllerBase
{
    [HttpPost("token")]
    public ActionResult<TokenResponse> IssueToken(TokenRequest request)
    {
        var jwtSection = configuration.GetSection("Authentication:Jwt");
        var key = jwtSection["Key"];
        if (string.IsNullOrWhiteSpace(key))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "JWT is not configured." });
        }

        var expires = DateTimeOffset.UtcNow.AddHours(12);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, request.UserId.ToString()),
            new("sub", request.UserId.ToString())
        };
        if (request.Admin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);
        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        return Ok(new TokenResponse(accessToken, expires));
    }
}
