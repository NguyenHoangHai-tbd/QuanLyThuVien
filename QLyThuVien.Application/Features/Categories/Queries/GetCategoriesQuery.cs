using MediatR;
using QLyThuVien.Application.Features.Categories.Common;

namespace QLyThuVien.Application.Features.Categories.Queries;

public sealed record GetCategoriesQuery(string? Search) : IRequest<IReadOnlyCollection<CategoryDto>>;
