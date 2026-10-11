using System.Text.RegularExpressions;
using CodeInspector.Leads.Core.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace CodeInspector.Leads.Infrastructure.Browser;

public partial class PlaywrightCompanyResearchBrowser : ICompanyResearchBrowser, IAsyncDisposable
{
    private readonly BrowserOptions _options;
    private readonly ILogger<PlaywrightCompanyResearchBrowser> _logger;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public PlaywrightCompanyResearchBrowser(IOptions<BrowserOptions> options, ILogger<PlaywrightCompanyResearchBrowser> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<CompanyResearchResult> ResearchCompanyWebsiteAsync(
        Guid userId,
        string normalizedWebsite,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(normalizedWebsite, UriKind.Absolute, out var uri))
        {
            return new CompanyResearchResult(false, true, [], [], "Invalid website URL.");
        }

        if (_options.AllowedDomains.Length > 0 &&
            !_options.AllowedDomains.Any(d => uri.Host.EndsWith(d, StringComparison.OrdinalIgnoreCase)))
        {
            return new CompanyResearchResult(false, true, [], [], "Domain is not in the allowlist.");
        }

        if (uri.Host.Contains("linkedin.com", StringComparison.OrdinalIgnoreCase))
        {
            return new CompanyResearchResult(false, true, [], [], "LinkedIn domains are not permitted for automated research.");
        }

        try
        {
            await EnsureBrowserAsync(cancellationToken);
            await using var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = "CodeInspector-LeadsBot/1.0 (+https://codeinspector.local; permitted company research)"
            });
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(_options.NavigationTimeoutMs);

            var response = await page.GotoAsync(normalizedWebsite, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = _options.NavigationTimeoutMs
            });

            if (response is null || !response.Ok)
            {
                return new CompanyResearchResult(false, true, [], [], "Unable to access website.");
            }

            var content = await page.ContentAsync();
            if (content.Length > _options.MaxResponseBytes)
            {
                content = content[.._options.MaxResponseBytes];
            }

            var evidence = new List<string>();
            var signals = new List<string>();

            if (DotNetPattern().IsMatch(content))
            {
                signals.Add(".NET");
                evidence.Add("Public page mentions .NET");
            }

            if (CSharpPattern().IsMatch(content))
            {
                signals.Add("C#");
                evidence.Add("Public page mentions C#");
            }

            if (SaaSPattern().IsMatch(content))
            {
                evidence.Add("Public page mentions SaaS or software product");
            }

            return new CompanyResearchResult(true, false, evidence, signals, null);
        }
        catch (PlaywrightException ex)
        {
            _logger.LogWarning(ex, "Browser research failed for {Website}", normalizedWebsite);
            return new CompanyResearchResult(false, true, [], [], ex.Message);
        }
    }

    private async Task EnsureBrowserAsync(CancellationToken cancellationToken)
    {
        if (_browser is not null)
        {
            return;
        }

        _playwright = await Playwright.CreateAsync();
        cancellationToken.ThrowIfCancellationRequested();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = _options.Headless
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
    }

    [GeneratedRegex(@"\bSaaS\b|\bsoftware product\b", RegexOptions.IgnoreCase)]
    private static partial Regex SaaSPattern();

    [GeneratedRegex(@"\b\.NET\b|\bdotnet\b", RegexOptions.IgnoreCase)]
    private static partial Regex DotNetPattern();

    [GeneratedRegex(@"\bC#\b", RegexOptions.IgnoreCase)]
    private static partial Regex CSharpPattern();
}
