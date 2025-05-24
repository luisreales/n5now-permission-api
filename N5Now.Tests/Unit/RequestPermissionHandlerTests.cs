using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Nest;
using N5Now.Application.Commands.RequestPermission;
using N5Now.Domain.Kafka;
using N5Now.Infrastructure.Data;
using N5Now.Infrastructure.Handlers.RequestPermission;
using N5Now.Infrastructure.Kafka;
using Xunit;
using System;

namespace N5Now.Tests.Unit;

public class RequestPermissionHandlerTests : IDisposable
{
    private readonly Mock<IKafkaProducerService> _kafkaProducerMock;
    private readonly Mock<IElasticClient> _elasticClientMock;
    private readonly Mock<ILogger<RequestPermissionHandler>> _loggerMock;
    private readonly AppDbContext _context;
    private readonly RequestPermissionHandler _handler;

    public RequestPermissionHandlerTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        // Setup mocks
        _kafkaProducerMock = new Mock<IKafkaProducerService>();
        _elasticClientMock = new Mock<IElasticClient>();
        _loggerMock = new Mock<ILogger<RequestPermissionHandler>>();

        // Setup Elasticsearch response
        var indexResponse = new Mock<IndexResponse>();
        indexResponse.Setup(x => x.IsValid).Returns(true);
        _elasticClientMock.Setup(x => x.IndexDocumentAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(indexResponse.Object);

        // Create handler instance
        _handler = new RequestPermissionHandler(
            _context,
            _kafkaProducerMock.Object,
            _elasticClientMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldPersistAndReturnGuid()
    {
        // Arrange
        var command = new RequestPermissionCommand
        {
            EmployeeName = "John Doe",
            PermissionTypeId = 1,
            PermissionDate = DateTime.UtcNow
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result);

        // Verify database persistence
        var savedPermission = await _context.Permissions.FirstOrDefaultAsync();
        Assert.NotNull(savedPermission);
        Assert.Equal(command.EmployeeName, savedPermission.EmployeeName);
        Assert.Equal(command.PermissionTypeId, savedPermission.PermissionTypeId);
        Assert.Equal(command.PermissionDate, savedPermission.PermissionDate);

        // Verify Kafka message was sent
        _kafkaProducerMock.Verify(x => x.PublishAsync(
            It.Is<PermissionEventDto>(dto =>
                dto.EmployeeName == command.EmployeeName &&
                dto.PermissionTypeId == command.PermissionTypeId &&
                dto.PermissionDate == command.PermissionDate &&
                dto.Operation == "request")),
            Times.Once);

        // Verify Elasticsearch indexing
        _elasticClientMock.Verify(x => x.IndexDocumentAsync(
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
