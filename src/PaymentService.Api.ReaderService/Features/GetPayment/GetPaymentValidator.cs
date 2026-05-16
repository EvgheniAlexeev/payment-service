// FILE: src/PaymentService.Api.ReaderService/Features/GetPayment/GetPaymentValidator.cs
using PaymentService.Shared.Dtos;
// VERSION: 2.0.0
// MODULE: M-READER
// PURPOSE: FluentValidation rules for request DTOs
// SEMANTIC_TAG: [VALIDATOR, INPUT_VALIDATION]
// START_MODULE M_READER

// FILE: src/PaymentService.Api.ReaderService/Features/GetPayment/GetPaymentValidator.cs
// VERSION: 1.0.0

using FluentValidation;

namespace PaymentService.Api.ReaderService.Features.GetPayment;

/// <summary>
/// Validator for GetPayment feature requests.
/// VSA feature: GetPayment (ReaderService)
/// </summary>
public class GetPaymentValidator : AbstractValidator<GetPaymentRequest>
{
    public GetPaymentValidator()
    {
        RuleFor(x => x.CorrelationId)
            .NotEmpty()
            .MinimumLength(1)
            .MaximumLength(128)
            .WithMessage("CorrelationId must be between 1 and 128 characters");
    }
}
