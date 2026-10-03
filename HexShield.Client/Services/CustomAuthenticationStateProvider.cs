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
    private bool _storageInitialized;

    private const string TokenStorageKey = "hexshield_access_token";

    public CustomAuthenticationStateProvider(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (string.IsNullOrWhiteSpace(_accessToken))
        {
            try
            {
                _accessToken = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenStorageKey);
                _storageInitialized = true;
            }
            catch (InvalidOperationException)
            {
                // Prerendering: JavaScript interop is not available yet.
                return Anonymous();
            }
            catch (JSDisconnectedException)
            {
                return Anonymous();
            }
            catch
            {
                return Anonymous();
            }
        }

        if (string.IsNullOrWhiteSpace(_accessToken))
        {
            ClearAuthorizationHeader();
            return Anonymous();
        }

        return CreateAuthenticationState(_accessToken);
    }

    public async Task MarkUserAsAuthenticated(string accessToken)
    {
        await ApplyTokenAsync(accessToken, persist: true);
    }

    public async Task MarkUserAsLoggedOut()
    {
        _accessToken = null;
        ClearAuthorizationHeader();

        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous()));

        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenStorageKey);
        }
        catch (InvalidOperationException)
        {
        }
        catch (JSDisconnectedException)
        {
        }
        catch
        {
        }
    }

    public async Task InitializeFromStorageAsync()
    {
        if (_storageInitialized && !string.IsNullOrWhiteSpace(_accessToken))
        {
            return;
        }

        try
        {
            var stored = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenStorageKey);
            _storageInitialized = true;

            if (string.IsNullOrWhiteSpace(stored))
            {
                if (!string.IsNullOrWhiteSpace(_accessToken))
                {
                    return;
                }

                ClearAuthorizationHeader();
                NotifyAuthenticationStateChanged(Task.FromResult(Anonymous()));
                return;
            }

            if (string.Equals(_accessToken, stored, StringComparison.Ordinal)
                && _httpClient.DefaultRequestHeaders.Authorization is not null)
            {
                return;
            }

            await ApplyTokenAsync(stored, persist: false);
        }
        catch (InvalidOperationException)
        {
        }
        catch (JSDisconnectedException)
        {
        }
        catch
        {
        }
    }

    public string? GetAccessToken()
    {
        return _accessToken;
    }

    private async Task ApplyTokenAsync(string accessToken, bool persist)
    {
        _accessToken = accessToken;
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        NotifyAuthenticationStateChanged(Task.FromResult(CreateAuthenticationState(accessToken)));

        if (!persist)
        {
            return;
        }

        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenStorageKey, accessToken);
            _storageInitialized = true;
        }
        catch (InvalidOperationException)
        {
        }
        catch (JSDisconnectedException)
        {
        }
        catch
        {
        }
    }

    private AuthenticationState CreateAuthenticationState(string accessToken)
    {
        try
        {
            var claims = ParseClaimsFromJwt(accessToken);
            var identity = new ClaimsIdentity(
                claims,
                authenticationType: "Bearer",
                nameType: ClaimTypes.Name,
                roleType: ClaimTypes.Role);

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch
        {
            _accessToken = null;
            ClearAuthorizationHeader();
            return Anonymous();
        }
    }

    private void ClearAuthorizationHeader()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    private static AuthenticationState Anonymous()
    {
        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();
        var parts = jwt.Split('.');
        if (parts.Length != 3)
        {
            return claims;
        }

        var jsonBytes = ParseBase64WithoutPadding(parts[1]);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonBytes);
        if (keyValuePairs is null)
        {
            return claims;
        }

        foreach (var pair in keyValuePairs)
        {
            if (IsRoleClaim(pair.Key))
            {
                AddRoleClaims(claims, pair.Value);
                continue;
            }

            if (pair.Value.ValueKind == JsonValueKind.String)
            {
                var claimType = pair.Key switch
                {
                    "sub" => ClaimTypes.NameIdentifier,
                    "email" => ClaimTypes.Email,
                    "unique_name" => ClaimTypes.Name,
                    "name" => ClaimTypes.Name,
                    _ => pair.Key
                };

                var value = pair.Value.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    claims.Add(new Claim(claimType, value));
                }
            }
        }

        if (!claims.Any(c => c.Type == ClaimTypes.Name))
        {
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (!string.IsNullOrWhiteSpace(email))
            {
                claims.Add(new Claim(ClaimTypes.Name, email));
            }
        }

        return claims;
    }

    private static bool IsRoleClaim(string key)
    {
        return key is "role" or "roles"
            || key == ClaimTypes.Role
            || key.EndsWith("/role", StringComparison.OrdinalIgnoreCase)
            || key.EndsWith("/roles", StringComparison.OrdinalIgnoreCase);
    }

    private static void AddRoleClaims(List<Claim> claims, JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var role in value.EnumerateArray())
            {
                AddRoleClaims(claims, role);
            }

            return;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            return;
        }

        var raw = value.GetString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        foreach (var role in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        base64 = base64.Replace('-', '+').Replace('_', '/');

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
