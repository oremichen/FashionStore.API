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
using FashionStore.Application;

var builder = WebApplication.CreateBuilder(args);
const string corsPolicyName = "FrontendCors";
var allowedCorsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddControllers();
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
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        options => options.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)));

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
    options.TokenLifespan = TimeSpan.FromDays(1));
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
                    if (user.IsDeactivated)
                    {
                        logger.LogWarning("Deactivated user {UserId} attempted access.", user.Id);
                        context.Fail("Account is deactivated.");
                        return;
                    }

                    // ── Fetch roles from Identity ──────────────────────────
                    var roles = await userManager.GetRolesAsync(user);

                    var claims = new List<System.Security.Claims.Claim>
                    {
                        new(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id),
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
                    logger.LogWarning("User {UserId} not found during token validation.", userIdClaim.Value);
                    context.Fail("User not found.");
                }
            }
            else
            {
                logger.LogWarning("User ID claim not found during token validation.");
                context.Fail("Invalid token claims.");
            }
        },

        OnChallenge = context =>
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILogger<JwtBearerEvents>>();
            logger.LogWarning("OnChallenge: {ErrorDescription}. Authentication failed.", context.ErrorDescription);

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
            logger.LogWarning("OnForbidden: Access to {Path} forbidden.", context.HttpContext.Request.Path);

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

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices();

var app = builder.Build();

app.UseSerilogRequestLogging();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
    var context = scope.ServiceProvider.GetRequiredService<FashionStoreDbContext>();
    await Seed.SeedData(context, roleManager, app.Configuration);
}

if (app.Environment.IsDevelopment())
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
