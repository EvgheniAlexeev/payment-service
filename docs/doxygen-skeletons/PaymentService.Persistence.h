/**
 * @contract M-MONGO-PAY
 * @purpose Provides MongoDB persistence layer for payments, saga state, and idempotency tracking
 * @module-type DATA_LAYER
 * @depends M-SHARED-PAY
 * @verification-ref V-M-MONGO-PAY
 * @semantic-domain Persistence, Storage, Idempotency
 * @invariant CorrelationId is unique key across all payments
 * @invariant Saga state transitions are atomic
 * @invariant Idempotency ledger entries TTL = 30 days
 * @error-strategy PersistenceException, DuplicateKeyException, TimeoutException
 * @stability STABLE
 */

namespace PaymentService.Persistence
{
    /**
     * @domain-concept IPaymentDocumentRepository
     * @purpose Data access interface for payment documents
     */
    public interface IPaymentDocumentRepository
    {
        /**
         * @contract-action GetByCorrelationIdAsync
         * @param correlationId Unique payment identifier
         * @param session MongoDB session (transactional)
         * @param ct Cancellation token
         * @return PaymentDocument or null
         * @throws TimeoutException — query exceeded timeout
         * @throws PersistenceException — database error
         * @log-event mongo.repository.get-by-correlation-id {correlationId}
         * @log-event mongo.repository.get-by-correlation-id-found {correlationId}
         * @log-event mongo.repository.get-by-correlation-id-not-found {correlationId}
         * @trace-span mongo.repo.get-by-correlation-id
         * @pre-condition correlationId != null && correlationId.Length > 0
         * @post-condition result == null || result.CorrelationId == correlationId
         * @complexity O(1) (index lookup)
         * @idempotent YES
         * @pure NO (I/O: database read)
         */
        Task<PaymentDocument?> GetByCorrelationIdAsync(string correlationId, IClientSessionHandle session, CancellationToken ct);

        /**
         * @contract-action InsertAsync
         * @param document PaymentDocument to insert
         * @param session MongoDB session (transactional)
         * @param ct Cancellation token
         * @throws DuplicateKeyException — CorrelationId already exists
         * @throws PersistenceException — database error
         * @log-event mongo.repository.insert-start {correlationId}
         * @log-event mongo.repository.insert-success {correlationId}
         * @log-event mongo.repository.insert-error {correlationId} {error}
         * @trace-span mongo.repo.insert
         * @pre-condition document != null && document.CorrelationId != null
         * @post-condition (no return value, success = no exception)
         * @complexity O(1) (direct write)
         * @idempotent NO (creates new document)
         * @pure NO (I/O: database write)
         */
        Task InsertAsync(PaymentDocument document, IClientSessionHandle session, CancellationToken ct);

        /**
         * @contract-action UpdateAsync
         * @param document PaymentDocument with updates
         * @param session MongoDB session (transactional)
         * @param ct Cancellation token
         * @throws PersistenceException — update failed
         * @log-event mongo.repository.update-start {correlationId}
         * @log-event mongo.repository.update-success {correlationId}
         * @trace-span mongo.repo.update
         * @pre-condition document != null && document.CorrelationId != null
         * @post-condition (no return value, success = no exception)
         * @idempotent NO (modifies document state)
         * @pure NO (I/O: database write)
         */
        Task UpdateAsync(PaymentDocument document, IClientSessionHandle session, CancellationToken ct);

        /**
         * @contract-action GetBatchAsync
         * @param correlationIds List of payment identifiers
         * @param ct Cancellation token
         * @return List of matching PaymentDocuments
         * @throws TimeoutException — query exceeded timeout
         * @log-event mongo.repository.get-batch {count}
         * @trace-span mongo.repo.get-batch
         * @pre-condition correlationIds != null && correlationIds.Count > 0
         * @post-condition result != null && result.Count <= correlationIds.Count
         * @complexity O(log n + k) where k = result set size
         * @idempotent YES
         * @pure NO (I/O: database read)
         */
        Task<List<PaymentDocument>> GetBatchAsync(List<string> correlationIds, CancellationToken ct);
    };

