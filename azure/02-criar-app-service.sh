#!/usr/bin/env bash
# cria o App Service Plan (Linux, .NET 8, sem container) + o Web App
# e joga a connection string e os segredos como App Settings (nada disso no código)
set -euo pipefail
cd "$(dirname "$0")"
source ./00-variaveis.sh

az account set --subscription "$SUBSCRIPTION"

echo "==> Runtimes Linux disponíveis (conferir a string exata do .NET 8):"
az webapp list-runtimes --os linux -o table | grep -i dotnet || true

echo "==> Criando App Service Plan ($PLAN_NAME, SKU B1 Linux)..."
# usei B1 em vez do Free porque o Free tem cota de 60min de CPU/dia e não
# deixar ligado o Always On
az appservice plan create \
    --resource-group "$RG" \
    --name "$PLAN_NAME" \
    --location "$LOCATION" \
    --is-linux \
    --sku B1

echo "==> Criando Web App ($APP_NAME, runtime DOTNETCORE:8.0, sem container)..."
az webapp create \
    --resource-group "$RG" \
    --plan "$PLAN_NAME" \
    --name "$APP_NAME" \
    --runtime "DOTNETCORE:8.0"

az webapp config set \
    --resource-group "$RG" \
    --name "$APP_NAME" \
    --always-on true

echo "==> Configurando connection string e segredos como App Settings..."
# uso "ConnectionStrings__DefaultConnection" com "__" em vez do comando de
# connection-string do az mesmo, porque o IConfiguration do ASP.NET já lê
# variável de ambiente com "__" sem precisar de nenhum pacote a mais
az webapp config appsettings set \
    --resource-group "$RG" \
    --name "$APP_NAME" \
    --settings \
        ConnectionStrings__DefaultConnection="Server=$MYSQL_SERVER.mysql.database.azure.com;Port=3306;Database=$MYSQL_DB;Uid=$MYSQL_ADMIN;Pwd=$MYSQL_PASSWORD;SslMode=Required;" \
        ASPNETCORE_ENVIRONMENT="Production" \
        Api__ApiKey="${API_KEY:-TROQUE_ESTE_VALOR}" \
        Twilio__AccountSid="${TWILIO_ACCOUNT_SID:-}" \
        Twilio__AuthToken="${TWILIO_AUTH_TOKEN:-}" \
        Twilio__NumeroSandbox="whatsapp:+14155238886" \
        WhatsApp__ApiKey="${WHATSAPP_API_KEY:-}" \
        Telegram__BotToken="${TELEGRAM_BOT_TOKEN:-}" \
        Telegram__ApiKey="${TELEGRAM_API_KEY:-}" \
        Telegram__BotUsername="${TELEGRAM_BOT_USERNAME:-}"

echo "==> Web App criado: https://$APP_NAME.azurewebsites.net"
