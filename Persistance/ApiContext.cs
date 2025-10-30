using Microsoft.EntityFrameworkCore;
using SolarneApi.Models;

namespace SolarneApi.Persistance;

public class ApiContext : DbContext
{
    public ApiContext(
        DbContextOptions<ApiContext> options
    ) : base(options)
    {

    }
    
    public DbSet<User> Users { get; set; }
    public DbSet<Solution> Solutions { get; set; }
    public DbSet<Contact> Contacts { get; set; }
}
