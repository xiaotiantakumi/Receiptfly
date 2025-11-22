# Google Cloud Vision API 認証情報の設定方法

Google Cloud Vision API の認証情報を設定する方法は複数あります。環境変数を使用する方法（[README](../README.md)参照）を推奨しますが、以下の方法も使用できます。

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

```bash
export GOOGLE_APPLICATION_CREDENTIALS="/path/to/your-service-account-key.json"
```

## 方法 5: サービスアカウントキー（JSON ファイル）を appsettings.Development.json で設定

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
