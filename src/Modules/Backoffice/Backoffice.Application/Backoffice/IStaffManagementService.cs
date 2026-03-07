using BuildingBlocks.Domain.Results;

namespace Backoffice.Application.Backoffice;

public interface IStaffManagementService
{
    Task<IReadOnlyCollection<BackofficeStaffRoleCatalogItemDto>> GetRoleCatalogAsync(CancellationToken cancellationToken);

    Task<BackofficeStaffPage> GetStaffAsync(
        string? query,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<BackofficeStaffUserDetailDto?> GetStaffUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<Result<Guid>> CreateStaffUserAsync(
        string email,
        string password,
        string? displayName,
        string? department,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken);

    Task<Result> SetStaffActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken);

    Task<Result> AssignRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken);

    Task<Result> RemoveRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken);
}
