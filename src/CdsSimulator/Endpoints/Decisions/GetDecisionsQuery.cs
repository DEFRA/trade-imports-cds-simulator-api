using FluentValidation;

namespace Defra.TradeImportsCdsSimulator.Endpoints.Decisions;

public class GetDecisionsQuery
{
    public string? Mrn { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    public class Validator : AbstractValidator<GetDecisionsQuery>
    {
        public Validator()
        {
            RuleFor(x => x)
                .Must(HaveAtLeastOneValue)
                .WithMessage("You must specify at least one query string parameter");
            RuleFor(x => x).Must(HaveValidDateRange).WithMessage("from cannnot be greater than to");
            RuleFor(x => x.From)
                .Must(x => x!.Value.Kind == DateTimeKind.Utc)
                .When(x => x.From.HasValue)
                .WithMessage("must be UTC");
            RuleFor(x => x.To)
                .Must(x => x!.Value.Kind == DateTimeKind.Utc)
                .When(x => x.To.HasValue)
                .WithMessage("must be UTC");
        }

        private static bool HaveValidDateRange(GetDecisionsQuery arg)
        {
            if (arg is { From: not null, To: not null })
            {
                return arg.To > arg.From;
            }

            return true;
        }

        private static bool HaveAtLeastOneValue(GetDecisionsQuery arg)
        {
            return !string.IsNullOrEmpty(arg.Mrn) || arg.From.HasValue || arg.To.HasValue;
        }
    }
}
