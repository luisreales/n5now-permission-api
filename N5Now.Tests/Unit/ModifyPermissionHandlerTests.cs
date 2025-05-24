using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Nest;
using N5Now.Application.Commands.ModifyPermission;
using N5Now.Domain.Entities;
using N5Now.Domain.Kafka;
using N5Now.Infrastructure.Data;
using N5Now.Infrastructure.Handlers.ModifyPermission;
using N5Now.Infrastructure.Kafka;
using Xunit;
using System;

namespace N5Now.Tests.Unit;

public class ModifyPermissionHandlerTests : IDisposable
{
    private readonly Mock<IKafkaProducerService> _kafkaProducerMock;
    private readonly Mock<IElasticClient> _elasticClientMock;
    private readonly Mock<ILogger<ModifyPermissionHandler>> _loggerMock;
    private readonly AppDbContext _context;
    private readonly ModifyPermissionHandler _handler;
    private readonly Guid _existingPermissionId;

    public ModifyPermissionHandlerTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        // Setup mocks
        _kafkaProducerMock = new Mock<IKafkaProducerService>();
        _elasticClientMock = new Mock<IElasticClient>();
        _loggerMock = new Mock<ILogger<ModifyPermissionHandler>>();

        // Setup Elasticsearch response
        var indexResponse = new Mock<IndexResponse>();
        indexResponse.Setup(x => x.IsValid).Returns(true);
        _elasticClientMock.Setup(x => x.IndexDocumentAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(indexResponse.Object);

        // Create handler instance
        _handler = new ModifyPermissionHandler(
            _context,
            _kafkaProducerMock.Object,
            _elasticClientMock.Object,
            _loggerMock.Object
        );

        // Create and seed test data
        _existingPermissionId = Guid.NewGuid();
        var existingPermission = new Permission
        {
            Id = _existingPermissionId,
            EmployeeName = "Original Name",
            PermissionTypeId = 1,
            PermissionDate = DateTime.UtcNow.AddDays(-1)
        };

        _context.Permissions.Add(existingPermission);
        _context.SaveChanges();
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldModifyAndReturnTrue()
    {
        // Arrange
        var command = new ModifyPermissionCommand
        {
            Id = _existingPermissionId,
            EmployeeName = "Updated Name",
            PermissionTypeId = 2,
            PermissionDate = DateTime.UtcNow
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);

        // Verify database changes
        var modifiedPermission = await _context.Permissions.FindAsync(_existingPermissionId);
        Assert.NotNull(modifiedPermission);
        Assert.Equal(command.EmployeeName, modifiedPermission.EmployeeName);
        Assert.Equal(command.PermissionTypeId, modifiedPermission.PermissionTypeId);
        Assert.Equal(command.PermissionDate, modifiedPermission.PermissionDate);

        // Verify Kafka message was sent
        _kafkaProducerMock.Verify(x => x.SendMessageAsync(
            It.Is<PermissionEventDto>(dto =>
                dto.Id == _existingPermissionId &&
                dto.EmployeeName == command.EmployeeName &&
                dto.PermissionTypeId == command.PermissionTypeId &&
                dto.PermissionDate == command.PermissionDate &&
                dto.Operation == "modify"),
            It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify Elasticsearch indexing
        _elasticClientMock.Verify(x => x.IndexDocumentAsync(
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentPermission_ShouldReturnFalse()
    {
        // Arrange
        var command = new ModifyPermissionCommand
        {
            Id = Guid.NewGuid(), // Non-existent ID
            EmployeeName = "Updated Name",
            PermissionTypeId = 2,
            PermissionDate = DateTime.UtcNow
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result);

        // Verify no Kafka message was sent
        _kafkaProducerMock.Verify(x => x.SendMessageAsync(
            It.IsAny<PermissionEventDto>(),
            It.IsAny<CancellationToken>()),
            Times.Never);

        // Verify no Elasticsearch indexing
        _elasticClientMock.Verify(x => x.IndexDocumentAsync(
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
