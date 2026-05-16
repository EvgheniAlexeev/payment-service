/**
 * @contract M-READER-PAY
 * @purpose Provides HTTP query endpoints for payment retrieval with fast synchronized reads from MongoDB
 * @module-type ENTRY_POINT
 * @depends M-SHARED-PAY, M-MONGO-PAY
 * @verification-ref V-M-READER-PAY
 * @semantic-domain Payment, Query, Retrieval
 * @invariant Response latency p99 ≤ 100ms
 * @invariant All queries validated before database access
 * @error-strategy ValidationException, NotFoundException, TimeoutException
 * @stability STABLE
 */

namespace PaymentService.Api.ReaderService
{
    /**
     * @domain-concept PaymentQueryController
     * @purpose Handles HTTP GET requests for payment queries
     */
    public class PaymentQueryController
    {
        /**
         * @contract-action GetPayment
         * @param correlationId Unique payment identifier
         * @return GetPaymentResponse with payment details
         * @throws NotFoundException — when payment not found
         * @throws ValidationException — when correlationId invalid
         * @throws TimeoutException — when database query exceeds timeout
         * @log-event reader.controller.get-payment-start {correlationId}
         * @log-event reader.controller.get-payment-success {correlationId}
         * @log-event reader.controller.get-payment-error {correlationId} {error}
         * @trace-span reader.get-payment
         * @pre-condition correlationId != null && correlationId.Length > 0
         * @post-condition result != null
         * @complexity O(1) (indexed query)
         * @idempotent YES
         * @side-effect Query audit log entry
         * @http GET /api/payments/{correlationId}
         * @http-response 200 GetPaymentResponse
         * @http-response 404 Payment not found
         * @http-response 400 Invalid input
         */
        public Task<IActionResult> GetPayment(string correlationId, CancellationToken ct);

        /**
         * @contract-action QueryPayments
         * @param request QueryPaymentsRequest with filters and pagination
         * @return Paginated list of payments
         * @throws ValidationException — when request constraints violated
         * @throws TimeoutException — when database query exceeds timeout
         * @log-event reader.controller.query-payments-start {skip} {take}
         * @log-event reader.controller.query-payments-success {count} {elapsed_ms}
         * @log-event reader.controller.query-payments-error {error}
         * @trace-span reader.query-payments
         * @pre-condition request != null && request.Skip >= 0 && request.Take > 0
         * @post-condition result != null && result.Items.Count <= request.Take
         * @complexity O(log n + k) where k = result set size
         * @idempotent YES
         * @side-effect Query audit log entry
         * @http POST /api/payments/query
         * @http-body QueryPaymentsRequest
         * @http-response 200 QueryPaymentsResponse
         * @http-response 400 Invalid input
         */
        public Task<IActionResult> QueryPayments([FromBody] QueryPaymentsRequest request, CancellationToken ct);
    };

    /**
     * @domain-concept GetPaymentHandler
     * @purpose Business logic for retrieving a single payment by correlation ID
     */
    public class GetPaymentHandler
    {
        /**
         * @contract-action Handle
         * @param query GetPaymentRequest with correlationId
         * @param ct Cancellation token
         * @return GetPaymentResponse with payment details
         * @throws NotFoundException — payment does not exist
         * @throws ValidationException — correlationId invalid format
         * @log-event reader.handler.get-payment-repository-query {correlationId}
         * @log-event reader.handler.get-payment-found {correlationId}
         * @log-event reader.handler.get-payment-not-found {correlationId}
         * @trace-span reader.handler.get-payment
         * @pre-condition query != null && query.CorrelationId.Length > 0
         * @post-condition result != null
         * @idempotent YES
         * @complexity O(1) (index lookup)
         * @pure NO (I/O: database read)
         */
        public Task<GetPaymentResponse> Handle(GetPaymentRequest query, CancellationToken ct);
    };

    /**
     * @domain-concept QueryPaymentsHandler
     * @purpose Business logic for querying multiple payments with filters
     */
    public class QueryPaymentsHandler
    {
        /**
         * @contract-action Handle
         * @param query QueryPaymentsRequest with filters
         * @param ct Cancellation token
         * @return Paginated QueryPaymentsResponse
         * @throws ValidationException — request validation failed
         * @log-event reader.handler.query-payments-filter-applied {status} {skip} {take}
         * @log-event reader.handler.query-payments-repository-query-start {skip} {take}
         * @log-event reader.handler.query-payments-repository-query-complete {count} {elapsed_ms}
         * @trace-span reader.handler.query-payments
         * @pre-condition query != null && query.Skip >= 0 && query.Take > 0 && query.Take <= 1000
         * @post-condition result != null && result.Items.Count <= query.Take
         * @idempotent YES
         * @complexity O(log n + k)
         * @pure NO (I/O: database read)
         */
        public Task<QueryPaymentsResponse> Handle(QueryPaymentsRequest query, CancellationToken ct);
    };

    /**
     * @domain-concept GetPaymentValidator
     * @contract-action Validate
     * @param request GetPaymentRequest to validate
     * @return ValidationResult
     * @throws ValidationException — correlationId invalid
     * @log-event reader.validator.get-payment-validate
     * @pre-condition request != null
     * @post-condition result.IsValid || result.Errors.Count > 0
     * @idempotent YES
     * @pure YES
     * @invariant CorrelationId required
     * @invariant CorrelationId length 1-50 chars
     */
    public class GetPaymentValidator : AbstractValidator<GetPaymentRequest>
    {
    };

    /**
     * @domain-concept QueryPaymentsValidator
     * @contract-action Validate
     * @param request QueryPaymentsRequest to validate
     * @return ValidationResult
     * @throws ValidationException — constraints violated
     * @log-event reader.validator.query-payments-validate
     * @pre-condition request != null
     * @post-condition result.IsValid || result.Errors.Count > 0
     * @idempotent YES
     * @pure YES
     * @invariant Skip >= 0
     * @invariant Take > 0 and Take <= 1000
     * @invariant Status if provided must be valid enum
     */
    public class QueryPaymentsValidator : AbstractValidator<QueryPaymentsRequest>
    {
    };
}
