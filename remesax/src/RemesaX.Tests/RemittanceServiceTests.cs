using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RemesaX.Core.Dtos;
using RemesaX.Core.Interfaces;
using RemesaX.Infrastructure.Persistence;
using RemesaX.Infrastructure.Stellar;
using Xunit;

namespace RemesaX.Tests;

public class RemittanceServiceTests
{
    private const string ValidAddress = "GABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZ1234";

    private static RemesaXDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<RemesaXDbContext>()
            .UseInMemoryDatabase($"test_{Guid.NewGuid()}")
            .Options;
        return new RemesaXDbContext(options);
    }

    private static RemittanceService CreateService(
        Mock<IStellarService>? stellarMock = null,
        RemesaXDbContext? db = null)
    {
        stellarMock ??= new Mock<IStellarService>();
        db ??= CreateInMemoryDb();
        return new RemittanceService(stellarMock.Object, db, NullLogger<RemittanceService>.Instance);
    }

    // -------------------------------------------------------------------------
    // CASOS DE ÉXITO
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateRemittance_WithValidMxn_ReturnsCompleted()
    {
        var stellarMock = new Mock<IStellarService>();
        stellarMock
            .Setup(s => s.SendPaymentAsync(It.IsAny<string>(), It.IsAny<decimal>(), "USDX"))
            .ReturnsAsync("fake-tx-hash-mxn");

        var service = CreateService(stellarMock);

        var request = new CreateRemittanceRequest
        {
            SenderName = "Juan Pérez",
            DestinationAddress = ValidAddress,
            Amount = 100m,
            TargetCurrency = "MXN"
        };

        var result = await service.CreateRemittanceAsync(request);

        result.Status.Should().Be("Completed");
        result.TransactionHash.Should().Be("fake-tx-hash-mxn");
        result.AmountUsdx.Should().Be(100m);
        result.AmountLocal.Should().BeGreaterThan(100m, "MXN tiene tasa > 1 a USD");
        result.ExplorerUrl.Should().Contain("stellar.expert/explorer/testnet/tx");
    }

    [Theory]
    [InlineData("MXN")]
    [InlineData("ARS")]
    [InlineData("COP")]
    [InlineData("PEN")]
    [InlineData("BRL")]
    public async Task CreateRemittance_AllSupportedCurrencies_ShouldComplete(string currency)
    {
        var stellarMock = new Mock<IStellarService>();
        stellarMock
            .Setup(s => s.SendPaymentAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync($"hash-{currency}");
        var service = CreateService(stellarMock);

        var request = new CreateRemittanceRequest
        {
            SenderName = "Test User",
            DestinationAddress = ValidAddress,
            Amount = 50m,
            TargetCurrency = currency
        };

        var result = await service.CreateRemittanceAsync(request);

        result.Status.Should().Be("Completed");
        result.TargetCurrency.Should().Be(currency);
        result.ExchangeRate.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateRemittance_CurrencyLowercase_ShouldBeNormalizedToUppercase()
    {
        var stellarMock = new Mock<IStellarService>();
        stellarMock
            .Setup(s => s.SendPaymentAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync("any-hash");
        var service = CreateService(stellarMock);

        var request = new CreateRemittanceRequest
        {
            SenderName = "Test",
            DestinationAddress = ValidAddress,
            Amount = 10m,
            TargetCurrency = "mxn"  // lowercase
        };

        var result = await service.CreateRemittanceAsync(request);

        result.TargetCurrency.Should().Be("MXN");
    }

    [Fact]
    public async Task CreateRemittance_ShouldPersistInDatabase()
    {
        var stellarMock = new Mock<IStellarService>();
        stellarMock.Setup(s => s.SendPaymentAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync("persisted-hash");

        var db = CreateInMemoryDb();
        var service = CreateService(stellarMock, db);

        var result = await service.CreateRemittanceAsync(new CreateRemittanceRequest
        {
            SenderName = "Persist Test",
            DestinationAddress = ValidAddress,
            Amount = 25m,
            TargetCurrency = "PEN"
        });

        var found = await db.Remittances.FindAsync(result.Id);
        found.Should().NotBeNull();
        found!.SenderName.Should().Be("Persist Test");
        found.TransactionHash.Should().Be("persisted-hash");
    }

    // -------------------------------------------------------------------------
    // CASOS DE ERROR
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateRemittance_UnsupportedCurrency_ShouldThrow()
    {
        var service = CreateService();

        var request = new CreateRemittanceRequest
        {
            SenderName = "Test",
            DestinationAddress = ValidAddress,
            Amount = 50m,
            TargetCurrency = "JPY"
        };

        var act = () => service.CreateRemittanceAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Moneda no soportada*");
    }

    [Fact]
    public async Task CreateRemittance_StellarFails_ShouldMarkAsFailed()
    {
        var stellarMock = new Mock<IStellarService>();
        stellarMock
            .Setup(s => s.SendPaymentAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Trustline missing"));

        var service = CreateService(stellarMock);

        var result = await service.CreateRemittanceAsync(new CreateRemittanceRequest
        {
            SenderName = "Sad Path",
            DestinationAddress = ValidAddress,
            Amount = 50m,
            TargetCurrency = "MXN"
        });

        result.Status.Should().Be("Failed");
        result.TransactionHash.Should().BeNull();
    }

    // -------------------------------------------------------------------------
    // QUERIES
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetById_NonExistent_ShouldReturnNull()
    {
        var service = CreateService();
        var result = await service.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRecent_ShouldReturnOrderedByCreatedAtDescending()
    {
        var stellarMock = new Mock<IStellarService>();
        stellarMock.Setup(s => s.SendPaymentAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync("hash");

        var db = CreateInMemoryDb();
        var service = CreateService(stellarMock, db);

        for (var i = 0; i < 3; i++)
        {
            await service.CreateRemittanceAsync(new CreateRemittanceRequest
            {
                SenderName = $"User {i}",
                DestinationAddress = ValidAddress,
                Amount = 10m + i,
                TargetCurrency = "MXN"
            });
            await Task.Delay(15); // separación temporal
        }

        var recent = await service.GetRecentAsync(10);

        recent.Should().HaveCount(3);
        recent.Should().BeInDescendingOrder(r => r.CreatedAt);
    }

    [Fact]
    public async Task GetRecent_WithTakeLimit_ShouldHonorIt()
    {
        var stellarMock = new Mock<IStellarService>();
        stellarMock.Setup(s => s.SendPaymentAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync("hash");

        var db = CreateInMemoryDb();
        var service = CreateService(stellarMock, db);

        for (var i = 0; i < 5; i++)
        {
            await service.CreateRemittanceAsync(new CreateRemittanceRequest
            {
                SenderName = "U",
                DestinationAddress = ValidAddress,
                Amount = 5m,
                TargetCurrency = "PEN"
            });
        }

        var recent = await service.GetRecentAsync(2);

        recent.Should().HaveCount(2);
    }
}
