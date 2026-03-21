using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


//services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")
//              , options => options.EnableRetryOnFailure(
//                maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null)));

//services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
//{
//    options.SignIn.RequireConfirmedEmail = true;
//    options.Lockout.AllowedForNewUsers = true;
//    options.Lockout.MaxFailedAccessAttempts = 3;
//    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
//    options.User.RequireUniqueEmail = true;
//    options.Password.RequireDigit = true;
//    options.Password.RequiredLength = 8;
//    options.Password.RequiredUniqueChars = 1;
//    options.Password.RequireUppercase = true;
//    options.Password.RequireLowercase = true;
//    options.Password.RequireNonAlphanumeric = true;
//}).AddEntityFrameworkStores<ApplicationDbContext>().
//AddDefaultTokenProviders();

//services.Configure<DataProtectionTokenProviderOptions>(options =>
//    options.TokenLifespan = TimeSpan.FromDays(5));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
