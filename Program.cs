using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SolarneApi;
using SolarneApi.Models;
using SolarneApi.Persistance;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy => 
        policy
            .WithOrigins("https://solarne.com.br")
            .AllowAnyHeader()
            .AllowAnyMethod()
    );
});

builder.Services.AddDbContext<ApiContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"));
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"] ?? string.Empty)),
        ValidateLifetime = true,
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app
    .MapAuthEndpoints(builder.Configuration)
    .MapSolutionsEndpoints()
    .MapContactsEndpoints();

await using(var scope = app.Services.CreateAsyncScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

    await context.Database.MigrateAsync();

    if (!context.Users.Any())
    {
        var adminEmail = builder.Configuration["AdminSettings:AdminEmail"];
        var adminPassword = builder.Configuration["AdminSettings:AdminPassword"];

        if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
        {   
            var admin = new User(
                adminEmail,
                BCrypt.Net.BCrypt.HashPassword(adminPassword)
            );

            await context.Users.AddAsync(admin);
            await context.SaveChangesAsync();
        }
    }
}

app.Run();
