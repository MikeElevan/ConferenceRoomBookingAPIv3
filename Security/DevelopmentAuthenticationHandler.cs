using System.Security.Claims;
using System.Text.Encodings.Web;
using ConferenceRoomBookingAPIv3.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ConferenceRoomBookingAPIv3.Security;

/// <summary>
/// Fake authentication handler used ONLY in the Development environment so the API can be
/// debugged locally without a real JWT/Identity Provider. The simulated user's name and roles
/// are read from configuration (DevelopmentAuth section in appsettings.Development.json), so
/// they can be changed without recompiling — e.g. remove "Administrator" to test a 403 scenario.
///
/// This handler must never be wired up outside Development (see Program.cs).
/// </summary>
public sealed class DevelopmentAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> schemeOptions,
    IOptionsMonitor<DevelopmentAuthOptions> devAuthOptions,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(schemeOptions, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        DevelopmentAuthOptions options = devAuthOptions.CurrentValue;

        List<Claim> claims = new()
        {
            new Claim(ClaimTypes.NameIdentifier, options.UserName),
            new Claim(ClaimTypes.Name, options.UserName)
        };
        claims.AddRange(options.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        ClaimsIdentity identity = new(claims, Scheme.Name);
        ClaimsPrincipal principal = new(identity);
        AuthenticationTicket ticket = new(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}