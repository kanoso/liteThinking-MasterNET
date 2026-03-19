using OrderSystem.Application.UseCases;
using OrderSystem.Domain.Repositories;
using OrderSystem.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// --- DEPENDENCY INJECTION ---
builder.Services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
builder.Services.AddScoped<CreateOrderUseCase>();
builder.Services.AddScoped<AddItemToOrderUseCase>();
builder.Services.AddScoped<GetOrdersUseCase>();

// --- HTTP CLIENT para comunicacion con notifications-api ---
builder.Services.AddHttpClient("notifications", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["NotificationsUrl"]!);
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- HEALTH CHECKS ---
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");
app.Run();
