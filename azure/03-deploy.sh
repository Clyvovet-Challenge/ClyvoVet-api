#!/usr/bin/env bash
# publica o app (dotnet publish, sem docker) e sobe via zip no Web App
# que o 02-criar-app-service.sh já criou
set -euo pipefail
cd "$(dirname "$0")"
source ./00-variaveis.sh

az account set --subscription "$SUBSCRIPTION"

PROJECT_DIR="../ClyvoVet.Api"
PUBLISH_DIR="$PROJECT_DIR/publish"
ZIP_PATH="$PROJECT_DIR/publish.zip"

echo "==> dotnet publish..."
rm -rf "$PUBLISH_DIR" "$ZIP_PATH"
dotnet publish "$PROJECT_DIR/ClyvoVet.Api.csproj" -c Release -o "$PUBLISH_DIR" /p:UseAppHost=false

echo "==> Empacotando zip..."
(cd "$PUBLISH_DIR" && zip -r "../publish.zip" . -x "*.pdb")

echo "==> Deploy via zip (az webapp deploy)..."
az webapp deploy \
    --resource-group "$RG" \
    --name "$APP_NAME" \
    --src-path "$ZIP_PATH" \
    --type zip

echo "==> Deploy concluído. Validando..."
HOST=$(az webapp show --resource-group "$RG" --name "$APP_NAME" --query defaultHostName -o tsv)
echo "==> URL: https://$HOST"
echo "==> Swagger: https://$HOST/swagger"
echo "==> Health: https://$HOST/health"
echo
echo "Para acompanhar logs em tempo real:"
echo "    az webapp log tail --resource-group $RG --name $APP_NAME"
