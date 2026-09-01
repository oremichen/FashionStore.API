using System.Text;
using FashionStore.API.Middleware;
using FashionStore.Domain.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using FashionStore.Infrastructure.Data;
using FashionStore.Infrastructure.Seed;
using FashionStore.Infrastructure;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using FashionStore.Shared.Constants;
using FashionStore.API.Filters;
using FashionStore.API.Caching;
using FashionStore.API.RateLimiting;
using FashionStore.API.Features.Auth;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpContextAccessor();
var vercelEnvironment = Environment.GetEnvironmentVariable("VERCEL");
var isVercel = string.Equals(vercelEnvironment, "1", StringComparison.OrdinalIgnoreCase)
    || string.Equals(vercelEnvironment, "true", StringComparison.OrdinalIgnoreCase);
const string corsPolicyName = "FrontendCors";
var allowedCorsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();

    if (isVercel)
    {
        // Vercel captures process stdout/stderr; its file system is ephemeral.
        configuration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .WriteTo.Console(outputTemplate:
                "{Timestamp:HH:mm:ss.fff} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");
    }
    else
    {
        configuration.ReadFrom.Configuration(context.Configuration);
    }

    var betterStackSourceToken = context.Configuration["BetterStack:SourceToken"];
    var betterStackEndpoint = context.Configuration["BetterStack:Endpoint"];
    if (!string.IsNullOrWhiteSpace(betterStackSourceToken) &&
        !string.IsNullOrWhiteSpace(betterStackEndpoint))
    {
        configuration.WriteTo.BetterStack(
            sourceToken: betterStackSourceToken,
            betterStackEndpoint: betterStackEndpoint);
    }
});

builder.Services
    .AddControllers(options => options.Filters.Add<ActionLoggingFilter>())
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("ModelValidation");
            var errors = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors
                        .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                            ? error.Exception?.Message ?? "The supplied value is invalid."
                            : error.ErrorMessage)
                        .ToArray());

            logger.LogWarning(
                "Model validation rejected {Method} {Path}. Errors: {@ValidationErrors}",
                context.HttpContext.Request.Method,
                context.HttpContext.Request.Path,
                errors);

            var problem = new ValidationProblemDetails(context.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred."
            };
            problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
            return new BadRequestObjectResult(problem);
        };
    });

#region RATE LIMITING
builder.Services.AddRateLimiter(options =>
{
    static string ClientKey(HttpContext context)
    {
        return context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown-client";
    }

    static RateLimitPartition<string> FixedWindow(HttpContext context, string policy, int permitLimit)
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            $"{policy}:{ClientKey(context)}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    }

    // A generous safety net for every endpoint without a more specific policy.
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        FixedWindow(context, "global", 300));

    options.AddPolicy(RateLimitPolicies.Authentication, context => FixedWindow(context, RateLimitPolicies.Authentication, 5));
    options.AddPolicy(RateLimitPolicies.Registration, context => FixedWindow(context, RateLimitPolicies.Registration, 4));
    options.AddPolicy(RateLimitPolicies.Submissions, context => FixedWindow(context, RateLimitPolicies.Submissions, 8));
    options.AddPolicy(RateLimitPolicies.ProductListing, context => FixedWindow(context, RateLimitPolicies.ProductListing, 100));
    options.AddPolicy(RateLimitPolicies.Cart, context => FixedWindow(context, RateLimitPolicies.Cart, 45));
    options.AddPolicy(RateLimitPolicies.Checkout, context => FixedWindow(context, RateLimitPolicies.Checkout, 8));
    options.AddPolicy(RateLimitPolicies.AdminUpload, context => FixedWindow(context, RateLimitPolicies.AdminUpload, 8));

    // Webhooks get an isolated, higher-throughput policy. Signature validation and
    // idempotency must remain the primary protections when a webhook is introduced.
    options.AddPolicy(RateLimitPolicies.PaymentWebhook, context =>
        FixedWindow(context, RateLimitPolicies.PaymentWebhook, 120));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString();

        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            StatusCode = StatusCodes.Status429TooManyRequests,
            Message = "Too many requests. Please try again later."
        }, cancellationToken);
    };
});
#endregion

builder.Services.AddRedisResponseCaching(builder.Configuration);
builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicyName, policy =>
    {
        policy.WithOrigins(allowedCorsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

#region Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FashionStore API",
        Version = "v1",
        Description = "REST API for FashionStore ecommerce platform",
    });

    // JWT Bearer auth button in Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token below. Example: eyJhbGci..."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
#endregion

#region Identity & EF Core
builder.Services.AddDbContext<FashionStoreDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        options => options.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null)));

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.SignIn.RequireConfirmedEmail = true;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 8;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.User.RequireUniqueEmail = true;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
})
.AddEntityFrameworkStores<FashionStoreDbContext>()
.AddDefaultTokenProviders();

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
    options.TokenLifespan = TimeSpan.FromDays(5));
#endregion

