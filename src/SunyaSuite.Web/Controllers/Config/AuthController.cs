using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SunyaSuite.Application.DTOs.Config;
using SunyaSuite.Application.Interfaces;
using SunyaSuite.Application.Interfaces.Config;
using SunyaSuite.Domain.Entities.Config;
using SunyaSuite.Domain.Enums;
using SunyaSuite.Infrastructure.Data.Config;
using SunyaSuite.Web.Api.Services.Config;

namespace SunyaSuite.Web.Api.Controllers.Config;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly JwtTokenService _jwtTokenService;
    private readonly IInviteService _inviteService;
    private readonly IEmailService _emailService;
    private readonly IDbContextFactory<ConfigDbContext> _configFactory;
    private readonly TimeProvider _timeProvider;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        JwtTokenService jwtTokenService,
        IInviteService inviteService,
        IEmailService emailService,
        IDbContextFactory<ConfigDbContext> configFactory,
        TimeProvider timeProvider
    )
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _inviteService = inviteService;
        _emailService = emailService;
        _configFactory = configFactory;
        _timeProvider = timeProvider;
    }

    public record LoginRequest(string Email, string Password, bool RememberMe);

    public record RegisterRequest(string Email, string Password, string? Name, string? InviteCode);

    public record AuthResponse(
        string AccessToken,
        DateTime ExpiresAt,
        string RefreshToken,
        string UserId,
        string Email,
        IList<string> Roles,
        List<OrganizationDto> Organizations
    );

    public record MessageResponse(string Message);

    public record ForgotPasswordRequest(string Email);

    public record RefreshRequest(string RefreshToken);

    public record ChangePasswordRequest
    {
        [Required]
        public string CurrentPassword { get; set; } = "";

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string NewPassword { get; set; } = "";

        [Required]
        [Compare(nameof(NewPassword))]
        public string ConfirmNewPassword { get; set; } = "";
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Unauthorized(new { message = "Invalid email or password" });

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, true);
        if (result.IsLockedOut)
            return Unauthorized(new { message = "Account is locked out. Try again later." });
        if (result.IsNotAllowed)
            return Unauthorized(
                new { message = "Email confirmation is required. Check your inbox." }
            );
        if (!result.Succeeded)
            return Unauthorized(new { message = "Invalid email or password" });

        var (accessToken, expiresAt) = await _jwtTokenService.GenerateAccessTokenAsync(user);
        var (refreshEntity, rawRefreshToken) = await _jwtTokenService.GenerateRefreshTokenAsync(
            user,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );
        var roles = await _userManager.GetRolesAsync(user);
        var orgs = await GetUserOrganizationsAsync(user.Id);

        return Ok(
            new AuthResponse(
                accessToken,
                expiresAt,
                rawRefreshToken,
                user.Id,
                user.Email ?? "",
                roles,
                orgs
            )
        );
    }

    [HttpPost("register")]
    public async Task<ActionResult<MessageResponse>> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.InviteCode))
            return BadRequest(
                new { message = "Registration is invite-only. A valid invite code is required." }
            );

        if (!await _inviteService.ValidateInviteAsync(request.InviteCode))
            return BadRequest(new { message = "Invalid or expired invite code." });

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
            return BadRequest(new { message = "Email is already registered" });

        var name = (request.Name ?? "").Trim();
        var firstName = name;
        var lastName = "";
        var spaceIdx = name.IndexOf(' ');
        if (spaceIdx > 0)
        {
            firstName = name[..spaceIdx];
            lastName = name[(spaceIdx + 1)..];
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = firstName,
            LastName = lastName,
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var firstError = result.Errors.FirstOrDefault()?.Description ?? "Registration failed.";
            return BadRequest(new { message = firstError });
        }

        var (role, _, organizationId) = await _inviteService.ConsumeInviteAsync(
            request.InviteCode,
            request.Email
        );
        await _userManager.AddToRoleAsync(user, role);

        var confirmToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        await _userManager.ConfirmEmailAsync(user, confirmToken);

        await AssignToOrganizationAsync(user.Id, organizationId);

        return Ok(new MessageResponse("Account created successfully. You can now sign in."));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest request)
    {
        var tokenHash = JwtTokenService.HashTokenString(request.RefreshToken);
        var refreshToken = await _jwtTokenService.GetRefreshTokenAsync(tokenHash);

        if (refreshToken is null)
            return Unauthorized(new { message = "Invalid refresh token" });

        if (refreshToken.IsExpired)
            return Unauthorized(new { message = "Refresh token has expired" });

        if (refreshToken.IsRevoked)
        {
            await _jwtTokenService.RevokeRefreshTokenFamilyAsync(
                refreshToken.TokenHash,
                "Token reuse detected"
            );
            return Unauthorized(
                new { message = "Refresh token reuse detected. All sessions revoked." }
            );
        }

        var user = await _userManager.FindByIdAsync(refreshToken.UserId);
        if (user is null)
            return Unauthorized(new { message = "User not found" });

        if (await _userManager.IsLockedOutAsync(user))
            return Unauthorized(new { message = "Account is locked out" });

        if (!await _userManager.IsEmailConfirmedAsync(user))
            return Unauthorized(new { message = "Email not confirmed" });

        var rotated = await _jwtTokenService.RotateRefreshTokenAsync(
            refreshToken,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );
        if (rotated is null)
            return Unauthorized(new { message = "Failed to rotate refresh token" });

        var (accessToken, expiresAt) = await _jwtTokenService.GenerateAccessTokenAsync(user);
        var roles = await _userManager.GetRolesAsync(user);
        var orgs = await GetUserOrganizationsAsync(user.Id);

        return Ok(
            new AuthResponse(
                accessToken,
                expiresAt,
                rotated.Value.rawToken,
                user.Id,
                user.Email ?? "",
                roles,
                orgs
            )
        );
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult<MessageResponse>> ForgotPassword(
        [FromBody] ForgotPasswordRequest request
    )
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !await _userManager.IsEmailConfirmedAsync(user))
            return Ok(new MessageResponse("If the email exists, a reset link has been sent."));

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetUrl =
            $"{Request.Scheme}://{Request.Host}/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(request.Email)}";

        try
        {
            await _emailService.SendAsync(
                request.Email,
                "Reset your password",
                $"Click the link to reset your password: {resetUrl}",
                CancellationToken.None
            );
        }
        catch
        {
            // Log but don't reveal email failures
        }

        return Ok(new MessageResponse("If the email exists, a reset link has been sent."));
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
            return Unauthorized(new { message = "User not found" });

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Unauthorized(new { message = "User not found" });

        var result = await _userManager.ChangePasswordAsync(
            user,
            request.CurrentPassword,
            request.NewPassword
        );
        if (!result.Succeeded)
            return BadRequest(
                new
                {
                    message = result.Errors.FirstOrDefault()?.Description
                        ?? "Password change failed",
                }
            );

        await _jwtTokenService.RevokeRefreshTokensByUserIdAsync(userId, "Password changed");

        return Ok(new { message = "Password changed successfully" });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout([FromBody] RefreshRequest request)
    {
        if (!string.IsNullOrEmpty(request.RefreshToken))
        {
            var tokenHash = JwtTokenService.HashTokenString(request.RefreshToken);
            var refreshToken = await _jwtTokenService.GetRefreshTokenAsync(tokenHash);
            if (refreshToken is not null)
            {
                await using var configDb = await _configFactory.CreateDbContextAsync();
                refreshToken.RevokedAt = _timeProvider.GetUtcNow().UtcDateTime;
                refreshToken.ReasonRevoked = "Logged out";
                configDb.RefreshTokens.Update(refreshToken);
                await configDb.SaveChangesAsync();
            }
        }

        return Ok(new { message = "Logged out" });
    }

    private async Task<List<OrganizationDto>> GetUserOrganizationsAsync(string userId)
    {
        await using var configDb = await _configFactory.CreateDbContextAsync();
        return await configDb
            .OrganizationUsers.AsNoTracking()
            .Where(ou => ou.UserId == userId)
            .Select(ou => new OrganizationDto
            {
                Id = ou.Organization.Id,
                Name = ou.Organization.Name,
                Slug = ou.Organization.Slug,
                HasSeparateDatabase = ou.Organization.ConnectionString != null,
                Role = ou.Role,
            })
            .ToListAsync();
    }

    private async Task AssignToOrganizationAsync(string userId, Guid organizationId)
    {
        await using var configDb = await _configFactory.CreateDbContextAsync();

        var exists = await configDb.OrganizationUsers.AnyAsync(ou =>
            ou.UserId == userId && ou.OrganizationId == organizationId
        );

        if (!exists)
        {
            configDb.OrganizationUsers.Add(
                new OrganizationUser
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    UserId = userId,
                    Role = OrgRoles.Member,
                    JoinedAt = _timeProvider.GetUtcNow().UtcDateTime,
                }
            );
            await configDb.SaveChangesAsync();
        }
    }
}
