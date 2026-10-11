using System.Globalization;

namespace CodeInspector.Leads.Core.Normalization;

public static class UrlNormalizer
{
    private static readonly HashSet<string> LinkedInHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "www.linkedin.com",
        "linkedin.com",
        "uk.linkedin.com",
        "ca.linkedin.com"
    };

    public static bool TryNormalizeLinkedInProfileUrl(string? url, out string normalized, out string? error)
    {
        normalized = string.Empty;
        error = null;
        if (string.IsNullOrWhiteSpace(url))
        {
            error = "URL is required.";
            return false;
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            error = "Invalid URL format.";
            return false;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            error = "LinkedIn profile URLs must use HTTPS.";
            return false;
        }

        var host = uri.Host.ToLowerInvariant();
        if (!LinkedInHosts.Contains(host))
        {
            error = "Host is not an allowed LinkedIn domain.";
            return false;
        }

        var path = uri.AbsolutePath.TrimEnd('/');
        if (!path.StartsWith("/in/", StringComparison.OrdinalIgnoreCase) &&
            !path.StartsWith("/pub/", StringComparison.OrdinalIgnoreCase))
        {
            error = "URL must be a LinkedIn profile path (/in/ or /pub/).";
            return false;
        }

        normalized = $"https://{host}{path}";
        return true;
    }

    public static bool TryNormalizeWebsite(string? url, out string normalized, out string? error)
    {
        normalized = string.Empty;
        error = null;
        if (string.IsNullOrWhiteSpace(url))
        {
            error = "Website is required.";
            return false;
        }

        var trimmed = url.Trim();
        if (!trimmed.Contains("://", StringComparison.Ordinal))
        {
            trimmed = "https://" + trimmed;
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            error = "Invalid website URL.";
            return false;
        }

        if (uri.Scheme is not ("http" or "https"))
        {
            error = "Website must use HTTP or HTTPS.";
            return false;
        }

        var host = uri.Host.ToLowerInvariant();
        if (host.StartsWith("www.", StringComparison.Ordinal))
        {
            host = host[4..];
        }

        var builder = new UriBuilder(uri.Scheme, host)
        {
            Port = uri.IsDefaultPort ? -1 : uri.Port,
            Path = string.IsNullOrEmpty(uri.AbsolutePath) ? "/" : uri.AbsolutePath.TrimEnd('/'),
            Query = string.Empty,
            Fragment = string.Empty
        };

        normalized = builder.Uri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped)
            .TrimEnd('/');
        return true;
    }

    public static string NormalizeCompanyName(string name) =>
        string.Join(' ', name.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    public static string NormalizePersonName(string name) =>
        name.Trim().ToLowerInvariant();

    public static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();
}
