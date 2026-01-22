using System.ComponentModel.DataAnnotations;

namespace SagaDemo.Models;

public class Order
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CustomerName { get; set; } = default!;
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Created"; // Created, Completed, Cancelled
    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;
}
