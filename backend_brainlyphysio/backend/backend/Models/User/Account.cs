namespace backend.Models.User;

public class Account
{
    public int IdAccount { get; init; }
    public string? Name { get; set; }
    public string? Prename { get; set; }
    public string? Email { get; set; }
    public string? Description { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Password { get; init; } 
    public string? ImagePath { get; set; }
    public string? ImageHash { get; set; }
    public string Role { get; init; } = null!;
    public DateTime ConfirmationLinkHour { get; set; }
    public string ActivationCode { get; set; } = null!;
    public bool IsVerified { get; set; }
    
    public ICollection<OperationPlace> OperationPlaces { get; set; } = new List<OperationPlace>();
    public ICollection<MemberQuality> Qualities { get; set; } = new List<MemberQuality>();
    public RememberUser? RememberUserSession { get; set; } 
    
}