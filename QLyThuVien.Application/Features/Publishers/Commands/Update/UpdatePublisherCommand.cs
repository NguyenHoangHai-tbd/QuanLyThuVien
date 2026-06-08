using MediatR;
using QLyThuVien.Application.Features.Publishers.Common;

namespace QLyThuVien.Application.Features.Publishers.Commands.Update;

public sealed record UpdatePublisherCommand(Guid Id, PublisherRequest Request) : IRequest<PublisherDto>;
