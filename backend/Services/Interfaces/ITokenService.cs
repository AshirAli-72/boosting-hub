using System.Security.Claims;
using BoostingHub.backend.DTOs;
using BoostingHub.backend.Models;

namespace BoostingHub.backend.Services.Interfaces;

public interface ITokenService
{
    Task<AuthResponseDto> GenerateTokensAsync(User user, HttpContext? context = null);
    ClaimsPrincipal? ValidateToken(string token);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
