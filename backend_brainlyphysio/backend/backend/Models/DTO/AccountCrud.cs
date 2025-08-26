using backend.Models.DTO.LocationDto;
using backend.Models.DTO.QualityDto;

namespace backend.Models.DTO;

public class AccountCrud
{
    public int IdAccount { get; init; }
    public string? Name { get; init; }
    public string? Prename { get; init; }
    public string? Email { get; set; }
    public string? Description { get; set; }
    public string? PhoneNumber { get; set; }
    // public FormFile? Image { get; set; } 
    public string? ImagePath { get; set; }
    public string? PresignedUrl { get; set; } = "empty";
    public IList<LocationCrud> MemberLocations { get; set; } = new List<LocationCrud>();
    public IList<QualityCrud> MemberQualities { get; set; } = new List<QualityCrud>();
}