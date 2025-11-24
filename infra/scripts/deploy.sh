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
APP_NAME="receiptfly"

echo "=========================================="
echo "Receiptfly インフラストラクチャ デプロイ"
echo "=========================================="
echo "環境名: $ENV"
echo "リージョン: $LOCATION"
echo "リソースグループ: $RESOURCE_GROUP"
echo "アプリ名: $APP_NAME"
echo ""

# Azure ログイン確認
echo "[$(date +%H:%M:%S)] Azure ログイン状態を確認中..."
if ! az account show > /dev/null 2>&1; then
    echo "[$(date +%H:%M:%S)] ❌ Azure にログインしていません"
    echo "  'az login' を実行してログインしてください"
    exit 1
fi

SUBSCRIPTION=$(az account show --query "{name:name, id:id}" -o tsv | head -1)
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
echo "[$(date +%H:%M:%S)] サブスクリプション: $SUBSCRIPTION"

# デプロイユーザーの Principal ID 取得
echo ""
echo "[$(date +%H:%M:%S)] デプロイユーザーの Principal ID を取得中..."
DEPLOY_USER_PRINCIPAL_ID=$(az ad signed-in-user show --query id -o tsv 2>/dev/null || echo "")
if [ -z "$DEPLOY_USER_PRINCIPAL_ID" ]; then
    echo "[$(date +%H:%M:%S)] ⚠️  デプロイユーザーの Principal ID を取得できませんでした"
    echo "  Key Vault への管理者権限は付与されません"
    DEPLOY_USER_PRINCIPAL_ID=""
fi
echo "[$(date +%H:%M:%S)] Principal ID: $DEPLOY_USER_PRINCIPAL_ID"

# リソースグループの確認・作成
echo ""
echo "[$(date +%H:%M:%S)] リソースグループを確認中..."
if az group show --name "$RESOURCE_GROUP" > /dev/null 2>&1; then
    echo "[$(date +%H:%M:%S)] リソースグループ '$RESOURCE_GROUP' は既に存在します"
else
    echo "[$(date +%H:%M:%S)] リソースグループ '$RESOURCE_GROUP' を作成中..."
    az group create --name "$RESOURCE_GROUP" --location "$LOCATION"
    echo "[$(date +%H:%M:%S)] ✓ リソースグループを作成しました"
fi

# デプロイ実行
echo ""
echo "[$(date +%H:%M:%S)] デプロイを開始します..."
DEPLOYMENT_NAME="receiptfly-deploy-$(date +%Y%m%d-%H%M%S)"

# スクリプトのディレクトリからmain.bicepへのパスを取得
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MAIN_BICEP="${SCRIPT_DIR}/../main.bicep"

az deployment group create \
    --resource-group "$RESOURCE_GROUP" \
    --name "$DEPLOYMENT_NAME" \
    --template-file "$MAIN_BICEP" \
    --parameters \
        appName="$APP_NAME" \
        env="$ENV" \
        location="$LOCATION" \
        deployUserPrincipalId="$DEPLOY_USER_PRINCIPAL_ID" \
    --output json

if [ $? -eq 0 ]; then
    echo ""
    echo "[$(date +%H:%M:%S)] デプロイが完了しました"
    echo ""
    echo "=========================================="
    echo "デプロイ結果"
    echo "=========================================="
    echo ""
    echo "作成されたリソースを確認するには、以下のコマンドを実行してください:"
    echo "  az resource list --resource-group $RESOURCE_GROUP --output table"
    echo ""
    echo "リソースの検証を実行するには、以下のコマンドを実行してください:"
    echo "  $(dirname "$0")/verify-deploy.sh $RESOURCE_GROUP"
    echo ""
else
    echo ""
    echo "[$(date +%H:%M:%S)] ❌ デプロイに失敗しました"
    exit 1
fi

