namespace backend.Models.User;

public class OperationPlace
{
    public int IdOperationPlace { get; init; }
    public int IdLocation { get; set; }
    public int IdAccount { get; set; }
    
    public Location Location { get; set; } = null!;
    public Account Account { get; set; } = null!;
}