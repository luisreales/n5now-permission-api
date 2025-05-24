using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Nest;
using N5Now.Application.Commands.ModifyPermission;
using N5Now.Application.Commands.RequestPermission;
using N5Now.Application.DTOs;
using N5Now.Application.Queries.GetPermissions;
using N5Now.Domain.Entities;
using N5Now.Domain.Kafka;
using N5Now.Infrastructure.Data;
using N5Now.Infrastructure.Handlers.GetPermissions;
using N5Now.Infrastructure.Handlers.ModifyPermission;
using N5Now.Infrastructure.Handlers.RequestPermission;
using N5Now.Infrastructure.Kafka;
using Xunit;
using System;

namespace N5Now.Tests.Integration;

[Trait("Category", "Integration")]
public class PermissionIntegrationTests : IDisposable
{
    private readonly Mock<IKafkaProducerService> _kafkaProducerMock;
    private readonly Mock<IElasticClient> _elasticClientMock;
    private readonly Mock<ILogger<RequestPermissionHandler>> _requestLoggerMock;
    private readonly Mock<ILogger<ModifyPermissionHandler>> _modifyLoggerMock;
    private readonly Mock<ILogger<GetPermissionsHandler>> _getLoggerMock;
    private readonly AppDbContext _context;
    private readonly RequestPermissionHandler _requestHandler;
    private readonly ModifyPermissionHandler _modifyHandler;
    private readonly GetPermissionsHandler _getHandler;
    private readonly Guid _testPermissionId;

    public PermissionIntegrationTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        // Setup mocks
        _kafkaProducerMock = new Mock<IKafkaProducerService>();
        _elasticClientMock = new Mock<IElasticClient>();
        _requestLoggerMock = new Mock<ILogger<RequestPermissionHandler>>();
        _modifyLoggerMock = new Mock<ILogger<ModifyPermissionHandler>>();
        _getLoggerMock = new Mock<ILogger<GetPermissionsHandler>>();

        // Setup Elasticsearch response
        var indexResponse = new Mock<IndexResponse>();
        indexResponse.Setup(x => x.IsValid).Returns(true);
        _elasticClientMock.Setup(x => x.IndexDocumentAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(indexResponse.Object);

        // Create handlers
        _requestHandler = new RequestPermissionHandler(
            _context,
            _kafkaProducerMock.Object,
            _elasticClientMock.Object,
            _requestLoggerMock.Object
        );

        _modifyHandler = new ModifyPermissionHandler(
            _context,
            _kafkaProducerMock.Object,
            _elasticClientMock.Object,
            _modifyLoggerMock.Object
        );

        _getHandler = new GetPermissionsHandler(
            _context,
            _kafkaProducerMock.Object,
            _elasticClientMock.Object,
            _getLoggerMock.Object
        );

        // Create test permission
        _testPermissionId = Guid.NewGuid();
        var testPermission = new Permission
        {
            Id = _testPermissionId,
            EmployeeName = "Test User",
            PermissionTypeId = 1,
            PermissionDate = DateTime.UtcNow
        };

        _context.Permissions.Add(testPermission);
        _context.SaveChanges();
    }

    [Fact]
    public async Task RequestPermission_ValidData_ShouldPersistAndPublishEvent()
    {
        // Arrange
        var command = new RequestPermissionCommand
        {
            EmployeeName = "John Doe",
            PermissionTypeId = 1,
            PermissionDate = DateTime.UtcNow
        };

        // Act
        var result = await _requestHandler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result);

        // Verify database persistence
        var savedPermission = await _context.Permissions.FindAsync(result);
        Assert.NotNull(savedPermission);
        Assert.Equal(command.EmployeeName, savedPermission.EmployeeName);
        Assert.Equal(command.PermissionTypeId, savedPermission.PermissionTypeId);
        Assert.Equal(command.PermissionDate, savedPermission.PermissionDate);

        // Verify Kafka message
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

    [Fact]
    public async Task ModifyPermission_ValidData_ShouldUpdateAndPublishEvent()
    {
        // Arrange
        var command = new ModifyPermissionCommand
        {
            Id = _testPermissionId,
            EmployeeName = "Updated Name",
            PermissionTypeId = 2,
            PermissionDate = DateTime.UtcNow.AddDays(1)
        };

        // Act
        var result = await _modifyHandler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);

        // Verify database update
        var updatedPermission = await _context.Permissions.FindAsync(_testPermissionId);
        Assert.NotNull(updatedPermission);
        Assert.Equal(command.EmployeeName, updatedPermission.EmployeeName);
        Assert.Equal(command.PermissionTypeId, updatedPermission.PermissionTypeId);
        Assert.Equal(command.PermissionDate, updatedPermission.PermissionDate);

        // Verify Kafka message
        _kafkaProducerMock.Verify(x => x.SendMessageAsync(
            It.Is<PermissionEventDto>(dto =>
                dto.Id == _testPermissionId &&
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
    public async Task GetPermissions_ShouldReturnAllPermissions()
    {
        // Arrange
        var query = new GetPermissionsQuery();

        // Act
        var result = await _getHandler.Handle(query, CancellationToken.None);

        // Assert
        var permissionsList = result.ToList();
        Assert.NotEmpty(permissionsList);

        // Verify each permission has correct data
        foreach (var permission in permissionsList)
        {
            var dbPermission = await _context.Permissions.FindAsync(permission.Id);
            Assert.NotNull(dbPermission);
            Assert.Equal(dbPermission.EmployeeName, permission.EmployeeName);
            Assert.Equal(dbPermission.PermissionTypeId, permission.PermissionTypeId);
            Assert.Equal(dbPermission.PermissionDate, permission.PermissionDate);
        }

        // Verify Kafka messages
        _kafkaProducerMock.Verify(x => x.SendMessageAsync(
            It.Is<PermissionEventDto>(dto =>
                dto.Operation == "get"),
            It.IsAny<CancellationToken>()),
            Times.Exactly(permissionsList.Count));

        // Verify Elasticsearch indexing
        _elasticClientMock.Verify(x => x.IndexDocumentAsync(
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()),
            Times.Exactly(permissionsList.Count));
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
