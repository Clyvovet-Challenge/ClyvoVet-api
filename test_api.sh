#!/usr/bin/env bash
# Roteiro ponta a ponta da API inteira contra uma instância no ar (ensaio geral).
#
# Uso:
#   API_URL=http://localhost:5191 API_KEY=... TOKEN=... TOKEN_ADMIN=... ./test_api.sh
#
#   TOKEN        access token de um TUTOR (é ele que cria lembretes e sugestões para o próprio animal)
#   TOKEN_ADMIN  access token de ADMIN ou VETERINARIO (escrita de produtos); sem ele, usa o TOKEN
#   API_KEY      a Api:ApiKey da API
#   ANIMAL_ID    animal do tutor do TOKEN (padrão: o primeiro das sugestões dele)
#   Opcionais, ligam checagens extras: TUTOR_ID (id do tutor do TOKEN), OUTRO_ANIMAL_ID
#   (animal de outro tutor), OUTRO_TUTOR_ID, TOKEN_REFRESH (um refresh token).
#
# Os tokens vêm do login da API Java; esta API não emite token.
API_URL="${API_URL:-http://localhost:5191}"
BASE="$API_URL/api/v1"
TOKEN_ADMIN="${TOKEN_ADMIN:-$TOKEN}"
pass=0; fail=0; pulado=0

# ── helpers ───────────────────────────────────────────────────────
chk() {
  local label="$1" got="$2" expect="$3" detail="${4:-}"
  if [ "$got" -eq "$expect" ] 2>/dev/null; then
    printf "[PASS] %-52s %s%s\n" "$label" "$got" "${detail:+  | $detail}"
    ((pass++))
  else
    printf "[FAIL] %-52s %s (esperado %s)%s\n" "$label" "$got" "$expect" "${detail:+  | $detail}"
    [ -n "$body" ] && printf "       corpo: %.200s\n" "$body"
    ((fail++))
  fi
}

pula() { printf "[SKIP] %-52s %s\n" "$1" "$2"; ((pulado++)); }

# Toda chamada leva o Bearer e a X-Api-Key; os helpers *_ADM usam o token da equipe.
AUTH=(-H "Authorization: Bearer $TOKEN" -H "X-Api-Key: $API_KEY")
AUTH_ADM=(-H "Authorization: Bearer $TOKEN_ADMIN" -H "X-Api-Key: $API_KEY")
GET()    { curl -s -w '\n%{http_code}' "${AUTH[@]}" "$BASE/$1"; }
POST()   { curl -s -w '\n%{http_code}' "${AUTH[@]}" -X POST   "$BASE/$1" -H "Content-Type: application/json" -d "$2"; }
PUT()    { curl -s -w '\n%{http_code}' "${AUTH[@]}" -X PUT    "$BASE/$1" -H "Content-Type: application/json" -d "$2"; }
DELETE() { curl -s -w '\n%{http_code}' "${AUTH[@]}" -X DELETE "$BASE/$1"; }
POST_ADM()   { curl -s -w '\n%{http_code}' "${AUTH_ADM[@]}" -X POST   "$BASE/$1" -H "Content-Type: application/json" -d "$2"; }
PUT_ADM()    { curl -s -w '\n%{http_code}' "${AUTH_ADM[@]}" -X PUT    "$BASE/$1" -H "Content-Type: application/json" -d "$2"; }
DELETE_ADM() { curl -s -w '\n%{http_code}' "${AUTH_ADM[@]}" -X DELETE "$BASE/$1"; }

# sed '$d' e não head -n -1: o head do macOS não aceita contagem negativa.
split_resp() { body=$(printf '%s\n' "$1" | sed '$d'); status=$(printf '%s\n' "$1" | tail -1); }
jq_f() { echo "$1" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d.get('$2',''))" 2>/dev/null || true; }
jq_len() { echo "$1" | python3 -c "import sys,json; d=json.load(sys.stdin); print(len(d) if isinstance(d,list) else '?')" 2>/dev/null || echo "?"; }

# Datas sempre no futuro: um roteiro com data fixa passa a falhar sozinho quando ela chega.
FUTURO=$(python3 -c "import datetime; print(datetime.date.today()+datetime.timedelta(days=30))")
FUTURO2=$(python3 -c "import datetime; print(datetime.date.today()+datetime.timedelta(days=60))")

