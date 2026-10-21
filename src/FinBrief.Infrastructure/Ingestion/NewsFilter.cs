using System.Text.RegularExpressions;

namespace FinBrief.Infrastructure.Ingestion;

// Cheap keyword rules that run before the AI, so we don't spend AI calls on obvious misses.
// The AI makes the final "is this finance?" decision.
public static partial class NewsFilter
{
    // Stock-tip sites we never link to.
    private static readonly string[] ExcludedDomains =
    [
        "fool.com", "zacks.com", "seekingalpha.com", "investorplace.com",
        "simplywall.st", "marketbeat.com", "insidermonkey.com",
    ];

    // True if the title/description mentions money, markets, the economy, business, etc.
    // Real estate counts as finance.
    public static bool HasFinanceSignal(string text) =>
        FinanceWords().IsMatch(text) || text.Contains('₹') || text.Contains('$');

    // True for stock-tip / clickbait headlines ("3 stocks to buy", "could soar", target prices...).
    // FinBrief explains news; it never gives buy/sell advice.
    public static bool IsStockTipClickbait(string text) => StockTipPatterns().IsMatch(text);

    public static bool IsExcludedDomain(string url)
    {
        var host = new Uri(url).Host;
        return ExcludedDomains.Any(d => host == d || host.EndsWith("." + d));
    }

    // Stories about market-moving geopolitics (oil, sanctions, tariffs, shipping routes) go to
    // "global-finance", whichever feed they came from. Only the title is checked, so a market wrap that
    // just mentions oil in passing stays in its own category.
    public static string PickCategory(FeedItem item) =>
        Geopolitics().IsMatch(item.Title) ? "global-finance" : item.CategorySlug;

    // \b(...)\w* means "a word starting with", so "invest" also matches "investors" and "investment".
    // "ai" and "rs" must be whole words, otherwise "air" or "first" would match.
    [GeneratedRegex(
        @"\b(market|stock|share|sensex|nifty|equit|ipo|fpo|fund|invest|bank|loan|credit|lend|borrow|"
        + @"rbi|sebi|repo|interest rate|inflation|gdp|econom|fiscal|budget|tax|gst|rupee|dollar|currenc|forex|"
        + @"bond|yield|debt|crude|oil|gold|silver|commodit|tariff|trade|export|import|sanction|shipping|"
        + @"revenue|profit|loss|earning|q[1-4]|result|valuation|acqui|merger|deal|startup|unicorn|"
        + @"fintech|payment|upi|wallet|crypto|bitcoin|insur|pension|salar|job|layoff|hiring|wage|"
        + @"real estate|property|housing|home loan|rent|price|crore|lakh|billion|million|"
        + @"company|companies|firm|industr|manufactur|business|ceo|growth|demand|"
        + @"money|income|saving|spend|subsid|energy)\w*"
        + @"|\b(ai|rs)\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex FinanceWords();

    [GeneratedRegex(
        @"prediction:|could soar|could double|could rally|history says|multibagger|"
        + @"stocks? to (buy|watch|sell)|buy or sell|buy,? sell or hold|should you buy|"
        + @"top (stock )?picks?|stock picks?|technical picks?|target price|price target|upside potential|"
        + @"(buy|sell|hold) rating|stock recommendations?|trading (ideas?|strateg)|intraday|breakout stocks?|"
        + @"penny stocks?|jackpot|millionaire|\d+% returns in|experts? (suggest|recommend)|"
        + @"stocks? under (rs|₹)|brokerages? (sees?|suggests?|recommends?)",
        RegexOptions.IgnoreCase)]
    private static partial Regex StockTipPatterns();

    [GeneratedRegex(
        @"\b(sanction\w*|tariff\w*|opec\w*|crude|brent|oil prices?|red sea|hormuz|suez|"
        + @"shipping (route|lane)s?|trade war\w*|geopolitic\w*)\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex Geopolitics();
}
