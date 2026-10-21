namespace FinBrief.Core.Entities;

// A URL the AI judged "not finance relevant".
// We remember it so the ingestion job never spends an AI call on it again.
public class ArticleRejection
{
    public int Id { get; set; }
    public string Url { get; set; } = "";
    public string Reason { get; set; } = "";
    public DateTime RejectedAt { get; set; }
}
