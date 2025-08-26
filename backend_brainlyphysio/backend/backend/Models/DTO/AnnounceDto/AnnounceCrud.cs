namespace backend.Models.DTO.AnnounceDto;

public class AnnounceCrud
{
    public int IdAnnounce { get; init; }
    public string CourseTitle { get; set; } = null!;
    public string CourseImagePath { get; set; } = null!;
    public string? PresignedUrl { get; set; }
    public int? EmcPoints { get; set; }
    public string? AboutCourse { get; set; }
    public string Format { get; set; } = null!;
    public string Trainers { get; set; } = null!;
    public string? ContactPhoneNumber { get; set; }
    public string Location { get; set; } = null!;
    public int Price { get; set; } 
    public int CourseDuration { get; set; }
    public DateTime ComingDate { get; set; }
    public DateTime? LeavingDate { get; set; }
    public DateTime ExpirationDate { get; set; }
    public IList<CourseSectionDto>? CourseSections { get; set; } = new List<CourseSectionDto>();
}
