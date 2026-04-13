using Orders.Application.Interfaces;
using Orders.Application.UseCases;
using Orders.Domain.Repositories;
using Orders.Infrastructure.Messaging;
using Orders.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// --- DOMAIN & APPLICATION ---
builder.Services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
builder.Services.AddScoped<CreateOrderUseCase>();
builder.Services.AddScoped<AddItemToOrderUseCase>();
builder.Services.AddScoped<GetOrdersUseCase>();

// --- MESSAGING ---
var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
builder.Services.AddSingleton<IMessageBus>(_ =>
    RabbitMqMessageBus.CreateAsync(rabbitHost).GetAwaiter().GetResult()
);

// --- API ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
