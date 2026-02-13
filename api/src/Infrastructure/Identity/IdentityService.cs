using System.Security.Claims;
using api.Application.Common.Interfaces;
using api.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace api.Infrastructure.Identity;

/// <summary>
/// Simplified IdentityService for Entra ID authentication.
/// User management is handled by Entra ID, not ASP.NET Core Identity.
/// </summary>
public class IdentityService : IIdentityService
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public IdentityService(
        IAuthorizationService authorizationService,
        IHttpContextAccessor httpContextAccessor)
    {
        _authorizationService = authorizationService;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<string?> GetUserNameAsync(string userId)
    {
        // In Entra ID, username is typically the email or UPN claim
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return Task.FromResult<string?>(null);

        var currentUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId != userId) return Task.FromResult<string?>(null);

        // Try to get the name from common claims
        var userName = user.FindFirstValue(ClaimTypes.Name)
                    ?? user.FindFirstValue(ClaimTypes.Email)
                    ?? user.FindFirstValue("preferred_username")
                    ?? userId;

        return Task.FromResult<string?>(userName);
    }

    public Task<(Result Result, string UserId)> CreateUserAsync(string userName, string password)
    {
        // User creation is handled by Entra ID, not this application
        throw new NotSupportedException("User creation is managed by Entra ID, not the application.");
    }

    public Task<bool> IsInRoleAsync(string userId, string role)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return Task.FromResult(false);

        var currentUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId != userId) return Task.FromResult(false);

        return Task.FromResult(user.IsInRole(role));
    }

    public async Task<bool> AuthorizeAsync(string userId, string policyName)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return false;

        var currentUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId != userId) return false;

        var result = await _authorizationService.AuthorizeAsync(user, policyName);
        return result.Succeeded;
    }

    public Task<Result> DeleteUserAsync(string userId)
    {
        // User deletion is handled by Entra ID, not this application
        throw new NotSupportedException("User deletion is managed by Entra ID, not the application.");
    }
}
