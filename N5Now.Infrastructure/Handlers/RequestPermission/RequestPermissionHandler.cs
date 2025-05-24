using MediatR;
using N5Now.Application.Commands.RequestPermission;
using N5Now.Domain.Entities;
using N5Now.Domain.Kafka;
using N5Now.Infrastructure.Data;
using N5Now.Infrastructure.Kafka;
using Nest; // <-- Elasticsearch
using Microsoft.Extensions.Logging;

namespace N5Now.Infrastructure.Handlers.RequestPermission;

public class RequestPermissionHandler : IRequestHandler<RequestPermissionCommand, Guid>
{
    private readonly AppDbContext _context;
    private readonly IKafkaProducerService _kafka;
    private readonly IElasticClient _elastic;
    private readonly ILogger<RequestPermissionHandler> _logger;

    public RequestPermissionHandler(
        AppDbContext context,
        IKafkaProducerService kafka,
        IElasticClient elastic,
        ILogger<RequestPermissionHandler> logger)
    {
        _context = context;
        _kafka = kafka;
        _elastic = elastic;
        _logger = logger;
    }

    public async Task<Guid> Handle(RequestPermissionCommand request, CancellationToken cancellationToken)
    {
        var entity = new Permission
        {
            Id = Guid.NewGuid(),
            EmployeeName = request.EmployeeName,
            PermissionTypeId = request.PermissionTypeId,
            PermissionDate = request.PermissionDate
        };

        _context.Permissions.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        // Guardar en Kafka
        await _kafka.PublishAsync(new PermissionEventDto
        {
            Id = entity.Id,
            Operation = "request",
            EmployeeName = entity.EmployeeName,
            PermissionTypeId = entity.PermissionTypeId,
            PermissionDate = entity.PermissionDate
        });

        // Guardar en Elasticsearch
        var response = await _elastic.IndexDocumentAsync(entity);

        if (!response.IsValid)
        {
            _logger.LogError("Error indexing document in Elasticsearch: {Error}", response.OriginalException.Message);
        }

        return entity.Id;
    }
}