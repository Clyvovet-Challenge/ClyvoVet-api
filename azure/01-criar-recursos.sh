#!/usr/bin/env bash
# cria o resource group + o MySQL Flexible Server na Azure via CLI
set -euo pipefail
cd "$(dirname "$0")"
source ./00-variaveis.sh

az account set --subscription "$SUBSCRIPTION"

echo "==> Criando Resource Group $RG em $RG_LOCATION..."
az group create \
    --name "$RG" \
    --location "$RG_LOCATION"

echo "==> SKUs Burstable disponíveis em $LOCATION (conferir antes de criar):"
az mysql flexible-server list-skus --location "$LOCATION" -o table || true

echo "==> Criando Azure Database for MySQL Flexible Server ($MYSQL_SERVER)..."
az mysql flexible-server create \
    --resource-group "$RG" \
    --name "$MYSQL_SERVER" \
    --location "$LOCATION" \
    --admin-user "$MYSQL_ADMIN" \
    --admin-password "$MYSQL_PASSWORD" \
    --sku-name Standard_B1ms \
    --tier Burstable \
    --storage-size 32 \
    --version 8.0.21 \
    --database-name "$MYSQL_DB" \
    --public-access 0.0.0.0 \
    --yes

echo "==> Liberando acesso do seu IP atual no firewall (para rodar o script_bd.sql via mysql cli)..."
# tem que ser IPv4, o firewall do MySQL Flexible Server nao aceita IPv6 e minha rede usa IPv6 por padrao
MY_IP="$(curl -4 -s https://api.ipify.org)"
az mysql flexible-server firewall-rule create \
    --resource-group "$RG" \
    --name "$MYSQL_SERVER" \
    --rule-name AllowMyIP \
    --start-ip-address "$MY_IP" \
    --end-ip-address "$MY_IP"

echo "==> Recursos de banco criados. Host: $MYSQL_SERVER.mysql.database.azure.com"
echo "==> Próximo passo: aplicar o schema/script_bd.sql contra esse servidor, ex:"
echo "    mysql -h $MYSQL_SERVER.mysql.database.azure.com -u $MYSQL_ADMIN -p$MYSQL_PASSWORD --ssl-mode=REQUIRED $MYSQL_DB < ../schema/script_bd.sql"
