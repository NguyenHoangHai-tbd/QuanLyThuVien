using MediatR;
using QLyThuVien.Application.Features.Publishers.Common;

namespace QLyThuVien.Application.Features.Publishers.Commands.Create;

public sealed record CreatePublisherCommand(PublisherRequest Request) : IRequest<PublisherDto>;
