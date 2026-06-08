using MediatR;
using QLyThuVien.Application.Features.Auth.Common;
using QLyThuVien.Application.Features.Auth.Commands.Logout;
using QLyThuVien.Application.Interfaces;

namespace QLyThuVien.Application.Features.Auth.Handlers;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, LogoutResponse>
{
    private readonly ICurrentUserContextWriter _currentUserWriter;

    public LogoutCommandHandler(ICurrentUserContextWriter currentUserWriter)
    {
        _currentUserWriter = currentUserWriter;
    }

    public Task<LogoutResponse> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        _currentUserWriter.Clear();
        return Task.FromResult(new LogoutResponse("Logged out successfully."));
    }
}
