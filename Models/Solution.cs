namespace SolarneApi.Models;

public class Solution : BaseEntity
{
    public Solution(
        string imageUrl,
        string location,
        double power,
        string annualSaving
    )
    {
        ImageUrl = imageUrl;
        Location = location;
        Power = power;
        AnnualSaving = annualSaving;
    }

    protected Solution() { }
    
    public string ImageUrl { get; set; }
    public string Location { get; set; }
    public double Power { get; set; }
    public string AnnualSaving { get; set; }
}
