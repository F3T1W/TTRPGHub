#!/usr/bin/env python3
"""Q.4 — RU-overlay для rule entries (feats, equipment, actions, classes, races, backgrounds).

Источник — pf2e_ru_translation (GitLab RST), тот же community-слой что M.4/L.6.
Импортируем только name/description поверх slug'ов из нашей базы (pf2e-feats.json,
pf2e-equipment.json и захардкоженных списков классов/рас/действий в сидерах).
"""

from __future__ import annotations

import json
import re
import tarfile
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "src/Infrastructure/TTRPGHub.Persistence/Seeding/Data"
OUT = ROOT / "src/Presentation/TTRPGHub.Web/wwwroot/locale/pf2e"
CACHE_DIR = ROOT / ".cache" / "pf2e-ru-translation"
ARCHIVE_URL = (
    "https://gitlab.com/api/v4/projects/pf2e_ru%2Fpf2e_ru_translation"
    "/repository/archive.tar.gz?sha=master"
)

FEAT_PREFIXES = frozenset({
    "feat", "class-feat", "ancestry-feat", "arch-feat", "archetype-feat",
    "skill-feat", "general-feat", "archetype",
})

KNOWN_CLASS_SLUGS = {
    "fighter", "wizard", "rogue", "cleric", "ranger", "barbarian", "bard", "druid",
    "monk", "champion", "sorcerer", "alchemist", "investigator", "magus", "oracle",
    "swashbuckler", "thaumaturge", "witch", "gunslinger", "inventor", "summoner",
    "psychic", "kineticist", "guardian", "exemplar",
}

KNOWN_ANCESTRY_SLUGS = {
    "human", "elf", "dwarf", "halfling", "gnome", "half-orc", "half-elf", "goblin",
    "orc", "tiefling", "gnoll", "hobgoblin", "catfolk", "kobold", "lizardfolk",
    "ratfolk", "leshy", "fetchling", "automaton", "kitsune", "grippli", "azarketi",
    "anadi", "android", "azarketi", "catfolk", "conrasu", "fetchling", "fleshwarp",
    "gnoll", "gnome", "goblin", "hobgoblin", "kholo", "kobold", "leshy", "lizardfolk",
    "orc", "ratfolk", "skeleton", "strix", "tengu", "tiefling", "vanara", "vishkanya",
}

KNOWN_ACTION_SLUGS = {
    "strike", "stride", "step", "raise-a-shield", "recall-knowledge", "seek",
    "demoralize", "aid", "escape", "grapple", "trip", "disarm", "shove", "delay",
    "ready", "sustain-a-spell", "take-cover", "hustle", "point-out", "request",
    "coerce", "perform", "treat-wounds", "search", "sense-motive", "high-jump",
    "long-jump", "swim", "climb", "craft", "repair", "interact", "release", "leap",
    "drop-prone", "crawl", "stand", "draw", "conceal-an-object", "hide", "sneak",
    "create-a-diversion", "feint", "palm-an-object", "steal", "disable-a-device",
    "pick-a-lock", "decipher-writing", "identify-magic", "learn-a-spell",
    "administer-first-aid", "command-an-animal", "subsist", "earn-income",
}

KNOWN_BACKGROUND_SLUGS = {
    "acolyte", "criminal", "farmhand", "guard", "hunter", "noble", "scholar",
    "warrior", "sailor", "herbalist", "gladiator", "merchant", "street-urchin",
    "barrister",
}

DIRECTIVE_LINE_RE = re.compile(r"^\.\. .*(\n[ \t]+.*)*$", re.MULTILINE)
ANCHOR_RE = re.compile(r"^\.\. _(?P<anchor>[^:]+):\s*$", re.MULTILINE)
HEADER_RE = re.compile(
    r"^(?P<name_ru>.+?)\s+\(`(?P<name_en>[^`]+)\s*<[^>]+>`_\)",
    re.MULTILINE,
)
CLASS_HEADER_RE = re.compile(
    r"^(?P<name_ru>.+?)\s+\(`(?P<name_en>[^`]+)\s*<[^>]+>`_\)\s*\n=+",
    re.MULTILINE,
)
EQ_REF_RE = re.compile(r":ref:`(?P<ru>[^<]+)\s*<(?:weapon|item)--(?P<slug>[^>]+)>`")


