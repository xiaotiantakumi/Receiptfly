#!/bin/bash
set -e

echo "=== SAS Token Upload Test ==="
echo ""

# バックエンドAPIの確認
echo "1. Testing SAS token generation..."
SAS_RESPONSE=$(curl -s --max-time 5 "http://localhost:7071/api/getSas?containerName=receipt-images&blobName=test-$(date +%s).pdf")

if [ $? -ne 0 ]; then
    echo "❌ Failed to connect to backend API"
    exit 1
fi

SAS_URL=$(echo "$SAS_RESPONSE" | python3 -c "import sys, json; print(json.load(sys.stdin)['sasUrl'])" 2>/dev/null)

if [ -z "$SAS_URL" ]; then
    echo "❌ Failed to get SAS URL"
    echo "Response: $SAS_RESPONSE"
    exit 1
fi

echo "✓ SAS Token obtained: ${SAS_URL:0:80}..."
echo ""

# ファイルアップロードのテスト
echo "2. Testing file upload..."
TEST_FILE="backend/Receiptfly.Api.Tests/data/20241222_data.pdf"

if [ ! -f "$TEST_FILE" ]; then
    echo "⚠ Test file not found, creating dummy data"
    echo "%PDF-1.4" > /tmp/test.pdf
    TEST_FILE="/tmp/test.pdf"
fi

UPLOAD_STATUS=$(curl -X PUT --max-time 10 \
    -H "x-ms-blob-type: BlockBlob" \
    -H "Content-Type: application/pdf" \
    --data-binary "@$TEST_FILE" \
    -w "%{http_code}" \
    -s -o /dev/null \
    "$SAS_URL")

if [ "$UPLOAD_STATUS" = "201" ]; then
    echo "✓ File uploaded successfully! (HTTP $UPLOAD_STATUS)"
    echo ""
    echo "=== Test Passed ==="
    exit 0
else
    echo "❌ Upload failed with HTTP status: $UPLOAD_STATUS"
    exit 1
fi

