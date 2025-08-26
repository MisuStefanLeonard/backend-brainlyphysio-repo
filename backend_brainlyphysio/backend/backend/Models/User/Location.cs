namespace backend.Models.User;

public class Location
{
    public int IdLocation { get; init; }
    public string City { get; set; } = null!;
    public string? County { get; set; } = null!;

    public ICollection<OperationPlace> OperationPlaces { get; set; } = new List<OperationPlace>();
}