def download_and_extract() -> Path:
    CACHE_DIR.mkdir(parents=True, exist_ok=True)
    archive_path = CACHE_DIR / "repo.tar.gz"
    if not archive_path.exists():
        print("Скачиваю архив репозитория...")
        urllib.request.urlretrieve(ARCHIVE_URL, archive_path)

    extracted = list(CACHE_DIR.glob("pf2e_ru_translation-*"))
    if extracted:
        return extracted[0]

    print("Распаковываю...")
    with tarfile.open(archive_path) as tar:
        tar.extractall(CACHE_DIR, filter="data")
    return next(CACHE_DIR.glob("pf2e_ru_translation-*"))


def clean_body(text: str) -> str:
    text = DIRECTIVE_LINE_RE.sub("", text)
    text = re.sub(r":[a-z_]+:`([^`]+)`", r"\1", text)
    text = re.sub(r"`([^`<]+)\s*<[^>]+>`_", r"\1", text)
    text = re.sub(r"\|[^|]+\|", "", text)
    text = text.replace("**", "")
    text = re.sub(r"\n~{5,}.*", "", text, flags=re.DOTALL)
    text = re.sub(r"\n{3,}", "\n\n", text)
    return text.strip()


def anchor_slug(anchor: str) -> str:
    return anchor.split("--")[-1].lower()


def is_feat_anchor(anchor: str) -> bool:
    prefix = anchor.split("--")[0]
    return prefix in FEAT_PREFIXES or prefix.endswith("-feat")


def is_equipment_anchor(anchor: str) -> bool:
    prefix = anchor.split("--")[0]
    return prefix in {"item", "weapon"}


def is_action_anchor(anchor: str) -> bool:
    return anchor.split("--")[0] == "action"


def is_background_anchor(anchor: str) -> bool:
    return anchor.split("--")[0] == "bg"


def parse_anchored_entries(
    text: str,
    predicate,
    *,
    include_description: bool = True,
) -> dict[str, dict[str, str]]:
    out: dict[str, dict[str, str]] = {}
    matches = list(ANCHOR_RE.finditer(text))
    for i, match in enumerate(matches):
        anchor = match.group("anchor")
        if not predicate(anchor):
            continue
        slug = anchor_slug(anchor)
        body = text[match.end():matches[i + 1].start() if i + 1 < len(matches) else len(text)]
        header = HEADER_RE.search(body)
        if not header:
            continue
        entry: dict[str, str] = {"name": header.group("name_ru").strip()}
        if include_description:
            description = clean_body(body[header.end():])
            if description:
                entry["description"] = description
        out[slug] = entry
    return out


def collect_from_tree(base: Path, predicate, *, include_description: bool = True) -> dict[str, dict[str, str]]:
    merged: dict[str, dict[str, str]] = {}
    for path in sorted(base.rglob("*.rst")):
        if path.name.lower() == "index.rst":
            continue
        text = path.read_text(encoding="utf-8", errors="ignore")
        merged.update(parse_anchored_entries(text, predicate, include_description=include_description))
    return merged


def collect_equipment_refs(base: Path) -> dict[str, dict[str, str]]:
    out: dict[str, dict[str, str]] = {}
    for path in sorted(base.rglob("*.rst")):
        text = path.read_text(encoding="utf-8", errors="ignore")
        for match in EQ_REF_RE.finditer(text):
            slug = match.group("slug").lower()
            out.setdefault(slug, {"name": match.group("ru").strip()})
    return out


