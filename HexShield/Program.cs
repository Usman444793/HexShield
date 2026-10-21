using HexShield.Client.Pages;
using HexShield.Client.Services;
using HexShield.Components;
using HexShield.Data;
using HexShield.Infrastructure;
using HexShield.Infrastructure.Authorization;
using HexShield.Infrastructure.Middleware;
using HexShield.Infrastructure.Tenancy;
using HexShield.Models.Identity;
using HexShield.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IO.Compression;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ── Database Configuration ───────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Default Connection not found");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
        sqlOptions.CommandTimeout(30);
    });
    // Standard tracking enabled for reliable entity updates and concurrency tokens (RowVersion)
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// ── Identity Configuration ───────────────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.Password.RequiredLength = 12;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.User.RequireUniqueEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ── JWT Authentication ────────────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("Jwt:SecretKey was not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer was not configured");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience was not configured");
var keybytes = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = false;
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = ctx =>
        {
            var logger = ctx.HttpContext.RequestServices.GetService<ILoggerFactory>()?.CreateLogger("JwtBearerEvents");
            logger?.LogDebug("OnMessageReceived: Scheme={Scheme} Path={Path}", ctx.Request?.Scheme, ctx.Request?.Path);
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = ctx =>
        {
            var logger = ctx.HttpContext.RequestServices.GetService<ILoggerFactory>()?.CreateLogger("JwtBearerEvents");
            logger?.LogWarning(ctx.Exception, "JWT authentication failed");
            return Task.CompletedTask;
        },
        OnTokenValidated = ctx =>
        {
            var logger = ctx.HttpContext.RequestServices.GetService<ILoggerFactory>()?.CreateLogger("JwtBearerEvents");
            var sub = ctx.Principal?.FindFirst("sub")?.Value;
            logger?.LogInformation("Token validated for sub={Sub}", sub);
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            var logger = context.HttpContext.RequestServices.GetService<ILoggerFactory>()?.CreateLogger("JwtBearerEvents");
            logger?.LogWarning("JwtBearer challenge for {Path}", context.Request.Path);
            return Task.CompletedTask;
        }
    };
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(keybytes),
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});

// ── Authorization Policies ───────────────────────────────────────────────────
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser())
    .AddPolicy("AdminOnly", policy => policy.RequireRole(HexShield.Shared.Enums.LmsRoles.Admin))
    .AddPolicy("TeacherOnly", policy => policy.RequireRole(HexShield.Shared.Enums.LmsRoles.Teacher))
    .AddPolicy("StudentOnly", policy => policy.RequireRole(HexShield.Shared.Enums.LmsRoles.Student))
    .AddPolicy("AdminOrTeacher", policy => policy.RequireRole(HexShield.Shared.Enums.LmsRoles.Admin, HexShield.Shared.Enums.LmsRoles.Teacher));

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped(sp =>
{
    var httpContext = sp.GetRequiredService<IHttpContextAccessor>().HttpContext;
    if (httpContext != null)
    {
        return new HttpClient { BaseAddress = new Uri($"{httpContext.Request.Scheme}://{httpContext.Request.Host}/") };
    }
    return new HttpClient { BaseAddress = new Uri("http://localhost:5282/") };
});

// ── Application & Domain Services ─────────────────────────────────────────────
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<ILessonService, LessonService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<IAssignmentService, AssignmentService>();
builder.Services.AddScoped<IQuizService, QuizService>();
builder.Services.AddScoped<IProgressAndAttendanceService, ProgressAndAttendanceService>();

builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();

// ── Diagnostics & Security ────────────────────────────────────────────────────
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("AuthPolicy", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 10;
        opt.QueueLimit = 0;
    });
    options.AddPolicy("ApiPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 100,
                QueueLimit = 10
            }));
});

builder.Services.AddDataProtection().SetApplicationName("HexShield").PersistKeysToDbContext<ApplicationDbContext>(); 
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(b => b.Expire(TimeSpan.FromMinutes(5)));
});

builder.Services.AddControllers();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultAllowLocalDebug", policy =>
    {
        policy.WithOrigins("https://localhost:7263", "http://localhost:5282")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
builder.Services.AddAntiforgery();

if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddHsts(options =>
    {
        options.MaxAge = TimeSpan.FromDays(365);
        options.IncludeSubDomains = true;
        options.Preload = true;
    });
}

var app = builder.Build();

// ── HTTP Request Pipeline ─────────────────────────────────────────────────────
app.UseExceptionHandler();
app.UseMiddleware<ConcurrencyExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

if (!app.Environment.IsDevelopment())
{
    app.UseResponseCompression();
}

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();
app.UseCors("DefaultAllowLocalDebug");

app.UseRateLimiter();
app.UseOutputCache();

app.UseAuthentication();
app.UseMiddleware<TenantResolverMiddleware>();
app.UseAuthorization();
app.UseAntiforgery();

var staticAssetsManifestPath = Path.Combine(AppContext.BaseDirectory, "HexShield.staticwebassets.endpoints.json");
if (File.Exists(staticAssetsManifestPath))
{
    app.MapStaticAssets(staticAssetsManifestPath);
}

app.MapControllers().RequireRateLimiting("ApiPolicy");

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(HexShield.Client._Imports).Assembly);

await DbInitializer.InitializeAsync(app.Services);

app.Run();
