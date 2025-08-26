namespace backend.Models.DTO.UserDto;

public class CreateAccountResponseDto
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? PresignedUrl { get; set; }
}