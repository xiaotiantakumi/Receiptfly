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
echo "Receiptfly Static Web App バックエンドAPI デプロイ"
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
API_FUNCTION_DIR="${PROJECT_ROOT}/backend/Receiptfly.Functions"
FRONTEND_DIR="${PROJECT_ROOT}/frontend/receiptfly-web"

# プロジェクトディレクトリの存在確認
if [ ! -d "$API_FUNCTION_DIR" ]; then
    echo "[$(date +%H:%M:%S)] ❌ API Function App のディレクトリが見つかりません: $API_FUNCTION_DIR"
    exit 1
fi

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

# API Function App をビルド
echo ""
echo "[$(date +%H:%M:%S)] API Function App をビルド中..."
cd "$API_FUNCTION_DIR"

# 一時的なapiフォルダを作成
API_BUILD_DIR="${FRONTEND_DIR}/api"
rm -rf "$API_BUILD_DIR"
mkdir -p "$API_BUILD_DIR"

# Function Appをビルドしてapiフォルダに配置
dotnet publish --configuration Release --output "$API_BUILD_DIR" /p:PublishSingleFile=false /p:IncludeNativeLibrariesForSelfExtract=true

if [ $? -ne 0 ]; then
    echo "[$(date +%H:%M:%S)] ❌ API Function App のビルドに失敗しました"
    exit 1
fi

# host.jsonをapiフォルダにコピー
cp "${API_FUNCTION_DIR}/host.json" "${API_BUILD_DIR}/host.json"

# staticwebapp.config.jsonをapiフォルダにもコピー（API統合に必要）
if [ -f "${FRONTEND_DIR}/staticwebapp.config.json" ]; then
    cp "${FRONTEND_DIR}/staticwebapp.config.json" "${API_BUILD_DIR}/staticwebapp.config.json"
    echo "[$(date +%H:%M:%S)] staticwebapp.config.jsonをapiフォルダにコピーしました"
fi

echo "[$(date +%H:%M:%S)] ✅ API Function App のビルドが完了しました"

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

# Static Web AppのバックエンドAPIとしてデプロイするため、APIベースURLは/apiを使用
echo "[$(date +%H:%M:%S)] API Base URL: /api (Static Web AppのバックエンドAPIとして統合)"

# 環境変数を設定してビルド実行
# Static Web AppのバックエンドAPIとして統合するため、/apiを使用
export VITE_API_BASE_URL="/api"
npm run build

if [ $? -ne 0 ]; then
    echo "[$(date +%H:%M:%S)] ❌ Frontend のビルドに失敗しました"
    exit 1
fi

# staticwebapp.config.jsonをdistフォルダにコピー（API統合に必要）
if [ -f "${FRONTEND_DIR}/staticwebapp.config.json" ]; then
    cp "${FRONTEND_DIR}/staticwebapp.config.json" "${FRONTEND_DIR}/dist/staticwebapp.config.json"
    echo "[$(date +%H:%M:%S)] staticwebapp.config.jsonをdistフォルダにコピーしました"
fi

# Static Web App にデプロイ（APIを含む）
echo ""
echo "[$(date +%H:%M:%S)] Static Web App にデプロイ中（API統合）..."

# swa-cli.config.json を使わず、明示的にすべてのパラメータを指定
# 注意: FRONTEND_DIR から実行する必要がある（apiLocation が相対パスのため）
# --api-language と --api-version を明示的に指定（swa deploy のヘルプで確認済み）
swa deploy \
    --app-location dist \
    --api-location api \
    --api-language dotnet-isolated \
    --api-version 8.0 \
    --deployment-token "$DEPLOYMENT_TOKEN" \
    --env production \
    --no-use-keychain

if [ $? -ne 0 ]; then
    echo "[$(date +%H:%M:%S)] ❌ Static Web App のデプロイに失敗しました"
    exit 1
fi

# apiフォルダをクリーンアップ（デプロイ完了後）
# 注意: デプロイが成功した場合のみ削除
if [ $? -eq 0 ]; then
    rm -rf "$API_BUILD_DIR"
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
echo "API Endpoint: https://${DEFAULT_HOSTNAME}/api"
echo ""