    /**
     * @domain-concept ISagaStateRepository
     * @purpose Data access for saga execution state
     */
    public interface ISagaStateRepository
    {
        /**
         * @contract-action GetByCorrelationIdAsync
         * @param correlationId Saga identifier
         * @param ct Cancellation token
         * @return SagaState or null
         * @log-event mongo.repository.saga-get {correlationId}
         * @trace-span mongo.repo.saga-get
         * @pre-condition correlationId != null
         * @post-condition result == null || result.CorrelationId == correlationId
         * @idempotent YES
         * @complexity O(1)
         */
        Task<SagaState?> GetByCorrelationIdAsync(string correlationId, CancellationToken ct);

        /**
         * @contract-action UpsertAsync
         * @param state SagaState to save (insert or update)
         * @param session MongoDB session (transactional)
         * @param ct Cancellation token
         * @log-event mongo.repository.saga-upsert {correlationId}
         * @trace-span mongo.repo.saga-upsert
         * @pre-condition state != null
         * @idempotent NO (modifies saga state)
         * @pure NO (I/O)
         */
        Task UpsertAsync(SagaState state, IClientSessionHandle session, CancellationToken ct);
    };

    /**
     * @domain-concept IIdempotencyLedger
     * @purpose Tracks completed saga steps for idempotent replay
     * @invariant Composite key: {correlationId}:{stepName}
     * @invariant TTL = 30 days, auto-cleanup after expiry
     */
    public interface IIdempotencyLedger
    {
        /**
         * @contract-action TryMarkCompleteAsync
         * @param correlationId Payment identifier
         * @param stepName Saga step name
         * @param session MongoDB session (transactional)
         * @param ct Cancellation token
         * @return true if marked complete, false if already complete
         * @throws PersistenceException — database error
         * @log-event mongo.ledger.mark-complete-attempt {correlationId} {stepName}
         * @log-event mongo.ledger.mark-complete-new {correlationId} {stepName}
         * @log-event mongo.ledger.mark-complete-duplicate {correlationId} {stepName}
         * @trace-span mongo.ledger.mark-complete
         * @pre-condition correlationId != null && stepName != null
         * @post-condition result == true || result == false
         * @complexity O(1)
         * @idempotent YES (safe to call multiple times)
         */
        Task<bool> TryMarkCompleteAsync(string correlationId, string stepName, IClientSessionHandle session, CancellationToken ct);

        /**
         * @contract-action IsStepCompleteAsync
         * @param correlationId Payment identifier
         * @param stepName Saga step name
         * @param ct Cancellation token
         * @return true if step already completed, false otherwise
         * @log-event mongo.ledger.is-complete-check {correlationId} {stepName}
         * @trace-span mongo.ledger.is-complete
         * @pre-condition correlationId != null && stepName != null
         * @post-condition result == true || result == false
         * @complexity O(1)
         * @idempotent YES
         * @pure NO (I/O: database read)
         */
        Task<bool> IsStepCompleteAsync(string correlationId, string stepName, CancellationToken ct);
    };

    /**
     * @domain-concept MongoDbContext
     * @purpose MongoDB connection and collection management
     */
    public class MongoDbContext
    {
        /**
         * @contract-action GetDatabase
         * @return IMongoDatabase instance
         * @log-event mongo.context.get-database
         * @pure YES
         * @idempotent YES
         */
        public IMongoDatabase GetDatabase();

        /**
         * @contract-action GetCollectionAsync
         * @param name Collection name
         * @param ct Cancellation token
         * @return MongoDB collection
         * @pre-condition name != null && name.Length > 0
         * @idempotent YES
         * @pure NO (initializes collection if needed)
         */
        public Task<IMongoCollection<PaymentDocument>> GetCollectionAsync(string name, CancellationToken ct);
    };

    /**
     * @domain-concept IndexConfiguration
     * @contract-action EnsureIndexesAsync
     * @param database IMongoDatabase instance
     * @param ct Cancellation token
     * @return Task completion
     * @log-event mongo.index.ensure-start
     * @log-event mongo.index.ensure-complete {index_count}
     * @trace-span mongo.index.ensure
     * @pre-condition database != null
     * @idempotent YES (safe to call multiple times)
     * @invariant Creates unique index on CorrelationId
     * @invariant Creates TTL index on CreatedAt (30 days)
     * @invariant Creates status query index
     */
    public static class IndexConfiguration
    {
        /**
         * @contract-action EnsureIndexesAsync
         * @param database MongoDB database
         * @param ct Cancellation token
         */
        public static async Task EnsureIndexesAsync(IMongoDatabase database, CancellationToken ct);
    };
}
