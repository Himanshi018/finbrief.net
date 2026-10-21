namespace FinBrief.Core.Entities;

// One jargon word with a one-line explanation, e.g. { "repo rate", "The rate at which RBI lends to banks." }.
// A list of these is stored as JSON inside the Article row.
public class KeyTerm
{
    public string Term { get; set; } = "";
    public string Meaning { get; set; } = "";
}