#region JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Secret"]!))
    };

    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var userManager = context.HttpContext.RequestServices
                .GetRequiredService<UserManager<ApplicationUser>>();
            var dbContext = context.HttpContext.RequestServices
                .GetRequiredService<FashionStoreDbContext>();
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILogger<JwtBearerEvents>>();

            var userIdClaim = context.Principal?.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier);

            if (userIdClaim != null)
            {
                var user = await userManager.FindByIdAsync(userIdClaim.Value);

                if (user != null)
                {
                    var tokenId = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                    if (string.IsNullOrWhiteSpace(tokenId))
                    {
                        logger.LogError("Token validation failed because the token id claim was missing for user {UserId}.", user.Id);
                        context.Fail("Invalid token.");
                        return;
                    }

                    var sessionId = context.Principal?.FindFirst("sid")?.Value;
                    var session = string.IsNullOrWhiteSpace(sessionId)
                        ? null
                        : await dbContext.UserSessions.SingleOrDefaultAsync(item => item.Id == sessionId && item.UserId == user.Id);
                    var now = DateTimeOffset.UtcNow;
                    if (session is null || session.RevokedAtUtc is not null || session.AbsoluteExpiresAtUtc <= now ||
                        session.IdleExpiresAtUtc <= now || session.SecurityStamp != (user.SecurityStamp ?? string.Empty))
                    {
                        logger.LogWarning("Rejected access token for inactive session {SessionId}.", sessionId);
                        context.Fail("Session is no longer active.");
                        return;
                    }

                    var revokedToken = await userManager.GetAuthenticationTokenAsync(
                        user,
                        AuthTokenConstants.JwtLoginProvider,
                        $"{AuthTokenConstants.RevokedTokenPrefix}{tokenId}");

                    if (!string.IsNullOrWhiteSpace(revokedToken))
                    {
                        logger.LogError("Rejected revoked token {TokenId} for user {UserId}.", tokenId, user.Id);
                        context.Fail("Token has been revoked.");
                        return;
                    }

                    if (user.IsDeactivated)
                    {
                        logger.LogError("Deactivated user {UserId} attempted access.", user.Id);
                        context.Fail("Account is deactivated.");
                        return;
                    }

                    // ── Fetch roles from Identity ──────────────────────────
                    var roles = await userManager.GetRolesAsync(user);

                    if (roles.Any(SessionPolicy.IsAdminRole))
                    {
                        session.LastUsedAtUtc = now;
                        session.IdleExpiresAtUtc = now.Add(SessionPolicy.AdminIdleLifetime);
                        await dbContext.SaveChangesAsync();
                    }

                    var claims = new List<System.Security.Claims.Claim>
                    {
                        new(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id),
                        new(System.Security.Claims.ClaimTypes.Name, user.UserName ?? user.Email ?? string.Empty),
                        new(System.Security.Claims.ClaimTypes.Email, user.Email!),
                        new(System.Security.Claims.ClaimTypes.GivenName, user.FirstName ?? string.Empty),
                        new(System.Security.Claims.ClaimTypes.Surname, user.LastName ?? string.Empty),
                    };

                    // Add each role as a separate claim
                    claims.AddRange(roles.Select(role =>
                        new System.Security.Claims.Claim(
                            System.Security.Claims.ClaimTypes.Role, role)));

                    var appIdentity = new System.Security.Claims.ClaimsIdentity(claims);
                    context.Principal?.AddIdentity(appIdentity);

                    // ── Update last login ──────────────────────────────────
                    user.LastLoginDate = DateTimeOffset.UtcNow;
                    await userManager.UpdateAsync(user);

                    logger.LogInformation(
                        "Token validated for user: {Email} with roles: {Roles}",
                        user.Email, string.Join(", ", roles));
                }
                else
                {
                    logger.LogError("User {UserId} not found during token validation.", userIdClaim.Value);
                    context.Fail("User not found.");
                }
            }
            else
            {
                logger.LogError("User ID claim not found during token validation.");
                context.Fail("Invalid token claims.");
            }
        },

        OnChallenge = context =>
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILogger<JwtBearerEvents>>();
            logger.LogError("OnChallenge: {ErrorDescription}. Authentication failed.", context.ErrorDescription);

            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

            var response = System.Text.Json.JsonSerializer.Serialize(new
            {
                StatusCode = 401,
                Message = "You are not authorized. Please provide a valid token."
            });

            return context.Response.WriteAsync(response);
        },

        OnForbidden = context =>
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILogger<JwtBearerEvents>>();
            logger.LogError("OnForbidden: Access to {Path} forbidden.", context.HttpContext.Request.Path);

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";

            var response = System.Text.Json.JsonSerializer.Serialize(new
            {
                StatusCode = 403,
                Message = "You do not have permission to access this resource."
            });

            return context.Response.WriteAsync(response);
        }
    };
});
#endregion

builder.Services.Scan(scan => scan
    .FromAssemblyOf<Program>()
    .AddClasses(classes => classes.Where(type =>
        type.Name.EndsWith("Service") &&
        type.Namespace is not null &&
        type.Namespace.Contains("Features")))
    .AsMatchingInterface()
    .WithScopedLifetime());
builder.Services.AddInfrastructureServices();

var app = builder.Build();
app.Lifetime.ApplicationStopped.Register(Log.CloseAndFlush);

app.UseSerilogRequestLogging();

var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
if (isVercel)
{
    startupLogger.LogInformation("Skipping database migration and seed initialization on Vercel.");
}
else
{
    await DatabaseInitializer.InitializeAsync(app.Services, app.Configuration, startupLogger);
}

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "FashionStore API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseMiddleware<GlobalErrorMiddleware>(); 
app.UseMiddleware<RequestPayloadLoggingMiddleware>();
//app.UseHttpsRedirection();                 
app.UseCors(corsPolicyName);               
app.UseAuthentication();                   
app.UseAuthorization();                    
app.UseRateLimiter();
app.UseMiddleware<RedisResponseCacheMiddleware>();
app.MapControllers();
app.MapGet("/", () => Results.Ok(new
{
    service = "FashionStore API",
    status = "running"
}));
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy"
}));
app.Run();
