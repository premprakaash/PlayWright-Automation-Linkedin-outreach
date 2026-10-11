using CodeInspector.Leads.Api.Auth;
using CodeInspector.Leads.Core.Contracts;
using CodeInspector.Leads.Infrastructure.Connectors;
using CodeInspector.Leads.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CodeInspector.Leads.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/imports")]
public class ImportsController(LeadsDbContext db, IUserContext user, CsvLeadConnector csvConnector) : ControllerBase
{
    [HttpPost("csv")]
    [RequestSizeLimit(10_485_760)]
    public async Task<IActionResult> ImportCsv(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest(new { error = "CSV file is empty." });
        }

        await using var stream = file.OpenReadStream();
        var result = await csvConnector.ImportAsync(new LeadImportInput(
            user.UserId,
            "csv",
            "upload",
            null,
            "User uploaded CSV import",
            null,
            CsvStream: stream,
            CsvFileName: file.FileName), cancellationToken);

        return Accepted(new
        {
            id = result.BatchId,
            result.TotalRows,
            result.ImportedRows,
            result.DuplicateRows,
            result.RejectedRows
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetBatch(Guid id, CancellationToken cancellationToken)
    {
        var batch = await db.ImportBatches.FirstOrDefaultAsync(b => b.Id == id && b.UserId == user.UserId, cancellationToken);
        return batch is null ? NotFound() : Ok(batch);
    }
}
