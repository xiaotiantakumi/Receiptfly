# Receiptfly インフラストラクチャ デプロイ

このディレクトリには、Receiptfly の Azure インフラストラクチャを Bicep で構築するためのファイルが含まれています。

## ディレクトリ構成

```
infra/
  main.bicep          # 全体のオーケストレーションファイル
  modules/
    storage.bicep     # Storage Account (Blob, Table)
    monitoring.bicep  # Log Analytics & App Insights
    keyvault.bicep    # Key Vault
    functions.bicep   # Function Apps (Consumption Plan)
    staticwebapp.bicep # Azure Static Web App
  scripts/
    deploy.sh         # デプロイ実行用スクリプト
    verify-deploy.sh  # リソースデプロイ確認用スクリプト
```

## 前提条件

- Azure CLI がインストールされていること
- Bicep CLI がインストールされていること（`az bicep install` でインストール可能）
- Azure にログインしていること（`az login`）
- 適切な権限があること（リソースグループの作成、リソースのデプロイ権限）

## デプロイ手順

### 1. Azure へのログイン

```bash
az login
```

### 2. デプロイスクリプトの実行

```bash
cd infra/scripts
chmod +x deploy.sh verify-deploy.sh
./deploy.sh dev japaneast rg-receiptfly-dev
```

パラメータ:

- `<環境名>`: `dev`, `test`, `prod` のいずれか
- `<リージョン>`: Azure リージョン名（例: `japaneast`, `eastasia`）
- `<リソースグループ名>`: 作成するリソースグループ名

### 3. デプロイ結果の確認

```bash
./verify-deploy.sh rg-receiptfly-dev
```

または、Azure Portal でリソースグループを確認してください。

## リソース設定詳細

### Storage Account (`storage.bicep`)

- **SKU**: `Standard_LRS` (Locally-redundant storage)
- **Kind**: `StorageV2`
- **Services**:
  - Blob Container: `receipt-images`
  - Table: `Receipts`
- **命名規則**: `streceiptfly{env}001` (例: `streceiptflydev001`)

### Monitoring (`monitoring.bicep`)

- **Log Analytics Workspace**:
  - **SKU**: `PerGB2018` (Pay-as-you-go、無料枠 5GB/月)
  - **Retention**: 30 日
- **Application Insights**:
  - **Kind**: `web`
  - **Type**: Workspace-based (Log Analytics に統合)

### Key Vault (`keyvault.bicep`)

- **SKU**: `Standard`
- **Access**: RBAC (Role Based Access Control)
- **命名規則**: `kv-receiptfly-{env}-001` (例: `kv-receiptfly-dev-001`)

### Function Apps (`functions.bicep`)

- **Plan**: Consumption Plan (Y1) - 自動的に作成される
- **Runtime**: .NET 8 Isolated (Linux)
- **Identity**: System Assigned Managed Identity
- **命名規則**:
  - API 用: `func-receiptfly-api-{env}-001`
  - Processing 用: `func-receiptfly-processing-{env}-001`

### Azure Static Web App (`staticwebapp.bicep`)

- **SKU**: `Free` - 無料プラン
- **命名規則**: `swa-receiptfly-{env}-001`

## シークレットの登録

デプロイ後、Key Vault に API キーを登録する必要があります：

```bash
# Google Cloud API Key
az keyvault secret set \
    --vault-name kv-receiptfly-dev-001 \
    --name GoogleCloudApiKey \
    --value "YOUR_GOOGLE_CLOUD_API_KEY"

# Gemini API Key
az keyvault secret set \
    --vault-name kv-receiptfly-dev-001 \
    --name GeminiApiKey \
    --value "YOUR_GEMINI_API_KEY"
```

## ソースコードのデプロイ

インフラストラクチャのデプロイ後、アプリケーションコードをデプロイします。

### 前提条件

- Azure Functions Core Tools がインストールされていること

  ```bash
  # macOS
  brew tap azure/functions
  brew install azure-functions-core-tools@4

  # または npm
  npm install -g azure-functions-core-tools@4 --unsafe-perm true
  ```

- Azure Static Web Apps CLI がインストールされていること

  ```bash
  npm install -g @azure/static-web-apps-cli
  ```

- .NET 8 SDK がインストールされていること
- Node.js と npm がインストールされていること

### Function Apps のデプロイ

```bash
cd infra/scripts
./deploy-functions.sh dev rg-receiptfly-dev
```

このスクリプトは以下を実行します：

1. API Function App (`func-receiptfly-api-dev-001`) をビルドしてデプロイ
2. Processing Function App (`func-receiptfly-processing-dev-001`) をビルドしてデプロイ

### Static Web App のデプロイ

```bash
cd infra/scripts
./deploy-staticwebapp.sh dev rg-receiptfly-dev
```

このスクリプトは以下を実行します：

1. Frontend をビルド（`npm run build`）
2. Static Web App にデプロイ

### 全体デプロイ（推奨）

インフラストラクチャとソースコードを一度にデプロイする場合：

```bash
cd infra/scripts
./deploy-all.sh dev japaneast rg-receiptfly-dev
```

このスクリプトは以下を順番に実行します：

1. インフラストラクチャのデプロイ（リソースグループが存在しない場合のみ）
2. Function Apps のデプロイ
3. Static Web App のデプロイ

## トラブルシューティング

### デプロイエラー

- リソース名が既に使用されている場合、環境番号を変更してください（`001` → `002` など）
- サブスクリプションのクォータを確認してください

### 検証エラー

- Managed Identity の権限設定が反映されるまで数分かかる場合があります
- Key Vault のシークレットは手動で登録する必要があります

### Function Apps のデプロイエラー

- Azure Functions Core Tools が正しくインストールされているか確認してください
- Function App が存在するか確認してください（インフラストラクチャのデプロイを先に実行）
- ビルドエラーが発生する場合、ローカルで `dotnet build` を実行して確認してください

### Static Web App のデプロイエラー

- Azure Static Web Apps CLI が正しくインストールされているか確認してください
- Static Web App が存在するか確認してください（インフラストラクチャのデプロイを先に実行）
- Deployment token が取得できない場合、Azure Portal から手動で取得してください

## コスト最適化

この構成は「固定費ゼロ」を目指して設計されています：

- **Storage Account**: Standard_LRS（最も安価な冗長構成）
- **Function Apps**: Consumption Plan（従量課金、月間 100 万リクエストまで無料）
- **Static Web App**: Free プラン（無料）
- **Log Analytics**: 無料枠 5GB/月
- **Application Insights**: Workspace-based（Log Analytics と統合）

## 参考資料

- [Azure Functions Consumption Plan](https://learn.microsoft.com/en-us/azure/azure-functions/consumption-plan)
- [Azure Static Web Apps](https://learn.microsoft.com/en-us/azure/static-web-apps/)
- [Azure Key Vault](https://learn.microsoft.com/en-us/azure/key-vault/)
- [Bicep ドキュメント](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/)
