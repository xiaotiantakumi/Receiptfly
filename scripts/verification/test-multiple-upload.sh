#!/bin/bash
set -e

# 詳細ログを有効化
set -x

echo "=== Multiple Receipt Files Upload Test ==="
echo "[$(date +%H:%M:%S)] Test started"
echo ""

DATA_DIR="backend/Receiptfly.Api.Tests/data"
API_BASE_URL="http://localhost:7071/api"
CONTAINER_NAME="receipt-images"

echo "[$(date +%H:%M:%S)] Configuration:"
echo "  DATA_DIR: $DATA_DIR"
echo "  API_BASE_URL: $API_BASE_URL"
echo "  CONTAINER_NAME: $CONTAINER_NAME"
echo ""

# ディレクトリの存在確認
if [ ! -d "$DATA_DIR" ]; then
    echo "[$(date +%H:%M:%S)] ❌ Data directory not found: $DATA_DIR"
    exit 1
fi
echo "[$(date +%H:%M:%S)] ✓ Data directory exists"

# PDFファイルのリストを取得
PDF_FILES=(
    "20241216_ローソン灘北通店.pdf"
    "20241216_Wio.pdf"
    "20241215_株式会社.pdf"
    "20241213_株式会社ココカラファイン　灘駅前店.pdf"
    "20241211_播磨屋本店　神戸店.pdf"
    "20241211_ライフコーポレーション春日野道店.pdf"
    "20241207_ガスト神戸ひよどり台店.pdf"
    "20241207_(店名).pdf"
    "20241222_data.pdf"
)

SUCCESS_COUNT=0
FAIL_COUNT=0
FAILED_FILES=()

echo "Found ${#PDF_FILES[@]} PDF files to test"
echo ""

