namespace backend.Models.User;

public class Quality
{
    public int IdQuality { get; init; }
    public string QualityName { get; set; } = null!;
    
    public ICollection<MemberQuality> MemberQualities { get; set; } = new List<MemberQuality>();
}