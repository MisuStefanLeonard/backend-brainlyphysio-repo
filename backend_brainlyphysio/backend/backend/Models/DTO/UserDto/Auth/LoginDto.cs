namespace backend.Models.DTO.UserDto.Auth;

public class LoginDto
{
    public string NumeProp { get; set; } = null!;
    public string? ParolaProp { get; set; }
    public string TokenProp { get; set; } = null!;
    public string RoleProp { get; set; } = null!;
    public string? RefreshTokenProp { get; set; } 
}