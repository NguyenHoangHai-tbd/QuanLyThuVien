using MediatR;

namespace QLyThuVien.Application.Features.Categories.Commands.Delete;

public sealed record DeleteCategoryCommand(Guid Id) : IRequest;
