// FILE: src/PaymentService.Api.ReaderService/Features/QueryPayments/QueryPaymentsValidator.cs
using PaymentService.Shared.Dtos;
// VERSION: 2.0.0
// MODULE: M-READER
// PURPOSE: FluentValidation rules for request DTOs
// SEMANTIC_TAG: [VALIDATOR, INPUT_VALIDATION]
// START_MODULE M_READER

// FILE: src/PaymentService.Api.ReaderService/Features/QueryPayments/QueryPaymentsValidator.cs
// VERSION: 1.0.0

using FluentValidation;

namespace PaymentService.Api.ReaderService.Features.QueryPayments;

/// <summary>
/// Validator for QueryPayments feature requests.
/// VSA feature: QueryPayments (ReaderService)
/// </summary>
public class QueryPaymentsValidator : AbstractValidator<QueryPaymentsRequest>
{
    public QueryPaymentsValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .MinimumLength(1)
            .MaximumLength(64)
            .WithMessage("Status must be between 1 and 64 characters");

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be >= 1");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100");
    }
}
