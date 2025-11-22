# Receiptfly

レシート管理アプリケーション

## 必要な環境

- .NET 8.0 SDK
- Node.js 18 以上
- npm または yarn

## 起動方法

### バックエンドの起動

```bash
cd backend
dotnet restore Receiptfly.sln
dotnet run --project Receiptfly.Api/Receiptfly.Api.csproj
```

バックエンドは `http://localhost:5159` で起動します。

#### ポートが使用中の場合の対処法

`address already in use` エラーが発生した場合、既に起動しているプロセスを停止します：

```bash
# ポート5159を使用しているプロセスを確認
lsof -i :5159

# プロセスIDを確認して停止（PIDは上記コマンドで確認した値に置き換える）
kill -9 <PID>
```

### フロントエンドの起動

```bash
cd frontend/receiptfly-web
npm install
npm run dev
```

フロントエンドは `http://localhost:5173` で起動します。