[ -n "$TOKEN" ] && [ -n "$API_KEY" ] || { echo "Defina TOKEN e API_KEY (veja o cabeçalho do script)."; exit 2; }

# ── PRODUTOS ─────────────────────────────────────────────────────
echo ""
echo "══════════════════════════════════════════════════════"
echo "  🛒  PRODUTOS"
echo "══════════════════════════════════════════════════════"

split_resp "$(GET "produtos")"
chk "GET /produtos" "$status" 200 "$(jq_len "$body") registros"

split_resp "$(GET "produtos?categoria=Racao")";        chk "GET /produtos?categoria=Racao"      "$status" 200
split_resp "$(GET "produtos?especieIndicada=Gato")";   chk "GET /produtos?especieIndicada=Gato" "$status" 200
split_resp "$(GET "produtos?page=0")";                 chk "GET /produtos?page=0 [400]"         "$status" 400
split_resp "$(GET "produtos?pageSize=200")";           chk "GET /produtos?pageSize=200 [400]"   "$status" 400

PROD_BODY='{"nome":"Shampoo Pet Neutro 500ml","descricao":"Hipoalergenico.","categoria":2,"preco":28.90,"especieIndicada":5,"ativo":true}'
split_resp "$(POST_ADM "produtos" "$PROD_BODY")"
chk "POST /produtos [201]" "$status" 201
PROD_ID=$(jq_f "$body" "id")
echo "     produto_id criado: $PROD_ID"

split_resp "$(GET "produtos/$PROD_ID")";               chk "GET /produtos/{id} [200]"           "$status" 200

PROD_UPD='{"nome":"Shampoo Pet Neutro 1L","descricao":"Versao 1L.","categoria":2,"preco":49.90,"especieIndicada":5,"ativo":true}'
split_resp "$(PUT_ADM "produtos/$PROD_ID" "$PROD_UPD")";   chk "PUT /produtos/{id} [200]"           "$status" 200

split_resp "$(GET "produtos/id-nao-existe-xxx")";      chk "GET /produtos/invalido [404]"       "$status" 404
split_resp "$(POST_ADM "produtos" '{"nome":"X","categoria":0,"preco":-50,"especieIndicada":0,"ativo":true}')";
                                                       chk "POST /produtos preco=-50 [400]"     "$status" 400
split_resp "$(POST_ADM "produtos" '{"categoria":0,"preco":10,"especieIndicada":0,"ativo":true}')";
                                                       chk "POST /produtos sem nome [400]"      "$status" 400

# ── EVENTOS PET ──────────────────────────────────────────────────
echo ""
echo "══════════════════════════════════════════════════════"
echo "  🐾  EVENTOS PET"
echo "══════════════════════════════════════════════════════"

split_resp "$(GET "eventos-pet")"
chk "GET /eventos-pet" "$status" 200 "$(jq_len "$body") registros"

split_resp "$(GET "eventos-pet?cidade=Sao+Paulo")";    chk "GET /eventos-pet?cidade=Sao Paulo"  "$status" 200
split_resp "$(GET "eventos-pet?tipo=Vacinacao")";      chk "GET /eventos-pet?tipo=Vacinacao"    "$status" 200
split_resp "$(GET "eventos-pet?especieAlvo=Todos")";   chk "GET /eventos-pet?especieAlvo=Todos" "$status" 200
split_resp "$(GET "eventos-pet?page=0")";              chk "GET /eventos-pet?page=0 [400]"      "$status" 400

EV_BODY='{"titulo":"Workshop Nutricao Pet","tipo":3,"rua":"Rua das Acacias","numero":"500","bairro":"Jardins","cidade":"Sao Paulo","estado":"SP","cep":"01425-000","dataInicio":"'"$FUTURO"'","dataFim":"'"$FUTURO"'","especieAlvo":0,"organizador":"Dr. Pet","gratuito":false,"ativo":true}'
split_resp "$(POST "eventos-pet" "$EV_BODY")"
chk "POST /eventos-pet [201]" "$status" 201
EVENTO_ID=$(jq_f "$body" "id")
echo "     evento_id criado: $EVENTO_ID"

split_resp "$(GET "eventos-pet/$EVENTO_ID")";          chk "GET /eventos-pet/{id} [200]"        "$status" 200

