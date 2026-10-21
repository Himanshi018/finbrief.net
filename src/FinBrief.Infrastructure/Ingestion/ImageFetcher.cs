using AngleSharp.Html.Parser;
using Microsoft.Extensions.Logging;

namespace FinBrief.Infrastructure.Ingestion;

// When the RSS item has no image, we look at the article page itself for its "og:image":
// the picture publishers set for link previews on WhatsApp, X, etc.
// If there is none, the article simply has no image (no placeholders).
public class ImageFetcher(HttpClient http, ILogger<ImageFetcher> logger)
{
    public async Task<string?> FindOgImageAsync(string articleUrl, CancellationToken ct)
    {
        try
        {
            var html = await http.GetStringAsync(articleUrl, ct);

            // AngleSharp parses HTML like a browser does, so we can use CSS selectors to find the tag.
            using var document = new HtmlParser().ParseDocument(html);
            var content = document
                .QuerySelector("meta[property='og:image'], meta[name='og:image'], meta[name='twitter:image']")
                ?.GetAttribute("content");

            return Uri.TryCreate(content, UriKind.Absolute, out var uri) && uri.Scheme == "https"
                ? content
                : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // A missing image is never a reason to fail the run.
            logger.LogWarning("Could not load {Url} to look for an image: {Error}", articleUrl, ex.Message);
            return null;
        }
    }
}
