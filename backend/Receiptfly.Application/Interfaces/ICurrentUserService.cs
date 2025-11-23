namespace Receiptfly.Application.Interfaces;

/// <summary>
/// 現在のユーザーIDを取得するサービス
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// 現在のユーザーIDを取得
    /// </summary>
    Task<string> GetCurrentUserIdAsync(CancellationToken cancellationToken = default);
}

