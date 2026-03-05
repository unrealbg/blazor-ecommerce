using BuildingBlocks.Domain.Results;
using MediatR;

namespace BuildingBlocks.Application.Abstractions;

public interface ICommand<TResponse> : IRequest<Result<TResponse>>;

public interface ICommand : IRequest<Result>;

public interface IQuery<TResponse> : IRequest<TResponse>;

public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>;

public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>;
