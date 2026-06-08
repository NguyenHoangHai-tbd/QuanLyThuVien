using MediatR;
using QLyThuVien.Application.Features.Publishers.Common;

namespace QLyThuVien.Application.Features.Publishers.Queries;

public sealed record GetPublishersQuery(string? Search) : IRequest<IReadOnlyCollection<PublisherDto>>;
