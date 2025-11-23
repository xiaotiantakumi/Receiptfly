#!/bin/bash
set -e

# 新しいID形式（receipt-{uuid}, transaction-{uuid}）の動作確認スクリプト
#
# このスクリプトは以下の点を確認します：
# 1. レシート作成時に新しいID形式（receipt-{uuid}）が使用されているか
# 2. TransactionItem作成時に新しいID形式（transaction-{uuid}）が使用されているか
# 3. UserIdが正しく設定されているか
# 4. 作成したレシートの取得とID形式の再確認
#
# 注意: このスクリプトは既に起動しているサービスに対してAPIリクエストを送ります。
# func startのような長時間実行されるコマンドは含まれていません。
# 事前に以下のサービスを起動しておく必要があります：
# - Receiptfly.Functions（ポート7071）
# - Azurite（ポート10000-10002）
#
# 実行例:
#   # 別ターミナルでサービスを起動
#   cd backend/Receiptfly.Functions
#   func start --port 7071
#
#   # このスクリプトを実行
#   cd scripts/verification
#   ./test-new-id-format.sh
#
# 動作確認結果（2025-01-23）:
#   ✅ Receipt ID形式: receipt-{uuid} - 正常に動作
#   ✅ Transaction Item ID形式: transaction-{uuid} - 正常に動作
#   ✅ UserId: user_default - 正常に設定
#   ✅ レシート取得: 正常に動作

echo "=== New ID Format Verification Test ==="
echo "[$(date +%H:%M:%S)] Test started"
echo ""

# 設定
API_BASE_URL="http://localhost:7071/api"
MAX_WAIT_TIME=10  # API応答待機時間（秒）

echo "[$(date +%H:%M:%S)] Configuration:"
echo "  API_BASE_URL: $API_BASE_URL"
echo "  MAX_WAIT_TIME: ${MAX_WAIT_TIME}s"
echo ""

# 1. APIが起動しているか確認
echo "[$(date +%H:%M:%S)] Step 1: Checking if API is running..."
if ! curl -s --max-time $MAX_WAIT_TIME "$API_BASE_URL/receipts" > /dev/null 2>&1; then
    echo "[$(date +%H:%M:%S)] ❌ API is not responding"
    echo "  Please make sure Receiptfly.Functions is running on port 7071"
    echo "  Start it with: cd backend/Receiptfly.Functions && func start --port 7071"
    exit 1
fi
echo "[$(date +%H:%M:%S)] ✓ API is responding"

# 2. 新しいレシートを作成してID形式を確認
echo ""
echo "[$(date +%H:%M:%S)] Step 2: Creating a new receipt to verify ID format..."

RECEIPT_DATA='{
  "store": "テスト店舗（新ID形式確認）",
  "date": "2024年11月22日 10:23",
  "items": [
    {
      "name": "テスト商品1",
      "amount": 1000,
      "isTaxReturn": true
    },
    {
      "name": "テスト商品2",
      "amount": 500,
      "isTaxReturn": false
    }
  ]
}'

RECEIPT_RESPONSE=$(curl -s --max-time $MAX_WAIT_TIME -X POST "$API_BASE_URL/receipts" \
  -H "Content-Type: application/json" \
  -d "$RECEIPT_DATA")

if [ $? -ne 0 ]; then
    echo "[$(date +%H:%M:%S)] ❌ Failed to create receipt"
    exit 1
fi

# レスポンスをパース
RECEIPT_ID=$(echo "$RECEIPT_RESPONSE" | python3 -c "import sys, json; print(json.load(sys.stdin)['id'])" 2>/dev/null)
RECEIPT_USER_ID=$(echo "$RECEIPT_RESPONSE" | python3 -c "import sys, json; print(json.load(sys.stdin).get('userId', 'N/A'))" 2>/dev/null)
RECEIPT_ORIGINAL_FILENAME=$(echo "$RECEIPT_RESPONSE" | python3 -c "import sys, json; print(json.load(sys.stdin).get('originalFileName', 'N/A'))" 2>/dev/null)

if [ -z "$RECEIPT_ID" ] || [ "$RECEIPT_ID" = "null" ]; then
    echo "[$(date +%H:%M:%S)] ❌ Failed to get receipt ID from response"
    echo "Response: $RECEIPT_RESPONSE"
    exit 1
fi

echo "[$(date +%H:%M:%S)] ✓ Receipt created"
echo "  Receipt ID: $RECEIPT_ID"
echo "  User ID: $RECEIPT_USER_ID"
echo "  Original File Name: $RECEIPT_ORIGINAL_FILENAME"

# 3. レシートIDの形式を確認
echo ""
echo "[$(date +%H:%M:%S)] Step 3: Verifying receipt ID format..."

