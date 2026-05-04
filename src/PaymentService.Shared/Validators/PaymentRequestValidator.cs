// FILE: src/PaymentService.Shared/Validators/PaymentRequestValidator.cs
// VERSION: 1.0.0

using FluentValidation;
using Microsoft.Extensions.Logging;
using PaymentService.Shared.Dtos;

namespace PaymentService.Shared.Validators;

/// <summary>
/// BLOCK_VALIDATE PaymentRequestDto validation rules.
/// Enforces: non-empty CorrelationId, valid amount, valid currency.
/// </summary>
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

        When(x => x.ValueDate.HasValue, () =>
        {
            RuleFor(x => x.ValueDate!.Value)
                .Must(d => d >= DateTime.UtcNow.Date.AddDays(-1))
                .WithMessage("ValueDate cannot be in the past");
        });
    }

    private static bool IsValidCurrencyCode(string code)
    {
        // Simple uppercase 3-letter validation; ISO check can be extended
        return code.Length == 3 && code.All(char.IsUpper);
    }

    /// <summary>
    /// Validate with semantic log markers.
    /// </summary>
    public new async Task<FluentValidation.Results.ValidationResult> ValidateAsync(
        PaymentRequestDto instance, CancellationToken ct = default)
    {
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

        return result;
    }
}
