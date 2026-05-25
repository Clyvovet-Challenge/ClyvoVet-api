#!/usr/bin/env bash
BASE="http://localhost:5191/api/v1"
pass=0; fail=0

# ── helpers ───────────────────────────────────────────────────────
chk() {
  local label="$1" got="$2" expect="$3" detail="${4:-}"
  if [ "$got" -eq "$expect" ] 2>/dev/null; then
    printf "[PASS] %-52s %s%s\n" "$label" "$got" "${detail:+  | $detail}"
    ((pass++))
  else
    printf "[FAIL] %-52s %s (esperado %s)%s\n" "$label" "$got" "$expect" "${detail:+  | $detail}"
    ((fail++))
  fi
}

GET()    { curl -s -w '\n%{http_code}' "$BASE/$1"; }
POST()   { curl -s -w '\n%{http_code}' -X POST   "$BASE/$1" -H "Content-Type: application/json" -d "$2"; }
PUT()    { curl -s -w '\n%{http_code}' -X PUT    "$BASE/$1" -H "Content-Type: application/json" -d "$2"; }
DELETE() { curl -s -w '\n%{http_code}' -X DELETE "$BASE/$1"; }

split_resp() { body=$(echo "$1" | head -n -1); status=$(echo "$1" | tail -1); }
jq_f() { echo "$1" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d.get('$2',''))" 2>/dev/null || true; }
jq_len() { echo "$1" | python3 -c "import sys,json; d=json.load(sys.stdin); print(len(d) if isinstance(d,list) else '?')" 2>/dev/null || echo "?"; }

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
split_resp "$(POST "produtos" "$PROD_BODY")"
chk "POST /produtos [201]" "$status" 201
PROD_ID=$(jq_f "$body" "id")
echo "     produto_id criado: $PROD_ID"

split_resp "$(GET "produtos/$PROD_ID")";               chk "GET /produtos/{id} [200]"           "$status" 200

PROD_UPD='{"nome":"Shampoo Pet Neutro 1L","descricao":"Versao 1L.","categoria":2,"preco":49.90,"especieIndicada":5,"ativo":true}'
split_resp "$(PUT "produtos/$PROD_ID" "$PROD_UPD")";   chk "PUT /produtos/{id} [200]"           "$status" 200

split_resp "$(GET "produtos/id-nao-existe-xxx")";      chk "GET /produtos/invalido [404]"       "$status" 404
split_resp "$(POST "produtos" '{"nome":"X","categoria":0,"preco":-50,"especieIndicada":0,"ativo":true}')";
                                                       chk "POST /produtos preco=-50 [400]"     "$status" 400
split_resp "$(POST "produtos" '{"categoria":0,"preco":10,"especieIndicada":0,"ativo":true}')";
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

EV_BODY='{"titulo":"Workshop Nutricao Pet","tipo":3,"rua":"Rua das Acacias","numero":"500","bairro":"Jardins","cidade":"Sao Paulo","estado":"SP","cep":"01425-000","dataInicio":"2026-10-05","dataFim":"2026-10-05","especieAlvo":0,"organizador":"Dr. Pet","gratuito":false,"ativo":true}'
split_resp "$(POST "eventos-pet" "$EV_BODY")"
chk "POST /eventos-pet [201]" "$status" 201
EVENTO_ID=$(jq_f "$body" "id")
echo "     evento_id criado: $EVENTO_ID"

split_resp "$(GET "eventos-pet/$EVENTO_ID")";          chk "GET /eventos-pet/{id} [200]"        "$status" 200

EV_UPD='{"titulo":"Workshop Atualizado","tipo":3,"cidade":"Sao Paulo","estado":"SP","dataInicio":"2026-10-05","especieAlvo":0,"gratuito":true,"ativo":true}'
split_resp "$(PUT "eventos-pet/$EVENTO_ID" "$EV_UPD")"; chk "PUT /eventos-pet/{id} [200]"       "$status" 200

split_resp "$(POST "eventos-pet" '{"titulo":"Ev Passado","tipo":0,"dataInicio":"2020-01-01","especieAlvo":5,"gratuito":true,"ativo":true}')";
                                                        chk "POST /eventos-pet data passada [400]" "$status" 400
split_resp "$(POST "eventos-pet" '{"tipo":0,"dataInicio":"2026-12-01","gratuito":true,"ativo":true}')";
                                                        chk "POST /eventos-pet sem titulo [400]"   "$status" 400
split_resp "$(GET "eventos-pet/id-nao-existe-xxx")";    chk "GET /eventos-pet/invalido [404]"      "$status" 404

# ── LEMBRETES ────────────────────────────────────────────────────
echo ""
echo "══════════════════════════════════════════════════════"
echo "  🔔  LEMBRETES"
echo "══════════════════════════════════════════════════════"

split_resp "$(GET "lembretes")"
chk "GET /lembretes" "$status" 200 "$(jq_len "$body") registros"
ANIMAL_ID=$(echo "$body" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d[0]['animalId'] if d else '')" 2>/dev/null || true)
echo "     animal_id do seed: $ANIMAL_ID"

