/**
 * @contract M-WORKER-PAY
 * @purpose Orchestrates payment saga with Wolverine message handlers, compensation, and DLQ integration
 * @module-type CORE_LOGIC
 * @depends M-SHARED-PAY, M-MONGO-PAY
 * @verification-ref V-M-WORKER-PAY
 * @semantic-domain Saga, Orchestration, Compensation
 * @invariant Saga steps: Validate → Enrich → Reserve Funds → Settle → Notify
 * @invariant Compensation on failure: Reverse settlement → Release reserved funds
 * @invariant Failed payments published to DLQ for manual intervention
 * @invariant Saga state persisted after each step for replay capability
 * @error-strategy TimeoutException, CompensationException, PublishException
 * @stability EVOLVING (Phase-5 extensions in progress)
 */

namespace PaymentService.WorkerService
{
    /**
     * @domain-concept PaymentSaga
     * @aggregate-root YES
     * @purpose Orchestrates multi-step payment settlement with compensation
     * @invariant Step execution order: validate → enrich → reserve → settle → notify
     * @invariant State persisted after each successful step
     * @invariant Failure triggers compensation sequence
     */
    public class PaymentSaga : Saga<PaymentSagaState>
    {
        /**
         * @contract-action ValidatePayment
         * @param command ValidatePaymentCommand
         * @log-event saga.payment-validate-step-start {correlationId}
         * @log-event saga.payment-validate-step-success {correlationId}
         * @log-event saga.payment-validate-step-error {correlationId} {reason}
         * @trace-span saga.validate-payment
         * @pre-condition saga.State == "Pending" || saga.State == "Retrying"
         * @post-condition saga.State == "Validated" || saga.State == "Failed"
         * @idempotent YES (via ledger check)
         * @invariant Validation must pass before proceeding to enrichment
         */
        public void HandleValidatePayment(ValidatePaymentCommand command);

        /**
         * @contract-action EnrichPayment
         * @param command EnrichPaymentCommand
         * @log-event saga.payment-enrich-step-start {correlationId}
         * @log-event saga.payment-enrich-step-success {correlationId}
         * @trace-span saga.enrich-payment
         * @pre-condition saga.State == "Validated"
         * @post-condition saga.State == "Enriched" || saga.State == "EnrichmentFailed"
         * @idempotent YES
         */
        public void HandleEnrichPayment(EnrichPaymentCommand command);

        /**
         * @contract-action ReserveFunds
         * @param command ReserveFundsCommand
         * @log-event saga.payment-reserve-start {correlationId} {amount}
         * @log-event saga.payment-reserve-success {correlationId}
         * @log-event saga.payment-reserve-error {correlationId} {error}
         * @trace-span saga.reserve-funds
         * @pre-condition saga.State == "Enriched"
         * @post-condition saga.State == "FundsReserved" || saga.State == "ReservationFailed"
         * @throws InsufficientFundsException — sender has insufficient balance
         * @idempotent YES
         */
        public void HandleReserveFunds(ReserveFundsCommand command);

        /**
         * @contract-action SettlePayment
         * @param command SettlePaymentCommand
         * @log-event saga.payment-settle-step-start {correlationId}
         * @log-event saga.payment-settle-step-success {correlationId} {settlementId}
         * @log-event saga.payment-settle-step-error {correlationId} {error}
         * @trace-span saga.settle-payment
         * @pre-condition saga.State == "FundsReserved"
         * @post-condition saga.State == "Settled" || saga.State == "SettlementFailed"
         * @idempotent YES
         */
        public void HandleSettlePayment(SettlePaymentCommand command);

        /**
         * @contract-action NotifyCompletion
         * @param command NotifyCompletionCommand
         * @log-event saga.payment-notify-step-start {correlationId}
         * @log-event saga.payment-notify-step-success {correlationId}
         * @trace-span saga.notify-completion
         * @pre-condition saga.State == "Settled"
         * @post-condition saga.State == "Completed"
         * @idempotent YES
         */
        public void HandleNotifyCompletion(NotifyCompletionCommand command);

        /**
         * @contract-action CompensateOnFailure
         * @param failureContext Failure information
         * @log-event saga.payment-compensate-start {correlationId} {failureStep}
         * @log-event saga.payment-compensate-settle-reverse {correlationId}
         * @log-event saga.payment-compensate-funds-release {correlationId}
         * @log-event saga.payment-compensate-success {correlationId}
         * @log-event saga.payment-compensate-error {correlationId} {error}
         * @trace-span saga.compensate-failure
         * @pre-condition failureContext != null
         * @post-condition saga.State == "Compensated" || saga.State == "CompensationFailed"
         * @idempotent NO (state transition to terminal state)
         */
        public void Compensate(FailureContext failureContext);

        /**
         * @contract-action PublishToDLQ
         * @param failureEvent FailedPaymentEvent
         * @log-event saga.payment-dlq-publish-start {correlationId}
         * @log-event saga.payment-dlq-publish-success {correlationId}
         * @log-event saga.payment-dlq-publish-error {correlationId} {error}
         * @trace-span saga.publish-dlq
         * @pre-condition failureEvent != null
         * @throws PublishException — DLQ publication failed
         * @idempotent NO (side effect: message queue)
         */
        public Task PublishToDLQ(FailedPaymentEvent failureEvent, CancellationToken ct);
    };

