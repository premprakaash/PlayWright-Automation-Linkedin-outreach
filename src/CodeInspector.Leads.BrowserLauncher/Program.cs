using CodeInspector.Leads.Core.Services;
using Microsoft.Playwright;

if (args.Length == 0 || string.IsNullOrWhiteSpace(string.Join(' ', args)))
{
    Console.Error.WriteLine("Usage: CodeInspector.Leads.BrowserLauncher \"CTO .NET SaaS\"");
    return 1;
}

var keywords = string.Join(' ', args).Trim();
string searchUrl;
try
{
    searchUrl = LinkedInSearchUrlBuilder.BuildPeopleSearchUrl(keywords);
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

Console.WriteLine($"Opening LinkedIn people search: {searchUrl}");
Console.WriteLine("Review results manually. Close the browser window to exit.");

using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = false
});
var page = await browser.NewPageAsync();
await page.GotoAsync(searchUrl);

var closed = new TaskCompletionSource();
browser.Disconnected += (_, _) => closed.TrySetResult();
await closed.Task;

return 0;
