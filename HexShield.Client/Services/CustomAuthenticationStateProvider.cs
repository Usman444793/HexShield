using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using HexShield.Shared.DTOs;
using Microsoft.AspNetCore.Components.Authorization;

namespace HexShield.Client.Services;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private const string TokenKey = "authToken";
    private const string RefreshTokenKey = "refreshToken";
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

    public CustomAuthenticationStateProvider(
        ILocalStorageService localStorage,
        IHttpClientFactory httpClientFactory)
    {
        _localStorage = localStorage;
        _httpClientFactory = httpClientFactory;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>(TokenKey);
            if (string.IsNullOrWhiteSpace(token))
            {
                // If access token is missing but a refresh token exists, try refreshing
                var storedRefresh = await _localStorage.GetItemAsync<string>(RefreshTokenKey);
                if (!string.IsNullOrWhiteSpace(storedRefresh))
                {
                    token = await TryRefreshTokenAsync();
                }

                if (string.IsNullOrWhiteSpace(token))
                {
                    return new AuthenticationState(Anonymous);
                }
            }

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            // If token is expired or expiring within 30 seconds, transparently refresh it
            if (jwt.ValidTo <= DateTime.UtcNow.AddSeconds(30))
            {
                var refreshedToken = await TryRefreshTokenAsync();
                if (string.IsNullOrWhiteSpace(refreshedToken))
                {
                    await _localStorage.RemoveItemAsync(TokenKey);
                    await _localStorage.RemoveItemAsync(RefreshTokenKey);
                    return new AuthenticationState(Anonymous);
                }

                token = refreshedToken;
                jwt = handler.ReadJwtToken(token);
            }

            var user = BuildClaimsPrincipal(jwt);
            return new AuthenticationState(user);
        }
        catch
        {
            return new AuthenticationState(Anonymous);
        }
    }

    public async Task<string?> TryRefreshTokenAsync(bool forceRefresh = false)
    {
        await _refreshLock.WaitAsync();
        try
        {
            var currentAccessToken = await _localStorage.GetItemAsync<string>(TokenKey);
            var currentRefreshToken = await _localStorage.GetItemAsync<string>(RefreshTokenKey);

            if (string.IsNullOrWhiteSpace(currentRefreshToken))
            {
                return null;
            }

            // Double-check inside lock: if another concurrent call already refreshed it and !forceRefresh, reuse it
            if (!forceRefresh && !string.IsNullOrWhiteSpace(currentAccessToken))
            {
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jwt = handler.ReadJwtToken(currentAccessToken);
                    if (jwt.ValidTo > DateTime.UtcNow.AddMinutes(1))
                    {
                        return currentAccessToken;
                    }
                }
                catch
                {
                }
            }

            var authClient = _httpClientFactory.CreateClient("HexShieldAuth");
            var requestDto = new RefreshTokenRequestDto(
                currentAccessToken ?? string.Empty,
                currentRefreshToken);

            var response = await authClient.PostAsJsonAsync("api/auth/refresh-token", requestDto);
            if (!response.IsSuccessStatusCode)
            {
                await _localStorage.RemoveItemAsync(TokenKey);
                await _localStorage.RemoveItemAsync(RefreshTokenKey);
                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(Anonymous)));
                return null;
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var authResult = JsonSerializer.Deserialize<AuthResponseDto>(
                responseBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (authResult == null || string.IsNullOrWhiteSpace(authResult.AccessToken))
            {
                return null;
            }

            await _localStorage.SetItemAsync(TokenKey, authResult.AccessToken);
            if (!string.IsNullOrWhiteSpace(authResult.RefreshToken))
            {
                await _localStorage.SetItemAsync(RefreshTokenKey, authResult.RefreshToken);
            }

            var newJwt = new JwtSecurityTokenHandler().ReadJwtToken(authResult.AccessToken);
            var principal = BuildClaimsPrincipal(newJwt);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));

            return authResult.AccessToken;
        }
        catch
        {
            return null;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public async Task<string?> GetAccessTokenAsync()
        => await _localStorage.GetItemAsync<string>(TokenKey);

    public async Task<string?> GetRefreshTokenAsync()
        => await _localStorage.GetItemAsync<string>(RefreshTokenKey);

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

    public async Task RevokeAndLogoutAsync()
    {
        try
        {
            var refreshToken = await _localStorage.GetItemAsync<string>(RefreshTokenKey);
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                var authClient = _httpClientFactory.CreateClient("HexShieldAuth");
                await authClient.PostAsJsonAsync("api/auth/revoke-token", new RevokeTokenRequestDto(refreshToken));
            }
        }
        catch
        {
        }
        finally
        {
            await MarkUserAsLoggedOut();
        }
    }

    public async Task MarkUserAsLoggedOut()
    {
        await _localStorage.RemoveItemAsync(TokenKey);
        await _localStorage.RemoveItemAsync(RefreshTokenKey);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(Anonymous)));
    }

    private static ClaimsPrincipal BuildClaimsPrincipal(JwtSecurityToken jwt)
    {
        var claims = new List<Claim>();
        foreach (var claim in jwt.Claims)
        {
            claims.Add(claim);

            if (claim.Type == "role" || claim.Type == ClaimTypes.Role)
            {
                claims.Add(new Claim(ClaimTypes.Role, claim.Value));
                if (claim.Type != "role")
                {
                    claims.Add(new Claim("role", claim.Value));
                }
            }

            if (claim.Type == "fullName" || claim.Type == "name" || claim.Type == "unique_name" || claim.Type == ClaimTypes.Name)
            {
                claims.Add(new Claim(ClaimTypes.Name, claim.Value));
            }

            if (claim.Type == "sub" || claim.Type == ClaimTypes.NameIdentifier)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, claim.Value));
            }
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "Bearer", nameType: ClaimTypes.Name, roleType: ClaimTypes.Role);
        return new ClaimsPrincipal(identity);
    }
}
