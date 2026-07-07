#!/usr/bin/env bash
# L.7 API smoke — регистрация, сессия, журнал visibility, low-light на токене.
set -euo pipefail
API="${API:-http://localhost:5014}"
SUFFIX=$(date +%s)
EMAIL="l7smoke${SUFFIX}@test.local"
USER="l7smoke${SUFFIX}"
PASS='L7Smoke!9Aa'

fail() { echo "FAIL: $1"; exit 1; }
ok() { echo "OK  $1"; }

register=$(curl -sf -X POST "$API/api/auth/register" \
  -H 'Content-Type: application/json' \
  -d "{\"username\":\"$USER\",\"email\":\"$EMAIL\",\"password\":\"$PASS\"}") || fail register
ok "register"

login=$(curl -sf -X POST "$API/api/auth/login" \
  -H 'Content-Type: application/json' \
  -d "{\"email\":\"$EMAIL\",\"password\":\"$PASS\"}") || fail login
TOKEN=$(echo "$login" | python3 -c "import sys,json; print(json.load(sys.stdin)['accessToken'])")
AUTH="Authorization: Bearer $TOKEN"
ok "login"

session=$(curl -sf -X POST "$API/api/sessions/" \
  -H 'Content-Type: application/json' -H "$AUTH" \
  -d "{\"title\":\"L7 smoke\",\"description\":\"\",\"system\":\"Pathfinder 2e\",\"maxPlayers\":4,\"scheduledAt\":\"2026-07-08T12:00:00Z\",\"format\":0,\"location\":null}")
SID=$(echo "$session" | python3 -c "import sys,json; print(json.load(sys.stdin)['sessionId'])")
ok "create session $SID"

start=$(curl -sf -o /dev/null -w '%{http_code}' -X PATCH "$API/api/sessions/$SID/status" \
  -H 'Content-Type: application/json' -H "$AUTH" \
  -d '{"status":1}')
[[ "$start" == "204" || "$start" == "200" ]] || fail "start session ($start)"
ok "start session"

entry=$(curl -sf -X POST "$API/api/table/$SID/journal" \
  -H 'Content-Type: application/json' -H "$AUTH" \
  -d '{"title":"L7 folder root","contentMarkdown":"test","parentId":null,"campaignId":null}')
EID=$(echo "$entry" | python3 -c "import sys,json; print(json.load(sys.stdin)['id'])")
PARENT=$(echo "$entry" | python3 -c "import sys,json; d=json.load(sys.stdin); assert d.get('parentId') is None; print('null')")
ok "journal create $EID"

vis=$(curl -sf -o /dev/null -w '%{http_code}' -X PUT "$API/api/table/$SID/journal/$EID/visibility" \
  -H 'Content-Type: application/json' -H "$AUTH" \
  -d '{"visibleToUserIds":[]}')
[[ "$vis" == "204" || "$vis" == "200" ]] || fail "journal visibility ($vis)"
ok "journal visibility endpoint"

cols=$(docker exec taverna_postgres psql -U taverna -d taverna_db -t -c \
  "SELECT column_name FROM information_schema.columns WHERE table_name='table_tokens' AND column_name='has_low_light_vision';" | tr -d ' ')
[[ "$cols" == "has_low_light_vision" ]] || fail "column has_low_light_vision"
ok "db column has_low_light_vision"

token=$(curl -sf -X POST "$API/api/table/$SID/tokens" \
  -H 'Content-Type: application/json' -H "$AUTH" \
  -d '{"label":"Smoke","imageUrl":null,"color":"#7c3aed","x":5,"y":5,"ownerUserId":null,"width":1,"height":1}')
TID=$(echo "$token" | python3 -c "import sys,json; print(json.load(sys.stdin)['id'])")
ok "add token $TID"

ll=$(curl -sf -o /dev/null -w '%{http_code}' -X PATCH "$API/api/table/$SID/tokens/$TID/stats" \
  -H 'Content-Type: application/json' -H "$AUTH" \
  -d '{"currentHp":null,"width":null,"height":null,"rotation":null,"hasLowLightVision":true}')
[[ "$ll" == "204" || "$ll" == "200" ]] || fail "hasLowLightVision patch ($ll)"
ok "token HasLowLightVision patch"

child=$(curl -sf -X POST "$API/api/table/$SID/journal" \
  -H 'Content-Type: application/json' -H "$AUTH" \
  -d "{\"title\":\"Child\",\"contentMarkdown\":\"nested\",\"parentId\":\"$EID\"}")
CHILD_PARENT=$(echo "$child" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d.get('parentId',''))")
[[ "$CHILD_PARENT" == "$EID" ]] || fail "journal parentId"
ok "journal folder parentId"

echo "All API smoke checks passed."
