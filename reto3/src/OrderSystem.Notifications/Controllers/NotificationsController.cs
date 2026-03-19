namespace OrderSystem.Notifications.Controllers;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(ILogger<NotificationsController> logger)
    {
        _logger = logger;
    }

    [HttpPost("notify")]
    public IActionResult Notify([FromBody] NotifyRequest request)
    {
        _logger.LogInformation("Notificacion recibida: Orden {OrderId} creada para el cliente {CustomerName}",
            request.OrderId, request.CustomerName);

        return Ok(new { message = "Notificacion procesada", orderId = request.OrderId });
    }
}

public record NotifyRequest(Guid OrderId, string CustomerName);
