using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace HexShield.Client.Services;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;

    private string? _accessToken;

    private const string TokenStorageKey = "hexshield_access_token";

    public CustomAuthenticationStateProvider(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // Try to restore token from localStorage when available
        if (string.IsNullOrWhiteSpace(_accessToken))
        {
            try
            {
                _accessToken = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TokenStorageKey);
            }
            catch
            {
                // ignore JS interop failures
            }
        }

        if (string.IsNullOrWhiteSpace(_accessToken))
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        try
        {
            var claims = ParseClaimsFromJwt(_accessToken);

            var identity = new ClaimsIdentity(
                claims,
                authenticationType: "Bearer",
                nameType: ClaimTypes.Name,
                roleType: ClaimTypes.Role);

            var user = new ClaimsPrincipal(identity);

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _accessToken);

            return new AuthenticationState(user);
        }
        catch
        {
            _accessToken = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;

            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    public void MarkUserAsAuthenticated(string accessToken)
    {
        _accessToken = accessToken;

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var claims = ParseClaimsFromJwt(accessToken);

        var identity = new ClaimsIdentity(
            claims,
            authenticationType: "Bearer",
            nameType: ClaimTypes.Name,
            roleType: ClaimTypes.Role);

        var user = new ClaimsPrincipal(identity);

        NotifyAuthenticationStateChanged(
            Task.FromResult(
                new AuthenticationState(user)));
        // Persist token to localStorage so auth survives page refresh
        try
        {
            _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenStorageKey, accessToken);
        }
        catch
        {
            // ignore persisting errors
        }
    }

    public void MarkUserAsLoggedOut()
    {
        _accessToken = null;

        _httpClient.DefaultRequestHeaders.Authorization = null;

        var anonymous = new ClaimsPrincipal(
            new ClaimsIdentity());

        NotifyAuthenticationStateChanged(
            Task.FromResult(
                new AuthenticationState(anonymous)));
        try
        {
            _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenStorageKey);
        }
        catch
        {
            // ignore
        }
    }

    public string? GetAccessToken()
    {
        return _accessToken;
    }

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();

        var parts = jwt.Split('.');

        if (parts.Length != 3)
            return claims;

        var payload = parts[1];

        var jsonBytes = ParseBase64WithoutPadding(payload);

        var keyValuePairs =
            JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                jsonBytes);

        if (keyValuePairs == null)
            return claims;

        foreach (var pair in keyValuePairs)
        {
            if (pair.Key == "role" || pair.Key == ClaimTypes.Role)
            {
                if (pair.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var role in pair.Value.EnumerateArray())
                    {
                        if (role.ValueKind == JsonValueKind.String)
                        {
                            claims.Add(
                                new Claim(
                                    ClaimTypes.Role,
                                    role.GetString()!));
                        }
                    }
                }
                else if (pair.Value.ValueKind == JsonValueKind.String)
                {
                    claims.Add(
                        new Claim(
                            ClaimTypes.Role,
                            pair.Value.GetString()!));
                }

                continue;
            }

            if (pair.Value.ValueKind == JsonValueKind.String)
            {
                claims.Add(
                    new Claim(
                        pair.Key,
                        pair.Value.GetString()!));
            }
        }

        return claims;
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        base64 = base64.Replace('-', '+')
                       .Replace('_', '/');

        switch (base64.Length % 4)
        {
            case 2:
                base64 += "==";
                break;

            case 3:
                base64 += "=";
                break;
        }

        return Convert.FromBase64String(base64);
    }
}