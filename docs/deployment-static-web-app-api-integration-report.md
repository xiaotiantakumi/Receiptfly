# Azure Static Web Apps API 統合デプロイ対応レポート

## 概要

Azure Static Web Apps の Free プランで、.NET 8 Isolated Functions をバックエンド API として統合する際に発生した問題とその解決策をまとめたレポートです。

## 発生した問題

### 1. Azure Portal で API が検知されない

**問題の詳細:**

- GitHub Actions でのデプロイは成功していたが、Azure Portal の「APIs」セクションで API が検知されない
- 「Backend Type: -」「Backend Resource Name: -」と表示される
- API エンドポイント（`/api/receipts`）が 404 を返す

**原因:**

- `skip_api_build: true`を使用していたため、API が「Bring your own API」として扱われていた
- Azure Static Web Apps の Free プランでは「Bring your own API」がサポートされていない
- Free プランでは「Managed functions」のみが利用可能

**参考資料:**

- [Microsoft Q&A - Azure Static Web Apps Limit 104857600 bytes](https://learn.microsoft.com/en-us/answers/questions/1631839/azure-static-web-apps-limit-104857600-bytes-why)
- [Azure Static Web Apps Hosting Plans](https://learn.microsoft.com/en-us/azure/static-web-apps/plans)

### 2. 100MB 制限エラー

**問題の詳細:**

```
The content server has rejected the request with: BadRequest
Reason: The size of the function content was too large. The limit for this Static Web App is 104857600 bytes.
```

**原因:**

- API functions のサイズが 100MB を超えていた
- 100MB 制限は「API functions」のサイズ制限であり、アプリ全体のサイズ制限（Free: 250MB、Standard: 500MB）とは別物
- Standard プランにアップグレードしても解決しない（API functions のサイズ制限はプランに関係なく 100MB）

**参考資料:**

- [Microsoft Q&A - Azure Static Web Apps Limit 104857600 bytes](https://learn.microsoft.com/en-us/answers/questions/1631839/azure-static-web-apps-limit-104857600-bytes-why)

### 3. PublishTrimmed エラー

**問題の詳細:**

```
error NETSDK1102: Optimizing assemblies for size is not supported for the selected publish configuration.
Please ensure that you are publishing a self-contained app.
```

**原因:**

- `PublishTrimmed: true`を設定していたが、`SelfContained: false`になっていた
- `PublishTrimmed`を使用するには`SelfContained: true`が必要
- Static Web Apps の Managed Functions では framework-dependent（`SelfContained: false`）が推奨

## 実施した解決策

### 1. Free プラン対応：Managed Functions への変更

**変更内容:**

- `skip_api_build: false`に変更
- `api_location`をソースコードディレクトリ（`backend/Receiptfly.Functions`）に変更
- 事前ビルドジョブ（`build_api_job`）を削除
- Static Web Apps が自動的に API をビルドするように変更

**変更ファイル:**

- `.github/workflows/azure-static-web-apps-deploy.yml`

**変更前:**

```yaml
skip_api_build: true
api_location: 'frontend/receiptfly-web/api' # 事前ビルド済み
```

**変更後:**

```yaml
skip_api_build: false
api_location: 'backend/Receiptfly.Functions' # ソースコード
```

### 2. ビルドサイズの最適化

#### 2.1 不要なパッケージの削除

**削除したパッケージ:**

- `PDFtoImage` - 廃止予定の関数でのみ使用されており、実際の PDF 処理は Processing Function で実行
- `SkiaSharp` - PDFtoImage と一緒に使用されていた
- `Npgsql.EntityFrameworkCore.PostgreSQL` - 使用していない

**変更ファイル:**

- `backend/Receiptfly.Functions/Receiptfly.Functions.csproj`
- `backend/Receiptfly.Functions/OcrFunctions.cs`

#### 2.2 ビルド設定の最適化

**追加した最適化設定:**

- `DebugType: None` - デバッグ情報を生成しない
- `DebugSymbols: false` - デバッグシンボルを生成しない
- `IncludeSymbols: false` - シンボルを含めない
- `IncludeSource: false` - ソースコードを含めない
- `DebuggerSupport: false` - デバッガーサポートを無効化
- `EnableSGen: false` - シリアル化ジェネレーターを無効化

**注意:**

- `PublishTrimmed: true`は`SelfContained: true`が必要なため、framework-dependent デプロイでは使用できない
- Static Web Apps の Managed Functions では framework-dependent（`SelfContained: false`）が推奨

**変更ファイル:**

- `backend/Receiptfly.Functions/Receiptfly.Functions.csproj`
- `backend/Receiptfly.Application/Receiptfly.Application.csproj`
- `backend/Receiptfly.Infrastructure/Receiptfly.Infrastructure.csproj`

#### 2.3 .funcignore ファイルの拡張

**追加した除外パターン:**

- `*.pdb`, `*.dbg` - デバッグシンボル
- `*.xml` - XML ドキュメント
- `*.Tests.dll`, `*.Test.dll` - テストファイル
- `*.cs`（`Program.cs`以外） - ソースファイル
- `*.tmp`, `*.temp`, `*~` - 一時ファイル
- `*.bak`, `*.backup` - バックアップファイル

**変更ファイル:**

- `backend/Receiptfly.Functions/.funcignore`

## 最終的な設定

### GitHub Actions ワークフロー

```yaml
jobs:
  build_and_deploy_job:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '20.x'
      - name: Install Frontend Dependencies
        working-directory: ./frontend/receiptfly-web
        run: npm ci
      - name: Build Frontend
        working-directory: ./frontend/receiptfly-web
        env:
          VITE_API_BASE_URL: '/api'
        run: npm run build
      - name: Build And Deploy
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          action: 'upload'
          app_location: 'frontend/receiptfly-web'
          api_location: 'backend/Receiptfly.Functions'
          output_location: 'dist'
          skip_api_build: false # Managed functionsとしてビルド
```

### .csproj ファイルの最適化設定

```xml
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>
  <AzureFunctionsVersion>V4</AzureFunctionsVersion>
  <OutputType>Exe</OutputType>
  <!-- ビルドサイズ最適化 -->
  <PublishSingleFile>false</PublishSingleFile>
  <SelfContained>false</SelfContained>
  <DebugType>None</DebugType>
  <DebugSymbols>false</DebugSymbols>
  <IncludeSymbols>false</IncludeSymbols>
  <IncludeSource>false</IncludeSource>
  <IncludeNativeLibrariesForSelfExtract>false</IncludeNativeLibrariesForSelfExtract>
  <EnableCompressionInSingleFile>false</EnableCompressionInSingleFile>
  <!-- PublishTrimmedはSelfContained: trueが必要だが、framework-dependentが推奨のため無効化 -->
  <PublishReadyToRun>false</PublishReadyToRun>
  <DebuggerSupport>false</DebuggerSupport>
  <EnableSGen>false</EnableSGen>
</PropertyGroup>
```

### staticwebapp.config.json

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
  ]
}
```

## 学んだ教訓

### 1. Free プランと Standard プランの違い

- **Free プラン**: 「Managed functions」のみサポート（100MB 制限）
- **Standard プラン**: 「Managed functions」と「Bring your own API」の両方をサポート
- API functions のサイズ制限はプランに関係なく 100MB

### 2. skip_api_build の使い分け

- `skip_api_build: false`（Managed functions）: Free プランで使用、Static Web Apps が自動ビルド
- `skip_api_build: true`（Bring your own API）: Standard プランでのみ使用可能、事前ビルドが必要

### 3. ビルドサイズの最適化

- 不要なパッケージの削除が最も効果的
- `PublishTrimmed`は framework-dependent デプロイでは使用できない
- デバッグシンボルやメタデータの削減も有効

### 4. 100MB 制限について

- 100MB 制限は「API functions」のサイズ制限
- アプリ全体のサイズ制限とは別物
- Standard プランにアップグレードしても解決しない

## 参考資料

- [Azure Static Web Apps Hosting Plans](https://learn.microsoft.com/en-us/azure/static-web-apps/plans)
- [Build configuration for Azure Static Web Apps](https://learn.microsoft.com/en-us/azure/static-web-apps/build-configuration)
- [Microsoft Q&A - Azure Static Web Apps Limit 104857600 bytes](https://learn.microsoft.com/en-us/answers/questions/1631839/azure-static-web-apps-limit-104857600-bytes-why)
- [GitHub Issue #795 - Help! Deploying pre-built API](https://github.com/Azure/static-web-apps/issues/795)

## 詳細なコミット履歴と対応内容

### フェーズ 1: GitHub Actions ワークフローの初期設定

#### コミット: `8962c94` - `:rocket: Add GitHub Actions workflow for Static Web App deployment with .NET 8 Isolated API`

**日時:** 2025-11-24 17:05:59

**変更内容:**

- GitHub Actions ワークフローファイル（`.github/workflows/azure-static-web-apps-deploy.yml`）を作成
- `.NET 8 Isolated API`のデプロイ設定を追加
- デプロイトラブルシューティングドキュメントを作成

**変更ファイル:**

- `.github/workflows/azure-static-web-apps-deploy.yml` (+98 行)
- `docs/deployment-troubleshooting-static-web-app-api-integration.md` (+349 行)
- `docs/github-actions-setup-checklist.md` (+128 行)
- `docs/github-actions-setup-guide.md` (+115 行)

**GitHub Actions 実行結果:**

- **結論:** `failure`
- **実行 ID:** [19627385296](https://github.com/xiaotiantakumi/Receiptfly/actions/runs/19627385296)
- **エラー内容:**
  - TypeScript ビルドエラー: `error TS6133: 'Wallet' is declared but its value is never read.` (`src/pages/Analytics/Analytics.tsx`)
  - フロントエンドのビルドが失敗し、デプロイが中断された

### フェーズ 2: 100MB 制限への対応（事前ビルドアプローチ）

#### コミット: `08bbe09` - `:zap: Optimize API build size to meet Static Web App 100MB limit`

**日時:** 2025-11-24 17:11:58

**変更内容:**

- `dotnet publish`コマンドにサイズ削減フラグを追加
  - `/p:PublishSingleFile=false` - シングルファイル公開を無効化
  - `/p:SelfContained=false` - 自己完結型デプロイを無効化（framework-dependent）

**変更ファイル:**

- `.github/workflows/azure-static-web-apps-deploy.yml` (+8 行, -1 行)

**GitHub Actions 実行結果:**

- **結論:** `failure`
- **実行 ID:** [19627524278](https://github.com/xiaotiantakumi/Receiptfly/actions/runs/19627524278)
- **エラー内容:**
  - 100MB 制限エラー: `The content server has rejected the request with: BadRequest`
  - `Reason: The size of the function content was too large. The limit for this Static Web App is 104857600 bytes.`
  - 事前ビルドした API のサイズが 100MB を超えていた
  - `skip_api_build: true`を使用していたが、API サイズが制限を超えていた

#### コミット: `8146046` - `:zap: Add file cleanup step to reduce API build size`

**日時:** 2025-11-24 17:18:59

**変更内容:**

- ビルド後のファイルクリーンアップステップを追加
  - `.pdb`ファイル（デバッグシンボル）を削除
  - `.xml`ファイル（XML ドキュメント）を削除
  - 不要な`.json`ファイルを削除
  - `runtimes`ディレクトリを削除
  - サイズ確認ステップを追加

**変更ファイル:**

- `.github/workflows/azure-static-web-apps-deploy.yml` (+14 行, -1 行)

**GitHub Actions 実行結果:**

- **結論:** `success`
- **実行 ID:** [19627686541](https://github.com/xiaotiantakumi/Receiptfly/actions/runs/19627686541)
- **結果:** デプロイは成功したが、Azure Portal で API が検知されない問題が発生（`skip_api_build: true`のため「Bring your own API」として扱われ、Free プランではサポートされていない）

#### コミット: `75e21a9` - `:bug: Fix API deployment by letting Static Web Apps build the API`

**日時:** 2025-11-24 17:29:36

**変更内容:**

- `skip_api_build: false`に変更して Static Web Apps に API をビルドさせる
- `api_location`を`backend/Receiptfly.Functions`（ソースコードディレクトリ）に変更
- 事前ビルドステップを削除（Static Web Apps が自動ビルド）

**変更ファイル:**

- `.github/workflows/azure-static-web-apps-deploy.yml` (+7 行, -39 行)

**GitHub Actions 実行結果:**

- **結論:** `failure`
- **実行 ID:** [19627933092](https://github.com/xiaotiantakumi/Receiptfly/actions/runs/19627933092)
- **エラー内容:**
  - 100MB 制限エラー: `The content server has rejected the request with: BadRequest`
  - `Reason: The size of the function content was too large. The limit for this Static Web App is 104857600 bytes.`
  - `skip_api_build: false`に変更して Static Web Apps に API をビルドさせたが、ビルド後のサイズが 100MB を超えていた
  - Oryx ビルドログ: `Function Runtime Information. OS: windows, Functions Runtime: ~4, dotnetisolated version: 8.0`
  - `Found functions.metadata file`は確認されたが、サイズ制限でデプロイが拒否された

#### コミット: `18d0f7a` - `:bug: Revert to pre-build approach and ensure functions.metadata is preserved`

**日時:** 2025-11-24 17:35:01

**変更内容:**

- 事前ビルドアプローチに戻す
- `functions.metadata`ファイルが削除されないようにクリーンアップステップを修正
- `skip_api_build: true`を維持

**変更ファイル:**

- `.github/workflows/azure-static-web-apps-deploy.yml` (+39 行, -5 行)

**GitHub Actions 実行結果:**

- **結論:** `success`
- **実行 ID:** [19628056876](https://github.com/xiaotiantakumi/Receiptfly/actions/runs/19628056876)
- **結果:** デプロイは成功したが、Azure Portal で API が検知されない問題が発生（`skip_api_build: true`のため「Bring your own API」として扱われ、Free プランではサポートされていない）

#### コミット: `433b9a9` - `:bug: Fix API deployment for .NET Isolated by letting Static Web Apps build on Windows`

**日時:** 2025-11-24 17:45:09

**変更内容:**

- 再度`skip_api_build: false`に変更
- `.NET Isolated Functions`は Windows 上で実行されるため、Linux でビルドした API が正しく動作しない可能性を考慮
- Static Web Apps が Windows 環境でビルドするように変更

**変更ファイル:**

- `.github/workflows/azure-static-web-apps-deploy.yml` (+5 行, -39 行)

**GitHub Actions 実行結果:**

- **結論:** `failure`
- **実行 ID:** [19628302283](https://github.com/xiaotiantakumi/Receiptfly/actions/runs/19628302283)
- **エラー内容:**
  - 100MB 制限エラー: `The content server has rejected the request with: BadRequest`
  - `Reason: The size of the function content was too large. The limit for this Static Web App is 104857600 bytes.`
  - `.csproj`に最適化設定を追加したが、まだサイズが 100MB を超えていた
  - Oryx ビルドログ: `Function Runtime Information. OS: windows, Functions Runtime: ~4, dotnetisolated version: 8.0`
  - `Found functions.metadata file`は確認されたが、サイズ制限でデプロイが拒否された

#### コミット: `0dcb210` - `:zap: Add build optimization properties to reduce API size for Static Web Apps`

**日時:** 2025-11-24 17:48:31

**変更内容:**

- `.csproj`ファイルにビルド最適化プロパティを追加
  - `PublishSingleFile: false`
  - `SelfContained: false`
  - デバッグシンボルとソースファイルを無効化

**変更ファイル:**

- `backend/Receiptfly.Functions/Receiptfly.Functions.csproj` (+9 行)

**GitHub Actions 実行結果:**

- **結論:** `failure`
- **実行 ID:** [19628384403](https://github.com/xiaotiantakumi/Receiptfly/actions/runs/19628384403)
- **エラー内容:**
  - 100MB 制限エラー: `The content server has rejected the request with: BadRequest`
  - `Reason: The size of the function content was too large. The limit for this Static Web App is 104857600 bytes.`
  - `.csproj`の最適化設定だけでは不十分で、まだサイズが 100MB を超えていた
  - Oryx ビルドログ: `Function Runtime Information. OS: windows, Functions Runtime: ~4, dotnetisolated version: 8.0`
  - `Found functions.metadata file`は確認されたが、サイズ制限でデプロイが拒否された

#### コミット: `f0caac9` - `:zap: Add .funcignore to exclude bin/obj folders from API deployment`

**日時:** 2025-11-24 17:52:38

**変更内容:**

- `.funcignore`ファイルを作成
- `bin/`と`obj/`フォルダを除外してデプロイサイズを削減

**変更ファイル:**

- `backend/Receiptfly.Functions/.funcignore` (+35 行)

**GitHub Actions 実行結果:**

- **結論:** `failure`
- **実行 ID:** [19628491990](https://github.com/xiaotiantakumi/Receiptfly/actions/runs/19628491990)
- **エラー内容:**
  - 100MB 制限エラー: `The content server has rejected the request with: BadRequest`
  - `Reason: The size of the function content was too large. The limit for this Static Web App is 104857600 bytes.`
  - `.funcignore`を追加したが、まだサイズが 100MB を超えていた
  - Oryx ビルドログ: `Function Runtime Information. OS: windows, Functions Runtime: ~4, dotnetisolated version: 8.0`
  - `Found functions.metadata file`は確認されたが、サイズ制限でデプロイが拒否された

#### コミット: `ce43f2b` - `:bug: Fix API deployment by pre-building on Linux and using skip_api_build`

**日時:** 2025-11-24 17:58:56

**変更内容:**

- Linux ランナーで API を事前ビルド（264MB → 最適化後）
- `skip_api_build: true`を使用して Static Web Apps のビルドプロセス 100MB 制限を回避
- 不要なファイル（`.pdb`、`.xml`、`runtimes`）を削除してサイズを削減
- GitHub Issue #1034 を参照

**変更ファイル:**

- `.github/workflows/azure-static-web-apps-deploy.yml` (+38 行, -5 行)

**GitHub Actions 実行結果:**

- **結論:** `success`
- **実行 ID:** [19628661954](https://github.com/xiaotiantakumi/Receiptfly/actions/runs/19628661954)
- **結果:** デプロイは成功したが、Azure Portal で API が検知されない問題が発生（`skip_api_build: true`のため「Bring your own API」として扱われ、Free プランではサポートされていない）

#### コミット: `492a2e8` - `:bug: Preserve worker.config.json and extensions.json in API build`

**日時:** 2025-11-24 18:07:37

**変更内容:**

- `worker.config.json`と`extensions.json`を削除対象から除外
- これらのファイルは`.NET Isolated Functions`が正しく動作するために必要
- 必要なファイルが存在することを確認する検証ステップを追加

**変更ファイル:**

- `.github/workflows/azure-static-web-apps-deploy.yml` (+4 行, -1 行)

**GitHub Actions 実行結果:**

- **結論:** `failure`
- **実行 ID:** [19628894112](https://github.com/xiaotiantakumi/Receiptfly/actions/runs/19628894112)
- **エラー内容:**
  - デプロイ失敗: `Deployment Failed :(`
  - `Deployment Failure Reason: Failed to deploy the Azure Functions.`
  - `runtimes`フォルダを削除したことで、必要なネイティブライブラリが不足していた可能性がある
  - デプロイは開始されたが、Azure Functions のデプロイ段階で失敗した

#### コミット: `520840b` - `:bug: Optimize runtimes folder instead of deleting it`

**日時:** 2025-11-24 18:24:03

**変更内容:**

- `runtimes`フォルダを完全に削除するのではなく、最適化
- `win-x64`、`win-x86`、`win`の runtimes を保持
- その他の runtimes を削除してスペースを節約
- Azure Static Web Apps（Windows）でネイティブ依存関係が動作することを確保しつつ、100MB 制限を満たす

**変更ファイル:**

- `.github/workflows/azure-static-web-apps-deploy.yml` (+11 行, -1 行)

**GitHub Actions 実行結果:**

- **結論:** `failure`
- **実行 ID:** [19629341392](https://github.com/xiaotiantakumi/Receiptfly/actions/runs/19629341392)
- **エラー内容:**
  - デプロイ失敗: `Deployment Failed :(`
  - `Deployment Failure Reason: Failed to deploy the Azure Functions.`
  - `runtimes`フォルダを最適化したが、Windows ランタイムで必要なネイティブライブラリが不足していた可能性がある
  - デプロイは開始されたが、Azure Functions のデプロイ段階で失敗した

### フェーズ 3: Windows ランナーでのビルド試行

#### コミット: `e51d0d8` - `:bug: Build API on Windows runner for Static Web Apps`

**日時:** 2025-11-24 18:36:27

**変更内容:**

- Windows ランナーで API をビルドする別ジョブ（`build_api_job`）を作成
- アーティファクトをアップロード/ダウンロードしてジョブ間で共有
- Windows ネイティブ依存関係（PDFtoImage）が正しく動作することを確保
- Linux でビルドした API が Windows ランタイムで失敗する問題に対処

**変更ファイル:**

- `.github/workflows/azure-static-web-apps-deploy.yml` (+48 行, -22 行)

**GitHub Actions 実行結果:**

- **結論:** `failure`
- **実行 ID:** [19629664087](https://github.com/xiaotiantakumi/Receiptfly/actions/runs/19629664087)
- **エラー内容:**
  - Windows ランナーでのビルドは成功（`Build API Job`は成功）
  - しかし、デプロイ段階で問題が発生した可能性がある
  - ログからは明確なエラーメッセージは確認できなかったが、デプロイが失敗した

#### コミット: `46b828d` - `:bug: Fix API artifacts download path`

**日時:** 2025-11-24 18:44:45

**変更内容:**

- アーティファクトのダウンロードパスを修正
- `frontend/receiptfly-web/` → `frontend/receiptfly-web/api`
- ダウンロード後に`api`フォルダが存在することを確保

**変更ファイル:**

- `.github/workflows/azure-static-web-apps-deploy.yml` (+1 行, -1 行)

**GitHub Actions 実行結果:**

- **結論:** `success`
- **実行 ID:** [19629888601](https://github.com/xiaotiantakumi/Receiptfly/actions/runs/19629888601)
- **結果:** デプロイは成功したが、Azure Portal で API が検知されない問題が発生（`skip_api_build: true`のため「Bring your own API」として扱われ、Free プランではサポートされていない）

### フェーズ 4: Free プラン対応（根本原因の解決）

#### コミット: `d6381ad` - `:bug: Change skip_api_build to false for Free plan compatibility`

**日時:** 2025-11-24 20:21:20

**変更内容:**

- **重要な発見:** Free プランでは「Bring your own API」がサポートされていない
- `skip_api_build: true`は API を「Bring your own API」として扱う
- `skip_api_build: false`に変更して Managed functions を使用
- 事前ビルド API ジョブを削除（Static Web Apps が自動ビルド）
- `api_location`をソースコードディレクトリに更新

**変更ファイル:**

- `.github/workflows/azure-static-web-apps-deploy.yml` (+8 行, -85 行)

**GitHub Actions 実行結果:**

- **結論:** `failure`
- **実行 ID:** [19632527799](https://github.com/xiaotiantakumi/Receiptfly/actions/runs/19632527799)
- **エラー内容:**
  - 100MB 制限エラー: `The content server has rejected the request with: BadRequest`
  - `Reason: The size of the function content was too large. The limit for this Static Web App is 104857600 bytes.`
  - `skip_api_build: false`に変更して Managed functions としてビルドしたが、ビルド後のサイズが 100MB を超えていた
  - Oryx ビルドログ: `Function Runtime Information. OS: windows, Functions Runtime: ~4, dotnetisolated version: 8.0`
  - `Found functions.metadata file`は確認されたが、サイズ制限でデプロイが拒否された
  - **重要な発見:** Free プランでは「Bring your own API」がサポートされていないため、`skip_api_build: false`（Managed functions）を使用する必要がある

### フェーズ 5: ビルドサイズの最適化（最終対応）

#### コミット: `9048783` - `:zap: Optimize build size for Static Web Apps 100MB limit`

**日時:** 2025-11-24 20:31:45

**変更内容:**

- `PublishTrimmed: true`と`TrimMode: partial`を追加して未使用コードを削除
- 未使用の PostgreSQL パッケージ（`Npgsql.EntityFrameworkCore.PostgreSQL`）を削除
- デバッガーとメタデータ削減設定を追加
- `.funcignore`を拡張して不要なファイルを除外
- Application と Infrastructure プロジェクトにも最適化設定を追加

**変更ファイル:**

- `backend/Receiptfly.Functions/Receiptfly.Functions.csproj` (+11 行, -1 行)
- `backend/Receiptfly.Application/Receiptfly.Application.csproj` (+5 行)
- `backend/Receiptfly.Infrastructure/Receiptfly.Infrastructure.csproj` (+5 行)
- `backend/Receiptfly.Functions/.funcignore` (+30 行)

**GitHub Actions 実行結果:**

- **結論:** `failure`
- **実行 ID:** [19632795220](https://github.com/xiaotiantakumi/Receiptfly/actions/runs/19632795220)
- **エラー内容:**
  - `error NETSDK1102: Optimizing assemblies for size is not supported for the selected publish configuration. Please ensure that you are publishing a self-contained app.`
  - `PublishTrimmed: true`を設定していたが、`SelfContained: false`（framework-dependent）になっていた
  - `PublishTrimmed`を使用するには`SelfContained: true`が必要
  - Static Web Apps の Managed Functions では framework-dependent（`SelfContained: false`）が推奨のため、`PublishTrimmed`は使用できない
  - Oryx ビルドログ: `Oryx has failed to build the solution.`

#### コミット: `4bf9aea` - `:zap: Remove unused PDFtoImage and SkiaSharp packages from API Functions`

**日時:** 2025-11-24 20:32:25

**変更内容:**

- `PDFtoImage`と`SkiaSharp`パッケージを削除
- これらは廃止予定の関数でのみ使用されており、実際の PDF 処理は Processing Function で実行
- これらのパッケージを削除することでビルドサイズを大幅に削減
- Microsoft Q&A を参照：100MB 制限は API functions のサイズ制限であり、アプリ全体のサイズ制限とは別物

**変更ファイル:**

- `backend/Receiptfly.Functions/Receiptfly.Functions.csproj` (+3 行, -1 行)
- `backend/Receiptfly.Functions/OcrFunctions.cs` (+2 行, -2 行)

**GitHub Actions 実行結果:**

- **結論:** `failure`
- **実行 ID:** [19632813065](https://github.com/xiaotiantakumi/Receiptfly/actions/runs/19632813065)
- **エラー内容:**
  - `error NETSDK1102: Optimizing assemblies for size is not supported for the selected publish configuration. Please ensure that you are publishing a self-contained app.`
  - `PublishTrimmed: true`がまだ有効になっていた
  - `PDFtoImage`と`SkiaSharp`パッケージを削除したが、`PublishTrimmed`エラーが先に発生した
  - Oryx ビルドログ: `Oryx has failed to build the solution.`

#### コミット: `7dee2dc` - `:bug: Disable PublishTrimmed for framework-dependent deployment`

**日時:** 2025-11-24 20:37:17

**変更内容:**

- `PublishTrimmed`を無効化（コメントアウト）
- `PublishTrimmed`を使用するには`SelfContained: true`が必要
- Static Web Apps の Managed Functions では framework-dependent（`SelfContained: false`）が推奨
- その他の最適化設定（`DebugType: None`、`DebugSymbols: false`など）は維持

**変更ファイル:**

- `backend/Receiptfly.Functions/Receiptfly.Functions.csproj` (+3 行, -3 行)

**GitHub Actions 実行結果:**

- **結論:** `success`
- **実行 ID:** [19632935302](https://github.com/xiaotiantakumi/Receiptfly/actions/runs/19632935302)
- **結果:** デプロイ成功、API が正常に動作
  - `PublishTrimmed`を無効化したことで、`NETSDK1102`エラーが解消された
  - `skip_api_build: false`（Managed functions）を使用することで、Free プランでも API が検知されるようになった
  - Azure Portal で API が正常に検知され、エンドポイントが動作することを確認

### その他の関連コミット

#### コミット: `e4a58ab` - `:wrench: Add experimental build outputs to gitignore`

**日時:** 2025-11-24 18:39:01

**変更内容:**

- 実験的なビルド出力（`api-test/`、`api/`）を`.gitignore`に追加
- デプロイ実験中に生成された一時ファイルを Git から除外

**変更ファイル:**

- `frontend/receiptfly-web/.gitignore` (+2 行)

## コミット履歴サマリー

時系列順の主要コミット：

1. **17:05:59** - GitHub Actions ワークフローの初期作成
2. **17:11:58** - ビルドサイズ最適化フラグの追加
3. **17:18:59** - ファイルクリーンアップステップの追加
4. **17:29:36** - Static Web Apps に API をビルドさせる試行（失敗）
5. **17:35:01** - 事前ビルドアプローチに戻す
6. **17:45:09** - Windows ビルドを試行（失敗）
7. **17:48:31** - `.csproj`にビルド最適化プロパティを追加
8. **17:52:38** - `.funcignore`ファイルを作成
9. **17:58:56** - Linux での事前ビルドアプローチに戻す
10. **18:07:37** - 必要なファイル（`worker.config.json`、`extensions.json`）を保護
11. **18:24:03** - `runtimes`フォルダの最適化
12. **18:36:27** - Windows ランナーでのビルド試行
13. **18:44:45** - アーティファクトダウンロードパスの修正
14. **20:21:20** - **Free プラン対応：`skip_api_build: false`に変更（根本原因の解決）**
15. **20:31:45** - ビルドサイズの包括的最適化
16. **20:32:25** - 不要パッケージ（PDFtoImage、SkiaSharp）の削除
17. **20:37:17** - `PublishTrimmed`の無効化（最終修正）

## 結論

Azure Static Web Apps の Free プランで.NET 8 Isolated Functions をバックエンド API として統合するには、以下の対応が必要でした：

1. `skip_api_build: false`にして Managed functions としてビルド
2. 不要なパッケージ（PDFtoImage、SkiaSharp、PostgreSQL）を削除
3. ビルド設定を最適化（デバッグシンボルやメタデータの削減）
4. `PublishTrimmed`は使用できない（framework-dependent デプロイのため）

これらの対応により、API functions のサイズを 100MB 以下に抑え、Free プランでも正常にデプロイできるようになりました。
