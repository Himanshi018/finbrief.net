namespace FinBrief.Core.Entities;

// A reader account. Login is not built yet; the table is ready for later.
public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string? DisplayName { get; set; }
    public DateTime CreatedAt { get; set; }
}