split_resp "$(GET "lembretes?status=Pendente")";       chk "GET /lembretes?status=Pendente"     "$status" 200
split_resp "$(GET "lembretes?tipo=Vacina")";           chk "GET /lembretes?tipo=Vacina"         "$status" 200
split_resp "$(GET "lembretes?animalId=$ANIMAL_ID")";   chk "GET /lembretes?animalId=..."        "$status" 200
split_resp "$(GET "lembretes?page=-1")";               chk "GET /lembretes?page=-1 [400]"       "$status" 400

LEM_BODY="{\"animalId\":\"$ANIMAL_ID\",\"titulo\":\"Consulta de Rotina\",\"descricao\":\"Checkup anual.\",\"tipo\":2,\"agendadoEm\":\"2026-10-20T14:00:00\",\"recorrente\":false,\"status\":0}"
split_resp "$(POST "lembretes" "$LEM_BODY")"
chk "POST /lembretes [201]" "$status" 201
LEM_ID=$(jq_f "$body" "id")
echo "     lembrete_id criado: $LEM_ID"

split_resp "$(GET "lembretes/$LEM_ID")";               chk "GET /lembretes/{id} [200]"          "$status" 200

STATUS_CRIADO=$(jq_f "$body" "status")
chk "Status forcado = Pendente(0) na criacao" "$([ "$STATUS_CRIADO" = "0" ] && echo 0 || echo 1)" 0 "status=$STATUS_CRIADO"

LEM_UPD="{\"animalId\":\"$ANIMAL_ID\",\"titulo\":\"Consulta de Rotina\",\"tipo\":2,\"agendadoEm\":\"2026-10-20T14:00:00\",\"recorrente\":false,\"status\":1}"
split_resp "$(PUT "lembretes/$LEM_ID" "$LEM_UPD")";    chk "PUT /lembretes/{id} status->Enviado [200]"  "$status" 200

LEM_PASSADO="{\"animalId\":\"$ANIMAL_ID\",\"titulo\":\"X\",\"tipo\":0,\"agendadoEm\":\"2020-01-01T10:00:00\",\"recorrente\":false,\"status\":0}"
split_resp "$(PUT "lembretes/$LEM_ID" "$LEM_PASSADO")"; chk "PUT /lembretes agendadoEm passado [400]"   "$status" 400

split_resp "$(POST "lembretes" '{"animalId":"00000000-0000-0000-0000-000000000000","titulo":"X","tipo":0,"agendadoEm":"2026-12-01T10:00:00","recorrente":false}')";
                                                         chk "POST /lembretes animalId invalido [404]"  "$status" 404
split_resp "$(POST "lembretes" '{"titulo":"X","tipo":0,"agendadoEm":"2026-12-01T10:00:00","recorrente":false}')";
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

PROD_SEED=$(GET "produtos?pageSize=1" | head -n -1 | python3 -c "import sys,json; d=json.load(sys.stdin); print(d[0]['id'] if d else '')" 2>/dev/null || true)
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

split_resp "$(POST "sugestoes-produto" "{\"animalId\":\"$ANIMAL_ID\",\"produtoId\":\"00000000-0000-0000-0000-000000000000\",\"ativo\":true}")";
                                                              chk "POST /sugestoes produtoId invalido [404]" "$status" 404
split_resp "$(POST "sugestoes-produto" "{\"animalId\":\"00000000-0000-0000-0000-000000000000\",\"produtoId\":\"$PROD_SEED\",\"ativo\":true}")";
                                                              chk "POST /sugestoes animalId invalido [404]"  "$status" 404
split_resp "$(POST "sugestoes-produto" '{"justificativa":"x","ativo":true}')";
                                                              chk "POST /sugestoes sem IDs [400]"            "$status" 400
split_resp "$(GET "sugestoes-produto/id-nao-existe-xxx")";    chk "GET /sugestoes-produto/invalido [404]"    "$status" 404

# ── DELETE (limpeza) ─────────────────────────────────────────────
echo ""
echo "══════════════════════════════════════════════════════"
echo "  🗑️   DELETE (limpeza dos registros criados)"
echo "══════════════════════════════════════════════════════"

split_resp "$(DELETE "sugestoes-produto/$SUG_ID")";    chk "DEL /sugestoes-produto/{id} [204]"  "$status" 204
split_resp "$(DELETE "lembretes/$LEM_ID")";            chk "DEL /lembretes/{id} [204]"          "$status" 204
split_resp "$(DELETE "eventos-pet/$EVENTO_ID")";       chk "DEL /eventos-pet/{id} [204]"        "$status" 204
split_resp "$(DELETE "produtos/$PROD_ID")";            chk "DEL /produtos/{id} [204]"           "$status" 204

split_resp "$(GET "produtos/$PROD_ID")";               chk "GET /produtos deletado [404]"       "$status" 404
split_resp "$(GET "lembretes/$LEM_ID")";               chk "GET /lembretes deletado [404]"      "$status" 404
split_resp "$(GET "eventos-pet/$EVENTO_ID")";          chk "GET /eventos-pet deletado [404]"    "$status" 404
split_resp "$(GET "sugestoes-produto/$SUG_ID")";       chk "GET /sugestoes deletado [404]"      "$status" 404

# ── resultado ────────────────────────────────────────────────────
echo ""
echo "══════════════════════════════════════════════════════"
printf "  RESULTADO FINAL: %d PASS  |  %d FAIL\n" "$pass" "$fail"
echo "══════════════════════════════════════════════════════"
