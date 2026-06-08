using MediatR;
using QLyThuVien.Application.Features.Categories.Common;

namespace QLyThuVien.Application.Features.Categories.Commands.Create;

public sealed record CreateCategoryCommand(CategoryRequest Request) : IRequest<CategoryDto>;
