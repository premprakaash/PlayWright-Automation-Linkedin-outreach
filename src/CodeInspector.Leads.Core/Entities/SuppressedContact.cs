namespace CodeInspector.Leads.Core.Entities;

public class SuppressedContact
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? NormalizedProfileUrl { get; set; }
    public string? NormalizedEmail { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
