#!/bin/bash
set -e

# OCR処理〜レシート登録フローの動作確認スクリプト

echo "=== OCR Processing to Receipt Registration Flow Verification ==="
echo "[$(date +%H:%M:%S)] Test started"
echo ""

# 設定
DATA_DIR="backend/Receiptfly.Api.Tests/data"
API_BASE_URL="http://localhost:7071/api"
CONTAINER_NAME="receipt-images"
TEST_FILE="20241222_data.pdf"  # テスト用のファイル（存在確認済み）

# 最大待機時間（秒）
MAX_WAIT_TIME=120
POLL_INTERVAL=3

echo "[$(date +%H:%M:%S)] Configuration:"
echo "  DATA_DIR: $DATA_DIR"
echo "  API_BASE_URL: $API_BASE_URL"
echo "  CONTAINER_NAME: $CONTAINER_NAME"
echo "  TEST_FILE: $TEST_FILE"
echo "  MAX_WAIT_TIME: ${MAX_WAIT_TIME}s"
echo ""

# 1. テストファイルの存在確認
FILE_PATH="$DATA_DIR/$TEST_FILE"
if [ ! -f "$FILE_PATH" ]; then
    echo "[$(date +%H:%M:%S)] ❌ Test file not found: $FILE_PATH"
    exit 1
fi
echo "[$(date +%H:%M:%S)] ✓ Test file found: $FILE_PATH"

# 2. アップロード前のレシート数を取得
echo ""
echo "[$(date +%H:%M:%S)] Step 1: Getting initial receipt count..."
INITIAL_RECEIPTS_RESPONSE=$(curl -s "$API_BASE_URL/receipts")
INITIAL_RECEIPT_COUNT=$(echo "$INITIAL_RECEIPTS_RESPONSE" | jq '. | length' 2>/dev/null || echo "0")
echo "[$(date +%H:%M:%S)] Initial receipt count: $INITIAL_RECEIPT_COUNT"

# 3. SASトークンを取得してファイルをアップロード
echo ""
echo "[$(date +%H:%M:%S)] Step 2: Uploading file to Blob Storage..."
TIMESTAMP=$(date +%s)
UNIQUE_FILE_NAME="${TIMESTAMP}-${TEST_FILE}"
BLOB_NAME="$UNIQUE_FILE_NAME"

# SASトークン取得
SAS_RESPONSE=$(curl -s "$API_BASE_URL/getSas?containerName=${CONTAINER_NAME}&blobName=${BLOB_NAME}")
SAS_URL=$(echo "$SAS_RESPONSE" | jq -r '.sasUrl' 2>/dev/null)

if [ -z "$SAS_URL" ] || [ "$SAS_URL" = "null" ]; then
    echo "[$(date +%H:%M:%S)] ❌ Failed to get SAS token"
    echo "Response: $SAS_RESPONSE"
    exit 1
fi
echo "[$(date +%H:%M:%S)] ✓ SAS token obtained"

# ファイルをアップロード
UPLOAD_RESPONSE=$(curl -s -w "\nHTTP_STATUS:%{http_code}" -X PUT "$SAS_URL" \
    -H "x-ms-blob-type: BlockBlob" \
    -H "Content-Type: application/pdf" \
    --data-binary "@$FILE_PATH")
UPLOAD_STATUS=$(echo "$UPLOAD_RESPONSE" | grep "HTTP_STATUS" | cut -d: -f2)

if [ "$UPLOAD_STATUS" != "201" ]; then
    echo "[$(date +%H:%M:%S)] ❌ File upload failed (HTTP $UPLOAD_STATUS)"
    exit 1
fi
echo "[$(date +%H:%M:%S)] ✓ File uploaded successfully (HTTP 201)"

# 4. QueueにOCR処理をキューイング
echo ""
echo "[$(date +%H:%M:%S)] Step 3: Queuing OCR processing..."
BLOB_PATH="${CONTAINER_NAME}/${BLOB_NAME}"
QUEUE_REQUEST=$(jq -n \
    --arg blobPath "$BLOB_PATH" \
    '{blobPaths: [$blobPath]}')

QUEUE_RESPONSE=$(curl -s -w "\nHTTP_STATUS:%{http_code}" -X POST "$API_BASE_URL/queue-ocr" \
    -H "Content-Type: application/json" \
    -d "$QUEUE_REQUEST")
QUEUE_STATUS=$(echo "$QUEUE_RESPONSE" | grep "HTTP_STATUS" | cut -d: -f2)

if [ "$QUEUE_STATUS" != "200" ] && [ "$QUEUE_STATUS" != "201" ]; then
    echo "[$(date +%H:%M:%S)] ❌ Failed to queue OCR processing (HTTP $QUEUE_STATUS)"
    echo "Response: $QUEUE_RESPONSE"
    exit 1
fi
echo "[$(date +%H:%M:%S)] ✓ OCR processing queued successfully"

# 5. OCR処理の完了を待機（ポーリング）
echo ""
echo "[$(date +%H:%M:%S)] Step 4: Waiting for OCR processing and receipt registration..."
echo "  Polling every ${POLL_INTERVAL}s, max wait: ${MAX_WAIT_TIME}s"

