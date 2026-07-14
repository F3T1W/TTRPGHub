#!/usr/bin/env python3
"""Extract immunities, auras, modifiers (and missing resistances/weaknesses) from Foundry pf2e NPC packs."""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "src/Infrastructure/TTRPGHub.Persistence/Seeding/Data"
MONSTERS = DATA / "pf2e-monsters.json"
CONDITIONS = DATA / "pf2e-conditions.json"
DEFAULT_PF2E = Path.home() / "RiderProjects/pf2e"


def slugify(name: str) -> str:
    s = name.lower()
    s = re.sub(r"[''`]", "", s)
    s = re.sub(r"[^a-z0-9]+", "-", s).strip("-")
    return s


def slugify_apostrophe_s(name: str) -> str:
    s = name.lower().replace("'", "-s-").replace("'", "-s-")
    s = re.sub(r"[^a-z0-9]+", "-", s).strip("-")
    return s


def load_condition_names() -> dict[str, str]:
    conds = json.loads(CONDITIONS.read_text(encoding="utf-8"))
    return {c["slug"]: c["name"] for c in conds}


def load_foundry_index(pf2e_root: Path) -> tuple[dict[str, dict], dict[str, dict]]:
    by_slug: dict[str, dict] = {}
    by_name: dict[str, dict] = {}
    packs = pf2e_root / "packs/pf2e"
    if not packs.is_dir():
        raise SystemExit(f"Foundry pf2e packs dir not found: {packs}")

    for path in packs.rglob("*.json"):
        try:
            data = json.loads(path.read_text(encoding="utf-8"))
        except (json.JSONDecodeError, OSError):
            continue
        if not (isinstance(data, dict) and data.get("type") == "npc"):
            continue
        name = data.get("name", "")
        if not name:
            continue
        by_name[name.lower()] = data
        for slug in {slugify(name), slugify_apostrophe_s(name)}:
            by_slug[slug] = data
    return by_slug, by_name


def resolve_foundry_npc(entry: dict, by_slug: dict[str, dict], by_name: dict[str, dict]) -> dict | None:
    slug = entry.get("slug", "")
    if slug in by_slug:
        return by_slug[slug]
    name = entry.get("name", "")
    return by_name.get(name.lower())


def normalize_exceptions(raw) -> list[str]:
    if not raw:
        return []
    if isinstance(raw, str):
        return [raw] if raw else []
    return [str(x) for x in raw if x]


def extract_immunities(attrs: dict) -> list[dict] | None:
    entries = attrs.get("immunities") or []
    result = []
    for entry in entries:
        typ = entry.get("type")
        if not typ:
            continue
        result.append({"type": typ, "exceptions": normalize_exceptions(entry.get("exceptions"))})
    return result or None


def extract_adjustments(entries) -> list[dict] | None:
    result = []
    for entry in entries or []:
        typ = entry.get("type")
        value = entry.get("value")
        if typ is None or value is None:
            continue
        result.append(
            {
                "type": typ,
                "value": value,
                "exceptions": normalize_exceptions(entry.get("exceptions")),
            }
        )
    return result or None


def strip_html(text: str) -> str:
    return re.sub(r"<[^>]+>", " ", text or "")


def extract_auras(npc: dict, condition_names: dict[str, str]) -> list[dict] | None:
    cond_slugs = set(condition_names)
    cond_uuid_re = re.compile(r"conditions-srd\.Item\.([A-Za-z -]+)", re.I)
    valued_re = re.compile(
        r"\b(" + "|".join(sorted(cond_slugs, key=len, reverse=True)) + r")\s+(\d+)\b",
        re.I,
    )
    auras: list[dict] = []
    seen: set[tuple] = set()

    for item in npc.get("items", []):
        if not isinstance(item, dict):
            continue
        rules = item.get("system", {}).get("rules", [])
        aura_rule = next((r for r in rules if r.get("key") == "Aura"), None)
        if not aura_rule:
            continue
        radius = aura_rule.get("radius")
        if radius is None:
            continue
        try:
            radius_feet = int(radius)
        except (TypeError, ValueError):
            continue

        desc = item.get("system", {}).get("description", {}).get("value") or ""
        plain = strip_html(desc)
        candidates: list[tuple[str, int | None]] = []

        for match in cond_uuid_re.finditer(desc):
            slug = match.group(1).lower().replace(" ", "-")
            if slug in cond_slugs:
                candidates.append((slug, None))

        for match in valued_re.finditer(plain):
            slug = match.group(1).lower()
            if slug in cond_slugs:
                candidates.append((slug, int(match.group(2))))

        if not candidates:
            continue

        slug, value = candidates[0]
        key = (radius_feet, slug, value)
        if key in seen:
            continue
        seen.add(key)
        auras.append(
            {
                "radiusFeet": radius_feet,
                "effectSlug": slug,
                "effectName": condition_names.get(slug, slug.replace("-", " ").title()),
                "value": value,
            }
        )

    return auras or None


def normalize_predicate(raw):
    if raw is None:
        return None
    if isinstance(raw, list) and len(raw) == 0:
        return None
    return raw


