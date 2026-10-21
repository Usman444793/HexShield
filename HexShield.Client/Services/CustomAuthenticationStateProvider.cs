using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace HexShield.Client.Services;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private const string TokenKey = "authToken";
    private const string RefreshTokenKey = "refreshToken";
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

    public CustomAuthenticationStateProvider(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>(TokenKey);
            if (string.IsNullOrWhiteSpace(token))
            {
                return new AuthenticationState(Anonymous);
            }

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            if (jwt.ValidTo <= DateTime.UtcNow)
            {
                await _localStorage.RemoveItemAsync(TokenKey);
                await _localStorage.RemoveItemAsync(RefreshTokenKey);
                return new AuthenticationState(Anonymous);
            }

            var claims = new List<Claim>();
            foreach (var claim in jwt.Claims)
            {
                claims.Add(claim);

                // Map role claims to both short-form and standard ClaimTypes.Role
                if (claim.Type == "role" || claim.Type == ClaimTypes.Role)
                {
                    claims.Add(new Claim(ClaimTypes.Role, claim.Value));
                    if (claim.Type != "role")
                    {
                        claims.Add(new Claim("role", claim.Value));
                    }
                }

                // Map name claims to standard ClaimTypes.Name
                if (claim.Type == "fullName" || claim.Type == "name" || claim.Type == "unique_name" || claim.Type == ClaimTypes.Name)
                {
                    claims.Add(new Claim(ClaimTypes.Name, claim.Value));
                }

                // Map sub/NameIdentifier
                if (claim.Type == "sub" || claim.Type == ClaimTypes.NameIdentifier)
                {
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, claim.Value));
                }
            }

            var identity = new ClaimsIdentity(claims, authenticationType: "Bearer", nameType: ClaimTypes.Name, roleType: ClaimTypes.Role);
            var user = new ClaimsPrincipal(identity);
            return new AuthenticationState(user);
        }
        catch
        {
            return new AuthenticationState(Anonymous);
        }
    }

    public async Task MarkUserAsAuthenticated(string token, string? refreshToken = null)
    {
        await _localStorage.SetItemAsync(TokenKey, token);
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            await _localStorage.SetItemAsync(RefreshTokenKey, refreshToken);
        }
        var authenticationState = await GetAuthenticationStateAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(authenticationState));
    }

    public async Task InitializeFromStorageAsync()
    {
        var authenticationState = await GetAuthenticationStateAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(authenticationState));
    }

    public async Task MarkUserAsLoggedOut()
    {
        await _localStorage.RemoveItemAsync(TokenKey);
        await _localStorage.RemoveItemAsync(RefreshTokenKey);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(Anonymous)));
    }
}