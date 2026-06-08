using MediatR;
using QLyThuVien.Application.Features.Policies.Common;

namespace QLyThuVien.Application.Features.Policies.Queries;

public sealed record GetCurrentPolicyQuery : IRequest<LibraryPolicyDto>;

