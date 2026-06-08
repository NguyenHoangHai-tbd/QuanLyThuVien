using QLyThuVien.Application.Common;

namespace QLyThuVien.Application.Interfaces;

public interface ICurrentUserContextWriter
{
    void Set(CurrentUserSnapshot snapshot);

    void Clear();
}
