namespace backend.Models.DTO.LocationDto;

public class LocationCrud
{
    public int IdLocation { get; init; }
    public string City { get; init; } = null!;
    public string? County { get; init; }
}