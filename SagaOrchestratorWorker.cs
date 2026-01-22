using Microsoft.EntityFrameworkCore;
using SagaDemo.Data;
using SagaDemo.Models;
using System.Text.Json;

namespace SagaDemo.Workers;

public class SagaOrchestratorWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SagaOrchestratorWorker> _logger;

    public SagaOrchestratorWorker(IServiceScopeFactory scopeFactory, ILogger<SagaOrchestratorWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Saga orchestrator worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Saga orchestrator loop error");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task ProcessOutboxAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var messages = await db.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(10)
            .ToListAsync(ct);

        foreach (var msg in messages)
        {
            try
            {
                await HandleEventAsync(db, msg, ct);
                msg.ProcessedOnUtc = DateTime.UtcNow;
                msg.LastError = null;
            }
            catch (Exception ex)
            {
                msg.AttemptCount += 1;
                msg.LastError = ex.Message;
                _logger.LogWarning(ex, "Failed handling outbox message {Id} Type={Type}", msg.Id, msg.Type);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task HandleEventAsync(AppDbContext db, OutboxMessage msg, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<OrderCreatedPayload>(msg.Payload)!;
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = msg.CorrelationId,
            ["SagaId"] = payload.SagaId,
            ["OrderId"] = payload.OrderId
        }))
        {
            _logger.LogInformation("Handling {EventType}", msg.Type);
            var saga = await db.SagaInstances.FirstAsync(s => s.SagaId == payload.SagaId, ct);
            // For demo we only handle OrderCreated and then pretend ReserveInventory and ProcessPayment
            if (msg.Type == "OrderCreated")
            {
                try
                {
                    saga.State = "InventoryReserved";
                    saga.Step = 1;
                    saga.UpdatedOnUtc = DateTime.UtcNow;

                    // Create next outbox event: InventoryReserved
                    db.OutboxMessages.Add(new OutboxMessage
                    {
                        Type = "InventoryReserved",
                        Payload = JsonSerializer.Serialize(new { payload.OrderId, payload.SagaId })
                    });

                    _logger.LogInformation("Saga {SagaId}: Inventory reserved.", saga.SagaId);
                }
                catch (Exception ex)
                {
                    _logger.LogError("I failed here at " + msg.Type + " because " + ex.Message);
                }

            }
            else if (msg.Type == "InventoryReserved")
            {
                try
                {
                    var order = await db.Orders.FirstAsync(o => o.Id == payload.OrderId, ct);
                    var paymentOk = order.Amount <= 100;

                    if (paymentOk)
                    {
                        saga.State = "PaymentProcessed";
                        saga.Step = 2;
                        saga.UpdatedOnUtc = DateTime.UtcNow;

                        db.OutboxMessages.Add(new OutboxMessage
                        {
                            Type = "PaymentProcessed",
                            Payload = JsonSerializer.Serialize(new { payload.OrderId, payload.SagaId })
                        });

                        _logger.LogInformation("Saga {SagaId}: Payment processed.", saga.SagaId);
                    }
                    else
                    {
                        // Trigger compensation
                        saga.State = "Compensating";
                        saga.Status = "Failed";
                        saga.LastError = "Payment failed (simulated)";
                        saga.UpdatedOnUtc = DateTime.UtcNow;

                        db.OutboxMessages.Add(new OutboxMessage
                        {
                            Type = "CompensateReleaseInventory",
                            Payload = JsonSerializer.Serialize(new { payload.OrderId, payload.SagaId })
                        });

                        _logger.LogInformation("Saga {SagaId}: Payment failed → compensating.", saga.SagaId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("I failed here at " + msg.Type + " because " + ex.Message);
                }
                // Simulate payment success/failure (for demo, fail if amount > 100)

            }
            else if (msg.Type == "PaymentProcessed")
            {
                try
                {
                    var order = await db.Orders.FirstAsync(o => o.Id == payload.OrderId, ct);

                    order.Status = "Completed";
                    saga.State = "Completed";
                    saga.Status = "Completed";
                    saga.Step = 3;
                    saga.UpdatedOnUtc = DateTime.UtcNow;

                    _logger.LogInformation("Saga {SagaId}: Order completed.", saga.SagaId);
                }
                catch (Exception ex)
                {
                    _logger.LogError("I failed here at " + msg.Type + " because " + ex.Message);
                }

            }
            else if (msg.Type == "CompensateReleaseInventory")
            {
                try
                {
                    var order = await db.Orders.FirstAsync(o => o.Id == payload.OrderId, ct);

                    // "Release inventory" step done → cancel order
                    db.OutboxMessages.Add(new OutboxMessage
                    {
                        Type = "CompensateCancelOrder",
                        Payload = JsonSerializer.Serialize(new { payload.OrderId, payload.SagaId })
                    });

                    _logger.LogInformation("Saga {SagaId}: Inventory released (simulated).", saga.SagaId);
                }
                catch (Exception ex)
                {
                    _logger.LogError("I failed here at " + msg.Type + " because " + ex.Message);
                }

            }
            else if (msg.Type == "CompensateCancelOrder")
            {
                try
                {
                    var order = await db.Orders.FirstAsync(o => o.Id == payload.OrderId, ct);

                    order.Status = "Cancelled";
                    saga.State = "Failed";
                    saga.Status = "Failed";
                    saga.Step = -1;
                    saga.UpdatedOnUtc = DateTime.UtcNow;

                    _logger.LogInformation("Saga {SagaId}: Order cancelled (compensation complete).", saga.SagaId);
                }
                catch (Exception ex)
                {
                    _logger.LogError("I failed here at " + msg.Type + " because " + ex.Message);
                }

            }
        }
    }
    private record OrderCreatedPayload(Guid OrderId, Guid SagaId);
}
