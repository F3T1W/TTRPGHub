#!/usr/bin/env python3
"""L.7 browser prep — login + session, print token and session id for CDP."""
import json, sys, urllib.request, time

API = "http://localhost:5014"
suffix = str(int(time.time()))
email = f"l7ui{suffix}@test.local"
user = f"l7ui{suffix}"
password = "L7Smoke!9Aa"

def post(path, body, token=None):
    req = urllib.request.Request(
        f"{API}{path}",
        data=json.dumps(body).encode(),
        headers={"Content-Type": "application/json", **({"Authorization": f"Bearer {token}"} if token else {})},
        method="POST",
    )
    with urllib.request.urlopen(req) as r:
        return json.load(r), r.status

def patch(path, body, token):
    req = urllib.request.Request(
        f"{API}{path}",
        data=json.dumps(body).encode(),
        headers={"Content-Type": "application/json", "Authorization": f"Bearer {token}"},
        method="PATCH",
    )
    with urllib.request.urlopen(req) as r:
        return r.status

post("/api/auth/register", {"username": user, "email": email, "password": password})
login, _ = post("/api/auth/login", {"email": email, "password": password})
token = login["accessToken"]
user_id = login["userId"]
sess, _ = post("/api/sessions/", {
    "title": "L7 UI", "description": "", "system": "Pathfinder 2e",
    "maxPlayers": 4, "scheduledAt": "2026-07-08T12:00:00Z", "format": 0, "location": None
}, token)
sid = sess["sessionId"]
patch(f"/api/sessions/{sid}/status", {"status": 1}, token)
print(json.dumps({"token": token, "userId": user_id, "sessionId": sid}))