EV_UPD='{"titulo":"Workshop Atualizado","tipo":3,"cidade":"Sao Paulo","estado":"SP","dataInicio":"'"$FUTURO"'","especieAlvo":0,"gratuito":true,"ativo":true}'
split_resp "$(PUT "eventos-pet/$EVENTO_ID" "$EV_UPD")"; chk "PUT /eventos-pet/{id} [200]"       "$status" 200

split_resp "$(POST "eventos-pet" '{"titulo":"Ev Passado","tipo":0,"dataInicio":"2020-01-01","especieAlvo":5,"gratuito":true,"ativo":true}')";
                                                        chk "POST /eventos-pet data passada [400]" "$status" 400
split_resp "$(POST "eventos-pet" '{"tipo":0,"dataInicio":"'"$FUTURO2"'","gratuito":true,"ativo":true}')";
                                                        chk "POST /eventos-pet sem titulo [400]"   "$status" 400
split_resp "$(GET "eventos-pet/id-nao-existe-xxx")";    chk "GET /eventos-pet/invalido [404]"      "$status" 404

# ── LEMBRETES ────────────────────────────────────────────────────
echo ""
echo "══════════════════════════════════════════════════════"
echo "  🔔  LEMBRETES"
echo "══════════════════════════════════════════════════════"

split_resp "$(GET "lembretes")"
chk "GET /lembretes" "$status" 200 "$(jq_len "$body") registros"
if [ -z "$ANIMAL_ID" ]; then
  ANIMAL_ID=$(echo "$body" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d[0]['animalId'] if d else '')" 2>/dev/null || true)
fi
if [ -z "$ANIMAL_ID" ]; then
  ANIMAL_ID=$(GET "sugestoes-produto" | sed '$d' | python3 -c "import sys,json; d=json.load(sys.stdin); print(d[0]['animalId'] if d else '')" 2>/dev/null || true)
fi
echo "     animal_id usado: $ANIMAL_ID"

split_resp "$(GET "lembretes?status=Pendente")";       chk "GET /lembretes?status=Pendente"     "$status" 200
split_resp "$(GET "lembretes?tipo=Vacina")";           chk "GET /lembretes?tipo=Vacina"         "$status" 200
split_resp "$(GET "lembretes?animalId=$ANIMAL_ID")";   chk "GET /lembretes?animalId=..."        "$status" 200
split_resp "$(GET "lembretes?page=-1")";               chk "GET /lembretes?page=-1 [400]"       "$status" 400

LEM_BODY="{\"animalId\":\"$ANIMAL_ID\",\"titulo\":\"Consulta de Rotina\",\"descricao\":\"Checkup anual.\",\"tipo\":2,\"agendadoEm\":\"${FUTURO}T14:00:00\",\"recorrente\":false,\"status\":0}"
split_resp "$(POST "lembretes" "$LEM_BODY")"
chk "POST /lembretes [201]" "$status" 201
LEM_ID=$(jq_f "$body" "id")
echo "     lembrete_id criado: $LEM_ID"

split_resp "$(GET "lembretes/$LEM_ID")";               chk "GET /lembretes/{id} [200]"          "$status" 200

STATUS_CRIADO=$(jq_f "$body" "status")
chk "Status forcado = Pendente(0) na criacao" "$([ "$STATUS_CRIADO" = "0" ] && echo 0 || echo 1)" 0 "status=$STATUS_CRIADO"

LEM_UPD="{\"animalId\":\"$ANIMAL_ID\",\"titulo\":\"Consulta de Rotina\",\"tipo\":2,\"agendadoEm\":\"${FUTURO}T14:00:00\",\"recorrente\":false,\"status\":1}"
split_resp "$(PUT "lembretes/$LEM_ID" "$LEM_UPD")";    chk "PUT /lembretes/{id} status->Enviado [200]"  "$status" 200

LEM_PASSADO="{\"animalId\":\"$ANIMAL_ID\",\"titulo\":\"X\",\"tipo\":0,\"agendadoEm\":\"2020-01-01T10:00:00\",\"recorrente\":false,\"status\":0}"
split_resp "$(PUT "lembretes/$LEM_ID" "$LEM_PASSADO")"; chk "PUT /lembretes agendadoEm passado [400]"   "$status" 400

