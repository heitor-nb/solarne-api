namespace SolarneApi.Models;

public class Contact : BaseEntity
{
    public Contact(
        string name,
        string number
    )
    {
        Name = name;
        Number = number;
    }

    protected Contact() { }

    public string Name { get; set; }
    public string Number { get; set; }
}
