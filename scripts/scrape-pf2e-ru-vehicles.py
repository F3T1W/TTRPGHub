#!/usr/bin/env python3
"""N.9 — импорт транспорта (vehicles) из pf2e_ru_translation (GitLab, RST).

Тот же источник и то же обоснование, что у N.1 (hazards) — своего англоязычного датасета
транспорта у нас не было и не появилось, RU-текст здесь первичный источник, не оверлей.
Источник — единый файл source/game_mastering/subsystems/Vehicles.rst: 30 статблоков
транспорта, разделённых заголовками "Имя (`EN Name <url>`_) / Транспорт N", тот же формат,
что и заголовки хазардов в hazards.rst.
"""

from __future__ import annotations

import json
import re
import tarfile
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "src/Infrastructure/TTRPGHub.Persistence/Seeding/Data"
CACHE_DIR = ROOT / ".cache" / "pf2e-ru-translation"
ARCHIVE_URL = (
    "https://gitlab.com/api/v4/projects/pf2e_ru%2Fpf2e_ru_translation"
    "/repository/archive.tar.gz?sha=master"
)


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


HEADER_RE = re.compile(
    r"^(?P<name_ru>.+?)\s+(?:\(`(?P<name_en>.+?)\s*<.*?>`_\)|\((?P<name_en2>[^()]+)\))"
    r"\s*/\s*Транспорт\s*(?P<level>-?\d+)\s*$",
    re.MULTILINE,
)
DIRECTIVE_LINE_RE = re.compile(r"^\.\. .*(\n[ \t]+.*)*$", re.MULTILINE)


def clean(text: str) -> str:
    text = DIRECTIVE_LINE_RE.sub("", text)
    text = re.sub(r":[a-z_]+:`([^<`]+)\s*<[^>]*>`", r"\1", text)
    text = re.sub(r":[a-z_]+:`([^`]+)`", r"\1", text)
    text = re.sub(r"`([^`<]+)\s*<[^>]+>`_", r"\1", text)
    text = re.sub(r"\|[^|]+\|", "", text)
    text = text.replace("**", "")
    text = re.sub(r"~{5,}$", "", text, flags=re.MULTILINE)
    text = re.sub(r"^-{5,}$", "", text, flags=re.MULTILINE)
    text = re.sub(r"\n{3,}", "\n\n", text)
    text = re.sub(r"[ \t]+,", ",", text)
    text = re.sub(r"[ \t]+\n", "\n", text)
    text = re.sub(r"[ \t]+", " ", text)
    return text.strip()


def slugify(name_en: str) -> str:
    return re.sub(r"[^a-zA-Z0-9]+", "-", name_en.strip()).strip("-").lower()


def extract_int(pattern: str, text: str) -> int | None:
    m = re.search(pattern, text)
    return int(m.group(1)) if m else None


def extract_field(label: str, text: str) -> str | None:
    m = re.search(rf"\*\*{label}\*\*:\s*(.+?)(?=\n\n|\*\*[А-ЯA-Zа-яa-z]|-{{5,}}|\Z)", text, re.DOTALL)
    return clean(m.group(1)) if m else None


