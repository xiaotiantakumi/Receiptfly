#!/bin/bash

# バケツリレー方式のSASトークン取得テスト

echo "=== SAS Token Relay Test ==="
echo ""

# テスト用のパラメータ
CONTAINER_NAME="receipt-images"
BLOB_NAME="test-relay-$(date +%s).pdf"

echo "1. Testing direct call to Processing Function (port 7072)..."
PROCESSING_FUNC_RESPONSE=$(curl -s -w "\nHTTP_STATUS:%{http_code}" "http://localhost:7072/api/getSas?containerName=${CONTAINER_NAME}&blobName=${BLOB_NAME}")
PROCESSING_FUNC_HTTP_STATUS=$(echo "$PROCESSING_FUNC_RESPONSE" | grep "HTTP_STATUS" | cut -d: -f2)
PROCESSING_FUNC_BODY=$(echo "$PROCESSING_FUNC_RESPONSE" | sed '/HTTP_STATUS/d')

if [ "$PROCESSING_FUNC_HTTP_STATUS" = "200" ]; then
    echo "✓ Processing Function responded successfully"
    echo "  Response: $(echo "$PROCESSING_FUNC_BODY" | jq -r '.sasUrl' 2>/dev/null | head -c 80)..."
else
    echo "✗ Processing Function failed (HTTP $PROCESSING_FUNC_HTTP_STATUS)"
    echo "  Response: $PROCESSING_FUNC_BODY"
    exit 1
fi

echo ""
echo "2. Testing relay through Receiptfly.Functions (port 7071)..."
RELAY_RESPONSE=$(curl -s -w "\nHTTP_STATUS:%{http_code}" "http://localhost:7071/api/getSas?containerName=${CONTAINER_NAME}&blobName=${BLOB_NAME}-relay")
RELAY_HTTP_STATUS=$(echo "$RELAY_RESPONSE" | grep "HTTP_STATUS" | cut -d: -f2)
RELAY_BODY=$(echo "$RELAY_RESPONSE" | sed '/HTTP_STATUS/d')

if [ "$RELAY_HTTP_STATUS" = "200" ]; then
    echo "✓ Relay through Receiptfly.Functions succeeded"
    echo "  Response: $(echo "$RELAY_BODY" | jq -r '.sasUrl' 2>/dev/null | head -c 80)..."
    
    # レスポンスの内容を確認
    SAS_URL=$(echo "$RELAY_BODY" | jq -r '.sasUrl' 2>/dev/null)
    if [ -n "$SAS_URL" ] && [ "$SAS_URL" != "null" ]; then
        echo "✓ SAS URL is valid"
    else
        echo "✗ SAS URL is missing or invalid"
        exit 1
    fi
else
    echo "✗ Relay failed (HTTP $RELAY_HTTP_STATUS)"
    echo "  Response: $RELAY_BODY"
    exit 1
fi

echo ""
echo "3. Comparing responses..."
PROCESSING_SAS=$(echo "$PROCESSING_FUNC_BODY" | jq -r '.sasUrl' 2>/dev/null)
RELAY_SAS=$(echo "$RELAY_BODY" | jq -r '.sasUrl' 2>/dev/null)

if [ -n "$PROCESSING_SAS" ] && [ -n "$RELAY_SAS" ]; then
    echo "✓ Both responses contain valid SAS URLs"
    echo "  Processing Func SAS: ${PROCESSING_SAS:0:80}..."
    echo "  Relay SAS: ${RELAY_SAS:0:80}..."
    echo ""
    echo "=== Test PASSED ==="
    exit 0
else
    echo "✗ One or both responses are invalid"
    exit 1
fi

