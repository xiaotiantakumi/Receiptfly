# Azure Static Web Apps API統合デプロイ対応レポート

## 概要

Azure Static Web AppsのFreeプランで、.NET 8 Isolated FunctionsをバックエンドAPIとして統合する際に発生した問題とその解決策をまとめたレポートです。

## 発生した問題

### 1. Azure PortalでAPIが検知されない

**問題の詳細:**
- GitHub Actionsでのデプロイは成功していたが、Azure Portalの「APIs」セクションでAPIが検知されない
- 「Backend Type: -」「Backend Resource Name: -」と表示される
- APIエンドポイント（`/api/receipts`）が404を返す

**原因:**
- `skip_api_build: true`を使用していたため、APIが「Bring your own API」として扱われていた
- Azure Static Web AppsのFreeプランでは「Bring your own API」がサポートされていない
- Freeプランでは「Managed functions」のみが利用可能

**参考資料:**
- [Microsoft Q&A - Azure Static Web Apps Limit 104857600 bytes](https://learn.microsoft.com/en-us/answers/questions/1631839/azure-static-web-apps-limit-104857600-bytes-why)
- [Azure Static Web Apps Hosting Plans](https://learn.microsoft.com/en-us/azure/static-web-apps/plans)

### 2. 100MB制限エラー

**問題の詳細:**
```
The content server has rejected the request with: BadRequest
Reason: The size of the function content was too large. The limit for this Static Web App is 104857600 bytes.
```

**原因:**
- API functionsのサイズが100MBを超えていた
- 100MB制限は「API functions」のサイズ制限であり、アプリ全体のサイズ制限（Free: 250MB、Standard: 500MB）とは別物
- Standardプランにアップグレードしても解決しない（API functionsのサイズ制限はプランに関係なく100MB）

**参考資料:**
- [Microsoft Q&A - Azure Static Web Apps Limit 104857600 bytes](https://learn.microsoft.com/en-us/answers/questions/1631839/azure-static-web-apps-limit-104857600-bytes-why)

### 3. PublishTrimmedエラー

**問題の詳細:**
```
error NETSDK1102: Optimizing assemblies for size is not supported for the selected publish configuration. 
Please ensure that you are publishing a self-contained app.
```

**原因:**
- `PublishTrimmed: true`を設定していたが、`SelfContained: false`になっていた
- `PublishTrimmed`を使用するには`SelfContained: true`が必要
- Static Web AppsのManaged Functionsではframework-dependent（`SelfContained: false`）が推奨

## 実施した解決策

### 1. Freeプラン対応：Managed Functionsへの変更

**変更内容:**
- `skip_api_build: false`に変更
- `api_location`をソースコードディレクトリ（`backend/Receiptfly.Functions`）に変更
- 事前ビルドジョブ（`build_api_job`）を削除
- Static Web Appsが自動的にAPIをビルドするように変更

**変更ファイル:**
- `.github/workflows/azure-static-web-apps-deploy.yml`

**変更前:**
```yaml
skip_api_build: true
api_location: "frontend/receiptfly-web/api"  # 事前ビルド済み
```

**変更後:**
```yaml
skip_api_build: false
api_location: "backend/Receiptfly.Functions"  # ソースコード
```

### 2. ビルドサイズの最適化

#### 2.1 不要なパッケージの削除

**削除したパッケージ:**
- `PDFtoImage` - 廃止予定の関数でのみ使用されており、実際のPDF処理はProcessing Functionで実行
- `SkiaSharp` - PDFtoImageと一緒に使用されていた
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
- `PublishTrimmed: true`は`SelfContained: true`が必要なため、framework-dependentデプロイでは使用できない
- Static Web AppsのManaged Functionsではframework-dependent（`SelfContained: false`）が推奨

**変更ファイル:**
- `backend/Receiptfly.Functions/Receiptfly.Functions.csproj`
- `backend/Receiptfly.Application/Receiptfly.Application.csproj`
- `backend/Receiptfly.Infrastructure/Receiptfly.Infrastructure.csproj`

#### 2.3 .funcignoreファイルの拡張

**追加した除外パターン:**
- `*.pdb`, `*.dbg` - デバッグシンボル
- `*.xml` - XMLドキュメント
- `*.Tests.dll`, `*.Test.dll` - テストファイル
- `*.cs`（`Program.cs`以外） - ソースファイル
- `*.tmp`, `*.temp`, `*~` - 一時ファイル
- `*.bak`, `*.backup` - バックアップファイル

**変更ファイル:**
- `backend/Receiptfly.Functions/.funcignore`

## 最終的な設定

### GitHub Actionsワークフロー

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
          VITE_API_BASE_URL: "/api"
        run: npm run build
      - name: Build And Deploy
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          action: "upload"
          app_location: "frontend/receiptfly-web"
          api_location: "backend/Receiptfly.Functions"
          output_location: "dist"
          skip_api_build: false  # Managed functionsとしてビルド
```

### .csprojファイルの最適化設定

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

### 1. FreeプランとStandardプランの違い

- **Freeプラン**: 「Managed functions」のみサポート（100MB制限）
- **Standardプラン**: 「Managed functions」と「Bring your own API」の両方をサポート
- API functionsのサイズ制限はプランに関係なく100MB

### 2. skip_api_buildの使い分け

- `skip_api_build: false`（Managed functions）: Freeプランで使用、Static Web Appsが自動ビルド
- `skip_api_build: true`（Bring your own API）: Standardプランでのみ使用可能、事前ビルドが必要

### 3. ビルドサイズの最適化

- 不要なパッケージの削除が最も効果的
- `PublishTrimmed`はframework-dependentデプロイでは使用できない
- デバッグシンボルやメタデータの削減も有効

### 4. 100MB制限について

- 100MB制限は「API functions」のサイズ制限
- アプリ全体のサイズ制限とは別物
- Standardプランにアップグレードしても解決しない

## 参考資料

- [Azure Static Web Apps Hosting Plans](https://learn.microsoft.com/en-us/azure/static-web-apps/plans)
- [Build configuration for Azure Static Web Apps](https://learn.microsoft.com/en-us/azure/static-web-apps/build-configuration)
- [Microsoft Q&A - Azure Static Web Apps Limit 104857600 bytes](https://learn.microsoft.com/en-us/answers/questions/1631839/azure-static-web-apps-limit-104857600-bytes-why)
- [GitHub Issue #795 - Help! Deploying pre-built API](https://github.com/Azure/static-web-apps/issues/795)

## 詳細なコミット履歴と対応内容

### フェーズ1: GitHub Actionsワークフローの初期設定

#### コミット: `8962c94` - `:rocket: Add GitHub Actions workflow for Static Web App deployment with .NET 8 Isolated API`
**日時:** 2025-11-24 17:05:59

**変更内容:**
- GitHub Actionsワークフローファイル（`.github/workflows/azure-static-web-apps-deploy.yml`）を作成
- `.NET 8 Isolated API`のデプロイ設定を追加
- デプロイトラブルシューティングドキュメントを作成

**変更ファイル:**
- `.github/workflows/azure-static-web-apps-deploy.yml` (+98行)
- `docs/deployment-troubleshooting-static-web-app-api-integration.md` (+349行)
- `docs/github-actions-setup-checklist.md` (+128行)
- `docs/github-actions-setup-guide.md` (+115行)

### フェーズ2: 100MB制限への対応（事前ビルドアプローチ）

#### コミット: `08bbe09` - `:zap: Optimize API build size to meet Static Web App 100MB limit`
**日時:** 2025-11-24 17:11:58

**変更内容:**
- `dotnet publish`コマンドにサイズ削減フラグを追加
  - `/p:PublishSingleFile=false` - シングルファイル公開を無効化
  - `/p:SelfContained=false` - 自己完結型デプロイを無効化（framework-dependent）

**変更ファイル:**
- `.github/workflows/azure-static-web-apps-deploy.yml` (+8行, -1行)

#### コミット: `8146046` - `:zap: Add file cleanup step to reduce API build size`
**日時:** 2025-11-24 17:18:59

**変更内容:**
- ビルド後のファイルクリーンアップステップを追加
  - `.pdb`ファイル（デバッグシンボル）を削除
  - `.xml`ファイル（XMLドキュメント）を削除
  - 不要な`.json`ファイルを削除
  - `runtimes`ディレクトリを削除
  - サイズ確認ステップを追加

**変更ファイル:**
- `.github/workflows/azure-static-web-apps-deploy.yml` (+14行, -1行)

#### コミット: `75e21a9` - `:bug: Fix API deployment by letting Static Web Apps build the API`
**日時:** 2025-11-24 17:29:36

**変更内容:**
- `skip_api_build: false`に変更してStatic Web AppsにAPIをビルドさせる
- `api_location`を`backend/Receiptfly.Functions`（ソースコードディレクトリ）に変更
- 事前ビルドステップを削除（Static Web Appsが自動ビルド）

**変更ファイル:**
- `.github/workflows/azure-static-web-apps-deploy.yml` (+7行, -39行)

**結果:** 404エラーが発生し、APIが検知されない問題が発生

#### コミット: `18d0f7a` - `:bug: Revert to pre-build approach and ensure functions.metadata is preserved`
**日時:** 2025-11-24 17:35:01

**変更内容:**
- 事前ビルドアプローチに戻す
- `functions.metadata`ファイルが削除されないようにクリーンアップステップを修正
- `skip_api_build: true`を維持

**変更ファイル:**
- `.github/workflows/azure-static-web-apps-deploy.yml` (+39行, -5行)

#### コミット: `433b9a9` - `:bug: Fix API deployment for .NET Isolated by letting Static Web Apps build on Windows`
**日時:** 2025-11-24 17:45:09

**変更内容:**
- 再度`skip_api_build: false`に変更
- `.NET Isolated Functions`はWindows上で実行されるため、LinuxでビルドしたAPIが正しく動作しない可能性を考慮
- Static Web AppsがWindows環境でビルドするように変更

**変更ファイル:**
- `.github/workflows/azure-static-web-apps-deploy.yml` (+5行, -39行)

**結果:** まだ問題が解決せず

#### コミット: `0dcb210` - `:zap: Add build optimization properties to reduce API size for Static Web Apps`
**日時:** 2025-11-24 17:48:31

**変更内容:**
- `.csproj`ファイルにビルド最適化プロパティを追加
  - `PublishSingleFile: false`
  - `SelfContained: false`
  - デバッグシンボルとソースファイルを無効化

**変更ファイル:**
- `backend/Receiptfly.Functions/Receiptfly.Functions.csproj` (+9行)

#### コミット: `f0caac9` - `:zap: Add .funcignore to exclude bin/obj folders from API deployment`
**日時:** 2025-11-24 17:52:38

**変更内容:**
- `.funcignore`ファイルを作成
- `bin/`と`obj/`フォルダを除外してデプロイサイズを削減

**変更ファイル:**
- `backend/Receiptfly.Functions/.funcignore` (+35行)

#### コミット: `ce43f2b` - `:bug: Fix API deployment by pre-building on Linux and using skip_api_build`
**日時:** 2025-11-24 17:58:56

**変更内容:**
- LinuxランナーでAPIを事前ビルド（264MB → 最適化後）
- `skip_api_build: true`を使用してStatic Web Appsのビルドプロセス100MB制限を回避
- 不要なファイル（`.pdb`、`.xml`、`runtimes`）を削除してサイズを削減
- GitHub Issue #1034を参照

**変更ファイル:**
- `.github/workflows/azure-static-web-apps-deploy.yml` (+38行, -5行)

#### コミット: `492a2e8` - `:bug: Preserve worker.config.json and extensions.json in API build`
**日時:** 2025-11-24 18:07:37

**変更内容:**
- `worker.config.json`と`extensions.json`を削除対象から除外
- これらのファイルは`.NET Isolated Functions`が正しく動作するために必要
- 必要なファイルが存在することを確認する検証ステップを追加

**変更ファイル:**
- `.github/workflows/azure-static-web-apps-deploy.yml` (+4行, -1行)

#### コミット: `520840b` - `:bug: Optimize runtimes folder instead of deleting it`
**日時:** 2025-11-24 18:24:03

**変更内容:**
- `runtimes`フォルダを完全に削除するのではなく、最適化
- `win-x64`、`win-x86`、`win`のruntimesを保持
- その他のruntimesを削除してスペースを節約
- Azure Static Web Apps（Windows）でネイティブ依存関係が動作することを確保しつつ、100MB制限を満たす

**変更ファイル:**
- `.github/workflows/azure-static-web-apps-deploy.yml` (+11行, -1行)

**結果:** デプロイは成功したが、APIが404を返す問題が継続

### フェーズ3: Windowsランナーでのビルド試行

#### コミット: `e51d0d8` - `:bug: Build API on Windows runner for Static Web Apps`
**日時:** 2025-11-24 18:36:27

**変更内容:**
- WindowsランナーでAPIをビルドする別ジョブ（`build_api_job`）を作成
- アーティファクトをアップロード/ダウンロードしてジョブ間で共有
- Windowsネイティブ依存関係（PDFtoImage）が正しく動作することを確保
- LinuxでビルドしたAPIがWindowsランタイムで失敗する問題に対処

**変更ファイル:**
- `.github/workflows/azure-static-web-apps-deploy.yml` (+48行, -22行)

**結果:** デプロイは成功したが、APIが404を返す問題が継続

#### コミット: `46b828d` - `:bug: Fix API artifacts download path`
**日時:** 2025-11-24 18:44:45

**変更内容:**
- アーティファクトのダウンロードパスを修正
- `frontend/receiptfly-web/` → `frontend/receiptfly-web/api`
- ダウンロード後に`api`フォルダが存在することを確保

**変更ファイル:**
- `.github/workflows/azure-static-web-apps-deploy.yml` (+1行, -1行)

**結果:** デプロイは成功したが、APIが404を返す問題が継続

### フェーズ4: Freeプラン対応（根本原因の解決）

#### コミット: `d6381ad` - `:bug: Change skip_api_build to false for Free plan compatibility`
**日時:** 2025-11-24 20:21:20

**変更内容:**
- **重要な発見:** Freeプランでは「Bring your own API」がサポートされていない
- `skip_api_build: true`はAPIを「Bring your own API」として扱う
- `skip_api_build: false`に変更してManaged functionsを使用
- 事前ビルドAPIジョブを削除（Static Web Appsが自動ビルド）
- `api_location`をソースコードディレクトリに更新

**変更ファイル:**
- `.github/workflows/azure-static-web-apps-deploy.yml` (+8行, -85行)

**結果:** デプロイは成功したが、100MB制限エラーが発生

### フェーズ5: ビルドサイズの最適化（最終対応）

#### コミット: `9048783` - `:zap: Optimize build size for Static Web Apps 100MB limit`
**日時:** 2025-11-24 20:31:45

**変更内容:**
- `PublishTrimmed: true`と`TrimMode: partial`を追加して未使用コードを削除
- 未使用のPostgreSQLパッケージ（`Npgsql.EntityFrameworkCore.PostgreSQL`）を削除
- デバッガーとメタデータ削減設定を追加
- `.funcignore`を拡張して不要なファイルを除外
- ApplicationとInfrastructureプロジェクトにも最適化設定を追加

**変更ファイル:**
- `backend/Receiptfly.Functions/Receiptfly.Functions.csproj` (+11行, -1行)
- `backend/Receiptfly.Application/Receiptfly.Application.csproj` (+5行)
- `backend/Receiptfly.Infrastructure/Receiptfly.Infrastructure.csproj` (+5行)
- `backend/Receiptfly.Functions/.funcignore` (+30行)

**結果:** `PublishTrimmed`エラーが発生

#### コミット: `4bf9aea` - `:zap: Remove unused PDFtoImage and SkiaSharp packages from API Functions`
**日時:** 2025-11-24 20:32:25

**変更内容:**
- `PDFtoImage`と`SkiaSharp`パッケージを削除
- これらは廃止予定の関数でのみ使用されており、実際のPDF処理はProcessing Functionで実行
- これらのパッケージを削除することでビルドサイズを大幅に削減
- Microsoft Q&Aを参照：100MB制限はAPI functionsのサイズ制限であり、アプリ全体のサイズ制限とは別物

**変更ファイル:**
- `backend/Receiptfly.Functions/Receiptfly.Functions.csproj` (+3行, -1行)
- `backend/Receiptfly.Functions/OcrFunctions.cs` (+2行, -2行)

#### コミット: `7dee2dc` - `:bug: Disable PublishTrimmed for framework-dependent deployment`
**日時:** 2025-11-24 20:37:17

**変更内容:**
- `PublishTrimmed`を無効化（コメントアウト）
- `PublishTrimmed`を使用するには`SelfContained: true`が必要
- Static Web AppsのManaged Functionsではframework-dependent（`SelfContained: false`）が推奨
- その他の最適化設定（`DebugType: None`、`DebugSymbols: false`など）は維持

**変更ファイル:**
- `backend/Receiptfly.Functions/Receiptfly.Functions.csproj` (+3行, -3行)

**結果:** デプロイ成功、APIが正常に動作

### その他の関連コミット

#### コミット: `e4a58ab` - `:wrench: Add experimental build outputs to gitignore`
**日時:** 2025-11-24 18:39:01

**変更内容:**
- 実験的なビルド出力（`api-test/`、`api/`）を`.gitignore`に追加
- デプロイ実験中に生成された一時ファイルをGitから除外

**変更ファイル:**
- `frontend/receiptfly-web/.gitignore` (+2行)

## コミット履歴サマリー

時系列順の主要コミット：

1. **17:05:59** - GitHub Actionsワークフローの初期作成
2. **17:11:58** - ビルドサイズ最適化フラグの追加
3. **17:18:59** - ファイルクリーンアップステップの追加
4. **17:29:36** - Static Web AppsにAPIをビルドさせる試行（失敗）
5. **17:35:01** - 事前ビルドアプローチに戻す
6. **17:45:09** - Windowsビルドを試行（失敗）
7. **17:48:31** - `.csproj`にビルド最適化プロパティを追加
8. **17:52:38** - `.funcignore`ファイルを作成
9. **17:58:56** - Linuxでの事前ビルドアプローチに戻す
10. **18:07:37** - 必要なファイル（`worker.config.json`、`extensions.json`）を保護
11. **18:24:03** - `runtimes`フォルダの最適化
12. **18:36:27** - Windowsランナーでのビルド試行
13. **18:44:45** - アーティファクトダウンロードパスの修正
14. **20:21:20** - **Freeプラン対応：`skip_api_build: false`に変更（根本原因の解決）**
15. **20:31:45** - ビルドサイズの包括的最適化
16. **20:32:25** - 不要パッケージ（PDFtoImage、SkiaSharp）の削除
17. **20:37:17** - `PublishTrimmed`の無効化（最終修正）

## 結論

Azure Static Web AppsのFreeプランで.NET 8 Isolated FunctionsをバックエンドAPIとして統合するには、以下の対応が必要でした：

1. `skip_api_build: false`にしてManaged functionsとしてビルド
2. 不要なパッケージ（PDFtoImage、SkiaSharp、PostgreSQL）を削除
3. ビルド設定を最適化（デバッグシンボルやメタデータの削減）
4. `PublishTrimmed`は使用できない（framework-dependentデプロイのため）

これらの対応により、API functionsのサイズを100MB以下に抑え、Freeプランでも正常にデプロイできるようになりました。

