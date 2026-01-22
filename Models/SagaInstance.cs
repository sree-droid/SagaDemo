using System.ComponentModel.DataAnnotations;

namespace SagaDemo.Models;

public class SagaInstance
{
    [Key]
    public Guid SagaId { get; set; } = Guid.NewGuid();

    public Guid OrderId { get; set; }

    public string State { get; set; } = "Started";
    // Started, InventoryReserved, PaymentProcessed, Completed, Compensating, Failed

    public int Step { get; set; } = 0;

    public string Status { get; set; } = "Running";
    // Running, Completed, Failed

    public string? LastError { get; set; }

    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedOnUtc { get; set; } = DateTime.UtcNow;
}
