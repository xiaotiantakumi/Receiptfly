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

## コミット履歴

1. `:bug: Change skip_api_build to false for Free plan compatibility`
2. `:zap: Optimize build size for Static Web Apps 100MB limit`
3. `:zap: Remove unused PDFtoImage and SkiaSharp packages from API Functions`
4. `:bug: Disable PublishTrimmed for framework-dependent deployment`

## 結論

Azure Static Web AppsのFreeプランで.NET 8 Isolated FunctionsをバックエンドAPIとして統合するには、以下の対応が必要でした：

1. `skip_api_build: false`にしてManaged functionsとしてビルド
2. 不要なパッケージ（PDFtoImage、SkiaSharp、PostgreSQL）を削除
3. ビルド設定を最適化（デバッグシンボルやメタデータの削減）
4. `PublishTrimmed`は使用できない（framework-dependentデプロイのため）

これらの対応により、API functionsのサイズを100MB以下に抑え、Freeプランでも正常にデプロイできるようになりました。

