# Google Cloud Vision API 認証情報の設定方法

Google Cloud Vision API の認証情報を設定する方法は複数あります。環境変数を使用する方法（方法 1）を推奨しますが、以下の方法も使用できます。

## 認証方法の優先順位

実装では以下の優先順位で認証情報を読み込みます：

1. `appsettings.Development.json` の `GoogleCloud:ApiKey` または `GoogleCloud:CredentialsPath`
2. 環境変数 `GOOGLE_CLOUD_API_KEY` または `GOOGLE_APPLICATION_CREDENTIALS`
3. API キーがファイルパスの場合、そのファイルから読み込む

## 方法 1: 環境変数で API キーを設定（推奨）

```bash
export GOOGLE_CLOUD_API_KEY="your-api-key-here"
```

**API キーの取得方法:**

1. [Google Cloud Console](https://console.cloud.google.com/) にアクセス
2. プロジェクトを作成または選択
3. 「API とサービス」→「認証情報」→「認証情報を作成」→「API キー」
4. 作成された API キーをコピー

**注意**: API キーは Git にコミットしないでください。`.gitignore` に追加済みです。

## 方法 2: API キーを appsettings.Development.json で設定

1. `backend/Receiptfly.Api/appsettings.Development.json.example` をコピーして `appsettings.Development.json` を作成
2. `GoogleCloud:ApiKey` に API キーを設定

```json
{
  "GoogleCloud": {
    "ApiKey": "hogehoge"
  }
}
```

## 方法 3: API キーを key.json ファイルに直接記述

この方法では、API キーをファイルに保存し、そのファイルパスを設定に指定します。実装では、`GoogleCloud:ApiKey` にファイルパスが指定されている場合、自動的にそのファイルから API キーを読み込みます。

1. `backend/key.json` ファイルを作成（既に存在する場合はそのまま使用）
2. API キーを直接記述

```json
hogehoge
```

3. `appsettings.Development.json` でファイルパスを指定

```json
{
  "GoogleCloud": {
    "ApiKey": "backend/key.json"
  }
}
```

**注意**: `key.json` ファイルは `.gitignore` に追加済みです。Git にコミットされません。

## 方法 4: サービスアカウントキー（JSON ファイル）を環境変数で設定

この方法では、Google Cloud の標準的な認証方法であるサービスアカウントキーを使用します。実装では、`GOOGLE_APPLICATION_CREDENTIALS` 環境変数が設定されている場合、そのパスのサービスアカウントキーファイルを使用して `ImageAnnotatorClient` を作成します。

```bash
export GOOGLE_APPLICATION_CREDENTIALS="/path/to/your-service-account-key.json"
```

## 方法 5: サービスアカウントキー（JSON ファイル）を appsettings.Development.json で設定

この方法では、`appsettings.Development.json` にサービスアカウントキーファイルのパスを指定します。実装では、このパスが指定されている場合、`GOOGLE_APPLICATION_CREDENTIALS` 環境変数に設定してから `ImageAnnotatorClient` を作成します。

1. `backend/Receiptfly.Api/appsettings.Development.json.example` をコピーして `appsettings.Development.json` を作成
2. `GoogleCloud:CredentialsPath` に認証情報ファイルのパスを設定

```json
{
  "GoogleCloud": {
    "CredentialsPath": "/path/to/your-service-account-key.json"
  }
}
```

**サービスアカウントキーの取得方法:**

1. [Google Cloud Console](https://console.cloud.google.com/) にアクセス
2. プロジェクトを作成または選択
3. 「API とサービス」→「認証情報」からサービスアカウントキーを作成
4. JSON 形式のキーファイルをダウンロード

## テスト用のモック OCR を使用する場合

開発時に実際の API を呼び出さずにテストする場合は、`appsettings.Development.json` に以下を追加：

```json
{
  "UseMockOcr": true
}
```

この設定により、`MockGoogleVisionOcrService` が使用され、実際の Google Cloud Vision API を呼び出すことなくテストできます。

## 実装の詳細

### API キー方式とサービスアカウントキー方式の違い

- **API キー方式**: REST API を直接呼び出します（`ExtractTextWithApiKeyAsync` メソッド）
  - シンプルで設定が容易
  - レート制限やクォータの管理が必要
- **サービスアカウントキー方式**: Google Cloud Vision API のクライアントライブラリを使用します（`ImageAnnotatorClient`）
  - より高度な機能や設定が可能
  - IAM による細かい権限管理が可能

### 認証情報の読み込み処理

`Program.cs` では以下の順序で認証情報を読み込みます：

1. `appsettings.Development.json` から `GoogleCloud:ApiKey` または `GoogleCloud:CredentialsPath` を読み込み
2. 環境変数 `GOOGLE_CLOUD_API_KEY` または `GOOGLE_APPLICATION_CREDENTIALS` を確認
3. API キーがファイルパス（`File.Exists(apiKey)` が true）の場合、そのファイルから API キーを読み込み
4. サービスアカウントキーのパスが指定されている場合、`GOOGLE_APPLICATION_CREDENTIALS` 環境変数に設定

### エラーハンドリング

- API キーが無効な場合、Google Cloud Vision API からエラーレスポンスが返されます
- サービスアカウントキーが無効な場合、`ImageAnnotatorClient.Create()` で例外が発生します
- 認証情報が設定されていない場合、OCR 処理時にエラーが発生します
