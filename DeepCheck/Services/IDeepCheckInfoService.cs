namespace DeepCheck.Services;

using DeepCheck.DTOs;

public interface IDeepCheckInfoService
{
    Task<DeepCheckInfo> GetDeepCheckInfo(CancellationToken cancellationToken = default);
}
