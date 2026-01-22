using Microsoft.EntityFrameworkCore;
using SagaDemo.Data;
using SagaDemo.Infrastructure;
using SagaDemo.Workers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var cs=builder.Configuration.GetConnectionString("Default");
    options.UseSqlServer(cs);
});
builder.Services.AddHostedService<SagaOrchestratorWorker>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();   
app.UseHttpsRedirection();
app.MapControllers();
app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
