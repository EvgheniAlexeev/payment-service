// FILE: src/PaymentService.Api.WriterService/Handlers/CreatePaymentHandler.cs
// VERSION: 2.0.0
// MODULE: M-WRITER
// PURPOSE: Business logic handler for command operations
// SEMANTIC_TAG: [HANDLER, COMMAND_PROCESSOR]
// START_MODULE M_WRITER

// FILE: src/PaymentService.Api.WriterService/Handlers/CreatePaymentHandler.cs
// VERSION: 1.0.0

using FluentValidation;
using Microsoft.Extensions.Logging;
using PaymentService.Api.WriterService.Validators;
using PaymentService.Persistence.Repositories;
using PaymentService.Shared;
using PaymentService.Shared.Commands;
using PaymentService.Shared.Dtos;
using PaymentService.Shared.Models;

namespace PaymentService.Api.WriterService.Handlers;

/// <summary>
/// <para><strong>@purpose:</strong> Payment creation handler with [BLOCK_HANDLER_CREATE] markers</para>
/// <para><strong>@contract:</strong> M-WRITER (command handler, writes payment document + publishes saga)</para>
/// <para><strong>@invariant:</strong> Idempotent: duplicate CorrelationId returns cached response</para>
/// </summary>
public class CreatePaymentHandler : ICreatePaymentHandler
{
    private readonly IPaymentDocumentRepository _repository;
    private readonly CreatePaymentValidator _validator;
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<CreatePaymentHandler> _logger;

    public CreatePaymentHandler(
        IPaymentDocumentRepository repository,
        CreatePaymentValidator validator,
        IMessagePublisher publisher,
        ILogger<CreatePaymentHandler> logger)
    {
        _repository = repository;
        _validator = validator;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Result<CreatePaymentResponse>> HandleAsync(
        CreatePaymentRequest request, CancellationToken ct)
    {
        // START_BLOCK_HANDLER_CREATE
        try
        {
            // Validate
            var validationResult = await _validator.ValidateAsync(request, ct);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning(
                    "[PaymentService.Api.WriterService][CreatePaymentHandler][BLOCK_HANDLER_CREATE] " +
                    "Validation failed {CorrelationId} {@Errors}",
                    request.CorrelationId, validationResult.Errors);
                return Result<CreatePaymentResponse>.Failure(
                    validationResult.Errors.First().ErrorMessage);
            }

            _logger.LogInformation(
                "[PaymentService.Api.WriterService][CreatePaymentHandler][BLOCK_HANDLER_CREATE] " +
                "Creating payment {CorrelationId}", request.CorrelationId);

            // Check for idempotent duplicate
            var exists = await _repository.ExistsByCorrelationIdAsync(request.CorrelationId, ct);
            if (exists)
            {
                _logger.LogInformation(
                    "[PaymentService.Api.WriterService][CreatePaymentHandler][BLOCK_HANDLER_CREATE] " +
                    "Duplicate payment detected, idempotent response {CorrelationId}",
                    request.CorrelationId);

                return Result<CreatePaymentResponse>.Success(
                    new CreatePaymentResponse
                    {
                        CorrelationId = request.CorrelationId,
                        Message = "Payment already accepted for processing",
                        AcceptedAt = DateTime.UtcNow
                    });
            }

            // Create payment document (initial state)
            var paymentDocument = new PaymentDocument
            {
                CorrelationId = request.CorrelationId,
                Request = new PaymentRequestDto
                {
                    CorrelationId = request.CorrelationId,
                    SenderAccount = request.SenderAccount,
                    ReceiverAccount = request.ReceiverAccount,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    ValueDate = request.ValueDate,
                    Description = request.Description
                },
                Status = "Pending",
                SagaState = "Validating",
                CreatedAt = DateTime.UtcNow
            };

            await _repository.InsertAsync(paymentDocument, ct: ct);

            // Publish saga command (Wolverine/Dapr pub-sub)
            var command = new PaymentCommand
            {
                IdempotencyKey = request.CorrelationId,
                CorrelationId = request.CorrelationId,
                Request = paymentDocument.Request
            };

            await _publisher.PublishAsync(command, ct);

            _logger.LogInformation(
                "[PaymentService.Api.WriterService][CreatePaymentHandler][BLOCK_HANDLER_CREATE] " +
                "Payment created and saga started {CorrelationId}", request.CorrelationId);

            return Result<CreatePaymentResponse>.Success(
                new CreatePaymentResponse
                {
                    CorrelationId = request.CorrelationId,
                    Message = "Payment accepted for processing",
                    AcceptedAt = DateTime.UtcNow
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[PaymentService.Api.WriterService][CreatePaymentHandler][BLOCK_HANDLER_CREATE] " +
                "Error creating payment {CorrelationId}", request.CorrelationId);
            return Result<CreatePaymentResponse>.Failure("Internal server error");
        }
        // END_BLOCK_HANDLER_CREATE
    }
}