split_resp "$(POST "lembretes" '{"animalId":"00000000-0000-0000-0000-000000000000","titulo":"Lembrete teste","tipo":0,"agendadoEm":"'"$FUTURO2"'T10:00:00","recorrente":false}')";
                                                         chk "POST /lembretes animalId invalido [404]"  "$status" 404
split_resp "$(POST "lembretes" '{"titulo":"X","tipo":0,"agendadoEm":"'"$FUTURO2"'T10:00:00","recorrente":false}')";
                                                         chk "POST /lembretes sem animalId [400]"       "$status" 400
split_resp "$(GET "lembretes/id-nao-existe-xxx")";       chk "GET /lembretes/invalido [404]"            "$status" 404

# ── SUGESTÕES ────────────────────────────────────────────────────
echo ""
echo "══════════════════════════════════════════════════════"
echo "  💡  SUGESTÕES DE PRODUTO"
echo "══════════════════════════════════════════════════════"

split_resp "$(GET "sugestoes-produto")"
chk "GET /sugestoes-produto" "$status" 200 "$(jq_len "$body") registros"

split_resp "$(GET "sugestoes-produto?animalId=$ANIMAL_ID")"; chk "GET /sugestoes-produto?animalId=..."  "$status" 200
split_resp "$(GET "sugestoes-produto?pageSize=999")";        chk "GET /sugestoes-produto?pS=999 [400]"  "$status" 400

PROD_SEED=$(GET "produtos?pageSize=1" | sed '$d' | python3 -c "import sys,json; d=json.load(sys.stdin); print(d[0]['id'] if d else '')" 2>/dev/null || true)
echo "     produto_id do seed: $PROD_SEED"

SUG_BODY="{\"animalId\":\"$ANIMAL_ID\",\"produtoId\":\"$PROD_SEED\",\"justificativa\":\"Animal com baixa imunidade.\",\"dataSugestao\":\"2026-05-24\",\"ativo\":true}"
split_resp "$(POST "sugestoes-produto" "$SUG_BODY")"
chk "POST /sugestoes-produto [201]" "$status" 201
SUG_ID=$(jq_f "$body" "id")
echo "     sugestao_id criada: $SUG_ID"

split_resp "$(GET "sugestoes-produto/$SUG_ID")"
chk "GET /sugestoes-produto/{id} [200]" "$status" 200
NOME_ANIMAL=$(jq_f "$body" "nomeAnimal"); NOME_PROD=$(jq_f "$body" "nomeProduto")
chk "Response tem nomeAnimal e nomeProduto" "$([ -n "$NOME_ANIMAL" ] && [ -n "$NOME_PROD" ] && echo 0 || echo 1)" 0 "animal='$NOME_ANIMAL' produto='$NOME_PROD'"

SUG_UPD="{\"animalId\":\"$ANIMAL_ID\",\"produtoId\":\"$PROD_SEED\",\"justificativa\":\"Atualizado.\",\"dataSugestao\":\"2026-05-24\",\"ativo\":false}"
split_resp "$(PUT "sugestoes-produto/$SUG_ID" "$SUG_UPD")";  chk "PUT /sugestoes-produto/{id} [200]"    "$status" 200

# JSON com vírgulas e variáveis vai numa variável antes: dentro de "$(… "{a,b}" …)" o bash 3.2
# do macOS faz brace expansion e o curl recebe só um pedaço do corpo.
SUG_PROD_INV="{\"animalId\":\"$ANIMAL_ID\",\"produtoId\":\"00000000-0000-0000-0000-000000000000\",\"ativo\":true}"
SUG_ANIM_INV="{\"animalId\":\"00000000-0000-0000-0000-000000000000\",\"produtoId\":\"$PROD_SEED\",\"ativo\":true}"
split_resp "$(POST "sugestoes-produto" "$SUG_PROD_INV")";
                                                              chk "POST /sugestoes produtoId invalido [404]" "$status" 404
split_resp "$(POST "sugestoes-produto" "$SUG_ANIM_INV")";
                                                              chk "POST /sugestoes animalId invalido [404]"  "$status" 404
split_resp "$(POST "sugestoes-produto" '{"justificativa":"x","ativo":true}')";
                                                              chk "POST /sugestoes sem IDs [400]"            "$status" 400
