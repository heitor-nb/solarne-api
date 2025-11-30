using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SolarneApi.Auth;
using SolarneApi.Dtos;
using SolarneApi.Models;
using SolarneApi.Persistance;

namespace SolarneApi;

public static class WebAppExtensions
{
    public static WebApplication MapAuthEndpoints(
        this WebApplication app,
        IConfiguration cfg
    )
    {   
        app.MapPost("signup", async ( // Only the admin can create a new user
            ClaimsPrincipal authenticatedUser,
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

            return Results.Created("/login", new { user.Email });
        }).RequireAuthorization(policy =>
        {   
            policy.RequireAuthenticatedUser(); // * necessary

            var adminEmail = cfg["AdminSettings:AdminEmail"];
            if(!string.IsNullOrWhiteSpace(adminEmail)) policy.RequireClaim(ClaimTypes.Email, adminEmail);
        });

        app.MapPost("login", async (
            [FromBody] UserDto userRequest,
            [FromServices] ApiContext context
        ) =>
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Email == userRequest.Email);
            if (user == null) return Results.BadRequest(); // BadRequest() instead of NotFound() for security reason

            if (!BCrypt.Net.BCrypt.Verify(userRequest.Password, user.Password)) return Results.BadRequest();

            var token = new JwtService(cfg).GenerateToken(user);

            return Results.Ok(new { token });  
        });

        return app;
    }

    public static WebApplication MapSolutionsEndpoints(
        this WebApplication app
    )
    {
        var solutions = app.MapGroup("solutions");

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

        return app;
    }

    public static WebApplication MapContactsEndpoints(
        this WebApplication app
    )
    {
        var contacts = app.MapGroup("contacts");

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

        contacts
            .MapGet("", async ([FromServices] ApiContext context) => Results.Ok(await context.Contacts.OrderByDescending(c => c.CreatedAt).ToListAsync()))
            .RequireAuthorization();

        return app;
    }
}
