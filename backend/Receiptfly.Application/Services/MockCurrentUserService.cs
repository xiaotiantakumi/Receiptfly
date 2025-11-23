using Receiptfly.Application.Interfaces;

namespace Receiptfly.Application.Services;

/// <summary>
/// 開発用のモックユーザーサービス（常に user_default を返す）
/// </summary>
public class MockCurrentUserService : ICurrentUserService
{
    public Task<string> GetCurrentUserIdAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult("user_default");
    }
}

