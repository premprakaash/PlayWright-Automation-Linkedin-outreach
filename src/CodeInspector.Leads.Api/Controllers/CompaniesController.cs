using CodeInspector.Leads.Api.Auth;
using CodeInspector.Leads.Api.Dtos;
using CodeInspector.Leads.Core.Entities;
using CodeInspector.Leads.Core.Enums;
using CodeInspector.Leads.Core.Normalization;
using CodeInspector.Leads.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CodeInspector.Leads.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/companies")]
public class CompaniesController(LeadsDbContext db, IUserContext user) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CompanyResponse>> Create(CreateCompanyRequest request, CancellationToken cancellationToken)
    {
        string? normalizedWebsite = null;
        if (!string.IsNullOrWhiteSpace(request.Website) &&
            !UrlNormalizer.TryNormalizeWebsite(request.Website, out normalizedWebsite, out var err))
        {
            return BadRequest(new { error = err });
        }

        var now = DateTimeOffset.UtcNow;
        var company = new Company
        {
            Id = Guid.NewGuid(),
            UserId = user.UserId,
            Name = request.Name.Trim(),
            NormalizedName = UrlNormalizer.NormalizeCompanyName(request.Name),
            Website = request.Website?.Trim(),
            NormalizedWebsite = normalizedWebsite,
            Industry = request.Industry?.Trim(),
            EmployeeRange = request.EmployeeRange?.Trim(),
            Country = request.Country?.Trim(),
            TechnologySignals = request.TechnologySignals ?? [],
            Status = CompanyStatus.Candidate,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Companies.Add(company);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = company.Id }, Map(company));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CompanyResponse>>> List([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var q = db.Companies.Where(c => c.UserId == user.UserId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            q = q.Where(c => c.NormalizedName.Contains(s) || (c.Website != null && c.Website.ToLower().Contains(s)));
        }

        var items = await q.OrderByDescending(c => c.UpdatedAt).Take(200).ToListAsync(cancellationToken);
        return Ok(items.Select(Map));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CompanyResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.UserId, cancellationToken);
        return company is null ? NotFound() : Ok(Map(company));
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<CompanyResponse>> Patch(Guid id, PatchCompanyRequest request, CancellationToken cancellationToken)
    {
        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.UserId, cancellationToken);
        if (company is null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            company.Name = request.Name.Trim();
            company.NormalizedName = UrlNormalizer.NormalizeCompanyName(request.Name);
        }

        if (request.Website is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Website))
            {
                company.Website = null;
                company.NormalizedWebsite = null;
            }
            else if (UrlNormalizer.TryNormalizeWebsite(request.Website, out var nw, out var err))
            {
                company.Website = request.Website.Trim();
                company.NormalizedWebsite = nw;
            }
            else
            {
                return BadRequest(new { error = err });
            }
        }

        company.Industry = request.Industry ?? company.Industry;
        company.EmployeeRange = request.EmployeeRange ?? company.EmployeeRange;
        company.Country = request.Country ?? company.Country;
        if (request.TechnologySignals is not null)
        {
            company.TechnologySignals = request.TechnologySignals;
        }

        if (request.Status.HasValue)
        {
            company.Status = request.Status.Value;
        }

        company.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(Map(company));
    }

    internal static CompanyResponse Map(Company c) =>
        new(c.Id, c.Name, c.Website, c.Industry, c.EmployeeRange, c.Country, c.TechnologySignals, c.Evidence,
            c.Status.ToString(), c.CreatedAt, c.UpdatedAt);
}
