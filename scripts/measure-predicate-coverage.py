#!/usr/bin/env python3
"""L.4 — оценка покрытия строковых предикатов PF2e при максимальном боевом контексте."""
import json
import re
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "src/Infrastructure/TTRPGHub.Persistence/Seeding/Data"

# Максимальный набор roll options, который L.4 умеет выдавать за столом.
MAX_OPTIONS = {
    "encounter",
    "action:demoralize", "action:request", "action:lie",
    "feat:power-attack", "feat:unburdened-iron", "feat:animal-empathy",
    "self:condition:frightened", "self:condition:prone", "self:condition:hidden",
    "target:condition:off-guard", "target:trait:animal", "target:trait:humanoid", "target:trait:undead",
    "item:longsword", "item:holy-water", "item:magical", "item:ranged",
    "item:trait:divine", "item:trait:detection", "item:damage:category:physical",
    "armor:equipped", "armor:category:light", "armor:slug:leather-armor",
    "self:ancestry:human", "self:trait:human", "self:trait:humanoid", "self:size:2",
    "self:armored",
    "terrain:forest", "terrain:wilderness",
    "lighting:dim-light", "lighting:darkness",
}

SKIP_PREFIXES = (
    "target:deity:", "target:tag:", "target:mark:", "target:goblin", "spell-effect:",
    "battle-form:", "origin:", "disguise:", "language:", "member-of", "using-physiology",
    "ghoul-", "right-hand", "analyze-", "fierce-", "disguised-", "deflecting", "receive-",
    "impersonate-human", "tumbling-", "terrain:{",
)


def collect_preds(obj, out):
    if isinstance(obj, dict):
        for v in obj.values():
            collect_preds(v, out)
    elif isinstance(obj, list):
        for x in obj:
            collect_preds(x, out)


def walk_pred(p, out):
    if isinstance(p, str):
        out.add(p)
    elif isinstance(p, list):
        for x in p:
            walk_pred(x, out)
    elif isinstance(p, dict):
        for k, v in p.items():
            if k in ("or", "and", "not", "nor", "nand"):
                walk_pred(v, out)
            elif k in ("gte", "lte", "gt", "lt"):
                pass
            else:
                walk_pred(v, out)


def can_evaluate(term: str) -> bool:
    if term in MAX_OPTIONS:
        return True
    if any(term.startswith(p) for p in SKIP_PREFIXES):
        return False
    if term.startswith(("item:", "armor:", "self:", "target:", "feat:", "action:", "terrain:", "lighting:")):
        return False
    return term not in {"tut-tut"}


def main():
    preds = set()
    for fname in ("pf2e-feats.json", "pf2e-conditions.json"):
        data = json.loads((DATA / fname).read_text())
        walk = set()
        collect_preds(data, walk)
        # re-walk only predicates
        def extract(obj):
            if isinstance(obj, dict):
                if "predicate" in obj:
                    walk_pred(obj["predicate"], preds)
                if "modifiers" in obj:
                    for m in obj["modifiers"]:
                        if "predicate" in m:
                            walk_pred(m["predicate"], preds)
                for v in obj.values():
                    extract(v)
            elif isinstance(obj, list):
                for x in obj:
                    extract(x)
        extract(data)

    evaluable = {p for p in preds if can_evaluate(p)}
    skipped = preds - evaluable
    cats = Counter(p.split(":")[0] + ":*" if ":" in p else p for p in preds)
    print(f"Unique string predicate terms in feats+conditions: {len(preds)}")
    print(f"Evaluable with L.4 max combat context: {len(evaluable)}")
    print(f"Still unevaluable (exotic/target-specific): {len(skipped)}")
    print("Categories:", cats.most_common(12))
    print("\nNewly covered L.4 categories sample:")
    for p in sorted(evaluable):
        if p.startswith(("item:", "armor:", "terrain:", "lighting:", "self:ancestry", "self:armored", "self:size")):
            print(" ", p)


if __name__ == "__main__":
    main()
