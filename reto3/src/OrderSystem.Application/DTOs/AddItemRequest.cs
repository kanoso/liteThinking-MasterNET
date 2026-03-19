namespace OrderSystem.Application.DTOs;

public record AddItemRequest(string ProductName, int Quantity, decimal UnitPrice, string Currency = "S/.");
