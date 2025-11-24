#!/bin/bash
set -e

# 引数チェック
if [ $# -lt 1 ]; then
    echo "使用方法: $0 <リソースグループ名>"
    echo "例: $0 rg-receiptfly-dev"
    exit 1
fi

RESOURCE_GROUP=$1

echo "=========================================="
echo "Receiptfly インフラストラクチャ 検証"
echo "=========================================="
echo "リソースグループ: $RESOURCE_GROUP"
echo ""

# リソースグループの存在確認
if ! az group show --name "$RESOURCE_GROUP" > /dev/null 2>&1; then
    echo "❌ リソースグループ '$RESOURCE_GROUP' が見つかりません"
    exit 1
fi

# リソース一覧取得
echo "[$(date +%H:%M:%S)] リソース一覧を取得中..."
RESOURCES=$(az resource list --resource-group "$RESOURCE_GROUP" --output json)

# Storage Account の確認
echo ""
echo "=== Storage Account ==="
STORAGE_ACCOUNT=$(echo "$RESOURCES" | jq -r '.[] | select(.type == "Microsoft.Storage/storageAccounts") | .name' | head -1)
if [ -n "$STORAGE_ACCOUNT" ]; then
    echo "✅ Storage Account: $STORAGE_ACCOUNT"
    
    # Blob Container の確認
    CONTAINERS=$(az storage container list --account-name "$STORAGE_ACCOUNT" --auth-mode login --output json 2>/dev/null || echo "[]")
    RECEIPT_IMAGES_CONTAINER=$(echo "$CONTAINERS" | jq -r '.[] | select(.name == "receipt-images") | .name')
    if [ -n "$RECEIPT_IMAGES_CONTAINER" ]; then
        echo "  ✅ Blob Container 'receipt-images' が存在します"
    else
        echo "  ⚠️  Blob Container 'receipt-images' が見つかりません"
    fi
    
    # Table の確認
    TABLES=$(az storage table list --account-name "$STORAGE_ACCOUNT" --auth-mode login --output json 2>/dev/null || echo "[]")
    RECEIPTS_TABLE=$(echo "$TABLES" | jq -r '.[] | select(.name == "Receipts") | .name')
    if [ -n "$RECEIPTS_TABLE" ]; then
        echo "  ✅ Table 'Receipts' が存在します"
    else
        echo "  ⚠️  Table 'Receipts' が見つかりません"
    fi
else
    echo "❌ Storage Account が見つかりません"
fi

# Key Vault の確認
echo ""
echo "=== Key Vault ==="
KEY_VAULT=$(echo "$RESOURCES" | jq -r '.[] | select(.type == "Microsoft.KeyVault/vaults") | .name' | head -1)
if [ -n "$KEY_VAULT" ]; then
    echo "✅ Key Vault: $KEY_VAULT"
    
    # RBAC の確認
    RBAC_ENABLED=$(az keyvault show --name "$KEY_VAULT" --query "properties.enableRbacAuthorization" -o tsv 2>/dev/null || echo "false")
    if [ "$RBAC_ENABLED" = "true" ]; then
        echo "  ✅ RBAC が有効になっています"
    else
        echo "  ⚠️  RBAC が無効です"
    fi
else
    echo "❌ Key Vault が見つかりません"
fi

# Function Apps の確認
echo ""
echo "=== Function Apps ==="
FUNCTION_APPS=$(echo "$RESOURCES" | jq -r '.[] | select(.type == "Microsoft.Web/sites" and (.kind | contains("functionapp"))) | .name')
if [ -n "$FUNCTION_APPS" ]; then
    echo "$FUNCTION_APPS" | while read -r FUNC_APP; do
        echo "✅ Function App: $FUNC_APP"
        
        # Managed Identity の確認
        IDENTITY=$(az functionapp identity show --name "$FUNC_APP" --resource-group "$RESOURCE_GROUP" --output json 2>/dev/null || echo "{}")
        PRINCIPAL_ID=$(echo "$IDENTITY" | jq -r '.principalId // empty')
        if [ -n "$PRINCIPAL_ID" ]; then
            echo "  ✅ Managed Identity が有効です (Principal ID: $PRINCIPAL_ID)"
        else
            echo "  ⚠️  Managed Identity が無効です"
        fi
        
        # App Settings の確認
        APP_SETTINGS=$(az functionapp config appsettings list --name "$FUNC_APP" --resource-group "$RESOURCE_GROUP" --output json 2>/dev/null || echo "[]")
        KEY_VAULT_REF=$(echo "$APP_SETTINGS" | jq -r '.[] | select(.name | contains("ApiKey")) | .value' | head -1)
        if echo "$KEY_VAULT_REF" | grep -q "@Microsoft.KeyVault"; then
            echo "  ✅ Key Vault 参照が設定されています"
        else
            echo "  ⚠️  Key Vault 参照が見つかりません"
        fi
    done
else
    echo "❌ Function App が見つかりません"
fi

# Static Web App の確認
echo ""
echo "=== Static Web App ==="
STATIC_WEB_APP=$(echo "$RESOURCES" | jq -r '.[] | select(.type == "Microsoft.Web/staticSites") | .name' | head -1)
if [ -n "$STATIC_WEB_APP" ]; then
    echo "✅ Static Web App: $STATIC_WEB_APP"
    DEFAULT_HOSTNAME=$(az staticwebapp show --name "$STATIC_WEB_APP" --resource-group "$RESOURCE_GROUP" --query "defaultHostname" -o tsv 2>/dev/null || echo "")
    if [ -n "$DEFAULT_HOSTNAME" ]; then
        echo "  ✅ Default Hostname: https://$DEFAULT_HOSTNAME"
    fi
else
    echo "❌ Static Web App が見つかりません"
fi

# Application Insights の確認
echo ""
echo "=== Application Insights ==="
APP_INSIGHTS=$(echo "$RESOURCES" | jq -r '.[] | select(.type == "Microsoft.Insights/components") | .name' | head -1)
if [ -n "$APP_INSIGHTS" ]; then
    echo "✅ Application Insights: $APP_INSIGHTS"
else
    echo "❌ Application Insights が見つかりません"
fi

# Log Analytics Workspace の確認
echo ""
echo "=== Log Analytics Workspace ==="
LOG_WORKSPACE=$(echo "$RESOURCES" | jq -r '.[] | select(.type == "Microsoft.OperationalInsights/workspaces") | .name' | head -1)
if [ -n "$LOG_WORKSPACE" ]; then
    echo "✅ Log Analytics Workspace: $LOG_WORKSPACE"
else
    echo "❌ Log Analytics Workspace が見つかりません"
fi

echo ""
echo "=========================================="
echo "検証完了"
echo "=========================================="

