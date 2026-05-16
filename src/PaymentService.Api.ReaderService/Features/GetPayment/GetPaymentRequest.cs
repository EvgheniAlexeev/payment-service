// FILE: src/PaymentService.Api.ReaderService/Features/GetPayment/GetPaymentRequest.cs
// VERSION: 2.0.0
// MODULE: M-READER
// PURPOSE: Query request DTO
// SEMANTIC_TAG: [QUERY_DTO, INPUT_VALIDATION]
// START_MODULE M_READER

// FILE: src/PaymentService.Api.ReaderService/Features/GetPayment/GetPaymentRequest.cs
// VERSION: 1.0.0

using System.ComponentModel.DataAnnotations;

namespace PaymentService.Api.ReaderService.Features.GetPayment;

/// <summary>
/// Request model for GetPayment feature.
/// VSA feature: GetPayment (ReaderService)
/// </summary>
public class GetPaymentRequest
{
    /// <summary>
    /// Correlation ID of the payment to retrieve.
    /// </summary>
    [Required]
    [MinLength(1)]
    [MaxLength(128)]
    public string CorrelationId { get; set; } = string.Empty;
}
