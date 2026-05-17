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
    }
}
