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
echo "Receiptfly Function Apps デプロイ"
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

# Function App名を取得
# 注意: API Function AppはStatic Web AppのバックエンドAPIとして統合されました
# このスクリプトではProcessing Function Appのみをデプロイします
PROCESSING_FUNCTION_APP_NAME="func-${APP_NAME}-processing-${ENV}-001"

echo "[$(date +%H:%M:%S)] Processing Function App: $PROCESSING_FUNCTION_APP_NAME"
echo "[$(date +%H:%M:%S)] 注意: API Function AppはStatic Web AppのバックエンドAPIとして統合されています"
echo ""

# Azure Functions Core Tools の確認
if ! command -v func &> /dev/null; then
    echo "[$(date +%H:%M:%S)] ❌ Azure Functions Core Tools がインストールされていません"
    echo "  インストール方法: https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local"
    exit 1
fi

# プロジェクトディレクトリのパスを取得
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
PROCESSING_FUNCTION_DIR="${PROJECT_ROOT}/backend/Receiptfly.ProcessingFunc"

# プロジェクトディレクトリの存在確認
if [ ! -d "$PROCESSING_FUNCTION_DIR" ]; then
    echo "[$(date +%H:%M:%S)] ❌ Processing Function App のディレクトリが見つかりません: $PROCESSING_FUNCTION_DIR"
    exit 1
fi

# 注意: API Function AppはStatic Web AppのバックエンドAPIとして統合されています
# このスクリプトではProcessing Function Appのみをデプロイします

# Processing Function App のデプロイ
echo ""
echo "[$(date +%H:%M:%S)] Processing Function App をビルド中..."
cd "$PROCESSING_FUNCTION_DIR"
dotnet build --configuration Release

if [ $? -ne 0 ]; then
    echo "[$(date +%H:%M:%S)] ❌ Processing Function App のビルドに失敗しました"
    exit 1
fi

echo ""
echo "[$(date +%H:%M:%S)] Processing Function App をデプロイ中..."
func azure functionapp publish "$PROCESSING_FUNCTION_APP_NAME" --dotnet-isolated

if [ $? -ne 0 ]; then
    echo "[$(date +%H:%M:%S)] ❌ Processing Function App のデプロイに失敗しました"
    exit 1
fi

echo "[$(date +%H:%M:%S)] ✅ Processing Function App のデプロイが完了しました"

echo ""
echo "=========================================="
echo "デプロイ完了"
echo "=========================================="
echo ""
echo "デプロイされた Function Apps:"
echo "  - Processing: https://${PROCESSING_FUNCTION_APP_NAME}.azurewebsites.net"
echo ""
echo "注意: API Function AppはStatic Web AppのバックエンドAPIとして統合されています"
echo "      Static Web Appのデプロイ時に一緒にデプロイされます"
echo ""

