namespace QLyThuVien.Application.Interfaces;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
