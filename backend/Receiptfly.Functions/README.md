# Receiptfly.Functions ローカル開発設定

## local.settings.json の設定

`local.settings.json.example` をコピーして `local.settings.json` を作成してください。

### 重要な設定

- **UseAzure**: デフォルトは `true`（Azure Storageを使用）
  - ローカル開発時のみ `false` を明示的に指定してください
  - Azure Function Appにデプロイする場合は、この設定を削除するか `true` に設定してください
  - Bicepテンプレートで自動的に `UseAzure=true` が設定されます

### ローカル開発時の設定例

```json
{
  "Values": {
    "UseAzure": "false",
    "AzureStorage": "UseDevelopmentStorage=true"
  }
}
```

### Azure Function Appでの設定

Bicepテンプレートにより、以下の設定が自動的に適用されます：
- `UseAzure=true`
- `AzureStorage__accountName`: Storage Account名
- `AzureStorage__credential`: managedidentity
