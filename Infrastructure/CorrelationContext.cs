namespace SagaDemo.Infrastructure;

public static class CorrelationContext
{
    public static string? GetCorrelationId(HttpContext context)
        => context.Items.TryGetValue("CorrelationId", out var id)
            ? id?.ToString()
            : null;
}