// FILE: src/PaymentService.Api.ReaderService/Features/GetPayment/GetPaymentResponse.cs
// VERSION: 2.0.0
// MODULE: M-READER
// PURPOSE: Query response DTO
// SEMANTIC_TAG: [RESPONSE_DTO, OUTPUT]
// START_MODULE M_READER

// FILE: src/PaymentService.Api.ReaderService/Features/GetPayment/GetPaymentResponse.cs
// VERSION: 1.0.0

using PaymentService.Shared.Dtos;

namespace PaymentService.Api.ReaderService.Features.GetPayment;

/// <summary>
/// Response wrapper for GetPayment feature.
/// VSA feature: GetPayment (ReaderService)
/// </summary>
public class GetPaymentResponse
{
    public PaymentStatusDto Payment { get; set; } = null!;
}
