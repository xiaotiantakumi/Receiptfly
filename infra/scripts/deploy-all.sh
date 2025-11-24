#!/bin/bash
set -e

# 引数チェック
if [ $# -lt 3 ]; then
    echo "使用方法: $0 <環境名> <リージョン> <リソースグループ名>"
    echo "例: $0 dev japaneast rg-receiptfly-dev"
    exit 1
fi

ENV=$1
LOCATION=$2
RESOURCE_GROUP=$3

echo "=========================================="
echo "Receiptfly 全体デプロイ"
echo "=========================================="
echo "環境名: $ENV"
echo "リージョン: $LOCATION"
echo "リソースグループ: $RESOURCE_GROUP"
echo ""

# スクリプトのディレクトリを取得
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# 1. インフラストラクチャのデプロイ（必要に応じて）
echo "=========================================="
echo "ステップ 1: インフラストラクチャの確認"
echo "=========================================="
echo ""

if az group show --name "$RESOURCE_GROUP" > /dev/null 2>&1; then
    echo "[$(date +%H:%M:%S)] リソースグループ '$RESOURCE_GROUP' は既に存在します"
    echo "[$(date +%H:%M:%S)] インフラストラクチャのデプロイをスキップします"
    echo "  インフラストラクチャを更新する場合は、以下を実行してください:"
    echo "  $SCRIPT_DIR/deploy.sh $ENV $LOCATION $RESOURCE_GROUP"
else
    echo "[$(date +%H:%M:%S)] リソースグループ '$RESOURCE_GROUP' が見つかりません"
    echo "[$(date +%H:%M:%S)] インフラストラクチャのデプロイを実行します..."
    "$SCRIPT_DIR/deploy.sh" "$ENV" "$LOCATION" "$RESOURCE_GROUP"
    
    if [ $? -ne 0 ]; then
        echo "[$(date +%H:%M:%S)] ❌ インフラストラクチャのデプロイに失敗しました"
        exit 1
    fi
fi

# 2. Processing Function App のデプロイ
echo ""
echo "=========================================="
echo "ステップ 2: Processing Function App のデプロイ"
echo "=========================================="
echo ""

"$SCRIPT_DIR/deploy-functions.sh" "$ENV" "$RESOURCE_GROUP"

if [ $? -ne 0 ]; then
    echo "[$(date +%H:%M:%S)] ❌ Processing Function App のデプロイに失敗しました"
    exit 1
fi

# 3. Static Web App のデプロイ（API統合）
echo ""
echo "=========================================="
echo "ステップ 3: Static Web App のデプロイ（API統合）"
echo "=========================================="
echo ""

"$SCRIPT_DIR/deploy-staticwebapp-api.sh" "$ENV" "$RESOURCE_GROUP"

if [ $? -ne 0 ]; then
    echo "[$(date +%H:%M:%S)] ❌ Static Web App のデプロイに失敗しました"
    exit 1
fi

echo ""
echo "=========================================="
echo "全体デプロイ完了"
echo "=========================================="
echo ""
echo "デプロイされたリソース:"
echo "  - Function Apps:"
echo "    - Processing: func-receiptfly-processing-${ENV}-001"
echo "    - API: Static Web AppのバックエンドAPIとして統合"
echo "  - Static Web App: swa-receiptfly-${ENV}-001"
echo ""
echo "検証を実行するには、以下のコマンドを実行してください:"
echo "  $SCRIPT_DIR/verify-deploy.sh $RESOURCE_GROUP"
echo ""

