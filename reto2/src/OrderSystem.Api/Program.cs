using OrderSystem.Application.UseCases;
using OrderSystem.Domain.Repositories;
using OrderSystem.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// --- DEPENDENCY INJECTION ---
builder.Services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
builder.Services.AddScoped<CreateOrderUseCase>();
builder.Services.AddScoped<AddItemToOrderUseCase>();
builder.Services.AddScoped<GetOrdersUseCase>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();
