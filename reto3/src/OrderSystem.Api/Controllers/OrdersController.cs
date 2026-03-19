namespace OrderSystem.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using OrderSystem.Application.DTOs;
using OrderSystem.Application.UseCases;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly CreateOrderUseCase _createOrder;
    private readonly AddItemToOrderUseCase _addItem;
    private readonly GetOrdersUseCase _getOrders;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        CreateOrderUseCase createOrder,
        AddItemToOrderUseCase addItem,
        GetOrdersUseCase getOrders,
        IHttpClientFactory httpClientFactory,
        ILogger<OrdersController> logger)
    {
        _createOrder = createOrder;
        _addItem = addItem;
        _getOrders = getOrders;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _getOrders.ExecuteAsync();
        return Ok(result);
    }

    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> GetById(Guid orderId)
    {
        var result = await _getOrders.ExecuteByIdAsync(orderId);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
    {
        var result = await _createOrder.ExecuteAsync(request);

        // Invocar al servicio de notificaciones
        try
        {
            var client = _httpClientFactory.CreateClient("notifications");
            await client.PostAsJsonAsync("/notifications/notify", new
            {
                orderId = result.Id,
                customerName = result.CustomerName
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning("No se pudo notificar al servicio de notificaciones: {Message}", ex.Message);
        }

        return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
    }

    [HttpPost("{orderId:guid}/items")]
    public async Task<IActionResult> AddItem(Guid orderId, [FromBody] AddItemRequest request)
    {
        var result = await _addItem.ExecuteAsync(orderId, request);
        return Ok(result);
    }
}
