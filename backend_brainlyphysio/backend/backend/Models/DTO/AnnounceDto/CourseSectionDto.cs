namespace backend.Models.DTO.AnnounceDto;

public class CourseSectionDto
{
    public int IdCourseSection { get; set; }
    public string? SectionTitle { get; set; } 
    public int CourseSectionOrder { get; set; }
    public IList<SectionContentDto>? SectionContents { get; set; } = new List<SectionContentDto>();
}