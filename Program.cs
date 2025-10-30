using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SolarneApi.Auth;
using SolarneApi.Dtos;
using SolarneApi.Models;
using SolarneApi.Persistance;

var builder = WebApplication.CreateBuilder(args);

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

app.UseAuthentication();
app.UseAuthorization();

// ---- AUTH ----------

app.MapPost("api/signup", async (
    [FromBody] UserDto userRequest,
    [FromServices] ApiContext context
) =>
{
    var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Email.Equals(userRequest.Email));
    if (existingUser != null) return Results.BadRequest();

    var hashedPassword = BCrypt.Net.BCrypt.HashPassword(userRequest.Password);
    var user = new User(userRequest.Email, hashedPassword);

    await context.Users.AddAsync(user);
    await context.SaveChangesAsync();

    return Results.Created();
});

app.MapPost("api/login", async (
    [FromBody] UserDto userRequest,
    [FromServices] ApiContext context
) =>
{
    var user = await context.Users.FirstOrDefaultAsync(u => u.Email == userRequest.Email);
    if (user == null) return Results.NotFound();

    if (!BCrypt.Net.BCrypt.Verify(userRequest.Password, user.Password)) return Results.BadRequest();

    var token = new JwtService(builder.Configuration).GenerateToken(user);

    return Results.Ok(new { token });  
});

// ---- Solutions ----------

var solutions = app.MapGroup("api/solutions");

solutions.MapPost("", async (
    [FromBody] SolutionDto solutionRequest,
    [FromServices] ApiContext context
) =>
{
    var solution = new Solution(
        solutionRequest.ImageUrl,
        solutionRequest.Location,
        solutionRequest.Power,
        solutionRequest.AnnualSaving
    );

    await context.Solutions.AddAsync(solution);
    await context.SaveChangesAsync();

    return Results.Created();
}).RequireAuthorization();

solutions.MapGet("", async ([FromServices] ApiContext context) => Results.Ok(await context.Solutions.OrderByDescending(s => s.CreatedAt).ToListAsync()));

solutions.MapDelete("{id}", async (
    [FromRoute] Guid id,
    [FromServices] ApiContext context
) =>
{
    var solution = await context.Solutions.FirstOrDefaultAsync(s => s.Id == id);
    if (solution == null) return Results.NotFound();

    context.Solutions.Remove(solution);
    await context.SaveChangesAsync();

    return Results.NoContent();
}).RequireAuthorization();

// ---- Contacts ----------

var contacts = app.MapGroup("api/contacts");

contacts.MapPost("", async (
    [FromBody] ContactDto contactRequest,
    [FromServices] ApiContext context
) =>
{
    var name = contactRequest.Name;
    var number = contactRequest.Number;

    if (name.Length > 64 || number.Length > 64) return Results.BadRequest();

    var contact = new Contact(name, number);

    await context.Contacts.AddAsync(contact);
    await context.SaveChangesAsync();

    return Results.Created();
});

contacts.MapGet("", async ([FromServices] ApiContext context) => Results
    .Ok(await context.Contacts.OrderByDescending(c => c.CreatedAt).ToListAsync()))
    .RequireAuthorization();

using(var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApiContext>();

    await context.Database.EnsureCreatedAsync();
}

app.Run();