def extract_modifiers(npc: dict) -> list[dict] | None:
    modifiers: list[dict] = []

    def collect_rules(rules):
        for rule in rules or []:
            if not isinstance(rule, dict) or rule.get("key") != "FlatModifier":
                continue
            value = rule.get("value")
            if value is None:
                continue
            selector = rule.get("selector")
            selectors = selector if isinstance(selector, list) else [selector]
            mod_type = rule.get("type") or "untyped"
            predicate = normalize_predicate(rule.get("predicate"))
            for sel in selectors:
                if not sel:
                    continue
                modifiers.append(
                    {
                        "selector": sel,
                        "value": value,
                        "type": mod_type,
                        "predicate": predicate,
                    }
                )

    collect_rules(npc.get("system", {}).get("rules", []))
    for item in npc.get("items", []):
        if isinstance(item, dict):
            collect_rules(item.get("system", {}).get("rules", []))

    return modifiers or None


def parse_foundry_item_range(item: dict) -> tuple[int | None, int | None]:
    system = item.get("system", {})
    traits = system.get("traits", {}).get("value", []) or []
    traits = [str(t).lower() for t in traits]
    range_feet = None
    reach_feet = None
    raw_range = system.get("range")
    if isinstance(raw_range, dict):
        inc = raw_range.get("increment")
        if inc is not None:
            try:
                range_feet = int(inc)
            except (TypeError, ValueError):
                pass
    elif raw_range is not None:
        try:
            range_feet = int(raw_range)
        except (TypeError, ValueError):
            pass
    for trait in traits:
        if trait == "reach":
            reach_feet = 10
        if trait.startswith("thrown-"):
            try:
                range_feet = int(trait.split("-", 1)[1])
            except (IndexError, ValueError):
                pass
    return range_feet, reach_feet


def enrich_attacks(npc: dict, attacks_json) -> str | None:
    if not attacks_json:
        return None
    try:
        attacks = json.loads(attacks_json) if isinstance(attacks_json, str) else attacks_json
    except json.JSONDecodeError:
        return None
    if not isinstance(attacks, list) or not attacks:
        return None

    range_by_name: dict[str, tuple[int | None, int | None]] = {}
    for item in npc.get("items", []):
        if not isinstance(item, dict) or item.get("type") not in ("melee", "ranged"):
            continue
        name = item.get("name")
        if not name:
            continue
        range_by_name[name.lower()] = parse_foundry_item_range(item)

    changed = False
    for attack in attacks:
        if not isinstance(attack, dict):
            continue
        name = (attack.get("name") or "").lower()
        if name not in range_by_name:
            continue
        range_feet, reach_feet = range_by_name[name]
        if range_feet is not None:
            attack["rangeFeet"] = range_feet
            changed = True
        if reach_feet is not None:
            attack["reachFeet"] = reach_feet
            changed = True

    return json.dumps(attacks, ensure_ascii=False)


def main() -> int:
    pf2e_root = Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_PF2E
    if not pf2e_root.is_dir():
        print(f"Usage: {Path(__file__).name} [path-to-pf2e-repo]", file=sys.stderr)
        return 1

    monsters = json.loads(MONSTERS.read_text(encoding="utf-8"))
    condition_names = load_condition_names()
    by_slug, by_name = load_foundry_index(pf2e_root)

    matched = with_imm = with_aura = with_mod = with_res = with_weak = with_attacks = 0
    for entry in monsters:
        npc = resolve_foundry_npc(entry, by_slug, by_name)
        if not npc:
            entry.pop("immunitiesJson", None)
            entry.pop("aurasJson", None)
            entry.pop("modifiersJson", None)
            continue
        matched += 1
        attrs = npc.get("system", {}).get("attributes", {})

        immunities = extract_immunities(attrs)
        if immunities:
            entry["immunitiesJson"] = immunities
            with_imm += 1
        else:
            entry.pop("immunitiesJson", None)

        auras = extract_auras(npc, condition_names)
        if auras:
            entry["aurasJson"] = auras
            with_aura += 1
        else:
            entry.pop("aurasJson", None)

        modifiers = extract_modifiers(npc)
        if modifiers:
            entry["modifiersJson"] = {"modifiers": modifiers}
            with_mod += 1
        else:
            entry.pop("modifiersJson", None)

        resistances = extract_adjustments(attrs.get("resistances"))
        if resistances:
            entry["resistancesJson"] = resistances
            with_res += 1

        weaknesses = extract_adjustments(attrs.get("weaknesses"))
        if weaknesses:
            entry["weaknessesJson"] = weaknesses
            with_weak += 1

        attacks = enrich_attacks(npc, entry.get("attacksJson"))
        if attacks:
            entry["attacksJson"] = attacks
            if "rangeFeet" in attacks or "reachFeet" in attacks:
                with_attacks += 1

    MONSTERS.write_text(json.dumps(monsters, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(
        f"Updated {MONSTERS.name}: matched {matched}/{len(monsters)}, "
        f"immunities {with_imm}, auras {with_aura}, modifiers {with_mod}, "
        f"resistances {with_res}, weaknesses {with_weak}, attacks {with_attacks}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
