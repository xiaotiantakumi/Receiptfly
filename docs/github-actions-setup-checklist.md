# GitHub Actions デプロイ設定チェックリスト

## ユーザーが実施する必要がある作業

### ✅ ステップ 1: Azure PortalでDeployment Tokenを取得

1. [Azure Portal](https://portal.azure.com)にログイン
2. Static Web Appリソース（`swa-receiptfly-dev-001`）を開く
3. **Overview**ページで**Manage deployment token**ボタンをクリック
4. 表示されたトークンをコピー（長い文字列です）
5. 次のステップでGitHub Secretsに設定します

**または、Azure CLIで取得:**

```bash
az staticwebapp secrets list \
    --name swa-receiptfly-dev-001 \
    --resource-group rg-receiptfly-dev \
    --query "properties.apiKey" \
    -o tsv
```

---

### ✅ ステップ 2: GitHub Secretsの設定

1. GitHubリポジトリのページに移動
2. **Settings**タブをクリック
3. 左側のメニューから**Secrets and variables** > **Actions**を選択
4. **New repository secret**ボタンをクリック
5. 以下の情報を入力：

   - **Name**: `AZURE_STATIC_WEB_APPS_API_TOKEN`
   - **Secret**: ステップ1で取得したDeployment Tokenを貼り付け
   
6. **Add secret**ボタンをクリック

**確認**: Secrets一覧に`AZURE_STATIC_WEB_APPS_API_TOKEN`が表示されていることを確認してください。

---

### ✅ ステップ 3: ワークフローファイルの確認

以下のファイルがリポジトリに存在することを確認：

- `.github/workflows/azure-static-web-apps-deploy.yml`

このファイルは既に作成済みです。リポジトリにコミット・プッシュしてください。

---

### ✅ ステップ 4: 設定ファイルの確認

以下のファイルが正しく設定されていることを確認：

- `frontend/receiptfly-web/staticwebapp.config.json`
  - `platform.apiRuntime: "dotnet-isolated:8.0"` が設定されている ✅

---

### ✅ ステップ 5: 初回デプロイの実行

1. ワークフローファイルをリポジトリにコミット・プッシュ：

```bash
git add .github/workflows/azure-static-web-apps-deploy.yml
git commit -m "Add GitHub Actions workflow for Static Web App deployment"
git push
```

2. GitHubリポジトリの**Actions**タブを開く
3. ワークフローが実行されていることを確認
4. 実行が成功するまで待つ（通常5-10分程度）

---

## 確認事項

デプロイ前に以下を確認してください：

- [ ] GitHubリポジトリが存在する
- [ ] Deployment Tokenが取得できている
- [ ] GitHub Secretsに`AZURE_STATIC_WEB_APPS_API_TOKEN`が設定されている
- [ ] ワークフローファイル（`.github/workflows/azure-static-web-apps-deploy.yml`）が存在する
- [ ] `staticwebapp.config.json`に`platform.apiRuntime: "dotnet-isolated:8.0"`が設定されている
- [ ] リポジトリに変更をコミット・プッシュする準備ができている

---

## トラブルシューティング

### デプロイが失敗する場合

1. **GitHub Actionsのログを確認**
   - リポジトリの**Actions**タブから最新のワークフロー実行をクリック
   - 失敗したジョブをクリックしてエラーメッセージを確認

2. **Deployment Tokenの確認**
   - Secret名が`AZURE_STATIC_WEB_APPS_API_TOKEN`で正しいか確認
   - Tokenの値が正しく設定されているか確認（コピー&ペーストのミスがないか）

3. **設定ファイルの確認**
   - `staticwebapp.config.json`の`apiRuntime`が`dotnet-isolated:8.0`であることを確認
   - ワークフローファイルのパス設定が正しいか確認

4. **ビルドエラーの確認**
   - .NET SDKのバージョンが8.0であることを確認
   - Node.jsのバージョンが20.xであることを確認
   - 依存関係のインストールが成功しているか確認

---

## 次のステップ

デプロイが成功したら：

1. Static Web AppのURLにアクセスして動作確認
2. APIエンドポイント（`/api/*`）が正常に動作することを確認
3. 必要に応じて、環境変数や設定を調整

---

## 参考リンク

- [GitHub Actions設定ガイド](./github-actions-setup-guide.md)
- [Azure Static Web Apps Build Configuration](https://learn.microsoft.com/en-us/azure/static-web-apps/build-configuration)
- [Deployment Token Management](https://learn.microsoft.com/en-us/azure/static-web-apps/deployment-token-management)

