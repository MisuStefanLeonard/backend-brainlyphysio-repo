namespace backend.Models.Utils;

public class SectionContent
{
    public int IdSectionContent { get; set; }
    public string Content { get; set; } = null!; 
    public int Order { get; set; }
    
    public int IdCourseSection { get; set; }
    public CourseSection CourseSectionContent { get; set; } = null!;

}