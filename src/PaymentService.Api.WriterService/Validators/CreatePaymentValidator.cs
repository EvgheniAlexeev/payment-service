// FILE: src/PaymentService.Api.WriterService/Validators/CreatePaymentValidator.cs
using PaymentService.Shared.Commands;
// VERSION: 2.0.0
// MODULE: M-WRITER
// PURPOSE: FluentValidation rules
// SEMANTIC_TAG: [VALIDATOR]
// START_MODULE M_WRITER

// FILE: src/PaymentService.Api.WriterService/Validators/CreatePaymentValidator.cs
// VERSION: 1.0.0

using FluentValidation;
using Microsoft.Extensions.Logging;

namespace PaymentService.Api.WriterService.Validators;

/// <summary>
/// BLOCK_VALIDATE CreatePaymentRequest validation rules.
/// Enforces: non-empty CorrelationId, valid amount > 0, valid currency code.
/// </summary>
public class CreatePaymentValidator : AbstractValidator<CreatePaymentRequest>
{
    private readonly ILogger<CreatePaymentValidator> _logger;

    public CreatePaymentValidator(ILogger<CreatePaymentValidator> logger)
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
            .Must(IsValidCurrencyCode).WithMessage("Currency must contain only uppercase letters A-Z");

        RuleFor(x => x.ValueDate)
            .Must(d => d >= DateTime.UtcNow.Date.AddDays(-1))
            .WithMessage("ValueDate cannot be in the past");
    }

    private static bool IsValidCurrencyCode(string code) =>
        code.Length == 3 && code.All(char.IsUpper);

    /// <summary>
    /// Validate with semantic log markers.
    /// </summary>
    public new async Task<FluentValidation.Results.ValidationResult> ValidateAsync(
        CreatePaymentRequest instance, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[PaymentService.Api.WriterService][CreatePaymentValidator][BLOCK_VALIDATE] " +
            "Validating create payment request {CorrelationId}", instance.CorrelationId);

        var result = await base.ValidateAsync(instance, ct);

        if (result.IsValid)
        {
            _logger.LogInformation(
                "[PaymentService.Api.WriterService][CreatePaymentValidator][BLOCK_VALIDATE] " +
                "Payment validation passed {CorrelationId}", instance.CorrelationId);
        }
        else
        {
            _logger.LogWarning(
                "[PaymentService.Api.WriterService][CreatePaymentValidator][BLOCK_VALIDATE] " +
                "Payment validation failed {CorrelationId} {@Errors}",
                instance.CorrelationId, result.Errors);
        }

        return result;
    }
}
