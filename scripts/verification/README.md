# 動作確認スクリプト

このディレクトリには、Receiptfly プロジェクトの各種機能を検証するためのシェルスクリプトが含まれています。

## スクリプト一覧

### 1. `verify-ocr-flow.sh`

**用途**: OCR 処理からレシート登録までのエンドツーエンドフローを検証

**機能**:

- テストファイルを Blob Storage にアップロード（SAS トークン経由）
- OCR 処理を Queue にキューイング
- レシート登録の完了を待機（ポーリング）
- 登録されたレシートデータの検証

**使用方法**:

```bash
./verify-ocr-flow.sh
```

**前提条件**:

- `Receiptfly.Functions`が起動している（ポート 7071）
- `Receiptfly.ProcessingFunc`が起動している（ポート 7072）
- Azurite が起動している（ポート 10000-10002）
- テストファイル `backend/Receiptfly.Api.Tests/data/20241222_data.pdf` が存在する

**設定可能な変数**:

- `DATA_DIR`: テストファイルのディレクトリ（デフォルト: `backend/Receiptfly.Api.Tests/data`）
- `API_BASE_URL`: API のベース URL（デフォルト: `http://localhost:7071/api`）
- `CONTAINER_NAME`: Blob Storage のコンテナ名（デフォルト: `receipt-images`）
- `TEST_FILE`: テストファイル名（デフォルト: `20241222_data.pdf`）
- `MAX_WAIT_TIME`: 最大待機時間（秒、デフォルト: 120）

---

### 2. `test-new-id-format.sh`

**用途**: 新しい ID 形式（`receipt-{uuid}`, `transaction-{uuid}`）の動作確認

**機能**:

- レシート作成時に新しい ID 形式（`receipt-{uuid}`）が使用されているか確認
- TransactionItem 作成時に新しい ID 形式（`transaction-{uuid}`）が使用されているか確認
- UserId が正しく設定されているか確認
- 作成したレシートの取得と ID 形式の再確認

**使用方法**:

```bash
./test-new-id-format.sh
```

**前提条件**:

- `Receiptfly.Functions`が起動している（ポート 7071）
- Azurite が起動している（ポート 10000-10002）

**注意事項**:

- このスクリプトは既に起動しているサービスに対して API リクエストを送ります
- `func start`のような長時間実行されるコマンドは含まれていません
- サービスが起動していない場合は、エラーメッセージを表示して終了します

---

### 3. `test-sas-upload.sh`

**用途**: SAS トークン取得とファイルアップロードの基本動作を確認

**機能**:

- SAS トークンの取得
- 取得した SAS URL を使用したファイルアップロード

**使用方法**:

```bash
./test-sas-upload.sh
```

**前提条件**:

- `Receiptfly.Functions`が起動している（ポート 7071）
- Azurite が起動している（ポート 10000-10002）
- テストファイル `backend/Receiptfly.Api.Tests/data/20241222_data.pdf` が存在する（存在しない場合はダミーデータを使用）

---

### 4. `test-multiple-upload.sh`

**用途**: 複数の PDF ファイルを一括アップロードして動作を確認

**機能**:

- 複数の PDF ファイルを順次アップロード
- 各ファイルのアップロード結果を記録
- 成功/失敗のサマリーを表示

**使用方法**:

```bash
./test-multiple-upload.sh
```

**前提条件**:

- `Receiptfly.Functions`が起動している（ポート 7071）
- Azurite が起動している（ポート 10000-10002）
- `backend/Receiptfly.Api.Tests/data/` ディレクトリにテスト用 PDF ファイルが存在する

**テスト対象ファイル**:

- 20241216\_ローソン灘北通店.pdf
- 20241216_Wio.pdf
- 20241215\_株式会社.pdf
- 20241213\_株式会社ココカラファイン　灘駅前店.pdf
- 20241211\_播磨屋本店　神戸店.pdf
- 20241211\_ライフコーポレーション春日野道店.pdf
- 20241207\_ガスト神戸ひよどり台店.pdf
- 20241207\_(店名).pdf
- 20241222_data.pdf

---

### 5. `test-sas-relay.sh`

**用途**: SAS トークンリレー（バケツリレー方式）の動作を確認

**機能**:

- `Receiptfly.Functions`経由で SAS トークンを取得
- `Receiptfly.ProcessingFunc`へのリレーが正常に動作することを確認

**使用方法**:

```bash
./test-sas-relay.sh
```

**前提条件**:

