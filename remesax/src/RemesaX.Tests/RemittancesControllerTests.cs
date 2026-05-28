using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RemesaX.Api.Controllers;
using RemesaX.Core.Dtos;
using RemesaX.Core.Interfaces;
using Xunit;

namespace RemesaX.Tests;

public class RemittancesControllerTests
{
    private const string ValidAddress = "GABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZ1234";

    private static RemittancesController CreateController(Mock<IRemittanceService>? svcMock = null)
    {
        svcMock ??= new Mock<IRemittanceService>();
        return new RemittancesController(svcMock.Object, NullLogger<RemittancesController>.Instance);
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreated201()
    {
        var fakeResponse = new RemittanceResponse
        {
            Id = Guid.NewGuid(),
            SenderName = "Juan",
            Status = "Completed",
            TransactionHash = "hash123",
            ExplorerUrl = "https://stellar.expert/explorer/testnet/tx/hash123"
        };

        var svcMock = new Mock<IRemittanceService>();
        svcMock.Setup(s => s.CreateRemittanceAsync(It.IsAny<CreateRemittanceRequest>()))
            .ReturnsAsync(fakeResponse);

        var controller = CreateController(svcMock);

        var result = await controller.Create(new CreateRemittanceRequest
        {
            SenderName = "Juan",
            DestinationAddress = ValidAddress,
            Amount = 50m,
            TargetCurrency = "MXN"
        });

        result.Should().BeOfType<CreatedAtActionResult>();
        var created = (CreatedAtActionResult)result;
        created.StatusCode.Should().Be(201);
        created.Value.Should().BeEquivalentTo(fakeResponse);
    }

    [Fact]
    public async Task Create_UnsupportedCurrency_Returns400()
    {
        var svcMock = new Mock<IRemittanceService>();
        svcMock.Setup(s => s.CreateRemittanceAsync(It.IsAny<CreateRemittanceRequest>()))
            .ThrowsAsync(new InvalidOperationException("Moneda no soportada: JPY"));

        var controller = CreateController(svcMock);

        var result = await controller.Create(new CreateRemittanceRequest
        {
            SenderName = "X", DestinationAddress = ValidAddress,
            Amount = 10m, TargetCurrency = "JPY"
        });

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        var svcMock = new Mock<IRemittanceService>();
        svcMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((RemittanceResponse?)null);

        var controller = CreateController(svcMock);

        var result = await controller.GetById(Guid.NewGuid());

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetById_Found_Returns200WithBody()
    {
        var id = Guid.NewGuid();
        var fakeResponse = new RemittanceResponse { Id = id, Status = "Completed" };

        var svcMock = new Mock<IRemittanceService>();
        svcMock.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(fakeResponse);

        var controller = CreateController(svcMock);

        var result = await controller.GetById(id);

        result.Should().BeOfType<OkObjectResult>();
        ((OkObjectResult)result).Value.Should().Be(fakeResponse);
    }

    [Fact]
    public async Task List_ReturnsCollectionFromService()
    {
        var fakeList = new List<RemittanceResponse>
        {
            new() { Id = Guid.NewGuid(), Status = "Completed" },
            new() { Id = Guid.NewGuid(), Status = "Failed" }
        };

        var svcMock = new Mock<IRemittanceService>();
        svcMock.Setup(s => s.GetRecentAsync(It.IsAny<int>())).ReturnsAsync(fakeList);

        var controller = CreateController(svcMock);

        var result = await controller.List(take: 10);

        result.Should().BeOfType<OkObjectResult>();
        var body = ((OkObjectResult)result).Value as List<RemittanceResponse>;
        body.Should().HaveCount(2);
    }
}