for pdf_file in "${PDF_FILES[@]}"; do
    file_path="$DATA_DIR/$pdf_file"
    file_index=$((SUCCESS_COUNT + FAIL_COUNT + 1))
    
    echo ""
    echo "[$(date +%H:%M:%S)] ========================================"
    echo "[$(date +%H:%M:%S)] File $file_index/${#PDF_FILES[@]}: $pdf_file"
    echo "[$(date +%H:%M:%S)] ========================================"
    
    if [ ! -f "$file_path" ]; then
        echo "[$(date +%H:%M:%S)] ⚠ File not found: $file_path"
        ((FAIL_COUNT++))
        FAILED_FILES+=("$pdf_file (not found)")
        continue
    fi
    
    # ファイルサイズを確認
    file_size=$(stat -f%z "$file_path" 2>/dev/null || stat -c%s "$file_path" 2>/dev/null || echo "unknown")
    echo "[$(date +%H:%M:%S)] File size: $file_size bytes"
    
    # ファイル名をURLエンコード
    echo "[$(date +%H:%M:%S)] Encoding filename..."
    encoded_filename=$(echo "$pdf_file" | python3 -c "import sys, urllib.parse; print(urllib.parse.quote(sys.stdin.read().strip()))" 2>&1)
    if [ $? -ne 0 ]; then
        echo "[$(date +%H:%M:%S)] ❌ Failed to encode filename: $encoded_filename"
        ((FAIL_COUNT++))
        FAILED_FILES+=("$pdf_file (encoding failed)")
        continue
    fi
    blob_name="test-$(date +%s)-$encoded_filename"
    echo "[$(date +%H:%M:%S)] Blob name: $blob_name"
    
    # SASトークンを取得
    echo "[$(date +%H:%M:%S)] Step 1: Getting SAS token..."
    sas_start_time=$(date +%s)
    SAS_RESPONSE=$(curl -s --max-time 10 -w "\nHTTP_CODE:%{http_code}\nTIME_TOTAL:%{time_total}\n" "$API_BASE_URL/getSas?containerName=$CONTAINER_NAME&blobName=$blob_name" 2>&1)
    sas_curl_exit=$?
    sas_end_time=$(date +%s)
    sas_duration=$((sas_end_time - sas_start_time))
    
    echo "[$(date +%H:%M:%S)] SAS request completed in ${sas_duration}s (exit code: $sas_curl_exit)"
    
    if [ $sas_curl_exit -ne 0 ]; then
        echo "[$(date +%H:%M:%S)] ❌ Failed to get SAS token (curl exit code: $sas_curl_exit)"
        echo "[$(date +%H:%M:%S)] Response: ${SAS_RESPONSE:0:200}"
        ((FAIL_COUNT++))
        FAILED_FILES+=("$pdf_file (SAS token failed)")
        continue
    fi
    
    # HTTPステータスコードを抽出
    http_code=$(echo "$SAS_RESPONSE" | grep "HTTP_CODE:" | cut -d: -f2)
    echo "[$(date +%H:%M:%S)] HTTP status: $http_code"
    
    if [ "$http_code" != "200" ]; then
        echo "[$(date +%H:%M:%S)] ❌ SAS token request failed (HTTP $http_code)"
        echo "[$(date +%H:%M:%S)] Response: ${SAS_RESPONSE:0:500}"
        ((FAIL_COUNT++))
        FAILED_FILES+=("$pdf_file (SAS HTTP $http_code)")
        continue
    fi
    
    # JSONからSAS URLを抽出
    echo "[$(date +%H:%M:%S)] Parsing SAS URL from response..."
    SAS_URL=$(echo "$SAS_RESPONSE" | grep -v "HTTP_CODE:" | grep -v "TIME_TOTAL:" | python3 -c "import sys, json; print(json.load(sys.stdin)['sasUrl'])" 2>&1)
    
    if [ $? -ne 0 ] || [ -z "$SAS_URL" ]; then
        echo "[$(date +%H:%M:%S)] ❌ Failed to parse SAS URL"
        echo "[$(date +%H:%M:%S)] Response: ${SAS_RESPONSE:0:500}"
        ((FAIL_COUNT++))
        FAILED_FILES+=("$pdf_file (SAS URL parse failed)")
        continue
    fi
    
    echo "[$(date +%H:%M:%S)] ✓ SAS URL obtained: ${SAS_URL:0:80}..."
    
    # ファイルをアップロード
    echo "[$(date +%H:%M:%S)] Step 2: Uploading file..."
    upload_start_time=$(date +%s)
    UPLOAD_RESPONSE=$(curl -X PUT --max-time 30 \
        -H "x-ms-blob-type: BlockBlob" \
        -H "Content-Type: application/pdf" \
        --data-binary "@$file_path" \
        -w "\nHTTP_CODE:%{http_code}\nTIME_TOTAL:%{time_total}\nSIZE_UPLOAD:%{size_upload}\n" \
        -s \
        "$SAS_URL" 2>&1)
    upload_curl_exit=$?
    upload_end_time=$(date +%s)
    upload_duration=$((upload_end_time - upload_start_time))
    
    echo "[$(date +%H:%M:%S)] Upload completed in ${upload_duration}s (exit code: $upload_curl_exit)"
    
    UPLOAD_STATUS=$(echo "$UPLOAD_RESPONSE" | grep "HTTP_CODE:" | cut -d: -f2)
    upload_size=$(echo "$UPLOAD_RESPONSE" | grep "SIZE_UPLOAD:" | cut -d: -f2)
    
    echo "[$(date +%H:%M:%S)] Upload HTTP status: $UPLOAD_STATUS"
    echo "[$(date +%H:%M:%S)] Upload size: $upload_size bytes"
    
    if [ "$UPLOAD_STATUS" = "201" ]; then
        echo "[$(date +%H:%M:%S)] ✓ Uploaded successfully! (HTTP $UPLOAD_STATUS)"
        ((SUCCESS_COUNT++))
    else
        echo "[$(date +%H:%M:%S)] ❌ Upload failed (HTTP $UPLOAD_STATUS)"
        echo "[$(date +%H:%M:%S)] Response: ${UPLOAD_RESPONSE:0:500}"
        ((FAIL_COUNT++))
        FAILED_FILES+=("$pdf_file (HTTP $UPLOAD_STATUS)")
    fi
    
    sleep 0.3  # 少し待機
done

echo "=== Test Summary ==="
echo "Total files: ${#PDF_FILES[@]}"
echo "✓ Success: $SUCCESS_COUNT"
echo "❌ Failed: $FAIL_COUNT"
echo ""

if [ $FAIL_COUNT -gt 0 ]; then
    echo "Failed files:"
    for failed in "${FAILED_FILES[@]}"; do
        echo "  - $failed"
    done
    echo ""
    exit 1
else
    echo "=== All Tests Passed ==="
    exit 0
fi

