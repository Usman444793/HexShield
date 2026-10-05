using System.Net.Http.Headers;
using Blazored.LocalStorage;
namespace HexShield.Client.Services.Auth;
public class JwtAuthorizationHandler : DelegatingHandler
{
    private readonly ILocalStorageService _localStorage;
    private const string TokenKey = "authToken";
    public JwtAuthorizationHandler(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
    {
        var token = await _localStorage.GetItemAsync<string>(TokenKey);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",token);
        }
        return await base.SendAsync(request,cancellationToken);
    }
}