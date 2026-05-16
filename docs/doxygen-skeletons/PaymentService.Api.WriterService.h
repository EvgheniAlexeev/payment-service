/**
 * @contract M-WRITER-PAY
 * @purpose Provides HTTP command endpoint for payment submission with async saga initiation via Dapr pub/sub
 * @module-type ENTRY_POINT
 * @depends M-SHARED-PAY, M-MONGO-PAY
 * @verification-ref V-M-WRITER-PAY
 * @semantic-domain Payment, Command, Ingestion
 * @invariant Response latency p99 ≤ 2s (202 Accepted returned immediately)
 * @invariant All requests validated before persistence
 * @invariant Idempotency key prevents duplicate processing
 * @error-strategy ValidationException, ConflictException, TimeoutException
 * @stability STABLE
 */

namespace PaymentService.Api.WriterService
{
    /**
     * @domain-concept PaymentCommandController
     * @purpose Handles HTTP POST requests for payment submission
     */
    public class PaymentCommandController
    {
        /**
         * @contract-action CreatePayment
         * @param request CreatePaymentRequest with payment details
         * @return 202 Accepted with correlationId
         * @throws ValidationException — request validation failed
         * @throws ConflictException — idempotency key already processed
         * @throws TimeoutException — database write exceeded timeout
         * @log-event writer.controller.create-payment-start {correlationId}
         * @log-event writer.controller.create-payment-accepted {correlationId}
         * @log-event writer.controller.create-payment-error {correlationId} {error}
         * @trace-span writer.create-payment
         * @pre-condition request != null
         * @post-condition response.StatusCode == 202
         * @complexity O(1) (direct write)
         * @idempotent YES (via idempotency key)
         * @side-effect Persists payment document, publishes event to message queue
         * @http POST /api/payments
         * @http-body CreatePaymentRequest
         * @http-response 202 Accepted with Location header
         * @http-response 400 Validation error
         * @http-response 409 Duplicate payment (idempotency key conflict)
         */
        public Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request, CancellationToken ct);
    };

    /**
     * @domain-concept CreatePaymentHandler
     * @purpose Business logic for payment creation with persistence and event publishing
     */
    public class CreatePaymentHandler
    {
        /**
         * @contract-action Handle
         * @param command PaymentCommand with request
         * @param ct Cancellation token
         * @return CreatePaymentResponse with correlationId
         * @throws ValidationException — command invalid
         * @throws ConflictException — payment already exists (idempotency)
         * @throws PersistenceException — database write failed
         * @log-event writer.handler.create-payment-validate
         * @log-event writer.handler.create-payment-document-persist {correlationId}
         * @log-event writer.handler.create-payment-event-publish {correlationId}
         * @log-event writer.handler.create-payment-success {correlationId}
         * @trace-span writer.handler.create-payment
         * @pre-condition command != null && command.Request != null
         * @post-condition result != null && result.CorrelationId != null
         * @idempotent YES (idempotency ledger check)
         * @complexity O(1) (direct write)
         * @pure NO (I/O: persistence + message publishing)
         */
        public Task<CreatePaymentResponse> Handle(PaymentCommand command, CancellationToken ct);
    };

    /**
     * @domain-concept IMessagePublisher
     * @purpose Interface for publishing payment commands to message queue
     */
    public interface IMessagePublisher
    {
        /**
         * @contract-action PublishPaymentCommand
         * @param command PaymentCommand to publish
         * @param ct Cancellation token
         * @throws PublishException — message publication failed
         * @log-event writer.publisher.payment-command-publish {correlationId}
         * @log-event writer.publisher.payment-command-publish-success {correlationId}
         * @log-event writer.publisher.payment-command-publish-error {correlationId} {error}
         * @trace-span writer.publisher.publish-command
         * @pre-condition command != null
         * @post-condition (no return value, success = no exception)
         * @idempotent NO (side effect: message queuing)
         * @pure NO (I/O: message broker)
         */
        Task PublishPaymentCommand(PaymentCommand command, CancellationToken ct);
    };

    /**
     * @domain-concept CreatePaymentValidator
     * @contract-action Validate
     * @param request CreatePaymentRequest to validate
     * @return ValidationResult
     * @throws ValidationException — constraints violated
     * @log-event writer.validator.create-payment-validate
     * @pre-condition request != null
     * @post-condition result.IsValid || result.Errors.Count > 0
     * @idempotent YES
     * @pure YES
     * @invariant Amount > 0
     * @invariant Currency is valid ISO 4217 code
     * @invariant SenderAccount and ReceiverAccount different
     * @invariant ValueDate >= today
     */
    public class CreatePaymentValidator : AbstractValidator<CreatePaymentRequest>
    {
    };
}
