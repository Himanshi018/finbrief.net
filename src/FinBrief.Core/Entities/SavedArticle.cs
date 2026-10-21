namespace FinBrief.Core.Entities;

// "User X saved article Y". Used once login exists; for now the app saves to localStorage.
public class SavedArticle
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int ArticleId { get; set; }
    public Article Article { get; set; } = null!;
    public DateTime SavedAt { get; set; }
}
