# GitHub Actions デプロイ設定ガイド

## 概要

Azure Static Web App にバックエンド API（.NET 8 Isolated Function App）を統合して GitHub Actions からデプロイするための設定ガイドです。

## 必要な準備作業（ユーザーが実施）

### 1. GitHub リポジトリの確認

- [ ] GitHub リポジトリが存在することを確認
- [ ] リポジトリへのアクセス権限があることを確認
- [ ] デプロイ対象のブランチを確認（例: `main`, `dev`）

### 2. Azure Portal での Deployment Token 取得

**方法 1: Azure Portal から取得（推奨）**

1. Azure Portal にログイン
2. Static Web App リソース（`swa-receiptfly-dev-001`）を開く
3. **Overview**ページで**Manage deployment token**をクリック
4. 表示されたトークンをコピー（後で GitHub Secrets に設定します）

**方法 2: Azure CLI から取得**

```bash
az staticwebapp secrets list \
    --name swa-receiptfly-dev-001 \
    --resource-group rg-receiptfly-dev \
    --query "properties.apiKey" \
    -o tsv
```

### 3. GitHub Secrets の設定

1. GitHub リポジトリのページに移動
2. **Settings**タブをクリック
3. 左側のメニューから**Secrets and variables** > **Actions**を選択
4. **New repository secret**をクリック
5. 以下のシークレットを追加：

| Secret 名                         | 値                                         | 説明                              |
| --------------------------------- | ------------------------------------------ | --------------------------------- |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | Azure Portal から取得した Deployment Token | Static Web App へのデプロイ認証用 |

**注意**: Secret 名は、ワークフローファイル内で使用する名前と一致させる必要があります。

### 4. （オプション）GitHub OIDC 認証の設定

Deployment Token の代わりに、よりセキュアな GitHub OIDC 認証を使用することもできます。

**必要な作業**:

1. Microsoft Entra アプリケーション（サービスプリンシパル）の作成
2. フェデレーテッド資格情報の設定
3. GitHub Secrets に以下を追加：
   - `AZURE_CLIENT_ID`
   - `AZURE_TENANT_ID`
   - `AZURE_SUBSCRIPTION_ID`

詳細は[Microsoft Docs](https://learn.microsoft.com/en-us/azure/static-web-apps/build-configuration#security)を参照してください。

## ワークフローファイルの配置

ワークフローファイルは以下の場所に配置されます：

```
.github/workflows/azure-static-web-apps-deploy.yml
```

このファイルは、リポジトリにコミット・プッシュすると自動的に GitHub Actions が実行されます。

## ワークフローの動作

1. **トリガー**: `main`ブランチへのプッシュ時
2. **ビルドステップ**:
   - Frontend（React）をビルド → `dist`フォルダに出力
   - API（.NET 8 Isolated Function App）をビルド → `api`フォルダに出力
3. **デプロイステップ**:
   - `skip_api_build: true`を設定して、事前にビルドした API をデプロイ
   - `staticwebapp.config.json`の`apiRuntime`設定を使用

## 確認事項

デプロイ前に以下を確認してください：

- [ ] GitHub リポジトリが存在する
- [ ] Deployment Token が取得できている
- [ ] GitHub Secrets に`AZURE_STATIC_WEB_APPS_API_TOKEN`が設定されている
- [ ] ワークフローファイルが`.github/workflows/`ディレクトリに配置されている
- [ ] `staticwebapp.config.json`に`platform.apiRuntime: "dotnet-isolated:8.0"`が設定されている

## トラブルシューティング

### デプロイが失敗する場合

1. **GitHub Actions のログを確認**

   - リポジトリの**Actions**タブから最新のワークフロー実行を確認
   - エラーメッセージを確認

2. **Deployment Token の確認**

   - Token が正しく設定されているか確認
   - Token が期限切れでないか確認（必要に応じて再生成）

3. **設定ファイルの確認**
   - `staticwebapp.config.json`の`apiRuntime`が正しいか確認
   - ワークフローファイルのパス設定が正しいか確認

## 参考リンク

- [Azure Static Web Apps Build Configuration](https://learn.microsoft.com/en-us/azure/static-web-apps/build-configuration)
- [Deployment Token Management](https://learn.microsoft.com/en-us/azure/static-web-apps/deployment-token-management)
- [GitHub Actions for Azure Static Web Apps](https://github.com/Azure/static-web-apps-deploy)
