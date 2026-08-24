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

var builder = WebApplication.CreateBuilder(args);
const string corsPolicyName = "FrontendCors";
var allowedCorsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddControllers(options => options.Filters.Add<ActionLoggingFilter>());
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
builder.Services.AddScoped<FashionStore.API.Features.Products.ProductOperations>();
builder.Services.AddScoped<FashionStore.API.Features.Brands.BrandOperations>();
builder.Services.AddScoped<FashionStore.API.Features.Categories.CategoryOperations>();
builder.Services.AddScoped<FashionStore.API.Features.CatalogOptions.CatalogOptionOperations>();
builder.Services.AddScoped<FashionStore.API.Features.MainCarousels.MainCarouselOperations>();
builder.Services.AddScoped<FashionStore.API.Features.PromotionBanners.PromotionBannerOperations>();
builder.Services.AddScoped<FashionStore.API.Features.PromotionVideos.PromotionVideoOperations>();
builder.Services.AddScoped<FashionStore.API.Features.Users.UserOperations>();
builder.Services.AddScoped<FashionStore.API.Features.Auth.AuthOperations>();
builder.Services.AddInfrastructureServices();

var app = builder.Build();

app.UseSerilogRequestLogging();

var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
await DatabaseInitializer.InitializeAsync(app.Services, app.Configuration, startupLogger);

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
//app.UseHttpsRedirection();                 
app.UseCors(corsPolicyName);               
app.UseAuthentication();                   
app.UseAuthorization();                    
app.MapControllers();
app.Run();