split_resp "$(GET "sugestoes-produto/id-nao-existe-xxx")";    chk "GET /sugestoes-produto/invalido [404]"    "$status" 404

# O1: um PUT sem dataSugestao mantém a data que já estava gravada.
SUG_SEM_DATA="{\"animalId\":\"$ANIMAL_ID\",\"produtoId\":\"$PROD_SEED\",\"justificativa\":\"Sem data.\",\"ativo\":true}"
split_resp "$(PUT "sugestoes-produto/$SUG_ID" "$SUG_SEM_DATA")"
DATA_APOS=$(jq_f "$body" "dataSugestao")
chk "PUT sugestao sem data mantem 2026-05-24" "$([ "$DATA_APOS" = "2026-05-24" ] && echo 0 || echo 1)" 0 "dataSugestao=$DATA_APOS"

# ── SPRINT 4: autenticação, HATEOAS, ordenação, saúde, Telegram ──
echo ""
echo "══════════════════════════════════════════════════════"
echo "  🔐  SPRINT 4"
echo "══════════════════════════════════════════════════════"

st() { curl -s -o /dev/null -w '%{http_code}' "$@"; }
chk "GET /produtos sem Bearer [401]"          "$(st -H "X-Api-Key: $API_KEY" "$BASE/produtos")" 401
chk "GET /produtos sem X-Api-Key [401]"       "$(st -H "Authorization: Bearer $TOKEN" "$BASE/produtos")" 401
chk "GET /produtos token invalido [401]"      "$(st -H "Authorization: Bearer x.y.z" -H "X-Api-Key: $API_KEY" "$BASE/produtos")" 401
if [ -n "$TOKEN_REFRESH" ]; then
  chk "GET /produtos com refresh token [401]" "$(st -H "Authorization: Bearer $TOKEN_REFRESH" -H "X-Api-Key: $API_KEY" "$BASE/produtos")" 401
else pula "refresh token recusado" "(defina TOKEN_REFRESH)"; fi
if [ "$TOKEN_ADMIN" != "$TOKEN" ]; then
  chk "POST /produtos como TUTOR [403]" "$(st "${AUTH[@]}" -X POST -H "Content-Type: application/json" -d "$PROD_BODY" "$BASE/produtos")" 403
else pula "POST /produtos como TUTOR [403]" "(defina TOKEN_ADMIN)"; fi

HDR=$(curl -s -D - -o /dev/null "${AUTH[@]}" "$BASE/produtos?pageSize=2")
chk "Header X-Total-Count"    "$(echo "$HDR" | grep -qi '^x-total-count:' && echo 0 || echo 1)" 0 "$(echo "$HDR" | grep -i '^x-total-count:' | tr -d '\r')"
chk "Header Link (rel=next)"  "$(echo "$HDR" | grep -qi '^link:.*rel="next"' && echo 0 || echo 1)" 0

split_resp "$(GET "produtos?ordenarPor=preco&direcao=desc")"
ORDEM=$(echo "$body" | python3 -c "import sys,json; p=[x['preco'] for x in json.load(sys.stdin)]; print(0 if p==sorted(p,reverse=True) else 1)" 2>/dev/null || echo 1)
chk "GET /produtos?ordenarPor=preco&direcao=desc" "$status" 200
chk "  ... precos em ordem decrescente" "$ORDEM" 0
split_resp "$(GET "produtos?ordenarPor=campoInexistente")"; chk "GET /produtos?ordenarPor=invalido [400]" "$status" 400

ENV=$(curl -s "${AUTH[@]}" -H "Accept: application/vnd.clyvovet.hateoas+json" "$BASE/produtos?pageSize=2" \
  | python3 -c "import sys,json; d=json.load(sys.stdin); print(0 if {'itens','total','_links'}<=set(d) else 1)" 2>/dev/null || echo 1)
chk "Envelope HATEOAS (Accept vnd.clyvovet)" "$ENV" 0
split_resp "$(GET "lembretes/$LEM_ID")"
LINKS=$(echo "$body" | python3 -c "import sys,json; print(0 if 'self' in json.load(sys.stdin).get('_links',{}) else 1)" 2>/dev/null || echo 1)
chk "GET /lembretes/{id} traz _links.self" "$LINKS" 0

