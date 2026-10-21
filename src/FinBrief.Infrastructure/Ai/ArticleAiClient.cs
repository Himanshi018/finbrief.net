using System.ClientModel;
using System.ClientModel.Primitives;
using FinBrief.Infrastructure.Ingestion;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;

namespace FinBrief.Infrastructure.Ai;

// The AI's verdict and explanation for one article.
public record AiBrief(
    bool IsFinanceRelevant,
    string Reason,
    string? SimpleTitle,
    string? Summary,
    string? WhyItMatters,
    List<Core.Entities.KeyTerm> KeyTerms);

// Thrown when no further AI calls make sense this run (bad key, unknown model, quota used up).
public class AiStopException(string message) : Exception(message);

// Thrown when the AI step failed for one article only; the next article may still work.
public class AiArticleException(string message) : Exception(message);

// Talks to Groq. Groq offers an OpenAI-compatible API, so we use the official OpenAI .NET SDK
// and just point it at Groq's address.
public class ArticleAiClient
{
    // Groq tells us how long to wait when we send requests too fast (per-minute limits).
    // Short waits are fine; a long wait means the daily quota is used up.
    private static readonly TimeSpan MaxRateLimitWait = TimeSpan.FromSeconds(60);
    private const int MaxAttempts = 5;

    private readonly ChatClient? _chat;
    private readonly GroqSettings _settings;
    private readonly ILogger<ArticleAiClient> _logger;
    private DateTime _lastCallUtc = DateTime.MinValue;

    public ArticleAiClient(GroqSettings settings, ILogger<ArticleAiClient> logger)
    {
        _settings = settings;
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            _chat = new ChatClient(settings.Model, new ApiKeyCredential(settings.ApiKey), new OpenAIClientOptions
            {
                Endpoint = new Uri(settings.BaseUrl),
                NetworkTimeout = TimeSpan.FromSeconds(60),
                // We handle retries ourselves below, so we can decide when to give up.
                RetryPolicy = new ClientRetryPolicy(maxRetries: 0),
            });
        }
    }

    // False when no API key is configured.
    public bool IsConfigured => _chat is not null;

    public async Task<AiBrief> ExplainAsync(FeedItem item, CancellationToken ct)
    {
        if (_chat is null)
        {
            throw new AiStopException("No Groq API key configured (Groq:ApiKey).");
        }

        List<ChatMessage> messages =
        [
            new SystemChatMessage(AiPrompt.System),
            new UserChatMessage(AiPrompt.ForArticle(item)),
        ];

        var options = new ChatCompletionOptions
        {
            MaxOutputTokenCount = _settings.MaxOutputTokens,
#pragma warning disable OPENAI001 // reasoning effort is marked "experimental" in the SDK
            ReasoningEffortLevel = ChatReasoningEffortLevel.Low,
#pragma warning restore OPENAI001
        };

        for (var attempt = 1; ; attempt++)
        {
            await WaitForNextCallSlotAsync(ct);

            try
            {
                ChatCompletion completion = await _chat.CompleteChatAsync(messages, options, ct);

                if (completion.FinishReason == ChatFinishReason.Length)
                {
                    throw new AiArticleException(
                        $"The answer was cut off at {_settings.MaxOutputTokens} tokens. Increase Groq:MaxOutputTokens.");
                }

                var text = string.Concat(completion.Content.Select(part => part.Text));
                return AiResponseParser.Parse(text);
            }
            catch (ClientResultException ex)
            {
                var wait = GetRetryAfter(ex);

                switch (ex.Status)
                {
                    case 401 or 403:
                        throw new AiStopException("Groq rejected the API key (HTTP " + ex.Status + "). Check Groq:ApiKey.");

                    case 404:
                        throw new AiStopException($"Groq does not know the model '{_settings.Model}'. Check Groq:Model.");

                    case 400 when ex.Message.Contains("decommissioned", StringComparison.OrdinalIgnoreCase):
                        throw new AiStopException($"The model '{_settings.Model}' has been retired by Groq. Pick a new Groq:Model.");

                    case 429 when wait is not null && wait <= MaxRateLimitWait && attempt < MaxAttempts:
                        _logger.LogInformation("Groq rate limit reached, waiting {Seconds:0}s...", wait.Value.TotalSeconds);
                        await Task.Delay(wait.Value, ct);
                        continue;

                    case 429 when wait is not null && wait <= MaxRateLimitWait:
                        throw new AiStopException(
                            "Groq kept rate-limiting us (HTTP 429). Increase Groq:MinSecondsBetweenCalls or use a smaller --max.");

                    case 429:
                        throw new AiStopException("Groq quota used up (HTTP 429). Try again later or tomorrow.");

                    case >= 500 when attempt < MaxAttempts:
                        _logger.LogInformation("Groq server error {Status}, retrying...", ex.Status);
                        await Task.Delay(TimeSpan.FromSeconds(2 * attempt), ct);
                        continue;

                    default:
                        throw new AiArticleException($"Groq error HTTP {ex.Status}: {FirstLine(ex.Message)}");
                }
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                throw new AiArticleException("Groq did not answer within 60 seconds.");
            }
        }
    }

    // Spaces calls at least MinSecondsBetweenCalls apart, so a big --max doesn't hit the per-minute limit.
    private async Task WaitForNextCallSlotAsync(CancellationToken ct)
    {
        var wait = _lastCallUtc.AddSeconds(_settings.MinSecondsBetweenCalls) - DateTime.UtcNow;
        if (wait > TimeSpan.Zero)
        {
            await Task.Delay(wait, ct);
        }

        _lastCallUtc = DateTime.UtcNow;
    }

    private static TimeSpan? GetRetryAfter(ClientResultException ex)
    {
        var headers = ex.GetRawResponse()?.Headers;
        if (headers is not null &&
            headers.TryGetValue("retry-after", out var value) &&
            double.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out var seconds))
        {
            return TimeSpan.FromSeconds(seconds);
        }

        return null;
    }

    private static string FirstLine(string text) => text.Split('\n')[0].Trim();
}