def collect_classes(source: Path) -> dict[str, dict[str, str]]:
    out: dict[str, dict[str, str]] = {}
    classes_dir = source / "classes"
    if not classes_dir.exists():
        return out
    for path in sorted(classes_dir.glob("*.rst")):
        slug = path.stem.lower()
        text = path.read_text(encoding="utf-8", errors="ignore")
        header = CLASS_HEADER_RE.search(text)
        if not header:
            continue
        out[slug] = {"name": header.group("name_ru").strip()}
    return out


def collect_ancestries(source: Path) -> dict[str, dict[str, str]]:
    out: dict[str, dict[str, str]] = {}
    ancestries_dir = source / "ancestries_and_backgrounds" / "ancestries"
    if not ancestries_dir.exists():
        return out
    for path in sorted(ancestries_dir.glob("*.rst")):
        slug = path.stem.lower()
        text = path.read_text(encoding="utf-8", errors="ignore")
        header = CLASS_HEADER_RE.search(text)
        if not header:
            continue
        out[slug] = {"name": header.group("name_ru").strip()}
    return out


def merge_with_slugs(parsed: dict[str, dict[str, str]], known_slugs: set[str]) -> dict[str, dict[str, str]]:
    return {slug: entry for slug, entry in parsed.items() if slug in known_slugs}


def write_json(path: Path, data: object) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def main() -> None:
    repo_dir = download_and_extract()
    source = repo_dir / "source"

    known_feat_slugs = {f["slug"].lower() for f in json.loads((DATA / "pf2e-feats.json").read_text(encoding="utf-8"))}
    known_equipment_slugs = {
        e["slug"].lower() for e in json.loads((DATA / "pf2e-equipment.json").read_text(encoding="utf-8"))
    }

    feats = collect_from_tree(source, is_feat_anchor)
    equipment = collect_from_tree(source, is_equipment_anchor)
    equipment.update(collect_equipment_refs(source / "equipment"))
    actions = collect_from_tree(source, is_action_anchor)
    backgrounds = collect_from_tree(source, is_background_anchor, include_description=True)
    classes = collect_classes(source)
    ancestries = collect_ancestries(source)

    feats_matched = merge_with_slugs(feats, known_feat_slugs)
    equipment_matched = merge_with_slugs(equipment, known_equipment_slugs)
    actions_matched = merge_with_slugs(actions, KNOWN_ACTION_SLUGS)
    backgrounds_matched = merge_with_slugs(backgrounds, KNOWN_BACKGROUND_SLUGS)
    classes_matched = merge_with_slugs(classes, KNOWN_CLASS_SLUGS)
    ancestries_matched = merge_with_slugs(ancestries, KNOWN_ANCESTRY_SLUGS)

    entries: dict[str, dict[str, str]] = {}
    for chunk in (
        feats_matched, equipment_matched, actions_matched,
        backgrounds_matched, classes_matched, ancestries_matched,
    ):
        for slug, entry in chunk.items():
            entries.setdefault(slug, {}).update(entry)

    print(f"Фиты: {len(feats_matched)} / {len(feats)} распознано ({len(known_feat_slugs)} в базе)")
    print(f"Снаряжение: {len(equipment_matched)} / {len(equipment)} ({len(known_equipment_slugs)} в базе)")
    print(f"Действия: {len(actions_matched)} / {len(actions)}")
    print(f"Предыстории: {len(backgrounds_matched)} / {len(backgrounds)}")
    print(f"Классы: {len(classes_matched)} / {len(classes)}")
    print(f"Предки: {len(ancestries_matched)} / {len(ancestries)}")
    print(f"Итого entries.ru.json: {len(entries)} slug'ов")

    write_json(OUT / "entries.ru.json", entries)
    print(f"\nЗаписано в {OUT / 'entries.ru.json'}")
    print("Пробелы без community-перевода: scripts/fill-pf2e-locale-gaps.py")


if __name__ == "__main__":
    main()
