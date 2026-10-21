namespace FinBrief.Core.Entities;

// A URL the AI judged "not finance relevant".
// We remember it so the ingestion job never spends an AI call on it again.
public class ArticleRejection
{
    // Matches the column size in AppDbContext.
    public const int ReasonMaxLength = 500;

    public int Id { get; set; }
    public string Url { get; set; } = "";
    public DateTime RejectedAt { get; set; }

    // The AI's explanation can be any length, so it is cut to fit the column
    // instead of making the database insert fail.
    private string _reason = "";
    public string Reason
    {
        get => _reason;
        set => _reason = value.Length > ReasonMaxLength ? value[..ReasonMaxLength] : value;
    }
}
