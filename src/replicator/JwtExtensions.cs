using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using replicator.Settings;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace replicator;

public static class JwtExtensions
{
    public static IServiceCollection AddJwtAuth(this IServiceCollection services, bool enableAuth, AuthSettings authSettings)
    {
        if (!enableAuth)
        {
            // Register a "fake" authentication scheme that always succeeds
            services.AddAuthentication("AllowAll")
                .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, AllowAllAuthenticationHandler>(
                    "AllowAll", options => { });
            return services;
        }

        var jwtSecret = authSettings.JwtSecret;
        var key = Encoding.ASCII.GetBytes(jwtSecret);
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.FromMinutes(2)
            };
        });
        return services;
    }
}

public class AllowAllAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public AllowAllAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "AllowAllUser") }, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
