using Nest;
using N5Now.Domain.Entities;

public class PermissionElasticService
{
    private readonly IElasticClient _elasticClient;
    private const string IndexName = "permissions";

    public PermissionElasticService(IElasticClient elasticClient)
    {
        _elasticClient = elasticClient;
    }

    public async Task IndexPermissionAsync(Permission permission)
    {
        await _elasticClient.IndexAsync(permission, idx => idx.Index(IndexName));
    }
}
