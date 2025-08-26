namespace backend.Models.User;

public class RememberUser
{
    public int IdSession { get; init; }
    public int IdAccount { get; set; }
    public Account CurrentUserSession { get; set; } = null!;
    public string SessionToken { get; set; } = null!;
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

}