split_resp "$(GET "saude-preditiva/$ANIMAL_ID")";          chk "GET /saude-preditiva/{animal} [200]"   "$status" 200 "origem=$(jq_f "$body" "origem")"
split_resp "$(GET "widget-saude-preditiva/$ANIMAL_ID")";   chk "GET /widget-saude-preditiva/{animal} [200]" "$status" 200
if [ -n "$OUTRO_ANIMAL_ID" ]; then
  split_resp "$(GET "saude-preditiva/$OUTRO_ANIMAL_ID")";  chk "GET /saude-preditiva animal alheio [404]" "$status" 404
  split_resp "$(GET "widget-saude-preditiva/$OUTRO_ANIMAL_ID")"; chk "GET /widget animal alheio [404]" "$status" 404
  LEM_ALHEIO="{\"animalId\":\"$OUTRO_ANIMAL_ID\",\"titulo\":\"Lembrete alheio\",\"tipo\":0,\"agendadoEm\":\"${FUTURO}T10:00:00\",\"recorrente\":false}"
  split_resp "$(POST "lembretes" "$LEM_ALHEIO")"
  chk "POST /lembretes animal alheio [404]" "$status" 404
else pula "recurso de outro tutor [404]" "(defina OUTRO_ANIMAL_ID)"; fi

if [ -n "$TUTOR_ID" ]; then
  split_resp "$(GET "telegram/link/$TUTOR_ID")";      chk "GET /telegram/link/{proprio} [200]"    "$status" 200
  split_resp "$(GET "telegram/vinculo/$TUTOR_ID")";   chk "GET /telegram/vinculo/{proprio} [200]" "$status" 200 "vinculado=$(jq_f "$body" "vinculado")"
else pula "Telegram link/vinculo" "(defina TUTOR_ID)"; fi
if [ -n "$OUTRO_TUTOR_ID" ]; then
  split_resp "$(GET "telegram/link/$OUTRO_TUTOR_ID")"; chk "GET /telegram/link/{outro tutor} [403]" "$status" 403
else pula "Telegram de outro tutor [403]" "(defina OUTRO_TUTOR_ID)"; fi
chk "POST /telegram/enviar sem a chave do Telegram [401]" \
  "$(st "${AUTH[@]}" -X POST -H "Content-Type: application/json" -d '{"chatId":1,"mensagem":"x"}' "$BASE/telegram/enviar")" 401

chk "GET /health/live [200]"  "$(st "$API_URL/health/live")"  200
chk "GET /health/ready [200]" "$(st "$API_URL/health/ready")" 200
chk "GET /metrics [200]"      "$(st "$API_URL/metrics")"      200

# ── DELETE (limpeza) ─────────────────────────────────────────────
echo ""
echo "══════════════════════════════════════════════════════"
echo "  🗑️   DELETE (limpeza dos registros criados)"
echo "══════════════════════════════════════════════════════"

split_resp "$(DELETE "sugestoes-produto/$SUG_ID")";    chk "DEL /sugestoes-produto/{id} [204]"  "$status" 204
split_resp "$(DELETE "lembretes/$LEM_ID")";            chk "DEL /lembretes/{id} [204]"          "$status" 204
split_resp "$(DELETE "eventos-pet/$EVENTO_ID")";       chk "DEL /eventos-pet/{id} [204]"        "$status" 204
split_resp "$(DELETE_ADM "produtos/$PROD_ID")";            chk "DEL /produtos/{id} [204]"           "$status" 204

split_resp "$(GET "produtos/$PROD_ID")";               chk "GET /produtos deletado [404]"       "$status" 404
split_resp "$(GET "lembretes/$LEM_ID")";               chk "GET /lembretes deletado [404]"      "$status" 404
split_resp "$(GET "eventos-pet/$EVENTO_ID")";          chk "GET /eventos-pet deletado [404]"    "$status" 404
split_resp "$(GET "sugestoes-produto/$SUG_ID")";       chk "GET /sugestoes deletado [404]"      "$status" 404

# ── resultado ────────────────────────────────────────────────────
echo ""
echo "══════════════════════════════════════════════════════"
printf "  RESULTADO FINAL: %d PASS  |  %d FAIL  |  %d SKIP\n" "$pass" "$fail" "$pulado"
echo "══════════════════════════════════════════════════════"
[ "$fail" -eq 0 ]
