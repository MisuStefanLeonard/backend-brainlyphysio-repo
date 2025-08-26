namespace backend.Models.User;

public class MemberQuality
{
    public int IdMemberQuality { get; init; }
    public int IdQuality { get; set; }
    public int IdMember { get; set; }
    
    public Quality Quality { get; set; } = null!;
    public Account Member { get; set; } = null!;
}