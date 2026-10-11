using CodeInspector.Leads.Api.Dtos;
using FluentValidation;

namespace CodeInspector.Leads.Api.Validators;

public class CreateResearchSessionValidator : AbstractValidator<CreateResearchSessionRequest>
{
    public CreateResearchSessionValidator()
    {
        RuleFor(x => x.Keywords).NotEmpty().MaximumLength(500);
    }
}

public class CreateLeadValidator : AbstractValidator<CreateLeadRequest>
{
    public CreateLeadValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(500);
    }
}

public class CreateCompanyValidator : AbstractValidator<CreateCompanyRequest>
{
    public CreateCompanyValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(500);
    }
}
