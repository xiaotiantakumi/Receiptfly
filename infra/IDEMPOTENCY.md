# 冪等性について

## 現在の状況

既存のリソースと新しい固定形式の名前の対応関係：

| リソースタイプ            | 既存リソース名                       | 新しい固定形式                       | 状態      |
| ------------------------- | ------------------------------------ | ------------------------------------ | --------- |
| Storage Account           | `streceiptflydev001`                 | `streceiptflydev001`                 | ✅ 一致   |
| Key Vault                 | `kv-receiptfly-dev-001`              | `kv-receiptfly-dev-001`              | ✅ 一致   |
| Function App (API)        | `func-receiptfly-api-dev-001`        | `func-receiptfly-api-dev-001`        | ✅ 一致   |
| Function App (Processing) | `func-receiptfly-processing-dev-001` | `func-receiptfly-processing-dev-001` | ✅ 一致   |
| Log Analytics Workspace   | `log-receiptfly-dev-il4ap7yckrbem`   | `log-receiptfly-dev-001`             | ⚠️ 不一致 |
| Application Insights      | `appi-receiptfly-dev-il4ap7yckrbem`  | `appi-receiptfly-dev-001`            | ⚠️ 不一致 |

## 冪等性の問題

### ✅ 問題なく再デプロイ可能なリソース

以下のリソースは既存のリソース名と一致しているため、Bicep の冪等性により安全に再デプロイできます：

1. **Storage Account** (`streceiptflydev001`)

   - 既存リソースを更新します
   - 設定が異なる場合は更新されます

2. **Key Vault** (`kv-receiptfly-dev-001`)

   - 既存リソースを更新します
   - RBAC 設定などが更新されます

3. **Function Apps** (`func-receiptfly-api-dev-001`, `func-receiptfly-processing-dev-001`)
   - 既存リソースを更新します
   - App Settings などが更新されます

### ⚠️ 問題が発生する可能性があるリソース

以下のリソースは既存のリソース名と異なるため、新しいリソースを作成しようとしてエラーになる可能性があります：

1. **Log Analytics Workspace**

   - 既存: `log-receiptfly-dev-il4ap7yckrbem`
   - 新規作成試行: `log-receiptfly-dev-001`
   - **問題**: 新しい名前でリソースを作成しようとしますが、既存のリソースが Function Apps で参照されているため、参照エラーが発生する可能性があります

2. **Application Insights**
   - 既存: `appi-receiptfly-dev-il4ap7yckrbem`
   - 新規作成試行: `appi-receiptfly-dev-001`
   - **問題**: 新しい名前でリソースを作成しようとしますが、既存のリソースが Function Apps で参照されているため、参照エラーが発生する可能性があります

## 解決策

### オプション 1: 既存リソース名に合わせる（推奨）

既存のリソース名を確認して、Bicep ファイルで既存リソースを参照するように修正します。

```bicep
// monitoring.bicep を修正
var logWorkspaceName = 'log-receiptfly-dev-il4ap7yckrbem'  // 既存の名前を使用
var appInsightsName = 'appi-receiptfly-dev-il4ap7yckrbem'  // 既存の名前を使用
```

### オプション 2: 既存リソースを削除してから再デプロイ

既存の Log Analytics Workspace と Application Insights を削除してから、新しい固定形式の名前で再作成します。

**注意**: Function Apps が既存の Application Insights を参照しているため、削除前に参照を解除する必要があります。

### オプション 3: `existing`キーワードを使用

既存のリソースを参照するために`existing`キーワードを使用します。ただし、この場合、既存リソースの設定を変更することはできません。

## 対応済み

**オプション 1 を採用しました**。`monitoring.bicep`を修正して、既存のリソース名を使用するようにしました。

- dev 環境: 既存のリソース名 (`log-receiptfly-dev-il4ap7yckrbem`, `appi-receiptfly-dev-il4ap7yckrbem`) を使用
- その他の環境: 固定形式 (`log-receiptfly-{env}-001`, `appi-receiptfly-{env}-001`) を使用

これにより、dev 環境での再デプロイ時に既存リソースが更新され、冪等性が保証されます。

## Bicep の冪等性について

Bicep のデプロイは基本的に冪等性があります：

- **既存リソースがある場合**: リソースを更新します（設定が異なる場合）
- **リソースが存在しない場合**: 新しいリソースを作成します
- **リソース名が異なる場合**: 新しいリソースを作成しようとします（エラーになる可能性があります）

リソース名が一致している限り、何度でも安全に再デプロイできます。
