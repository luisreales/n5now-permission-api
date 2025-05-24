using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Nest;
using N5Now.Application.DTOs;
using N5Now.Application.Queries.GetPermissions;
using N5Now.Domain.Entities;
using N5Now.Domain.Kafka;
using N5Now.Infrastructure.Data;
using N5Now.Infrastructure.Handlers.GetPermissions;
using N5Now.Infrastructure.Kafka;
using Xunit;
using System;

namespace N5Now.Tests.Unit;

public class GetPermissionsHandlerTests : IDisposable
{
    private readonly Mock<IKafkaProducerService> _kafkaProducerMock;
    private readonly Mock<IElasticClient> _elasticClientMock;
    private readonly Mock<ILogger<GetPermissionsHandler>> _loggerMock;
    private readonly AppDbContext _context;
    private readonly GetPermissionsHandler _handler;
    private readonly Guid[] _permissionIds;

    public GetPermissionsHandlerTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        // Setup mocks
        _kafkaProducerMock = new Mock<IKafkaProducerService>();
        _elasticClientMock = new Mock<IElasticClient>();
        _loggerMock = new Mock<ILogger<GetPermissionsHandler>>();

        // Setup Elasticsearch response
        var indexResponse = new Mock<IndexResponse>();
        indexResponse.Setup(x => x.IsValid).Returns(true);
        _elasticClientMock.Setup(x => x.IndexDocumentAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(indexResponse.Object);

        // Create handler instance
        _handler = new GetPermissionsHandler(
            _context,
            _kafkaProducerMock.Object,
            _elasticClientMock.Object,
            _loggerMock.Object
        );

        // Create and seed test data
        _permissionIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var permissions = new[]
        {
            new Permission
            {
                Id = _permissionIds[0],
                EmployeeName = "John Doe",
                PermissionTypeId = 1,
                PermissionDate = DateTime.UtcNow.AddDays(-2)
            },
            new Permission
            {
                Id = _permissionIds[1],
                EmployeeName = "Jane Smith",
                PermissionTypeId = 2,
                PermissionDate = DateTime.UtcNow.AddDays(-1)
            },
            new Permission
            {
                Id = _permissionIds[2],
                EmployeeName = "Bob Johnson",
                PermissionTypeId = 3,
                PermissionDate = DateTime.UtcNow
            }
        };

        _context.Permissions.AddRange(permissions);
        _context.SaveChanges();
    }

    [Fact]
    public async Task Handle_ShouldReturnAllPermissions()
    {
        // Arrange
        var query = new GetPermissionsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        var permissionsList = result.ToList();
        Assert.Equal(3, permissionsList.Count);

        // Verify each permission was returned with correct data
        for (int i = 0; i < permissionsList.Count; i++)
        {
            var permission = permissionsList[i];
            var originalPermission = await _context.Permissions.FindAsync(_permissionIds[i]);
            Assert.NotNull(originalPermission);
            Assert.Equal(originalPermission.Id, permission.Id);
            Assert.Equal(originalPermission.EmployeeName, permission.EmployeeName);
            Assert.Equal(originalPermission.PermissionTypeId, permission.PermissionTypeId);
            Assert.Equal(originalPermission.PermissionDate, permission.PermissionDate);
        }

        // Verify Kafka messages were sent for each permission
        _kafkaProducerMock.Verify(x => x.SendMessageAsync(
            It.Is<PermissionEventDto>(dto =>
                dto.Operation == "get" &&
                _permissionIds.Contains(dto.Id)),
            It.IsAny<CancellationToken>()),
            Times.Exactly(3));

        // Verify Elasticsearch indexing for each permission
        _elasticClientMock.Verify(x => x.IndexDocumentAsync(
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
