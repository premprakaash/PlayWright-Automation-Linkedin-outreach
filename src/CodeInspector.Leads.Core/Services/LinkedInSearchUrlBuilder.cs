namespace CodeInspector.Leads.Core.Services;

public static class LinkedInSearchUrlBuilder
{
    private const string SearchPath = "https://www.linkedin.com/search/results/people/";

    public static string BuildPeopleSearchUrl(string keywords)
    {
        if (string.IsNullOrWhiteSpace(keywords))
        {
            throw new ArgumentException("Keywords are required.", nameof(keywords));
        }

        var trimmed = keywords.Trim();
        if (trimmed.Length > 500)
        {
            throw new ArgumentException("Keywords exceed maximum length.", nameof(keywords));
        }

        var encoded = Uri.EscapeDataString(trimmed);
        return $"{SearchPath}?keywords={encoded}&origin=GLOBAL_SEARCH_HEADER";
    }
}
