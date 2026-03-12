using System.Security.Claims;
using System.Text.Encodings.Web;
using MemberTracker.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace NextHorizon.Security;

public sealed class DevelopmentAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "DevAuth";

    private readonly IWebHostEnvironment _webHostEnvironment;

    public DevelopmentAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IWebHostEnvironment webHostEnvironment)
        : base(options, logger, encoder)
    {
        _webHostEnvironment = webHostEnvironment;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!_webHostEnvironment.IsDevelopment())
        {
            return Task.FromResult(AuthenticateResult.Fail("Development authentication is disabled outside Development environment."));
        }

        var userId = ResolveUserId();
        var roles = ResolveRoles();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, $"dev-user-{userId}"),
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private int ResolveUserId()
    {
        if (!Request.Headers.TryGetValue("X-Debug-UserId", out var values))
        {
            return 1;
        }

        return int.TryParse(values.ToString(), out var userId) && userId > 0 ? userId : 1;
    }

    private IReadOnlyList<string> ResolveRoles()
    {
        if (!Request.Headers.TryGetValue("X-Debug-Roles", out var values))
        {
            return new[] { UploadRoles.Consumer, UploadRoles.Admin };
        }

        var roles = values
            .ToString()
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static role => !string.IsNullOrWhiteSpace(role))
            .ToArray();

        return roles.Length > 0 ? roles : new[] { UploadRoles.Consumer, UploadRoles.Admin };
    }
}
