using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Results;

namespace Customers.Application.Auth;

public sealed class ForgotPasswordCommandHandler(IIdentityAuthService identityAuthService)
    : ICommandHandler<ForgotPasswordCommand, bool>
{
    public async Task<Result<bool>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var result = await identityAuthService.ForgotPasswordAsync(request.Email, cancellationToken);
        return result.IsSuccess
            ? Result<bool>.Success(true)
            : Result<bool>.Failure(result.Error);
    }
}
