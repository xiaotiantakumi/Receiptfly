# Static Web App API統合デプロイ トラブルシューティング記録

## 概要

Azure Static Web AppにバックエンドAPI（.NET 8 Isolated Function App）を統合してデプロイする際に発生した問題と、その解決に向けた試行錯誤の記録です。

## 問題の発生

### エラーメッセージ

```
✖ Cannot deploy to the function app because Function language info isn't provided, 
use flags "--api-language" and "--api-version" or add a "platform.apiRuntime" 
property to your staticwebapp.config.json file, or create one in dist.
```

### 発生環境

- Azure Static Web Apps CLI: 2.0.6
- .NET SDK: 8.0.302
- Azure Functions Core Tools: 4.0.6610
- Node.js: v22.14.0
- デプロイ対象: Static Web App (`swa-receiptfly-dev-001`) に .NET 8 Isolated Function App を統合

## 試行した解決策

### 試行 1: swa-cli.config.json に apiLanguage と apiVersion を追加

**実施内容:**
- `frontend/receiptfly-web/swa-cli.config.json` に以下を追加：
  - `apiLanguage: "dotnet-isolated"`
  - `apiVersion: "8.0"`
  - `apiLocation: "api"` (相対パスに変更)

**結果:** ❌ 失敗
- `swa deploy receiptfly` を実行してもエラーが継続
- `swa-cli.config.json` の設定が認識されていない可能性

**コード変更:**
```json
{
  "$schema": "https://aka.ms/azure/static-web-apps-cli/schema",
  "configurations": {
    "receiptfly": {
      "appLocation": ".",
      "apiLocation": "api",
      "outputLocation": "dist",
      "apiLanguage": "dotnet-isolated",
      "apiVersion": "8.0",
      "appBuildCommand": "npm run build",
      "appDevserverUrl": "http://localhost:5173"
    }
  }
}
```

---

### 試行 2: deploy-staticwebapp-api.sh で swa-cli.config.json を使わずにコマンドライン引数を指定

**実施内容:**
- `infra/scripts/deploy-staticwebapp-api.sh` の `swa deploy` コマンドを変更
- `swa-cli.config.json` を使わず、すべてのパラメータをコマンドライン引数で指定

**結果:** ❌ 失敗
- `--api-language` と `--api-version` フラグを指定してもエラーが継続
- `swa deploy` がこれらのフラグを認識していない可能性

**コード変更:**
```bash
swa deploy \
    --app-location dist \
    --api-location api \
    --api-language dotnet-isolated \
    --api-version 8.0 \
    --deployment-token "$DEPLOYMENT_TOKEN" \
    --env production \
    --no-use-keychain
```

---

### 試行 3: staticwebapp.config.json の apiRuntime 設定を確認

**実施内容:**
- `frontend/receiptfly-web/staticwebapp.config.json` の内容を確認
- `dist/staticwebapp.config.json` が正しくコピーされているか確認

**確認結果:**
- ✅ `staticwebapp.config.json` には `platform.apiRuntime: "dotnet-isolated:8.0"` が正しく設定されている
- ✅ `dist/staticwebapp.config.json` にも正しくコピーされている
- ✅ `api/staticwebapp.config.json` にもコピーされている（スクリプトで実装済み）

**結果:** ❌ 失敗
- `staticwebapp.config.json` の設定が正しいにもかかわらず、`swa deploy` が認識していない

**確認したファイル内容:**
```json
{
  "platform": {
    "apiRuntime": "dotnet-isolated:8.0"
  },
  "routes": [
    {
      "route": "/api/*",
      "allowedRoles": ["anonymous"]
    }
  ],
  ...
}
```

---

### 試行 4: swa-cli.config.json を一時的に無効化してテスト

**実施内容:**
- `swa-cli.config.json` を `swa-cli.config.json.bak` にリネームして一時的に無効化
- コマンドライン引数のみでデプロイを試行

**結果:** ❌ 失敗
- `swa-cli.config.json` がなくてもエラーが継続
- `swa deploy` が `dist/staticwebapp.config.json` を見つけているが、`apiRuntime` を認識していない

**実行コマンド:**
```bash
cd frontend/receiptfly-web
mv swa-cli.config.json swa-cli.config.json.bak
cd ../..
bash infra/scripts/deploy-staticwebapp-api.sh dev rg-receiptfly-dev
```

**出力:**
```
Found configuration file:
  /Users/takumi/Documents/src/private_src/Receiptfly/frontend/receiptfly-web/dist/staticwebapp.config.json

Deploying project to Azure Static Web Apps...
- Preparing deployment. Please wait...
✖ Cannot deploy to the function app because Function language info isn't provided...
```

---

### 試行 5: swa deploy のヘルプで利用可能なフラグを確認

