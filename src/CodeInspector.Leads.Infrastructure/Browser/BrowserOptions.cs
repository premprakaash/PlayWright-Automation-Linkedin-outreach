namespace CodeInspector.Leads.Infrastructure.Browser;

public class BrowserOptions
{
    public bool Headless { get; set; } = true;
    public int NavigationTimeoutMs { get; set; } = 30_000;
    public int MaxPages { get; set; } = 3;
    public int MaxResponseBytes { get; set; } = 512_000;
    public string[] AllowedDomains { get; set; } = [];
}
