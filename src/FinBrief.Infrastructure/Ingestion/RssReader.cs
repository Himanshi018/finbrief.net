using System.Net;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace FinBrief.Infrastructure.Ingestion;

// Downloads one RSS feed and turns its <item>s into FeedItems.
// HttpClient is given to us by DI (see DependencyInjection.AddIngestion).
public partial class RssReader(HttpClient http)
{
    private const int TitleMaxLength = 500;
    private const int DescriptionMaxLength = 280;   // legal rule: short description only
    private const string MediaRssNamespace = "http://search.yahoo.com/mrss/";

    public async Task<List<FeedItem>> ReadAsync(FeedSettings feed, CancellationToken ct)
    {
        var xml = await http.GetStringAsync(feed.Url, ct);

        using var reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore });
        var syndicationFeed = SyndicationFeed.Load(reader);

        var items = new List<FeedItem>();
        foreach (var item in syndicationFeed.Items)
        {
            var url = NormalizeUrl(item.Links.FirstOrDefault(l => l.RelationshipType == "alternate")?.Uri
                                   ?? item.Links.FirstOrDefault()?.Uri);
            var title = Clean(item.Title?.Text, TitleMaxLength);
            if (url is null || title is null)
            {
                continue;
            }

            // Some feeds only fill one of the two dates.
            var published = item.PublishDate != default ? item.PublishDate : item.LastUpdatedTime;

            items.Add(new FeedItem(
                Title: title,
                Description: Clean(item.Summary?.Text, DescriptionMaxLength),
                Url: url,
                PublishedAt: published.UtcDateTime,
                ImageUrl: FindImage(item),
                SourceName: feed.Source,
                CategorySlug: feed.Category));
        }

        return items;
    }

    // Economic Times puts the image in <enclosure type="image/jpeg" url="...">,
    // Livemint in <media:content url="..."> (Media RSS).
    private static string? FindImage(SyndicationItem item)
    {
        var enclosure = item.Links.FirstOrDefault(l =>
            l.RelationshipType == "enclosure" && l.MediaType?.StartsWith("image/") == true);
        if (enclosure is not null)
        {
            return enclosure.Uri.ToString();
        }

        foreach (var extension in item.ElementExtensions)
        {
            if (extension.OuterNamespace == MediaRssNamespace &&
                extension.OuterName is "content" or "thumbnail")
            {
                var url = extension.GetObject<XElement>().Attribute("url")?.Value;
                if (Uri.TryCreate(url, UriKind.Absolute, out _))
                {
                    return url;
                }
            }
        }

        return null;
    }

    // Removes tracking parts like "?utm_source=rss" so the same story always has the same URL.
    private static string? NormalizeUrl(Uri? uri)
    {
        if (uri is null || !uri.IsAbsoluteUri || (uri.Scheme != "https" && uri.Scheme != "http"))
        {
            return null;
        }

        return uri.GetLeftPart(UriPartial.Path);
    }

    // Strips HTML tags and entities (&amp; → &), collapses whitespace, and cuts the text to maxLength
    // at a word boundary.
    private static string? Clean(string? text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        text = HtmlTag().Replace(text, " ");
        text = WebUtility.HtmlDecode(text);
        text = Whitespace().Replace(text, " ").Trim();

        if (text.Length == 0)
        {
            return null;
        }

        if (text.Length > maxLength)
        {
            var cut = text[..(maxLength - 1)];
            var lastSpace = cut.LastIndexOf(' ');
            text = (lastSpace > maxLength / 2 ? cut[..lastSpace] : cut).TrimEnd() + "…";
        }

        return text;
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTag();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
