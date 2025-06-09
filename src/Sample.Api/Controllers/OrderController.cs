using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Sample.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly ILogger<OrderController> _logger;
    private readonly HttpClient _httpClient;
    private static readonly ActivitySource ActivitySource = new("Sample.Api.OrderService");

    public OrderController(ILogger<OrderController> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        using var activity = ActivitySource.StartActivity("CreateOrder");
        activity?.SetTag("order.id", request.OrderId);
        activity?.SetTag("customer.id", request.CustomerId);
        
        _logger.LogInformation("Starting order creation for OrderId: {OrderId}, CustomerId: {CustomerId}", 
            request.OrderId, request.CustomerId);

        try
        {
            // Step 1: Check inventory
            var inventoryResult = await CheckInventory(request.ProductId, request.Quantity);
            if (!inventoryResult.Available)
            {
                _logger.LogWarning("Insufficient inventory for ProductId: {ProductId}, Quantity: {Quantity}", 
                    request.ProductId, request.Quantity);
                return BadRequest("Insufficient inventory");
            }

            // Step 2: Process payment
            var paymentResult = await ProcessPayment(request.CustomerId, request.Amount);
            if (!paymentResult.Success)
            {
                _logger.LogError("Payment failed for CustomerId: {CustomerId}, Amount: {Amount}", 
                    request.CustomerId, request.Amount);
                return BadRequest("Payment failed");
            }

            // Step 3: Send notification
            await SendNotification(request.CustomerId, request.OrderId);

            // Step 4: Update inventory
            await UpdateInventory(request.ProductId, request.Quantity);

            var orderResponse = new CreateOrderResponse(
                request.OrderId,
                "Completed",
                paymentResult.PaymentId,
                DateTime.UtcNow
            );

            _logger.LogInformation("Order creation completed successfully for OrderId: {OrderId}", request.OrderId);
            return Ok(orderResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order for OrderId: {OrderId}", request.OrderId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return StatusCode(500, "Internal server error");
        }
    }

    private async Task<InventoryCheckResult> CheckInventory(string productId, int quantity)
    {
        using var activity = ActivitySource.StartActivity("CheckInventory");
        activity?.SetTag("product.id", productId);
        activity?.SetTag("quantity", quantity);

        _logger.LogInformation("Checking inventory for ProductId: {ProductId}, Quantity: {Quantity}", 
            productId, quantity);

        // Simulate API call delay
        await Task.Delay(Random.Shared.Next(100, 300));

        // Simulate inventory check logic
        var available = Random.Shared.NextDouble() > 0.1; // 90% success rate
        
        _logger.LogInformation("Inventory check result for ProductId: {ProductId} - Available: {Available}", 
            productId, available);

        return new InventoryCheckResult(available, productId);
    }

    private async Task<PaymentResult> ProcessPayment(string customerId, decimal amount)
    {
        using var activity = ActivitySource.StartActivity("ProcessPayment");
        activity?.SetTag("customer.id", customerId);
        activity?.SetTag("amount", amount);

        _logger.LogInformation("Processing payment for CustomerId: {CustomerId}, Amount: {Amount}", 
            customerId, amount);

        // Simulate payment processing delay
        await Task.Delay(Random.Shared.Next(200, 500));

        // Simulate payment processing logic
        var success = Random.Shared.NextDouble() > 0.05; // 95% success rate
        var paymentId = success ? Guid.NewGuid().ToString() : null;

        if (success)
        {
            _logger.LogInformation("Payment processed successfully for CustomerId: {CustomerId}, PaymentId: {PaymentId}", 
                customerId, paymentId);
        }
        else
        {
            _logger.LogWarning("Payment failed for CustomerId: {CustomerId}", customerId);
            activity?.SetStatus(ActivityStatusCode.Error, "Payment processing failed");
        }

        return new PaymentResult(success, paymentId);
    }

    private async Task SendNotification(string customerId, string orderId)
    {
        using var activity = ActivitySource.StartActivity("SendNotification");
        activity?.SetTag("customer.id", customerId);
        activity?.SetTag("order.id", orderId);

        _logger.LogInformation("Sending notification for CustomerId: {CustomerId}, OrderId: {OrderId}", 
            customerId, orderId);

        // Simulate notification sending delay
        await Task.Delay(Random.Shared.Next(50, 150));

        _logger.LogInformation("Notification sent successfully for CustomerId: {CustomerId}, OrderId: {OrderId}", 
            customerId, orderId);
    }

    private async Task UpdateInventory(string productId, int quantity)
    {
        using var activity = ActivitySource.StartActivity("UpdateInventory");
        activity?.SetTag("product.id", productId);
        activity?.SetTag("quantity", quantity);

        _logger.LogInformation("Updating inventory for ProductId: {ProductId}, Quantity: {Quantity}", 
            productId, quantity);

        // Simulate inventory update delay
        await Task.Delay(Random.Shared.Next(80, 200));

        _logger.LogInformation("Inventory updated successfully for ProductId: {ProductId}", productId);
    }
}

public record CreateOrderRequest(
    string OrderId,
    string CustomerId,
    string ProductId,
    int Quantity,
    decimal Amount
);

public record CreateOrderResponse(
    string OrderId,
    string Status,
    string? PaymentId,
    DateTime CreatedAt
);

public record InventoryCheckResult(bool Available, string ProductId);
public record PaymentResult(bool Success, string? PaymentId); 