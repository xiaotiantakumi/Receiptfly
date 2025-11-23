#!/bin/bash

# バケツリレー方式の動作確認テスト（詳細版）

echo "=== SAS Token Relay Detailed Test ==="
echo ""

# テスト用のパラメータ
CONTAINER_NAME="receipt-images"
BLOB_NAME="detailed-test-$(date +%s).pdf"

echo "Step 1: Direct call to Processing Function (port 7072)"
echo "  Request: GET http://localhost:7072/api/getSas?containerName=${CONTAINER_NAME}&blobName=${BLOB_NAME}"
START_TIME=$(date +%s%N)
PROCESSING_RESPONSE=$(curl -s -w "\nHTTP_STATUS:%{http_code}\nTIME_TOTAL:%{time_total}" "http://localhost:7072/api/getSas?containerName=${CONTAINER_NAME}&blobName=${BLOB_NAME}")
END_TIME=$(date +%s%N)
DURATION=$((($END_TIME - $START_TIME) / 1000000))

PROCESSING_HTTP_STATUS=$(echo "$PROCESSING_RESPONSE" | grep "HTTP_STATUS" | cut -d: -f2)
PROCESSING_TIME=$(echo "$PROCESSING_RESPONSE" | grep "TIME_TOTAL" | cut -d: -f2)
PROCESSING_BODY=$(echo "$PROCESSING_RESPONSE" | sed '/HTTP_STATUS/d' | sed '/TIME_TOTAL/d')

if [ "$PROCESSING_HTTP_STATUS" = "200" ]; then
    PROCESSING_SAS=$(echo "$PROCESSING_BODY" | jq -r '.sasUrl' 2>/dev/null)
    echo "  ✓ Success (HTTP $PROCESSING_HTTP_STATUS, ${DURATION}ms)"
    echo "  SAS URL: ${PROCESSING_SAS:0:100}..."
else
    echo "  ✗ Failed (HTTP $PROCESSING_HTTP_STATUS)"
    exit 1
fi

echo ""
echo "Step 2: Relay through Receiptfly.Functions (port 7071)"
echo "  Request: GET http://localhost:7071/api/getSas?containerName=${CONTAINER_NAME}&blobName=${BLOB_NAME}-relay"
START_TIME=$(date +%s%N)
RELAY_RESPONSE=$(curl -s -w "\nHTTP_STATUS:%{http_code}\nTIME_TOTAL:%{time_total}" "http://localhost:7071/api/getSas?containerName=${CONTAINER_NAME}&blobName=${BLOB_NAME}-relay")
END_TIME=$(date +%s%N)
DURATION=$((($END_TIME - $START_TIME) / 1000000))

RELAY_HTTP_STATUS=$(echo "$RELAY_RESPONSE" | grep "HTTP_STATUS" | cut -d: -f2)
RELAY_TIME=$(echo "$RELAY_RESPONSE" | grep "TIME_TOTAL" | cut -d: -f2)
RELAY_BODY=$(echo "$RELAY_RESPONSE" | sed '/HTTP_STATUS/d' | sed '/TIME_TOTAL/d')

if [ "$RELAY_HTTP_STATUS" = "200" ]; then
    RELAY_SAS=$(echo "$RELAY_BODY" | jq -r '.sasUrl' 2>/dev/null)
    echo "  ✓ Success (HTTP $RELAY_HTTP_STATUS, ${DURATION}ms)"
    echo "  SAS URL: ${RELAY_SAS:0:100}..."
    echo "  Note: This request was forwarded to Processing Function by Receiptfly.Functions"
else
    echo "  ✗ Failed (HTTP $RELAY_HTTP_STATUS)"
    echo "  Response: $RELAY_BODY"
    exit 1
fi

echo ""
echo "Step 3: Verify SAS token can be used for upload"
echo "  Uploading test file using relayed SAS token..."
echo "test content from relay test" > /tmp/test-relay-upload.txt
UPLOAD_RESPONSE=$(curl -s -w "\nHTTP_STATUS:%{http_code}" -X PUT "$RELAY_SAS" \
    -H "x-ms-blob-type: BlockBlob" \
    -H "Content-Type: text/plain" \
    --data-binary @/tmp/test-relay-upload.txt)
UPLOAD_STATUS=$(echo "$UPLOAD_RESPONSE" | grep "HTTP_STATUS" | cut -d: -f2)

if [ "$UPLOAD_STATUS" = "201" ]; then
    echo "  ✓ File upload successful (HTTP 201)"
    echo "  The relayed SAS token is valid and can be used for uploads"
else
    echo "  ✗ File upload failed (HTTP $UPLOAD_STATUS)"
    exit 1
fi

rm -f /tmp/test-relay-upload.txt

echo ""
echo "=== Test Summary ==="
echo "✓ Processing Function direct call: OK"
echo "✓ Receiptfly.Functions relay: OK"
echo "✓ SAS token validation: OK"
echo "✓ File upload with relayed token: OK"
echo ""
echo "=== All Tests PASSED ==="
echo ""
echo "The relay mechanism is working correctly:"
echo "  Frontend → Receiptfly.Functions (port 7071) → Processing Function (port 7072) → SAS Token"

