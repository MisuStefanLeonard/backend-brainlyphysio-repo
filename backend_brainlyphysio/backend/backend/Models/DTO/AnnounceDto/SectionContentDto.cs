namespace backend.Models.DTO.AnnounceDto;

public class SectionContentDto
{
    public int IdSectionContent { get; set; }
    public string Content { get; set; } = null!; 
    public int Order { get; set; }
}