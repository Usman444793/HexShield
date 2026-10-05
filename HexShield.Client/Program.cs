using Blazored.LocalStorage;
using HexShield.Client.Services;
using HexShield.Client.Services.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("Authenticated",policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("AdminOnly",policy => policy.RequireRole("Admin"));
    options.AddPolicy("TeacherOnly",policy => policy.RequireRole("Teacher"));
    options.AddPolicy("StudentOnly",policy => policy.RequireRole("Student"));
    options.AddPolicy("AdminOrTeacher",policy => policy.RequireRole("Admin", "Teacher"));
});
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(
    sp => sp.GetRequiredService<CustomAuthenticationStateProvider>());
builder.Services.AddTransient<JwtAuthorizationHandler>();
builder.Services.AddHttpClient("HexShield", client =>
{
    client.BaseAddress = new Uri(
        builder.HostEnvironment.BaseAddress);
})
.AddHttpMessageHandler<JwtAuthorizationHandler>();
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("HexShield"));
var host = builder.Build();
try
{
    var authStateProvider = host.Services.GetRequiredService<CustomAuthenticationStateProvider>();
    await authStateProvider.InitializeFromStorageAsync();
}
catch
{

}
await host.RunAsync();