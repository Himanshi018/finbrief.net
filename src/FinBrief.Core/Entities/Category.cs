namespace FinBrief.Core.Entities;

// A news category such as "Markets" or "Fintech".
// The Slug ("ai-business") is used in URLs and stored on each article.
public class Category
{
    public int Id { get; set; }
    public string Slug { get; set; } = "";
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
}