**実施内容:**
- `swa deploy --help` を実行して、`--api-language` と `--api-version` フラグの存在を確認

**確認結果:**
- ✅ `--api-language` フラグは存在する（デフォルト: "node"）
- ✅ `--api-version` フラグは存在する（デフォルト: "16"）
- フラグは存在するが、`dotnet-isolated` と `8.0` を指定してもエラーが継続

**ヘルプ出力:**
```
-al, --api-language <apiLanguage>    the runtime language of the function/api (default: "node")
-av, --api-version <apiVersion>       the version of the function runtime language (default: "16")
```

---

### 試行 6: Microsoft Docs を確認

**実施内容:**
- Azure Static Web Apps CLI のデプロイに関する公式ドキュメントを確認
- `staticwebapp.config.json` の `apiRuntime` 設定方法を確認
- `.NET Isolated` のサポート状況を確認

**確認結果:**
- ✅ MS Docs によると、`.NET 8.0 isolated` の `apiRuntime` 値は `dotnet-isolated:8.0` で正しい
- ✅ `staticwebapp.config.json` に `platform.apiRuntime` を設定する方法が推奨されている
- ⚠️ 「Skip building the API」セクションは GitHub Actions や Azure Pipelines のワークフロー用の設定
- ⚠️ `swa deploy` CLI コマンドには `skip_api_build` オプションが存在しない
- ⚠️ `swa deploy` CLI はソースコードから API をビルドすることを前提としている可能性

**重要な発見:**
- Microsoft Docs の「Skip building the API」機能は、GitHub Actions や Azure Pipelines のワークフロー専用
- `swa deploy` CLI コマンドで事前にビルドした API をデプロイする方法が明確に記載されていない
- `.NET Isolated` のサポートは確認できたが、CLI でのデプロイ方法に関する詳細な説明がない

**参考URL:**
- https://learn.microsoft.com/en-us/azure/static-web-apps/static-web-apps-cli-deploy
- https://learn.microsoft.com/azure/static-web-apps/build-configuration?tabs=github-actions#skip-building-the-api
- https://learn.microsoft.com/en-us/azure/static-web-apps/configuration#platform

**確認した apiRuntime の値:**
| Language runtime version | apiRuntime value |
| --- | --- |
| .NET 8.0 isolated | `dotnet-isolated:8.0` |
| .NET 6.0 isolated | `dotnet-isolated:6.0` |
| Node.js 20.x | `node:20` |

---

### 試行 7: Web検索で類似問題を調査

**実施内容:**
- 「swa deploy dotnet-isolated Function language info isn't provided」で検索
- GitHub の issue や Stack Overflow で同様の問題を調査
- Azure Static Web Apps CLI の既知の問題を確認

**検索結果:**
- 直接的な解決策は見つからなかった
- 一般的なトラブルシューティング手順は見つかったが、`.NET Isolated` 特有の問題に関する情報は限定的
- GitHub Actions や Azure Pipelines を使用したデプロイ方法に関する情報は豊富

**参考情報:**
- Azure Static Web Apps のトラブルシューティングガイド
- API 統合に関する一般的な問題解決手順

---

## 現在の状況

### 確認済みの設定

1. ✅ `staticwebapp.config.json` に `platform.apiRuntime: "dotnet-isolated:8.0"` が設定されている
2. ✅ `dist/staticwebapp.config.json` に正しくコピーされている
3. ✅ `api/staticwebapp.config.json` にもコピーされている
4. ✅ `swa-cli.config.json` に `apiLanguage` と `apiVersion` が設定されている
5. ✅ `swa deploy` コマンドに `--api-language` と `--api-version` フラグを指定している

### 問題点

- `swa deploy` コマンドが `staticwebapp.config.json` の `apiRuntime` 設定を認識していない
- `--api-language` と `--api-version` フラグを指定してもエラーが継続
- Azure Static Web Apps CLI 2.0.6 では `.NET Isolated` のサポートに問題がある可能性

---

## 重要な発見と考察

### Microsoft Docs からの重要な情報

1. **`.NET Isolated` のサポート確認**
   - ✅ `.NET 8.0 isolated` は正式にサポートされている
   - ✅ `apiRuntime` の値は `dotnet-isolated:8.0` で正しい
   - ✅ Windows 上で動作し、Azure Functions version 4.x を使用

2. **「Skip building the API」機能の制限**
   - ⚠️ この機能は GitHub Actions や Azure Pipelines のワークフロー専用
   - ⚠️ `swa deploy` CLI コマンドには `skip_api_build` オプションが存在しない
   - ⚠️ CLI での事前ビルド済み API のデプロイ方法が明確に記載されていない

