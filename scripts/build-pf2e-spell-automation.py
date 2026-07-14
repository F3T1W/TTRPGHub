#!/usr/bin/env python3
"""Extract structured spell damage/heightening/defense from Foundry pf2e packs into pf2e-spells.json."""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "src/Infrastructure/TTRPGHub.Persistence/Seeding/Data/pf2e-spells.json"
DEFAULT_PF2E = Path.home() / "RiderProjects/pf2e"


def slugify(name: str) -> str:
    s = name.lower()
    s = re.sub(r"[''`]", "", s)
    s = re.sub(r"[^a-z0-9]+", "-", s).strip("-")
    return s


def slugify_apostrophe_s(name: str) -> str:
    s = name.lower()
    s = s.replace("'", "-s-").replace("'", "-s-")
    s = re.sub(r"[^a-z0-9]+", "-", s).strip("-")
    return s


def iter_spells(data):
    if isinstance(data, list):
        for item in data:
            yield from iter_spells(item)
    elif isinstance(data, dict):
        if data.get("type") == "spell":
            yield data
        else:
            for v in data.values():
                if isinstance(v, (dict, list)):
                    yield from iter_spells(v)


def extract_damage(system: dict) -> dict | None:
    raw = system.get("damage")
    if not raw:
        return None
    instances = []
    for _key in sorted(raw.keys(), key=lambda k: (0, int(k)) if str(k).isdigit() else (1, str(k))):
        entry = raw[_key]
        formula = (entry.get("formula") or "").strip()
        if not formula:
            continue
        kinds = entry.get("kinds") or []
        instances.append(
            {
                "formula": formula,
                "type": entry.get("type"),
                "kinds": kinds,
                "applyMod": bool(entry.get("applyMod")),
            }
        )
    return {"instances": instances} if instances else None


def extract_heightening(system: dict) -> dict | None:
    raw = system.get("heightening")
    if not raw:
        return None
    htype = raw.get("type")
    if htype != "interval":
        return None
    interval = raw.get("interval", 1)
    damage = raw.get("damage") or {}
    increments = []
    for key in sorted(damage.keys(), key=lambda k: (0, int(k)) if str(k).isdigit() else (1, str(k))):
        formula = (damage[key] or "").strip()
        if formula:
            increments.append(formula)
    if not increments:
        return None
    return {"type": "interval", "interval": interval, "damage": increments}


def extract_defense(system: dict) -> dict | None:
    defense = system.get("defense")
    if not defense:
        return None
    save = defense.get("save")
    if not save:
        return None
    stat = save.get("statistic")
    if not stat:
        return None
    return {"save": stat, "basic": bool(save.get("basic"))}


def load_foundry_index(pf2e_root: Path) -> tuple[dict[str, dict], dict[str, dict]]:
    by_slug: dict[str, dict] = {}
    by_name: dict[str, dict] = {}
    spells_dir = pf2e_root / "packs/pf2e/spells"
    if not spells_dir.is_dir():
        raise SystemExit(f"Foundry pf2e spells dir not found: {spells_dir}")

    for path in spells_dir.rglob("*.json"):
        try:
            data = json.loads(path.read_text(encoding="utf-8"))
        except (json.JSONDecodeError, OSError):
            continue
        for spell in iter_spells(data):
            name = spell.get("name", "")
            if not name:
                continue
            by_name[name.lower()] = spell
            for slug in {slugify(name), slugify_apostrophe_s(name)}:
                by_slug[slug] = spell
    return by_slug, by_name


def resolve_foundry_spell(entry: dict, by_slug: dict[str, dict], by_name: dict[str, dict]) -> dict | None:
    slug = entry.get("slug", "")
    if slug in by_slug:
        return by_slug[slug]
    name = entry.get("name", "")
    if name.lower() in by_name:
        return by_name[name.lower()]
    return None


def main() -> int:
    pf2e_root = Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_PF2E
    if not pf2e_root.is_dir():
        print(f"Usage: {Path(__file__).name} [path-to-pf2e-repo]", file=sys.stderr)
        return 1

    spells = json.loads(DATA.read_text(encoding="utf-8"))
    by_slug, by_name = load_foundry_index(pf2e_root)

    matched = with_damage = with_heighten = with_defense = 0
    for entry in spells:
        foundry = resolve_foundry_spell(entry, by_slug, by_name)
        if not foundry:
            entry.pop("damageJson", None)
            entry.pop("heighteningJson", None)
            entry.pop("defenseJson", None)
            continue
        matched += 1
        system = foundry.get("system", {})

        damage = extract_damage(system)
        heightening = extract_heightening(system)
        defense = extract_defense(system)

        if damage:
            entry["damageJson"] = damage
            with_damage += 1
        else:
            entry.pop("damageJson", None)

        if heightening:
            entry["heighteningJson"] = heightening
            with_heighten += 1
        else:
            entry.pop("heighteningJson", None)

        if defense:
            entry["defenseJson"] = defense
            with_defense += 1
        else:
            entry.pop("defenseJson", None)

    DATA.write_text(json.dumps(spells, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(
        f"Updated {DATA.name}: matched {matched}/{len(spells)}, "
        f"damage {with_damage}, heightening {with_heighten}, defense {with_defense}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
