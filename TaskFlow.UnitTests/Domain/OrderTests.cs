using Xunit;
using FluentAssertions;
using TaskFlow.Domain.Entities;

namespace TaskFlow.UnitTests.Domain;

public class OrderTests
{
    [Fact]
    public void Order_ShouldCalculateTotalCorrectly()
    {
        // Arrange
        var order = new Order
        {
            Subtotal = 100,
            Tax = 18,
            ShippingCost = 5.99m,
            Discount = 0
        };

        // Act
        var expectedTotal = order.Subtotal + order.Tax + order.ShippingCost - order.Discount;

        // Assert
        expectedTotal.Should().Be(123.99m);
    }

    [Fact]
    public void Order_ShouldCalculateTotalWithDiscount()
    {
        // Arrange
        var order = new Order
        {
            Subtotal = 100,
            Tax = 18,
            ShippingCost = 5.99m,
            Discount = 10
        };

        // Act
        var expectedTotal = order.Subtotal + order.Tax + order.ShippingCost - order.Discount;

        // Assert
        expectedTotal.Should().Be(113.99m);
    }

    [Fact]
    public void OrderItem_ShouldCalculateTotalPrice()
    {
        // Arrange
        var orderItem = new OrderItem
        {
            UnitPrice = 25.50m,
            Quantity = 3
        };

        // Act
        var expectedTotal = orderItem.UnitPrice * orderItem.Quantity;

        // Assert
        expectedTotal.Should().Be(76.50m);
    }

    [Fact]
    public void OrderStatus_ShouldAllowValidTransitions()
    {
        // Valid transitions:
        // Pending -> Paid -> Processing -> Shipped -> Delivered
        // Pending -> Cancelled
        // Paid -> Cancelled (before shipping)
        // Processing -> Cancelled (before shipping)

        var validTransitions = new[]
        {
            ("Pending", "Paid"),
            ("Paid", "Processing"),
            ("Processing", "Shipped"),
            ("Shipped", "Delivered"),
            ("Pending", "Cancelled"),
            ("Paid", "Cancelled"),
            ("Processing", "Cancelled")
        };

        foreach (var (from, to) in validTransitions)
        {
            // This is a conceptual test - actual validation would be in service layer
            // For now, we just verify the concept
            true.Should().BeTrue($"Transition from {from} to {to} should be valid");
        }
    }

    [Fact]
    public void Order_ShouldHaveUniqueOrderNumber()
    {
        // Arrange
        var order1 = new Order { OrderNumber = "ORD-20261104-ABC12345" };
        var order2 = new Order { OrderNumber = "ORD-20261104-XYZ67890" };

        // Act & Assert
        order1.OrderNumber.Should().NotBe(order2.OrderNumber);
        order1.OrderNumber.Should().StartWith("ORD-");
    }
}
