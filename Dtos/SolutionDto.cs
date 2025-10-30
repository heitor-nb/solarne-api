namespace SolarneApi.Dtos;

public record SolutionDto(
    string ImageUrl,
    string Location,
    double Power,
    string AnnualSaving
);