- `Receiptfly.Functions`が起動している（ポート 7071）
- `Receiptfly.ProcessingFunc`が起動している（ポート 7072）
- Azurite が起動している（ポート 10000-10002）

---

### 6. `test-sas-relay-detailed.sh`

**用途**: SAS トークンリレーの詳細な動作確認（ログ付き）

**機能**:

- `test-sas-relay.sh`と同様だが、より詳細なログを出力
- 各ステップの実行時間を記録
- エラー時の詳細情報を表示

**使用方法**:

```bash
./test-sas-relay-detailed.sh
```

**前提条件**:

- `Receiptfly.Functions`が起動している（ポート 7071）
- `Receiptfly.ProcessingFunc`が起動している（ポート 7072）
- Azurite が起動している（ポート 10000-10002）

---

## 今後の確認作業の手順

### 1. 事前準備

#### 必要なサービスの起動

```bash
# プロジェクトルートで実行
make start
```

または、個別に起動する場合：

```bash
# Azuriteの起動
docker-compose up -d azurite

# Receiptfly.Functionsの起動（ポート7071）
cd backend/Receiptfly.Functions
func start --port 7071

# Receiptfly.ProcessingFuncの起動（ポート7072）
cd backend/Receiptfly.ProcessingFunc
func start --port 7072

# フロントエンドの起動（Vite + SWA CLI）
cd frontend/receiptfly-web
npm run dev  # 別ターミナル
swa start  # 別ターミナル
```

#### サービスの動作確認

```bash
# APIが応答するか確認
curl http://localhost:7071/api/receipts

# Processing Functionが起動しているか確認
curl http://localhost:7072/api/getSas?containerName=receipt-images&blobName=test.pdf
```

### 2. 確認作業の実行

#### 基本的な動作確認（推奨）

```bash
cd scripts/verification
./verify-ocr-flow.sh
```

このスクリプトは、アップロードからレシート登録までの全フローを検証します。

#### 特定の機能のみ確認する場合

**新しい ID 形式の確認**:

```bash
./test-new-id-format.sh
```

**SAS トークン取得とアップロードのみ確認**:

```bash
./test-sas-upload.sh
```

**複数ファイルのアップロードを確認**:

```bash
./test-multiple-upload.sh
```

**SAS トークンリレーの動作を確認**:

```bash
./test-sas-relay.sh
# または詳細版
./test-sas-relay-detailed.sh
```

### 3. トラブルシューティング

#### エラーが発生した場合

1. **サービスが起動しているか確認**

   ```bash
   # プロセス確認
   ps aux | grep "func start"
   ps aux | grep azurite

   # ポート確認
   lsof -i :7071  # Receiptfly.Functions
   lsof -i :7072  # Receiptfly.ProcessingFunc
   lsof -i :10000 # Azurite Blob
   lsof -i :10001 # Azurite Queue
   ```

2. **ログを確認**

   - Processing Function のログ: `/tmp/processing-func.log` またはコンソール出力
   - Functions のログ: コンソール出力
   - Azurite のログ: `docker logs receiptify-azurite`

3. **設定ファイルを確認**

   - `backend/Receiptfly.Functions/local.settings.json`
   - `backend/Receiptfly.ProcessingFunc/local.settings.json`
   - 特に `AzureWebJobsStorage` と `AzureStorage` の接続文字列

4. **Queue メッセージが処理されているか確認**

   ```bash
   # Processing Functionのログで以下を確認
   tail -f /tmp/processing-func.log | grep "Processing OCR"
   ```

5. **OCR 結果が空の場合**
   - PDF ファイルが正しくアップロードされているか確認
   - Google Vision API キーが正しく設定されているか確認
   - `UseMockOcr` が `false` になっているか確認

### 4. 新しい確認スクリプトを作成する場合

1. **スクリプトのテンプレート**

   ```bash
   #!/bin/bash
   set -e

   echo "=== [スクリプト名] ==="
   echo "[$(date +%H:%M:%S)] Test started"
   echo ""

   # 設定
   API_BASE_URL="http://localhost:7071/api"

   # テスト実行
   # ...

   echo "[$(date +%H:%M:%S)] === Test completed ==="
   ```

2. **実行権限を付与**

   ```bash
   chmod +x scripts/verification/your-script.sh
   ```

3. **README に追加**
   - この README にスクリプトの説明を追加

### 5. 継続的な確認作業

#### 開発中の確認フロー

