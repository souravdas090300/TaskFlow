using FluentAssertions;
using Moq;
using TaskFlow.Application;
using TaskFlow.Application.Services;
using TaskFlow.Domain.Constants;
using TaskFlow.Domain.Entities;
using Xunit;

namespace TaskFlow.UnitTests.Checkout;

public class PaymentIssueTests : CheckoutTestBase
{
    private const string Pi = "pi_123";
    private const string EventId = "evt_001";

    [Fact]
    public async Task UnknownOrder_RecordsOrderNotFoundIssue()
    {
        Orders.Setup(o => o.GetByIdAsync(OrderId)).ReturnsAsync((Order?)null);

        (await CreateSut().ProcessStripePaymentAsync(OrderId, Pi, EventId)).Should().Be(PaymentResult.OrderNotFound);
        VerifyIssue(PaymentIssueType.OrderNotFound, EventId, Times.Once());
    }

    [Theory]
    [InlineData(OrderStatuses.Cancelled)]
    [InlineData(OrderStatuses.Shipped)]
    [InlineData(OrderStatuses.Delivered)]
    public async Task ClosedOrder_RecordsOrderClosedIssue(string status)
    {
        GivenOrder(MakeOrder(status));

        (await CreateSut().ProcessStripePaymentAsync(OrderId, Pi, EventId)).Should().Be(PaymentResult.Rejected);
        VerifyIssue(PaymentIssueType.OrderClosed, EventId, Times.Once());
    }

    [Fact]
    public async Task SecondPaymentOnPaidOrder_RecordsDoubleChargeIssue()
    {
        GivenOrder(MakeOrder(OrderStatuses.Paid, paymentIntent: "pi_FIRST"));

        (await CreateSut().ProcessStripePaymentAsync(OrderId, Pi, EventId)).Should().Be(PaymentResult.Rejected);
        VerifyIssue(PaymentIssueType.DoubleCharge, EventId, Times.Once());
    }

    [Fact]
    public async Task StockConflict_FlagsOrder_AndRecordsIssueInsideTheReviewTransaction()
    {
        GivenOrder(MakeOrder());
        Events.Setup(e => e.TryMarkProcessedAsync(EventId)).ReturnsAsync(true);
        Orders.Setup(o => o.TryMarkPaidAsync(OrderId, Pi, EventId, It.IsAny<DateTime>())).ReturnsAsync(true);
        Stock.Setup(s => s.TryDecrementAsync(It.IsAny<Guid>(), It.IsAny<int>())).ReturnsAsync(false);

        (await CreateSut().ProcessStripePaymentAsync(OrderId, Pi, EventId)).Should().Be(PaymentResult.StockConflict);

        Orders.Verify(o => o.MarkPaymentReviewAsync(OrderId, Pi), Times.Once);
        VerifyIssue(PaymentIssueType.StockConflict, EventId, Times.Once());
    }

    [Fact]
    public async Task SuccessfulPayment_RecordsNoIssue()
    {
        GivenOrder(MakeOrder());
        Events.Setup(e => e.TryMarkProcessedAsync(EventId)).ReturnsAsync(true);
        Orders.Setup(o => o.TryMarkPaidAsync(OrderId, Pi, EventId, It.IsAny<DateTime>())).ReturnsAsync(true);
        StockAlwaysAvailable();

        (await CreateSut().ProcessStripePaymentAsync(OrderId, Pi, EventId)).Should().Be(PaymentResult.Processed);
        Issues.Verify(i => i.AddIfNotExistsAsync(It.IsAny<PaymentIssue>()), Times.Never);
    }
}
