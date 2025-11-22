# Receiptfly

レシート管理アプリケーション

## 必要な環境

- .NET 8.0 SDK
- Node.js 18 以上
- npm または yarn
- Google Cloud Vision API の認証情報（OCR 機能を使用する場合）

## セットアップ

### Google Cloud Vision API の認証情報設定

OCR 機能を使用するには、Google Cloud Vision API の認証情報が必要です。環境変数に API キーを設定してください。

```bash
export GOOGLE_CLOUD_API_KEY="hogehoge"
```

**API キーの取得方法:**

1. [Google Cloud Console](https://console.cloud.google.com/) にアクセス
2. プロジェクトを作成または選択
3. 「API とサービス」→「認証情報」→「認証情報を作成」→「API キー」
4. 作成された API キーをコピー

**注意**: API キーは Git にコミットしないでください。`.gitignore` に追加済みです。

その他の設定方法については、[docs/google-auth.md](docs/google-auth.md) を参照してください。

## 起動方法

### バックエンドの起動

```bash
lsof -ti :5159 | xargs kill -9 2>/dev/null || true
cd backend
dotnet restore Receiptfly.sln
dotnet run --project Receiptfly.Api/Receiptfly.Api.csproj
```

バックエンドは `http://localhost:5159` で起動します。

### フロントエンドの起動

```bash
cd frontend/receiptfly-web
npm install
npm run dev
```

フロントエンドは `http://localhost:5173` で起動します。
