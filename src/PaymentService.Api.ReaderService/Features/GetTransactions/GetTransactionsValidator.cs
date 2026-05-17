// FILE: src/PaymentService.Api.ReaderService/Features/GetTransactions/GetTransactionsValidator.cs
// VERSION: 2.0.0
// MODULE: M-READER
// PURPOSE: FluentValidation rules for account transaction query
// SEMANTIC_TAG: [VALIDATOR, INPUT_VALIDATION]
// START_MODULE M_READER

using FluentValidation;

namespace PaymentService.Api.ReaderService.Features.GetTransactions;

/// <summary>
/// Validator for GetTransactions feature requests.
/// VSA feature: GetTransactions (ReaderService)
/// </summary>
/// <remarks>
/// <para><strong>@contract:</strong> M-READER</para>
/// <para><strong>@purpose:</strong> Validates account transaction query input parameters including date range</para>
/// <para><strong>@module-type:</strong> UTILITY (validator)</para>
/// <para><strong>@invariant:</strong> AccountId 1-64 characters, non-empty</para>
/// <para><strong>@invariant:</strong> Skip ≥ 0, Limit 1-100</para>
/// <para><strong>@invariant:</strong> Date range, if provided, spans ≤ days-in-year (considering leap years)</para>
/// <para><strong>@verification-ref:</strong> V-M-READER</para>
/// </remarks>
public class GetTransactionsValidator : AbstractValidator<GetTransactionsRequest>
{
    private const int MaxLimit = 100;

    public GetTransactionsValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .MaximumLength(64)
            .WithMessage("AccountId must be between 1 and 64 characters");

        RuleFor(x => x.Skip)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Skip must be non-negative");

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, MaxLimit)
            .WithMessage($"Limit must be between 1 and {MaxLimit}");

        // Date range validation: if both provided, span must not exceed days in current year
        RuleFor(x => x)
            .Must(request =>
            {
                // If only one date is set or both null, defaults apply — no validation needed
                if (request.DateFrom == null || request.DateTo == null)
                    return true;

                // Resolve defaults the same way the handler will
                var dateFrom = request.DateFrom.Value;
                var dateTo = request.DateTo.Value;

                // Ensure from ≤ to
                if (dateFrom > dateTo)
                    return false;

                // Calculate max allowed days in the year of dateFrom
                var daysInYear = DateTime.IsLeapYear(dateFrom.Year) ? 366 : 365;
                var span = (dateTo - dateFrom).Days;
                return span <= daysInYear;
            })
            .WithMessage(request =>
            {
                if (request.DateFrom > request.DateTo)
                    return "DateFrom must be before or equal to DateTo";

                var daysInYear = DateTime.IsLeapYear(request.DateFrom!.Value.Year) ? 366 : 365;
                return $"Date range cannot exceed {daysInYear} days (days in year {request.DateFrom.Value.Year})";
            });
    }
}
