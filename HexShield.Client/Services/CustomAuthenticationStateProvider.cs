using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
namespace HexShield.Client.Services;
public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private const string TokenKey = "authToken";
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());
    public CustomAuthenticationStateProvider(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }
    public override async Task<AuthenticationState>
        GetAuthenticationStateAsync()
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
                return new AuthenticationState(Anonymous);
            }
            var identity = new ClaimsIdentity(jwt.Claims,authenticationType: "Bearer");
            var user = new ClaimsPrincipal(identity);
            return new AuthenticationState(user);
        }
        catch
        {
            return new AuthenticationState(Anonymous);
        }
    }
    public async Task MarkUserAsAuthenticated(string token)
    {
        await _localStorage.SetItemAsync(TokenKey,token);
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
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(Anonymous)));
    }
}