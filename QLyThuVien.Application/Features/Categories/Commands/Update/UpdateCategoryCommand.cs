using MediatR;
using QLyThuVien.Application.Features.Categories.Common;

namespace QLyThuVien.Application.Features.Categories.Commands.Update;

public sealed record UpdateCategoryCommand(Guid Id, CategoryRequest Request) : IRequest<CategoryDto>;
