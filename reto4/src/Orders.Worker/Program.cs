using Orders.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<OrderCreatedWorker>();

var host = builder.Build();
host.Run();
