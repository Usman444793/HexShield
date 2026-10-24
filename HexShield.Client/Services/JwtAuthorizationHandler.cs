using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using Blazored.LocalStorage;

namespace HexShield.Client.Services.Auth;

public class JwtAuthorizationHandler : DelegatingHandler
{
    private readonly ILocalStorageService _localStorage;
    private readonly CustomAuthenticationStateProvider _authStateProvider;
    private const string TokenKey = "authToken";

    public JwtAuthorizationHandler(
        ILocalStorageService localStorage,
        CustomAuthenticationStateProvider authStateProvider)
    {
        _localStorage = localStorage;
        _authStateProvider = authStateProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        var isAuthEndpoint = path.Contains("/api/auth/login", StringComparison.OrdinalIgnoreCase)
                          || path.Contains("/api/auth/register", StringComparison.OrdinalIgnoreCase)
                          || path.Contains("/api/auth/refresh-token", StringComparison.OrdinalIgnoreCase);

        var token = await _localStorage.GetItemAsync<string>(TokenKey, cancellationToken);

        // Proactively refresh token if expired or expiring within 30 seconds
        if (!isAuthEndpoint && !string.IsNullOrWhiteSpace(token))
        {
            try
            {
                var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
                if (jwt.ValidTo <= DateTime.UtcNow.AddSeconds(30))
                {
                    var refreshed = await _authStateProvider.TryRefreshTokenAsync();
                    if (!string.IsNullOrWhiteSpace(refreshed))
                    {
                        token = refreshed;
                    }
                }
            }
            catch
            {
            }
        }

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        // Reactive refresh & retry if server returns 401 Unauthorized
        if (!isAuthEndpoint && response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var newToken = await _authStateProvider.TryRefreshTokenAsync(forceRefresh: true);
            if (!string.IsNullOrWhiteSpace(newToken))
            {
                using var retryRequest = await CloneHttpRequestMessageAsync(request);
                retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
                response.Dispose();
                return await base.SendAsync(retryRequest, cancellationToken);
            }
        }

        return response;
    }

    private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version
        };

        if (request.Content != null)
        {
            var contentBytes = await request.Content.ReadAsByteArrayAsync();
            var byteContent = new ByteArrayContent(contentBytes);

            foreach (var header in request.Content.Headers)
            {
                byteContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            clone.Content = byteContent;
        }

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }
}
