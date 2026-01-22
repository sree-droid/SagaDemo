using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SagaDemo.Data;
using SagaDemo.Models;
using System.Text.Json;

namespace SagaDemo.Controller;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(ILogger<OrdersController> logger, AppDbContext db)
    {
        this._logger = logger;
        _db = db;
    }


    [HttpGet("{sagaId}/timeline")]
    public async Task<IActionResult> GetTimeline(Guid sagaId)
    {
        var saga = await _db.SagaInstances.FindAsync(sagaId);
        var events = await _db.OutboxMessages
            .Where(m => m.Payload.Contains(sagaId.ToString()))
            .OrderBy(m => m.OccurredOnUtc)
            .ToListAsync();

        return Ok(new
        {
            Saga = saga,
            Events = events.Select(e => new
            {
                e.Type,
                e.OccurredOnUtc,
                e.ProcessedOnUtc,
                e.AttemptCount,
                e.LastError
            })
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest req)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        var order = new Order { CustomerName = req.CustomerName, Amount = req.Amount };
        _db.Orders.Add(order);

        var saga = new SagaInstance { OrderId = order.Id, State = "Started", Step = 0 };
        _db.SagaInstances.Add(saga);
        var correlationId = HttpContext.Items["CorrelationId"]!.ToString();
        // outbox event: "OrderCreated"
        var evt = JsonSerializer.Serialize(new { OrderId = order.Id, SagaId = saga.SagaId });
        _db.OutboxMessages.Add(new OutboxMessage { Type = "OrderCreated", Payload = evt, CorrelationId = correlationId });

        _logger.LogInformation(
            "Creating order {CustomerName} Amount={Amount} CorrelationId={CorrelationId}",
            req.CustomerName,
            req.Amount,
            HttpContext.Items["CorrelationId"]
        );

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return Ok(new { orderId = order.Id, sagaId = saga.SagaId });
    }
}

public record CreateOrderRequest(string CustomerName, decimal Amount);
