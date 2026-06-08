using MediatR;
using QLyThuVien.Application.Features.Authors.Common;

namespace QLyThuVien.Application.Features.Authors.Queries;

public sealed record GetAuthorsQuery(string? Search) : IRequest<IReadOnlyCollection<AuthorDto>>;