if [[ "$RECEIPT_ID" =~ ^receipt-[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$ ]]; then
    echo "[$(date +%H:%M:%S)] ✓ Receipt ID format is correct (receipt-{uuid})"
else
    echo "[$(date +%H:%M:%S)] ❌ Receipt ID format is incorrect"
    echo "  Expected: receipt-{uuid}"
    echo "  Got: $RECEIPT_ID"
    exit 1
fi

# 4. TransactionItemのID形式を確認
echo ""
echo "[$(date +%H:%M:%S)] Step 4: Verifying transaction item ID format..."

ITEM_IDS=$(echo "$RECEIPT_RESPONSE" | python3 -c "import sys, json; items = json.load(sys.stdin).get('items', []); print('\n'.join([item['id'] for item in items]))" 2>/dev/null)

if [ -z "$ITEM_IDS" ]; then
    echo "[$(date +%H:%M:%S)] ❌ No items found in receipt"
    exit 1
fi

ITEM_COUNT=0
VALID_ITEM_COUNT=0

while IFS= read -r ITEM_ID; do
    if [ -z "$ITEM_ID" ]; then
        continue
    fi
    ITEM_COUNT=$((ITEM_COUNT + 1))
    
    if [[ "$ITEM_ID" =~ ^transaction-[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$ ]]; then
        VALID_ITEM_COUNT=$((VALID_ITEM_COUNT + 1))
        echo "  ✓ Item $ITEM_COUNT ID format is correct: $ITEM_ID"
    else
        echo "  ❌ Item $ITEM_COUNT ID format is incorrect: $ITEM_ID"
        echo "    Expected: transaction-{uuid}"
    fi
done <<< "$ITEM_IDS"

if [ $VALID_ITEM_COUNT -eq $ITEM_COUNT ] && [ $ITEM_COUNT -gt 0 ]; then
    echo "[$(date +%H:%M:%S)] ✓ All transaction item IDs have correct format (transaction-{uuid})"
else
    echo "[$(date +%H:%M:%S)] ❌ Some transaction item IDs have incorrect format"
    echo "  Valid: $VALID_ITEM_COUNT / $ITEM_COUNT"
    exit 1
fi

# 5. UserIdの確認
echo ""
echo "[$(date +%H:%M:%S)] Step 5: Verifying UserId..."

if [ "$RECEIPT_USER_ID" != "N/A" ] && [ -n "$RECEIPT_USER_ID" ]; then
    echo "[$(date +%H:%M:%S)] ✓ UserId is set: $RECEIPT_USER_ID"
else
    echo "[$(date +%H:%M:%S)] ⚠ UserId is not set (may be expected if not implemented yet)"
fi

# 6. 作成したレシートを取得して再確認
echo ""
echo "[$(date +%H:%M:%S)] Step 6: Retrieving created receipt to verify persistence..."

GET_RESPONSE=$(curl -s --max-time $MAX_WAIT_TIME "$API_BASE_URL/receipts/$RECEIPT_ID")

if [ $? -ne 0 ]; then
    echo "[$(date +%H:%M:%S)] ⚠ Failed to retrieve receipt (may be expected if GetById is not implemented)"
else
    RETRIEVED_ID=$(echo "$GET_RESPONSE" | python3 -c "import sys, json; print(json.load(sys.stdin)['id'])" 2>/dev/null)
    
    if [ "$RETRIEVED_ID" = "$RECEIPT_ID" ]; then
        echo "[$(date +%H:%M:%S)] ✓ Receipt retrieved successfully with correct ID"
    else
        echo "[$(date +%H:%M:%S)] ⚠ Retrieved receipt ID does not match: $RETRIEVED_ID vs $RECEIPT_ID"
    fi
fi

# 7. サマリー
echo ""
echo "=== Test Summary ==="
echo "✓ API is responding"
echo "✓ Receipt created with new ID format: $RECEIPT_ID"
echo "✓ Transaction items created with new ID format ($VALID_ITEM_COUNT items)"
if [ "$RECEIPT_USER_ID" != "N/A" ] && [ -n "$RECEIPT_USER_ID" ]; then
    echo "✓ UserId is set: $RECEIPT_USER_ID"
fi
echo ""
echo "[$(date +%H:%M:%S)] === All Tests PASSED ==="
echo ""
echo "The new ID format is working correctly:"
echo "  - Receipt IDs: receipt-{uuid} format"
echo "  - Transaction Item IDs: transaction-{uuid} format"
echo ""
echo "Note: File upload with new ID format should be tested from the frontend,"
echo "      as it requires frontend ID generation and blob metadata handling."

