using MediatR;

namespace QLyThuVien.Application.Features.Publishers.Commands.Delete;

public sealed record DeletePublisherCommand(Guid Id) : IRequest;