    /**
     * @domain-concept ValidatePaymentHandler
     * @purpose Executes payment validation step
     */
    public class ValidatePaymentHandler
    {
        /**
         * @contract-action Handle
         * @param command ValidatePaymentCommand
         * @param ct Cancellation token
         * @log-event worker.handler.validate-payment-contract-check {correlationId}
         * @log-event worker.handler.validate-payment-ledger-check {correlationId}
         * @log-event worker.handler.validate-payment-success {correlationId}
         * @trace-span worker.validate-payment
         * @throws ValidationException — payment validation failed
         * @throws PersistenceException — ledger check failed
         * @pre-condition command != null && command.Payment != null
         * @post-condition (side effect: saga state updated)
         * @idempotent YES (ledger-based deduplication)
         */
        public Task Handle(ValidatePaymentCommand command, CancellationToken ct);
    };

    /**
     * @domain-concept ReserveFundsHandler
     * @purpose Executes fund reservation step
     */
    public class ReserveFundsHandler
    {
        /**
         * @contract-action Handle
         * @param command ReserveFundsCommand
         * @param ct Cancellation token
         * @throws InsufficientFundsException — sender balance insufficient
         * @log-event worker.handler.reserve-funds-query-balance {correlationId} {senderAccount}
         * @log-event worker.handler.reserve-funds-success {correlationId} {reservationId}
         * @log-event worker.handler.reserve-funds-insufficient {correlationId}
         * @trace-span worker.reserve-funds
         * @idempotent YES
         */
        public Task Handle(ReserveFundsCommand command, CancellationToken ct);
    };

    /**
     * @domain-concept SettlePaymentHandler
     * @purpose Executes settlement step
     */
    public class SettlePaymentHandler
    {
        /**
         * @contract-action Handle
         * @param command SettlePaymentCommand
         * @param ct Cancellation token
         * @log-event worker.handler.settle-payment-transfer-execute {correlationId}
         * @log-event worker.handler.settle-payment-success {correlationId} {settlementId}
         * @trace-span worker.settle-payment
         * @idempotent YES
         */
        public Task Handle(SettlePaymentCommand command, CancellationToken ct);
    };

    /**
     * @domain-concept CompensationService
     * @purpose Handles compensation logic when saga fails
     */
    public interface ICompensationService
    {
        /**
         * @contract-action ReverseSettlementAsync
         * @param correlationId Payment identifier
         * @param settlementId Settlement to reverse
         * @param ct Cancellation token
         * @log-event worker.compensation.settle-reverse {correlationId} {settlementId}
         * @trace-span worker.compensation.reverse-settlement
         * @throws CompensationException — reversal failed
         * @idempotent NO
         */
        Task ReverseSettlementAsync(string correlationId, string settlementId, CancellationToken ct);

        /**
         * @contract-action ReleaseFundsAsync
         * @param correlationId Payment identifier
         * @param reservationId Reservation to release
         * @param ct Cancellation token
         * @log-event worker.compensation.funds-release {correlationId} {reservationId}
         * @trace-span worker.compensation.release-funds
         * @idempotent NO
         */
        Task ReleaseFundsAsync(string correlationId, string reservationId, CancellationToken ct);
    };

    /**
     * @domain-concept IDLQPublisher
     * @purpose Publishes failed payments to dead-letter queue
     */
    public interface IDLQPublisher
    {
        /**
         * @contract-action PublishFailedPaymentAsync
         * @param failureEvent FailedPaymentEvent with original request + error
         * @param ct Cancellation token
         * @log-event worker.dlq.publish-failed-payment {correlationId}
         * @log-event worker.dlq.publish-success {correlationId}
         * @trace-span worker.dlq.publish
         * @throws PublishException — publication failed
         * @idempotent NO (side effect)
         */
        Task PublishFailedPaymentAsync(FailedPaymentEvent failureEvent, CancellationToken ct);
    };

    /**
     * @domain-concept PaymentSagaMetrics
     * @purpose Observability: Prometheus metrics for saga execution
     */
    public class PaymentSagaMetrics
    {
        /**
         * @contract-action RecordValidationAttempt
         * @param correlationId Payment identifier
         * @param durationMs Step execution duration
         * @log-event metrics.saga.validation-recorded {correlationId} {durationMs}ms
         * @pure NO (metrics collection)
         */
        public void RecordValidationAttempt(string correlationId, long durationMs);

        /**
         * @contract-action RecordSagaCompletion
         * @param correlationId Payment identifier
         * @param totalDurationMs Total saga execution time
         * @log-event metrics.saga.completion-recorded {correlationId} {totalDurationMs}ms
         */
        public void RecordSagaCompletion(string correlationId, long totalDurationMs);

        /**
         * @contract-action RecordCompensation
         * @param correlationId Payment identifier
         * @param compensationSteps Number of compensation steps executed
         * @log-event metrics.saga.compensation-recorded {correlationId} {compensationSteps} steps
         */
        public void RecordCompensation(string correlationId, int compensationSteps);
    };
}
