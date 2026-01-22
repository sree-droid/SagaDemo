using System.ComponentModel.DataAnnotations;

namespace SagaDemo.Models;

public class OutboxMessage
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public DateTime OccurredOnUtc { get; set; } = DateTime.UtcNow;
    public string CorrelationId { get; set; } = default!;
    public DateTime? ProcessedOnUtc { get; set; }
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
}