START_TIME=$(date +%s)
ELAPSED=0
RECEIPT_REGISTERED=false

while [ $ELAPSED -lt $MAX_WAIT_TIME ]; do
    sleep $POLL_INTERVAL
    ELAPSED=$(($(date +%s) - START_TIME))
    
    # レシート数を確認
    CURRENT_RECEIPTS_RESPONSE=$(curl -s "$API_BASE_URL/receipts")
    CURRENT_RECEIPT_COUNT=$(echo "$CURRENT_RECEIPTS_RESPONSE" | jq '. | length' 2>/dev/null || echo "0")
    
    if [ "$CURRENT_RECEIPT_COUNT" -gt "$INITIAL_RECEIPT_COUNT" ]; then
        echo "[$(date +%H:%M:%S)] ✓ New receipt detected! (Count: $INITIAL_RECEIPT_COUNT -> $CURRENT_RECEIPT_COUNT)"
        RECEIPT_REGISTERED=true
        break
    fi
    
    if [ $((ELAPSED % 10)) -eq 0 ]; then
        echo "[$(date +%H:%M:%S)]   Still waiting... (${ELAPSED}s elapsed, receipt count: $CURRENT_RECEIPT_COUNT)"
    fi
done

if [ "$RECEIPT_REGISTERED" = false ]; then
    echo "[$(date +%H:%M:%S)] ❌ Timeout: Receipt was not registered within ${MAX_WAIT_TIME}s"
    echo "  Initial count: $INITIAL_RECEIPT_COUNT"
    echo "  Final count: $CURRENT_RECEIPT_COUNT"
    exit 1
fi

# 6. 登録されたレシートの内容を確認
echo ""
echo "[$(date +%H:%M:%S)] Step 5: Verifying receipt data..."
LATEST_RECEIPTS_RESPONSE=$(curl -s "$API_BASE_URL/receipts")
LATEST_RECEIPT=$(echo "$LATEST_RECEIPTS_RESPONSE" | jq '.[0]' 2>/dev/null)

if [ -z "$LATEST_RECEIPT" ] || [ "$LATEST_RECEIPT" = "null" ]; then
    echo "[$(date +%H:%M:%S)] ❌ Failed to get latest receipt"
    exit 1
fi

# レシートの基本情報を確認
RECEIPT_ID=$(echo "$LATEST_RECEIPT" | jq -r '.id' 2>/dev/null)
RECEIPT_STORE=$(echo "$LATEST_RECEIPT" | jq -r '.store' 2>/dev/null)
RECEIPT_TOTAL=$(echo "$LATEST_RECEIPT" | jq -r '.total' 2>/dev/null)
RECEIPT_ITEMS_COUNT=$(echo "$LATEST_RECEIPT" | jq '.items | length' 2>/dev/null)

echo "[$(date +%H:%M:%S)] Receipt details:"
echo "  ID: $RECEIPT_ID"
echo "  Store: $RECEIPT_STORE"
echo "  Total: $RECEIPT_TOTAL"
echo "  Items count: $RECEIPT_ITEMS_COUNT"

# 簡易バリデーション
VALIDATION_FAILED=false

if [ -z "$RECEIPT_ID" ] || [ "$RECEIPT_ID" = "null" ]; then
    echo "  ❌ Receipt ID is missing"
    VALIDATION_FAILED=true
fi

if [ -z "$RECEIPT_STORE" ] || [ "$RECEIPT_STORE" = "null" ] || [ "$RECEIPT_STORE" = "" ]; then
    echo "  ❌ Store name is missing"
    VALIDATION_FAILED=true
fi

if [ -z "$RECEIPT_TOTAL" ] || [ "$RECEIPT_TOTAL" = "null" ] || [ "$RECEIPT_TOTAL" = "0" ]; then
    echo "  ⚠ Total amount is 0 or missing (may be valid)"
fi

if [ -z "$RECEIPT_ITEMS_COUNT" ] || [ "$RECEIPT_ITEMS_COUNT" = "null" ] || [ "$RECEIPT_ITEMS_COUNT" = "0" ]; then
    echo "  ⚠ No items found (may be valid)"
fi

if [ "$VALIDATION_FAILED" = true ]; then
    echo ""
    echo "[$(date +%H:%M:%S)] ❌ Receipt validation failed"
    echo "Full receipt data:"
    echo "$LATEST_RECEIPT" | jq '.' 2>/dev/null || echo "$LATEST_RECEIPT"
    exit 1
fi

echo ""
echo "[$(date +%H:%M:%S)] ✓ Receipt validation passed"

# 7. サマリー
echo ""
echo "=== Test Summary ==="
echo "✓ File uploaded successfully"
echo "✓ OCR processing queued"
echo "✓ Receipt registered (ID: $RECEIPT_ID)"
echo "✓ Receipt data validated"
echo ""
echo "[$(date +%H:%M:%S)] === All Tests PASSED ==="
echo ""
echo "The complete flow is working correctly:"
echo "  Upload → Queue → OCR → Receipt Generation → Database Registration"

