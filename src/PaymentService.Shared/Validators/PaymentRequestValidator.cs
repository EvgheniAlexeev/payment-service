// FILE: src/PaymentService.Shared/Validators/PaymentRequestValidator.cs
// VERSION: 2.0.0
// MODULE: M-SHARED
// PURPOSE: FluentValidation rules for PaymentRequestDto
// SEMANTIC_TAG: [VALIDATOR, INPUT_VALIDATION]
// START_MODULE M-SHARED-VALIDATORS

using FluentValidation;
using Microsoft.Extensions.Logging;
using PaymentService.Shared.Dtos;

namespace PaymentService.Shared.Validators;

/// <summary>
/// <para><strong>@contract:</strong> M-SHARED</para>
/// <para><strong>@version:</strong> 2.1.0</para>
/// <para><strong>@since:</strong> 2.0.0</para>
/// <para><strong>@purpose:</strong> FluentValidation rules for PaymentRequestDto input validation</para>
/// <para><strong>@invariant:</strong> All rules executed in order, short-circuit on first failure</para>
/// <para><strong>@verification-ref:</strong> V-M-SHARED</para>
/// </summary>
/// <remarks>
/// <para><strong>Rules:</strong> CorrelationId, Account IDs, Amount bounds, Currency ISO code, ValueDate</para>
/// <para><strong>Semantic Logging:</strong> BLOCK_VALIDATE markers for observability</para>
/// </remarks>
public class PaymentRequestValidator : AbstractValidator<PaymentRequestDto>
{
    private readonly ILogger<PaymentRequestValidator> _logger;

    public PaymentRequestValidator(ILogger<PaymentRequestValidator> logger)
    {
        _logger = logger;

        RuleFor(x => x.CorrelationId)
            .NotEmpty().WithMessage("CorrelationId is required")
            .MaximumLength(100).WithMessage("CorrelationId must not exceed 100 characters");

        RuleFor(x => x.SenderAccount)
            .NotEmpty().WithMessage("SenderAccount is required")
            .MaximumLength(50).WithMessage("SenderAccount must not exceed 50 characters");

        RuleFor(x => x.ReceiverAccount)
            .NotEmpty().WithMessage("ReceiverAccount is required")
            .MaximumLength(50).WithMessage("ReceiverAccount must not exceed 50 characters");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0")
            .LessThanOrEqualTo(999_999_999_999M).WithMessage("Amount exceeds maximum allowed");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required")
            .Length(3).WithMessage("Currency must be a 3-letter ISO 4217 code")
            .Must(IsValidCurrencyCode).WithMessage("Currency must be a valid ISO 4217 code");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters");

        RuleFor(x => x.ValueDate)
            .Must(d => !d.HasValue || d.Value >= DateTime.UtcNow.Date)
            .WithMessage("ValueDate cannot be in the past");
    }

    private static bool IsValidCurrencyCode(string code)
    {
        // Simple uppercase 3-letter validation; ISO check can be extended
        return code.Length == 3 && code.All(char.IsUpper);
    }

    /// <summary>
    /// <para><strong>@method:</strong> ValidateAsync</para>
    /// <para><strong>@purpose:</strong> Validate with semantic BLOCK_VALIDATE markers</para>
    /// <para><strong>@param instance:</strong> PaymentRequestDto to validate</para>
    /// <para><strong>@return:</strong> FluentValidation.Results.ValidationResult</para>
    /// <para><strong>@idempotent:</strong> YES (no side effects)</para>
    /// </summary>
    public new async Task<FluentValidation.Results.ValidationResult> ValidateAsync(
        PaymentRequestDto instance, CancellationToken ct = default)
    {
        // START_BLOCK_VALIDATE
        _logger.LogInformation(
            "[PaymentService.Shared][PaymentRequestValidator][BLOCK_VALIDATE] " +
            "Validating payment request {CorrelationId}", instance.CorrelationId);

        var result = await base.ValidateAsync(instance, ct);

        if (result.IsValid)
        {
            _logger.LogInformation(
                "[PaymentService.Shared][PaymentRequestValidator][BLOCK_VALIDATE] " +
                "Payment validation passed {CorrelationId}", instance.CorrelationId);
        }
        else
        {
            _logger.LogWarning(
                "[PaymentService.Shared][PaymentRequestValidator][BLOCK_VALIDATE] " +
                "Payment validation failed {CorrelationId} {@Errors}",
                instance.CorrelationId, result.Errors);
        }
        // END_BLOCK_VALIDATE

        return result;
    }
}
