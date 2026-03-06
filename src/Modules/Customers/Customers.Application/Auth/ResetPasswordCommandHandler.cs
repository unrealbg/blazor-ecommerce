using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Results;

namespace Customers.Application.Auth;

public sealed class ResetPasswordCommandHandler(IIdentityAuthService identityAuthService)
    : ICommandHandler<ResetPasswordCommand, bool>
{
    public async Task<Result<bool>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var result = await identityAuthService.ResetPasswordAsync(
            request.Email,
            request.Token,
            request.NewPassword,
            cancellationToken);

        return result.IsSuccess
            ? Result<bool>.Success(true)
            : Result<bool>.Failure(result.Error);
    }
}
