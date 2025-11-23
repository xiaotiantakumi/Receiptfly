namespace Receiptfly.Application.Helpers;

/// <summary>
/// ID生成ヘルパー
/// </summary>
public static class IdGenerator
{
    /// <summary>
    /// レシートIDを生成
    /// </summary>
    /// <returns>receipt-{uuid} 形式のID</returns>
    public static string GenerateReceiptId()
    {
        return $"receipt-{Guid.NewGuid()}";
    }

    /// <summary>
    /// 取引明細項目IDを生成
    /// </summary>
    /// <returns>transaction-{uuid} 形式のID</returns>
    public static string GenerateTransactionItemId()
    {
        return $"transaction-{Guid.NewGuid()}";
    }
}

