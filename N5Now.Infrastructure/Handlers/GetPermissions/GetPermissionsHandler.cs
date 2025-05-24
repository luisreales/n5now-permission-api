using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nest;
using N5Now.Application.DTOs;
using N5Now.Application.Queries.GetPermissions;
using N5Now.Domain.Entities;
using N5Now.Domain.Kafka;
using N5Now.Infrastructure.Data;
using N5Now.Infrastructure.Kafka;

namespace N5Now.Infrastructure.Handlers.GetPermissions;

public class GetPermissionsHandler : IRequestHandler<GetPermissionsQuery, IEnumerable<PermissionDto>>
{
    private readonly AppDbContext _context;
    private readonly IKafkaProducerService _kafka;
    private readonly IElasticClient _elastic;
    private readonly ILogger<GetPermissionsHandler> _logger;

    public GetPermissionsHandler(
        AppDbContext context,
        IKafkaProducerService kafka,
        IElasticClient elastic,
        ILogger<GetPermissionsHandler> logger)
    {
        _context = context;
        _kafka = kafka;
        _elastic = elastic;
        _logger = logger;
    }

    public async Task<IEnumerable<PermissionDto>> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
    {
        var permissions = await _context.Permissions.ToListAsync(cancellationToken);

        foreach (var entity in permissions)
        {
            var eventDto = new PermissionEventDto
            {
                Id = entity.Id,
                Operation = "get",
                EmployeeName = entity.EmployeeName,
                PermissionTypeId = entity.PermissionTypeId,
                PermissionDate = entity.PermissionDate
            };

            // Kafka
            await _kafka.SendMessageAsync(eventDto, cancellationToken);

            // Elasticsearch
            var response = await _elastic.IndexDocumentAsync(entity);
            if (!response.IsValid)
            {
                _logger.LogError("Failed to index permission {Id} in Elasticsearch: {Error}", entity.Id, response.OriginalException.Message);
            }
        }

        return permissions.Select(p => new PermissionDto
        {
            Id = p.Id,
            EmployeeName = p.EmployeeName,
            PermissionTypeId = p.PermissionTypeId,
            PermissionDate = p.PermissionDate
        });
    }
}