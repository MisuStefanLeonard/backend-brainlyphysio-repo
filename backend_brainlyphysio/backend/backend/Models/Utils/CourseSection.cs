namespace backend.Models.Utils;

public class CourseSection
{
    public int IdCourseSection { get; set; }
    public string? SectionTitle { get; set; } 
    public int CourseSectionOrder { get; set; }
    
    public int IdAnnounce { get; set; }
    public Announce SectionAnnounce { get; set; } = null!;

    public IList<SectionContent>? SectionContents { get; set; } = new List<SectionContent>();
}