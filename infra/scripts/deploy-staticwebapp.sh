#!/bin/bash
set -e

# 引数チェック
if [ $# -lt 2 ]; then
    echo "使用方法: $0 <環境名> <リソースグループ名>"
    echo "例: $0 dev rg-receiptfly-dev"
    exit 1
fi

ENV=$1
RESOURCE_GROUP=$2
APP_NAME="receiptfly"

echo "=========================================="
echo "Receiptfly Static Web App デプロイ"
echo "=========================================="
echo "環境名: $ENV"
echo "リソースグループ: $RESOURCE_GROUP"
echo ""

# Azure ログイン確認
echo "[$(date +%H:%M:%S)] Azure ログイン状態を確認中..."
if ! az account show > /dev/null 2>&1; then
    echo "[$(date +%H:%M:%S)] ❌ Azure にログインしていません"
    echo "  'az login' を実行してログインしてください"
    exit 1
fi

# Static Web App名を取得
STATIC_WEB_APP_NAME="swa-${APP_NAME}-${ENV}-001"

echo "[$(date +%H:%M:%S)] Static Web App: $STATIC_WEB_APP_NAME"
echo ""

# Azure Static Web Apps CLI の確認
if ! command -v swa &> /dev/null; then
    echo "[$(date +%H:%M:%S)] ❌ Azure Static Web Apps CLI がインストールされていません"
    echo "  インストール方法: npm install -g @azure/static-web-apps-cli"
    exit 1
fi

# プロジェクトディレクトリのパスを取得
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
FRONTEND_DIR="${PROJECT_ROOT}/frontend/receiptfly-web"

# プロジェクトディレクトリの存在確認
if [ ! -d "$FRONTEND_DIR" ]; then
    echo "[$(date +%H:%M:%S)] ❌ Frontend のディレクトリが見つかりません: $FRONTEND_DIR"
    exit 1
fi

# Static Web App のリソースIDを取得
STATIC_WEB_APP_ID=$(az staticwebapp show \
    --name "$STATIC_WEB_APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --query id \
    -o tsv 2>/dev/null || echo "")

if [ -z "$STATIC_WEB_APP_ID" ]; then
    echo "[$(date +%H:%M:%S)] ❌ Static Web App '$STATIC_WEB_APP_NAME' が見つかりません"
    echo "  インフラストラクチャのデプロイを先に実行してください"
    exit 1
fi

# Deployment token を取得
echo "[$(date +%H:%M:%S)] Deployment token を取得中..."
DEPLOYMENT_TOKEN=$(az staticwebapp secrets list \
    --name "$STATIC_WEB_APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --query "properties.apiKey" \
    -o tsv 2>/dev/null || echo "")

if [ -z "$DEPLOYMENT_TOKEN" ]; then
    echo "[$(date +%H:%M:%S)] ⚠️  Deployment token を取得できませんでした"
    echo "  Azure Portal から手動で取得するか、リソースの権限を確認してください"
    exit 1
fi

# Frontend をビルド
echo ""
echo "[$(date +%H:%M:%S)] Frontend をビルド中..."
cd "$FRONTEND_DIR"

if [ ! -f "package.json" ]; then
    echo "[$(date +%H:%M:%S)] ❌ package.json が見つかりません"
    exit 1
fi

# 依存関係のインストール（必要に応じて）
if [ ! -d "node_modules" ]; then
    echo "[$(date +%H:%M:%S)] 依存関係をインストール中..."
    npm install
fi

# API Function AppのURLを取得
API_FUNCTION_APP_NAME="func-${APP_NAME}-api-${ENV}-001"
API_BASE_URL="https://${API_FUNCTION_APP_NAME}.azurewebsites.net/api"

echo "[$(date +%H:%M:%S)] API Base URL: $API_BASE_URL"

# 環境変数を設定してビルド実行
# 注意: この環境変数は .env ファイルの値を上書きします
# ローカル開発時は .env ファイルの値（localhost）が使用されます
export VITE_API_BASE_URL="$API_BASE_URL"
npm run build

if [ $? -ne 0 ]; then
    echo "[$(date +%H:%M:%S)] ❌ Frontend のビルドに失敗しました"
    exit 1
fi

# Static Web App にデプロイ
echo ""
echo "[$(date +%H:%M:%S)] Static Web App にデプロイ中..."

# Function Appsは別途デプロイ済みのため、API統合は不要
# Frontendのみをデプロイ（distフォルダを直接指定）
swa deploy dist \
    --deployment-token "$DEPLOYMENT_TOKEN" \
    --env production \
    --no-use-keychain

if [ $? -ne 0 ]; then
    echo "[$(date +%H:%M:%S)] ❌ Static Web App のデプロイに失敗しました"
    exit 1
fi

# Default hostname を取得
DEFAULT_HOSTNAME=$(az staticwebapp show \
    --name "$STATIC_WEB_APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --query "defaultHostname" \
    -o tsv)

echo ""
echo "=========================================="
echo "デプロイ完了"
echo "=========================================="
echo ""
echo "Static Web App URL: https://${DEFAULT_HOSTNAME}"
echo ""