def parse_block(name_ru: str, name_en: str, level: int, body: str) -> dict | None:
    rest = re.sub(r"^\s*~{5,}\s*\n+", "", body)

    size_m = re.search(r"^-\s*:size:`([^`]+)`", rest, re.MULTILINE)
    size = size_m.group(1).strip() if size_m else None

    price = extract_field("Цена", rest)
    dimensions = extract_field("Габариты", rest)
    crew = extract_field("Экипаж", rest)
    passengers = extract_field("Пассажиры", rest)
    piloting_check = extract_field("Проверка пилотирования", rest)
    source = extract_field("Источник", rest) or "GM Core / Core Rulebook vehicle appendix"

    ac = extract_int(r"\*\*КБ\*\*:\s*(\d+)", rest)
    fortitude = extract_int(r"\*\*Стойкость\*\*:\s*\+?(-?\d+)", rest)
    hardness = extract_int(r"\*\*Твердость\*\*:\s*(\d+)", rest)
    hp = extract_int(r"\*\*ОЗ\*\*:\s*(\d+)", rest)
    broken_threshold = extract_int(r"\*\*ОЗ\*\*:\s*\d+\s*\(ПП\s*(\d+)\)", rest)

    immunities_raw = extract_field("Иммунитеты", rest)
    immunities = None
    if immunities_raw:
        items = [s.strip().rstrip(",") for s in immunities_raw.splitlines() if s.strip()]
        immunities = ", ".join(items)

    speed = extract_field("Скорость", rest)
    collision = extract_field("Столкновение", rest)

    # Всё, что не структурированные поля выше — способности транспорта единым текстовым блоком
    # (аналог Pf2eHazard.AbilitiesText) — не структурируем каждую способность отдельно.
    abilities_body = rest
    for pattern in [
        r"^-\s*:size:`[^`]+`.*$",
        r"\*\*Цена\*\*:.*?(?=\n\n|\Z)",
        r"\*\*Источник\*\*:.*?(?=\n\n|\Z)",
        r"\*\*Габариты\*\*:.*?(?=\n\n|\Z)",
        r"\*\*Экипаж\*\*:.*?(?=\n\n|\Z)",
        r"\*\*Пассажиры\*\*:.*?(?=\n\n|\Z)",
        r"\*\*Проверка пилотирования\*\*:.*?(?=\n\n|\Z)",
        r"\*\*КБ\*\*:.*?(?=\n\n|\Z)",
        r"\*\*Стойкость\*\*:.*?(?=\n\n|\Z)",
        r"\*\*Твердость\*\*:.*?(?=\n\n|\Z)",
        r"\*\*ОЗ\*\*:.*?(?=\n\n|\Z)",
        r"\*\*Иммунитеты\*\*:.*?(?=\n\n|\Z)",
        r"\*\*Скорость\*\*:.*?(?=\n\n|\Z)",
        r"\*\*Столкновение\*\*:.*?(?=\n\n|\Z)",
    ]:
        abilities_body = re.sub(pattern, "", abilities_body, flags=re.DOTALL | re.MULTILINE)
    abilities_text = clean(abilities_body)
    if len(abilities_text) < 5:
        abilities_text = None

    name_en_clean = name_en.strip()
    slug = slugify(name_en_clean) or slugify(name_ru)

    return {
        "slug": slug,
        "name": name_en_clean,
        "nameRu": name_ru.strip(),
        "level": level,
        "size": size,
        "price": price,
        "dimensions": dimensions,
        "crew": crew,
        "passengers": passengers,
        "pilotingCheck": piloting_check,
        "ac": ac,
        "fortitude": fortitude,
        "hardness": hardness,
        "hp": hp,
        "brokenThreshold": broken_threshold,
        "immunities": immunities,
        "speed": speed,
        "collision": collision,
        "abilitiesText": abilities_text,
        "source": source,
    }


def main() -> None:
    repo_dir = download_and_extract()
    vehicles_file = repo_dir / "source" / "game_mastering" / "subsystems" / "Vehicles.rst"
    text = vehicles_file.read_text(encoding="utf-8")

    headers = list(HEADER_RE.finditer(text))
    print(f"Найдено заголовков-статблоков: {len(headers)}")

    results = []
    failed = 0
    for i, m in enumerate(headers):
        name_en = m.group("name_en") or m.group("name_en2") or ""
        body_start = m.end()
        body_end = headers[i + 1].start() if i + 1 < len(headers) else len(text)
        body = text[body_start:body_end]

        entry = parse_block(m.group("name_ru"), name_en, int(m.group("level")), body)
        if entry is None:
            failed += 1
            print(f"  не распознано: {m.group('name_ru')}")
            continue
        results.append(entry)

    print(f"Успешно распознано: {len(results)}, не распознано: {failed}")

    seen_slugs: dict[str, int] = {}
    for entry in results:
        slug = entry["slug"]
        if slug in seen_slugs:
            seen_slugs[slug] += 1
            entry["slug"] = f"{slug}-{seen_slugs[slug]}"
        else:
            seen_slugs[slug] = 0

    out_path = DATA / "pf2e-vehicles.json"
    out_path.write_text(json.dumps(results, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"Записано {len(results)} транспортных средств в {out_path}")


if __name__ == "__main__":
    main()
