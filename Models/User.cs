using System;

namespace SolarneApi.Models;

public class User : BaseEntity
{
    public User(
        string email,
        string password
    )
    {
        Email = email;
        Password = password;
    }

    protected User() { }

    public string Email { get; set; }
    public string Password { get; set; }
}
