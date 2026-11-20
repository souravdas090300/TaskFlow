using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace TaskFlow.API.Services;

public class GoogleTokenValidator
{
    private readonly IConfiguration _configuration;
    private readonly HttpDocumentRetriever _retriever = new();
    private ConfigurationManager<OpenIdConnectConfiguration>? _configManager;

    public GoogleTokenValidator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<ClaimsPrincipal?> ValidateAsync(string idToken)
    {
        var clientId = _configuration["Authentication:Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(idToken))
            return null;

        var config = await GetConfigurationAsync();
        if (config is null)
            return null;

        var handler = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers = new[] { "https://accounts.google.com", "accounts.google.com" },
            ValidateAudience = true,
            ValidAudience = clientId,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = config.SigningKeys,
            ClockSkew = TimeSpan.FromMinutes(1),
        };

        try
        {
            return handler.ValidateToken(idToken, parameters, out _);
        }
        catch
        {
            return null;
        }
    }

    private async Task<OpenIdConnectConfiguration?> GetConfigurationAsync()
    {
        _configManager ??= new ConfigurationManager<OpenIdConnectConfiguration>(
            "https://accounts.google.com/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever(),
            _retriever);

        try
        {
            return await _configManager.GetConfigurationAsync();
        }
        catch
        {
            return null;
        }
    }
}
