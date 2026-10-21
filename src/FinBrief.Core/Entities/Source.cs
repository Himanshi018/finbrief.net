namespace FinBrief.Core.Entities;

// A news publisher we link back to, e.g. "Economic Times".
// The RSS feed URLs live in configuration (appsettings.json), not here,
// so feeds can be changed without a database migration.
public class Source
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Domain { get; set; } = "";
}
