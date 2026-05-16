/**
 * @contract M-SHARED-PAY
 * @purpose Provides shared DTOs, events, commands, validators, and domain models for payment operations
 * @module-type UTILITY
 * @depends none (leaf module)
 * @verification-ref V-M-SHARED-PAY
 * @semantic-domain Payment, Settlement, Finance
 * @invariant PaymentRequestDto.Amount > 0
 * @invariant PaymentRequestDto.CorrelationId is unique across system lifetime
 * @invariant PaymentDocument status transitions: Pending → Validating → Enriching → Settling → Settled/Failed
 * @error-strategy ValidationException, SerializationException, DomainException
 * @stability STABLE
 */

namespace PaymentService.Shared
{
    /**
     * @domain-concept PaymentRequest
     * @value-object YES
     * @invariant Amount must be positive decimal value
     * @invariant Currency must be valid ISO 4217 code
     */
    public record PaymentRequestDto
    {
        /**
         * @contract-action GetCorrelationId
         * @return Unique payment identifier
         * @log-event shared.payment-request.correlation-id
         * @pure YES
         */
        public string CorrelationId { get; init; }

        /**
         * @contract-action GetAmount
         * @return Payment amount in specified currency
         * @log-event shared.payment-request.amount
         * @valid-range 0.01 to 999999999.99
         * @unit currency-unit
         * @pure YES
         */
        public decimal Amount { get; init; }

        /**
         * @contract-action GetCurrency
         * @return ISO 4217 currency code
         * @log-event shared.payment-request.currency
         * @pure YES
         */
        public string Currency { get; init; }

        /**
         * @contract-action GetValueDate
         * @return Settlement date for payment
         * @log-event shared.payment-request.value-date
         * @pure YES
         */
        public DateTime ValueDate { get; init; }

        /**
         * @contract-action GetDescription
         * @return Payment purpose (may contain sensitive data)
         * @log-event shared.payment-request.description [REDACTED]
         * @pure YES
         */
        public string Description { get; init; }
    };

    /**
     * @domain-concept PaymentCommand
     * @value-object YES
     * @invariant IdempotencyKey must be set for deduplication
     */
    public record PaymentCommand : ICommand
    {
        /**
         * @contract-action GetCorrelationId
         * @return Saga correlation identifier
         * @pure YES
         */
        public string CorrelationId { get; init; }

        /**
         * @contract-action GetRequest
         * @return Original payment request
         * @pure YES
         */
        public PaymentRequestDto Request { get; init; }

        /**
         * @contract-action GetIdempotencyKey
         * @return Idempotency key for replay detection
         * @pure YES
         */
        public string IdempotencyKey { get; init; }
    };

    /**
     * @domain-concept PaymentSettled
     * @value-object YES
     */
    public record PaymentSettled : IEvent
    {
        /**
         * @contract-action GetCorrelationId
         * @return Associated payment identifier
         * @pure YES
         */
        public string CorrelationId { get; init; }

        /**
         * @contract-action GetSettlementId
         * @return Settlement transaction identifier
         * @pure YES
         */
        public string SettlementId { get; init; }

        /**
         * @contract-action GetStatus
         * @return Settlement status: Settled, Failed, Compensated
         * @pure YES
         */
        public string Status { get; init; }

        /**
         * @contract-action GetSettledAt
         * @return Timestamp of settlement completion
         * @pure YES
         */
        public DateTime SettledAt { get; init; }
    };

    /**
     * @domain-concept PaymentDocument
     * @aggregate-root YES
     * @identity-field CorrelationId
     * @invariant Status must be one of: Pending, Validating, Enriching, Settling, Settled, Failed
     * @invariant SagaState must match current step in payment saga
     */
    public record PaymentDocument
    {
        /**
         * @contract-action GetId
         * @return MongoDB document identifier
         * @pure YES
         */
        public string Id { get; init; }

        /**
         * @contract-action GetCorrelationId
         * @return Payment correlation identifier (query key)
         * @pure YES
         */
        public string CorrelationId { get; init; }

        /**
         * @contract-action GetRequest
         * @return Original payment request
         * @pure YES
         */
        public PaymentRequestDto Request { get; init; }

        /**
         * @contract-action GetStatus
         * @return Current payment status
         * @pure YES
         */
        public string Status { get; init; }

        /**
         * @contract-action GetSagaState
         * @return Current saga step: Validating, Enriching, Settling, Notifying
         * @pure YES
         */
        public string SagaState { get; init; }

        /**
         * @contract-action GetCreatedAt
         * @return Document creation timestamp
         * @pure YES
         */
        public DateTime CreatedAt { get; init; }

        /**
         * @contract-action GetSettledAt
         * @return Settlement completion timestamp (null if not settled)
         * @pure YES
         */
        public DateTime? SettledAt { get; init; }
    };

    /**
     * @domain-concept PaymentRequestValidator
     * @contract-action Validate
     * @param request PaymentRequestDto to validate
     * @return ValidationResult with errors if present
     * @throws ValidationException — when required fields missing or constraints violated
     * @log-event shared.validator.payment-request-validate
     * @pre-condition request != null
     * @post-condition result.IsValid || result.Errors.Count > 0
     * @idempotent YES
     * @pure YES
     * @invariant Amount must be > 0
     * @invariant Currency must be valid ISO 4217 code
     * @invariant CorrelationId length 1-50 chars
     */
    public class PaymentRequestValidator : AbstractValidator<PaymentRequestDto>
    {
    };

    /**
     * @domain-concept PaymentSerializer
     * @contract-action SerializePayment
     * @param payment PaymentDocument to serialize
     * @return JSON string representation
     * @throws SerializationException — on serialization failure
     * @log-event shared.serializer.payment-serialize
     * @pre-condition payment != null
     * @post-condition result != null && result.Length > 0
     * @idempotent YES
     * @pure YES
     */
    public class PaymentSerializer
    {
        /**
         * @contract-action DeserializePayment
         * @param json JSON string to deserialize
         * @return Deserialized PaymentDocument
         * @throws DeserializationException — on invalid JSON
         * @log-event shared.serializer.payment-deserialize
         * @pre-condition json != null && json.Length > 0
         * @post-condition result != null
         * @idempotent YES
         * @pure YES
         */
        public PaymentDocument DeserializePayment(string json);
    };
}
