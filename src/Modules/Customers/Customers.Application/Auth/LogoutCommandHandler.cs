using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Results;

namespace Customers.Application.Auth;

public sealed class LogoutCommandHandler(IIdentityAuthService identityAuthService)
    : ICommandHandler<LogoutCommand, bool>
{
    public async Task<Result<bool>> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        await identityAuthService.LogoutAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
