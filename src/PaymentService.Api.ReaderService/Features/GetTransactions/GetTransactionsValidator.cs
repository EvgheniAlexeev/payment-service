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
/// <para><strong>@purpose:</strong> Validates account transaction query input parameters</para>
/// <para><strong>@module-type:</strong> UTILITY (validator)</para>
/// <para><strong>@invariant:</strong> AccountId 1-64 characters, non-empty</para>
/// <para><strong>@invariant:</strong> Skip ≥ 0, Limit 1-100</para>
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
    }
}
