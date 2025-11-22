using Receiptfly.Application.Services;

namespace Receiptfly.Infrastructure.Services;

public class MockGoogleVisionOcrService : IOcrService
{
    public Task<string> ExtractTextAsync(string imageFilePath, CancellationToken cancellationToken = default)
    {
        // モックデータを返す（実際のAPIを呼び出さない）
        var mockText = $@"スーパーライフ
東京都渋谷区1-2-3
TEL: 03-1234-5678
2023年11月22日 10:23

コピー用紙 A4    450円
豚肉 300g       890円

合計           1,340円
支払方法: クレジットカード
登録番号: T1234567890123";

        return Task.FromResult(mockText);
    }
}

