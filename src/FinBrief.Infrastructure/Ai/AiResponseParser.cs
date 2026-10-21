using System.Text.Json;
using System.Text.RegularExpressions;
using FinBrief.Core.Entities;

namespace FinBrief.Infrastructure.Ai;

// Turns the AI's reply into an AiBrief. Models don't always follow instructions exactly, so this is
// tolerant: it strips ```json code fences, text around the JSON, and labels like "**Why It Matters:**".
public static partial class AiResponseParser
{
    private const int MaxKeyTerms = 5;

    public static AiBrief Parse(string text)
    {
        var json = ExtractJson(text)
                   ?? throw new AiArticleException("The AI reply contained no JSON object.");

        JsonElement root;
        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip,
            });
            root = document.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            throw new AiArticleException("The AI reply was not valid JSON: " + ex.Message);
        }

        var isRelevant = GetBool(root, "is_finance_relevant");
        var reason = CleanText(GetString(root, "reason")) ?? "";

        if (!isRelevant)
        {
            return new AiBrief(false, reason, null, null, null, []);
        }

        var brief = new AiBrief(
            true,
            reason,
            SimpleTitle: CleanText(GetString(root, "simple_title")),
            Summary: CleanText(GetString(root, "summary")),
            WhyItMatters: CleanText(GetString(root, "why_it_matters")),
            KeyTerms: GetKeyTerms(root));

        // A relevant article must have every field, otherwise it would be hidden from the feed anyway.
        if (brief.SimpleTitle is null || brief.Summary is null || brief.WhyItMatters is null || brief.KeyTerms.Count == 0)
        {
            throw new AiArticleException("The AI reply was missing some fields.");
        }

        return brief;
    }

    // Removes ```json fences and anything before the first "{" or after the last "}".
    private static string? ExtractJson(string text)
    {
        var fence = CodeFence().Match(text);
        if (fence.Success)
        {
            text = fence.Groups[1].Value;
        }

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        return start >= 0 && end > start ? text[start..(end + 1)] : null;
    }

    // Looks up a property by its snake_case name, or the same name in camelCase ("simpleTitle").
    private static JsonElement? Find(JsonElement root, string snakeName)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var camelName = Regex.Replace(snakeName, "_([a-z])", m => m.Groups[1].Value.ToUpperInvariant());
        foreach (var property in root.EnumerateObject())
        {
            if (property.Name.Equals(snakeName, StringComparison.OrdinalIgnoreCase) ||
                property.Name.Equals(camelName, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value;
            }
        }

        return null;
    }

    private static string? GetString(JsonElement root, string name) =>
        Find(root, name) is { ValueKind: JsonValueKind.String } value ? value.GetString() : null;

    // Accepts true, "true" and "yes".
    private static bool GetBool(JsonElement root, string name) => Find(root, name) switch
    {
        { ValueKind: JsonValueKind.True } => true,
        { ValueKind: JsonValueKind.String } value => value.GetString()?.Trim().ToLowerInvariant() is "true" or "yes",
        _ => false,
    };

    // Accepts [{"term": "...", "meaning": "..."}], ["term: meaning"] and {"term": "meaning"}.
    private static List<KeyTerm> GetKeyTerms(JsonElement root)
    {
        var terms = new List<KeyTerm>();
        var value = Find(root, "key_terms");

        if (value is { ValueKind: JsonValueKind.Array } array)
        {
            foreach (var element in array.EnumerateArray())
            {
                if (element.ValueKind == JsonValueKind.Object)
                {
                    AddTerm(terms,
                        GetString(element, "term") ?? GetString(element, "name"),
                        GetString(element, "meaning") ?? GetString(element, "definition") ?? GetString(element, "explanation"));
                }
                else if (element.ValueKind == JsonValueKind.String)
                {
                    var parts = element.GetString()!.Split([':', '–', '—'], 2);
                    if (parts.Length == 2)
                    {
                        AddTerm(terms, parts[0], parts[1]);
                    }
                }
            }
        }
        else if (value is { ValueKind: JsonValueKind.Object } map)
        {
            foreach (var property in map.EnumerateObject())
            {
                AddTerm(terms, property.Name, property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : null);
            }
        }

        return terms.Take(MaxKeyTerms).ToList();
    }

    private static void AddTerm(List<KeyTerm> terms, string? term, string? meaning)
    {
        term = CleanText(term);
        meaning = CleanText(meaning);
        if (term is not null && meaning is not null)
        {
            terms.Add(new KeyTerm { Term = term, Meaning = meaning });
        }
    }

    // "**Why It Matters:** Prices may rise." → "Prices may rise."
    private static string? CleanText(string? text)
    {
        if (text is null)
        {
            return null;
        }

        text = LeadingLabel().Replace(text, "");
        text = text.Replace("**", "").Replace("__", "").Trim().Trim('"').Trim();
        return text.Length == 0 ? null : text;
    }

    [GeneratedRegex(@"```(?:json)?\s*(.*?)```", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex CodeFence();

    [GeneratedRegex(
        @"^\s*[*_#]*\s*(simple title|title|headline|summary|why (it|this) matters|reason|meaning)\s*[*_]*\s*[:\-–—]\s*[*_]*\s*",
        RegexOptions.IgnoreCase)]
    private static partial Regex LeadingLabel();
}
