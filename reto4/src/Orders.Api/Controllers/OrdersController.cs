namespace Orders.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Orders.Application.DTOs;
using Orders.Application.UseCases;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly CreateOrderUseCase _createOrder;
    private readonly AddItemToOrderUseCase _addItem;
    private readonly GetOrdersUseCase _getOrders;

    public OrdersController(
        CreateOrderUseCase createOrder,
        AddItemToOrderUseCase addItem,
        GetOrdersUseCase getOrders)
    {
        _createOrder = createOrder;
        _addItem = addItem;
        _getOrders = getOrders;
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
        return CreatedAtAction(nameof(GetById), new { orderId = result.Id }, result);
    }

    [HttpPost("{orderId:guid}/items")]
    public async Task<IActionResult> AddItem(Guid orderId, [FromBody] AddItemRequest request)
    {
        var result = await _addItem.ExecuteAsync(orderId, request);
        return Ok(result);
    }
}
