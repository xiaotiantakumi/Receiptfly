# 動作確認スクリプト

このディレクトリには、Receiptflyプロジェクトの各種機能を検証するためのシェルスクリプトが含まれています。

## スクリプト一覧

### 1. `verify-ocr-flow.sh`
**用途**: OCR処理からレシート登録までのエンドツーエンドフローを検証

**機能**:
- テストファイルをBlob Storageにアップロード（SASトークン経由）
- OCR処理をQueueにキューイング
- レシート登録の完了を待機（ポーリング）
- 登録されたレシートデータの検証

**使用方法**:
```bash
./verify-ocr-flow.sh
```

**前提条件**:
- `Receiptfly.Functions`が起動している（ポート7071）
- `Receiptfly.ProcessingFunc`が起動している（ポート7072）
- Azuriteが起動している（ポート10000-10002）
- テストファイル `backend/Receiptfly.Api.Tests/data/20241222_data.pdf` が存在する

**設定可能な変数**:
- `DATA_DIR`: テストファイルのディレクトリ（デフォルト: `backend/Receiptfly.Api.Tests/data`）
- `API_BASE_URL`: APIのベースURL（デフォルト: `http://localhost:7071/api`）
- `CONTAINER_NAME`: Blob Storageのコンテナ名（デフォルト: `receipt-images`）
- `TEST_FILE`: テストファイル名（デフォルト: `20241222_data.pdf`）
- `MAX_WAIT_TIME`: 最大待機時間（秒、デフォルト: 120）

---

### 2. `test-sas-upload.sh`
**用途**: SASトークン取得とファイルアップロードの基本動作を確認

**機能**:
- SASトークンの取得
- 取得したSAS URLを使用したファイルアップロード

**使用方法**:
```bash
./test-sas-upload.sh
```

**前提条件**:
- `Receiptfly.Functions`が起動している（ポート7071）
- Azuriteが起動している（ポート10000-10002）
- テストファイル `backend/Receiptfly.Api.Tests/data/20241222_data.pdf` が存在する（存在しない場合はダミーデータを使用）

---

### 3. `test-multiple-upload.sh`
**用途**: 複数のPDFファイルを一括アップロードして動作を確認

**機能**:
- 複数のPDFファイルを順次アップロード
- 各ファイルのアップロード結果を記録
- 成功/失敗のサマリーを表示

**使用方法**:
```bash
./test-multiple-upload.sh
```

**前提条件**:
- `Receiptfly.Functions`が起動している（ポート7071）
- Azuriteが起動している（ポート10000-10002）
- `backend/Receiptfly.Api.Tests/data/` ディレクトリにテスト用PDFファイルが存在する

**テスト対象ファイル**:
- 20241216_ローソン灘北通店.pdf
- 20241216_Wio.pdf
- 20241215_株式会社.pdf
- 20241213_株式会社ココカラファイン　灘駅前店.pdf
- 20241211_播磨屋本店　神戸店.pdf
- 20241211_ライフコーポレーション春日野道店.pdf
- 20241207_ガスト神戸ひよどり台店.pdf
- 20241207_(店名).pdf
- 20241222_data.pdf

---

### 4. `test-sas-relay.sh`
**用途**: SASトークンリレー（バケツリレー方式）の動作を確認

**機能**:
- `Receiptfly.Functions`経由でSASトークンを取得
- `Receiptfly.ProcessingFunc`へのリレーが正常に動作することを確認

**使用方法**:
```bash
./test-sas-relay.sh
```

**前提条件**:
- `Receiptfly.Functions`が起動している（ポート7071）
- `Receiptfly.ProcessingFunc`が起動している（ポート7072）
- Azuriteが起動している（ポート10000-10002）

---

### 5. `test-sas-relay-detailed.sh`
**用途**: SASトークンリレーの詳細な動作確認（ログ付き）

**機能**:
- `test-sas-relay.sh`と同様だが、より詳細なログを出力
- 各ステップの実行時間を記録
- エラー時の詳細情報を表示

**使用方法**:
```bash
./test-sas-relay-detailed.sh
```

**前提条件**:
- `Receiptfly.Functions`が起動している（ポート7071）
- `Receiptfly.ProcessingFunc`が起動している（ポート7072）
- Azuriteが起動している（ポート10000-10002）

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

**SASトークン取得とアップロードのみ確認**:
```bash
./test-sas-upload.sh
```

**複数ファイルのアップロードを確認**:
```bash
./test-multiple-upload.sh
```

**SASトークンリレーの動作を確認**:
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
   - Processing Functionのログ: `/tmp/processing-func.log` またはコンソール出力
   - Functionsのログ: コンソール出力
   - Azuriteのログ: `docker logs receiptify-azurite`

3. **設定ファイルを確認**
   - `backend/Receiptfly.Functions/local.settings.json`
   - `backend/Receiptfly.ProcessingFunc/local.settings.json`
   - 特に `AzureWebJobsStorage` と `AzureStorage` の接続文字列

4. **Queueメッセージが処理されているか確認**
   ```bash
   # Processing Functionのログで以下を確認
   tail -f /tmp/processing-func.log | grep "Processing OCR"
   ```

5. **OCR結果が空の場合**
   - PDFファイルが正しくアップロードされているか確認
   - Google Vision APIキーが正しく設定されているか確認
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

3. **READMEに追加**
   - このREADMEにスクリプトの説明を追加

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

3. **CI/CDへの統合（将来）**
   - これらのスクリプトをCI/CDパイプラインに統合
   - プルリクエスト作成時に自動実行

---

## 注意事項

- すべてのスクリプトは、ローカル開発環境（Azurite使用）を前提としています
- 本番環境で実行する場合は、接続文字列やエンドポイントを適切に変更してください
- テストファイルは `backend/Receiptfly.Api.Tests/data/` に配置してください
- スクリプト実行時は、必要なサービスがすべて起動していることを確認してください

---

## 関連ドキュメント

- [アーキテクチャドキュメント](../../docs/architecture.md)
- [要件定義](../../docs/requirements.md)
- [Google認証設定](../../docs/google-auth.md)

