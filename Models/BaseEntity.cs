namespace SolarneApi.Models;

public class BaseEntity
{
    public BaseEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }
    
    public Guid Id { get; private set; }
    public DateTime CreatedAt { get; private set; }
}
