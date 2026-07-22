using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SunyaSuite.Application.DTOs.Config;
using SunyaSuite.Application.Interfaces.Config;
using SunyaSuite.Domain.Entities.Config;
using SunyaSuite.Domain.Enums;
using SunyaSuite.Infrastructure.Data.Config;
using SunyaSuite.Web.Api.Services.Config;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace SunyaSuite.Web.Api.Controllers.Config;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly JwtTokenService _jwtTokenService;
    private readonly IInviteService _inviteService;
    private readonly IDbContextFactory<ConfigDbContext> _configFactory;
    private readonly TimeProvider _timeProvider;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        JwtTokenService jwtTokenService,
        IInviteService inviteService,
        IDbContextFactory<ConfigDbContext> configFactory,
        TimeProvider timeProvider)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _inviteService = inviteService;
        _configFactory = configFactory;
        _timeProvider = timeProvider;
    }

    public record LoginRequest(string Email, string Password, bool RememberMe);
    public record RegisterRequest(string Email, string Password, string? Name, string? InviteCode);
    public record AuthResponse(string Token, DateTime ExpiresAt, string UserId, string Email, IList<string> Roles, List<OrganizationDto> Organizations);
    public record MessageResponse(string Message);
    public record ForgotPasswordRequest(string Email);
    public record RenewRequest(string Token);

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
            return Unauthorized(new { message = "Email confirmation is required. Check your inbox." });
        if (!result.Succeeded)
            return Unauthorized(new { message = "Invalid email or password" });

        var (token, expiresAt) = await _jwtTokenService.GenerateTokenAsync(user);
        var roles = await _userManager.GetRolesAsync(user);
        var orgs = await GetUserOrganizationsAsync(user.Id);

        return Ok(new AuthResponse(token, expiresAt, user.Id, user.Email ?? "", roles, orgs));
    }

    [HttpPost("register")]
    public async Task<ActionResult<MessageResponse>> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.InviteCode))
            return BadRequest(new { message = "Registration is invite-only. A valid invite code is required." });

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
            LastName = lastName
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var firstError = result.Errors.FirstOrDefault()?.Description ?? "Registration failed.";
            return BadRequest(new { message = firstError });
        }

        var (role, _, organizationId) = await _inviteService.ConsumeInviteAsync(request.InviteCode, request.Email);
        await _userManager.AddToRoleAsync(user, role);

        // Auto-confirm email (email sending is no-op in dev)
        var confirmToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        await _userManager.ConfirmEmailAsync(user, confirmToken);

        // Assign user to the invite's organization
        await AssignToOrganizationAsync(user.Id, organizationId);

        return Ok(new MessageResponse("Account created successfully. You can now sign in."));
    }

    public class ChangePasswordRequest
    {
        [Required] public string CurrentPassword { get; set; } = "";
        [Required][StringLength(100, MinimumLength = 6)] public string NewPassword { get; set; } = "";
        [Required][Compare(nameof(NewPassword))] public string ConfirmNewPassword { get; set; } = "";
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult<MessageResponse>> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        // Always return success to prevent email enumeration
        if (user is null || !await _userManager.IsEmailConfirmedAsync(user))
            return Ok(new MessageResponse("If the email exists, a reset link has been sent."));

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

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

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { message = result.Errors.FirstOrDefault()?.Description ?? "Password change failed" });

        return Ok(new { message = "Password changed successfully" });
    }

    [HttpPost("renew")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Renew([FromBody] RenewRequest request)
    {
        var principal = _jwtTokenService.ValidateToken(request.Token, validateLifetime: false);
        if (principal is null)
            return Unauthorized(new { message = "Invalid token" });

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized(new { message = "Invalid token" });

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null || user.LockoutEnabled)
            return Unauthorized(new { message = "User not found or locked out" });

        if (!await _userManager.IsEmailConfirmedAsync(user))
            return Unauthorized(new { message = "Email not confirmed" });

        var (token, expiresAt) = await _jwtTokenService.GenerateTokenAsync(user);
        var roles = await _userManager.GetRolesAsync(user);
        var orgs = await GetUserOrganizationsAsync(user.Id);

        return Ok(new AuthResponse(token, expiresAt, user.Id, user.Email ?? "", roles, orgs));
    }

    private async Task<List<OrganizationDto>> GetUserOrganizationsAsync(string userId)
    {
        await using var configDb = await _configFactory.CreateDbContextAsync();
        return await configDb.OrganizationUsers
            .AsNoTracking()
            .Where(ou => ou.UserId == userId)
            .Select(ou => new OrganizationDto
            {
                Id = ou.Organization.Id,
                Name = ou.Organization.Name,
                Slug = ou.Organization.Slug,
                HasSeparateDatabase = ou.Organization.ConnectionString != null,
                Role = ou.Role
            })
            .ToListAsync();
    }

    private async Task AssignToOrganizationAsync(string userId, Guid organizationId)
    {
        await using var configDb = await _configFactory.CreateDbContextAsync();

        var exists = await configDb.OrganizationUsers
            .AnyAsync(ou => ou.UserId == userId && ou.OrganizationId == organizationId);

        if (!exists)
        {
            configDb.OrganizationUsers.Add(new OrganizationUser
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                UserId = userId,
                Role = OrgRoles.Member,
                JoinedAt = _timeProvider.GetUtcNow().UtcDateTime
            });
            await configDb.SaveChangesAsync();
        }
    }
}