1. **コード変更後**

   ```bash
   # ビルド
   cd backend/Receiptfly.ProcessingFunc
   dotnet build

   # 再起動
   pkill -f "func start.*7072"
   func start --port 7072

   # 動作確認
   cd ../../scripts/verification
   ./verify-ocr-flow.sh
   ```

2. **定期的な確認**

   - 主要な機能変更後は必ず `verify-ocr-flow.sh` を実行
   - 複数ファイルのアップロード機能を変更した場合は `test-multiple-upload.sh` を実行

3. **CI/CD への統合（将来）**
   - これらのスクリプトを CI/CD パイプラインに統合
   - プルリクエスト作成時に自動実行

---

## 注意事項

- すべてのスクリプトは、ローカル開発環境（Azurite 使用）を前提としています
- 本番環境で実行する場合は、接続文字列やエンドポイントを適切に変更してください
- テストファイルは `backend/Receiptfly.Api.Tests/data/` に配置してください
- スクリプト実行時は、必要なサービスがすべて起動していることを確認してください

## 長時間実行コマンドの扱いについて

**重要**: これらの動作確認スクリプトは、既に起動しているサービスに対して API リクエストを送る設計になっています。

### なぜ`func start`をスクリプト内で実行しないのか

`func start`のような長時間実行されるコマンド（サーバープロセス）は、スクリプト内で直接実行しません。理由は以下の通りです：

1. **スクリプトが終了しない**: `func start`はサーバーを起動し続けるため、スクリプトが終了しません
2. **バックグラウンド実行の複雑さ**: バックグラウンドで実行すると、プロセス管理やエラーハンドリングが複雑になります
3. **既存の設計パターン**: 既存のスクリプト（`test-sas-upload.sh`など）も同様の設計を採用しています

### 正しい使い方

1. **事前準備**: 必要なサービスを別途起動する

   ```bash
   # 別ターミナルで実行
   cd backend/Receiptfly.Functions
   func start --port 7071
   ```

2. **動作確認**: スクリプトを実行する

   ```bash
   cd scripts/verification
   ./test-new-id-format.sh
   ```

3. **エラーハンドリング**: サービスが起動していない場合、スクリプトはエラーメッセージを表示して終了します

### サービス起動確認方法

スクリプト実行前に、以下のコマンドでサービスが起動しているか確認できます：

```bash
# APIが応答するか確認
curl -s --max-time 3 "http://localhost:7071/api/receipts" > /dev/null && echo "✅ API is running" || echo "❌ API is not running"

# Azuriteが起動しているか確認
docker ps | grep azurite && echo "✅ Azurite is running" || echo "❌ Azurite is not running"
```

---

## Playwright E2E テスト

フロントエンド経由の動作確認には、Playwright E2E テストを使用します。

### 新しい ID 形式の確認テスト: `new-id-format.spec.ts`

**用途**: フロントエンド経由の新しい ID 形式の動作確認

**機能**:

- フロントエンドからファイルアップロード時に新しい ID 形式が使用されているか確認
- ファイル名が `receipt-{uuid}.{ext}` 形式になっているか確認
- Blob Storage のメタデータに `original_filename` が保存されているか確認
- レシート作成時に新しい ID 形式が使用されているか確認

**使用方法**:

```bash
cd frontend/receiptfly-web
npm run test:e2e -- e2e/new-id-format.spec.ts
```

**前提条件**:

- `Receiptfly.Functions`が起動している（ポート 7071）
- Azurite が起動している（ポート 10000-10002）
- フロントエンドは自動起動される（`playwright.config.ts`の`webServer`設定により）

**動作確認結果（2025-01-23）**:

- ✅ レシート作成時の ID 形式: receipt-{uuid} - 正常に動作
- ✅ Transaction Item ID 形式: transaction-{uuid} - 正常に動作
- ✅ UserId: user_default - 正常に設定
- ✅ ファイルアップロード時の ID 形式: receipt-{uuid}.pdf - 正常に動作
- ✅ Blob メタデータ: original_filename - 正常に設定

**注意事項**:

- このテストは Playwright を使用してブラウザで実行されます
- フロントエンドが起動していない場合、自動的に起動されます（`playwright.config.ts`の`webServer`設定により）
- テスト実行中はブラウザが自動的に開閉されます（ヘッドレスモード）

---

## 関連ドキュメント

- [アーキテクチャドキュメント](../../docs/architecture.md)
- [要件定義](../../docs/requirements.md)
- [Google 認証設定](../../docs/google-auth.md)