3. **`swa deploy` CLI の動作前提**
   - `swa deploy` CLI は、ソースコードから API をビルドすることを前提としている可能性
   - 事前にビルドした API をデプロイする場合は、GitHub Actions や Azure Pipelines のワークフローを使用する必要がある可能性

### 問題の根本原因の仮説

1. **CLI の制限**
   - `swa deploy` CLI コマンドが、事前にビルドした `.NET Isolated` API を正しく認識できない
   - `staticwebapp.config.json` の `apiRuntime` 設定が CLI で正しく読み取られていない

2. **ビルドプロセスの違い**
   - GitHub Actions や Azure Pipelines では、ビルドプロセス中に `apiRuntime` を認識して適切に処理する
   - CLI では、事前にビルドした API に対して `apiRuntime` を適用する仕組みが不完全

## 次のステップ（未実施）

### 検討事項

1. **GitHub Actions または Azure Pipelines への移行**
   - `swa deploy` CLI の代わりに、GitHub Actions や Azure Pipelines のワークフローを使用
   - `skip_api_build: true` を設定して、事前にビルドした API をデプロイ
   - これにより、「Skip building the API」機能を活用できる

2. **API をソースコードからビルドする方法に変更**
   - `swa deploy` CLI で API のソースコードを直接指定
   - CLI が自動的にビルドすることを期待
   - ただし、`.NET Isolated` のビルドが CLI でサポートされているか確認が必要

3. **別のデプロイ方法の検討**
   - Azure Portal からの手動デプロイ（ZIP ファイルアップロード）
   - Azure CLI (`az staticwebapp`) を使用したデプロイ
   - Azure Functions Core Tools (`func azure functionapp deploy`) を使用した別途デプロイ

4. **API のデプロイ方法の変更**
   - Static Web App の統合 API ではなく、別の Function App としてデプロイ
   - CORS 設定を追加して別ドメインからアクセス可能にする
   - 「Bring your own functions」機能を活用

5. **Azure Static Web Apps CLI のバージョンアップ**
   - 最新版への更新を確認
   - `.NET Isolated` サポートが改善されたバージョンを確認
   - GitHub のリリースノートや issue を確認

6. **設定ファイルの形式確認**
   - `staticwebapp.config.json` の JSON 構文エラーがないか確認
   - `apiRuntime` の値の形式が正しいか確認（`dotnet-isolated:8.0` で正しいことを確認済み）

---

## 関連ファイル

### 設定ファイル

- `frontend/receiptfly-web/swa-cli.config.json`
- `frontend/receiptfly-web/staticwebapp.config.json`
- `backend/Receiptfly.Functions/Receiptfly.Functions.csproj`

### デプロイスクリプト

- `infra/scripts/deploy-staticwebapp-api.sh`

### 参考ドキュメント

- [Azure Static Web Apps CLI Deploy](https://learn.microsoft.com/en-us/azure/static-web-apps/static-web-apps-cli-deploy)
- [Static Web Apps Build Configuration](https://learn.microsoft.com/azure/static-web-apps/build-configuration?tabs=github-actions#skip-building-the-api)

---

## 参考情報とリソース

### Microsoft Docs

- [Azure Static Web Apps CLI Deploy](https://learn.microsoft.com/en-us/azure/static-web-apps/static-web-apps-cli-deploy)
- [Static Web Apps Build Configuration](https://learn.microsoft.com/azure/static-web-apps/build-configuration?tabs=github-actions#skip-building-the-api)
- [Configure Azure Static Web Apps](https://learn.microsoft.com/en-us/azure/static-web-apps/configuration#platform)
- [Supported languages and runtimes](https://learn.microsoft.com/en-us/azure/static-web-apps/languages-runtimes#api)
- [API support in Azure Static Web Apps with Azure Functions](https://learn.microsoft.com/en-us/azure/static-web-apps/apis-functions)
- [Bring your own functions to Azure Static Web Apps](https://learn.microsoft.com/en-us/azure/static-web-apps/functions-bring-your-own)

### 検索キーワード

以下のキーワードで検索したが、直接的な解決策は見つからなかった：
- "swa deploy" "dotnet-isolated" "Function language info isn't provided"
- "Azure Static Web Apps CLI" "pre-built API" "dotnet isolated"
- "swa deploy" "skip_api_build" "CLI"

### 関連する可能性のある GitHub Issues

（調査が必要）

---

## 更新履歴

- 2024-12-XX: 初版作成
  - 試行 1-6 の記録を追加
  - 現在の状況と次のステップを整理
- 2024-12-XX: 調査結果を追加
  - Microsoft Docs の詳細な調査結果を追加
  - Web検索の結果を追加
  - 重要な発見と考察セクションを追加
  - 問題の根本原因の仮説を追加
  - 次のステップを詳細化

