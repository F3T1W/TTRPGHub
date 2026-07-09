#!/usr/bin/env python3
"""N.1 — импорт бестиария опасностей (hazards) из pf2e_ru_translation (GitLab, RST).

В отличие от M.4 (RU-оверлей поверх уже существующего английского набора монстров/заклинаний),
у хазардов нет предзагруженного английского датасета вообще — этот скрипт формирует
ПЕРВИЧНЫЙ набор данных (Seeding/Data/pf2e-hazards.json), который сеется как обычный контент
через Pf2eImporter, а не оверлей через Pf2eLocaleService. Лицензия та же (Community Use Policy
+ OGL, см. /licenses) — см. подробный разбор в scrape-pf2e-ru-translation.py.

Источник — единый файл source/game_mastering/hazards.rst (не отдельные файлы на хазард, как
у existures/spells): 98 статблоков опасностей внутри одного файла правил, разделённых
заголовками "Имя (`EN Name <url>`_) / Опасность N".
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
    r"\s*/\s*Опасность\s*(?P<level>-?\d+)\s*$",
    re.MULTILINE,
)
DIRECTIVE_LINE_RE = re.compile(r"^\.\. .*(\n[ \t]+.*)*$", re.MULTILINE)
LABEL_RE = re.compile(r"^\*\*([^*]+)\*\*:?\s*(.*)$")


def clean(text: str) -> str:
    text = DIRECTIVE_LINE_RE.sub("", text)
    # :ref:`Текст <anchor>` — роль с якорем внутри теста ссылки (в отличие от `Текст <url>`_,
    # у которой якорь снаружи backtick) — сначала срезаем " <anchor>" внутри, потом сам :ref:.
    text = re.sub(r":[a-z_]+:`([^<`]+)\s*<[^>]*>`", r"\1", text)
    text = re.sub(r":[a-z_]+:`([^`]+)`", r"\1", text)
    text = re.sub(r"`([^`<]+)\s*<[^>]+>`_", r"\1", text)
    text = re.sub(r"\|[^|]+\|", "", text)
    text = text.replace("**", "")
    text = re.sub(r"~{5,}$", "", text, flags=re.MULTILINE)
    text = re.sub(r"^-{5,}$", "", text, flags=re.MULTILINE)
    text = re.sub(r"\n{3,}", "\n\n", text)
    text = re.sub(r"[ \t]+,", ",", text)  # "слово ," после срезания якоря :ref: → "слово,"
    text = re.sub(r"[ \t]+\n", "\n", text)
    return text.strip()


def slugify(name_en: str) -> str:
    s = re.sub(r"[^a-zA-Z0-9]+", "-", name_en.strip()).strip("-").lower()
    return s


def extract_int(pattern: str, text: str) -> int | None:
    m = re.search(pattern, text)
    return int(m.group(1)) if m else None


def parse_block(name_ru: str, name_en: str, level: int, body: str) -> dict | None:
    # Тело начинается с RST-подчёркивания заголовка "~~~~~~~" (часть секции, не контент) —
    # его нужно срезать до поиска трейтов, иначе маркированный список трейтов не в самом
    # начале строки body и re.match("^- ...") с якорем на позицию 0 не сработает.
    rest = re.sub(r"^\s*~{5,}\s*\n+", "", body)

    # Трейты — маркированный список сразу после заголовка, до первого **Label**.
    traits_match = re.match(r"((?:^- .+\n?)+)", rest, re.MULTILINE)
    traits = []
    if traits_match:
        traits = [t.strip("- ").strip() for t in traits_match.group(1).splitlines() if t.strip()]
        rest = rest[traits_match.end():]

    stealth_m = re.search(r"\*\*Скрытность\*\*:\s*(?:КС\s*)?([+-]?\d+)\s*(\([^)]*\))?", rest)
    if not stealth_m:
        return None
    stealth_dc = int(stealth_m.group(1))
    stealth_note = stealth_m.group(2)

    desc_m = re.search(r"\*\*Описание\*\*:\s*(.+?)(?=\n\n|\*\*|-{5,})", rest, re.DOTALL)
    description = clean(desc_m.group(1)) if desc_m else None

    disable_m = re.search(r"\*\*Отключение\*\*:\s*(.+?)(?=\n\n\*\*|\n\n-{5,}|\Z)", rest, re.DOTALL)
    disable_text = clean(disable_m.group(1)) if disable_m else None

    ac = extract_int(r"\*\*КБ\*\*:\s*(\d+)", rest)
    fort = extract_int(r"\*\*Стойкость\*\*:\s*\+?(-?\d+)", rest)
    reflex = extract_int(r"\*\*Рефлекс\*\*:\s*\+?(-?\d+)", rest)
    hardness = extract_int(r"\*\*Твердость[^*:]*\*\*:\s*(\d+)", rest)
    hp = extract_int(r"\*\*ОЗ[^*:]*\*\*:\s*(\d+)", rest)

    immunities_m = re.search(r"\*\*Иммунитеты\*\*:\s*(.+?)(?=\n\n|\*\*[А-ЯA-Z][а-яa-z]+\*\* \||\Z)", rest, re.DOTALL)
    immunities = None
    if immunities_m:
        cleaned = clean(immunities_m.group(1))
        # Список идёт по одной позиции на строку, каждая уже с запятой на конце (кроме
        # последней) — джойним запятой+пробелом одним разом, а не оставляем "X,\nY,\nZ".
        items = [s.strip().rstrip(",") for s in cleaned.splitlines() if s.strip()]
        immunities = ", ".join(items)

    reset_m = re.search(r"\*\*Сброс\*\*:\s*(.+?)\Z", rest, re.DOTALL)
    reset_text = clean(reset_m.group(1)) if reset_m else None

    # Всё, что не "Отключение/КБ/Стойкость/Рефлекс/Твердость/ОЗ/Иммунитеты/Сброс" — способности/
    # реакции/рутина опасности (аналог Pf2eMonster.Abilities — единым блоком, не структурируем
    # каждую атаку отдельно, это отдельная задача не для первого среза).
    abilities_body = rest
    for pattern in [
        r"\*\*Источник\*\*:.*?(?=\n\n|\Z)",
        r"\*\*Скрытность\*\*:.*?(?=\n\n|\Z)",
        r"\*\*Описание\*\*:.*?(?=\n\n|\*\*|-{5,}|\Z)",
        r"\*\*Отключение\*\*:.*?(?=\n\n|\Z)",
        r"\*\*КБ\*\*:.*?(?=\n\n|\Z)",
        r"\*\*Твердость[^*:]*\*\*:.*?(?=\n\n|\Z)",
        r"\*\*ОЗ[^*:]*\*\*:.*?(?=\n\n|\Z)",
        r"\*\*Иммунитеты\*\*:.*?(?=\n\n|\*\*[А-ЯA-Z][а-яa-z]+\*\* \||\Z)",
        r"\*\*Сброс\*\*:.*\Z",
    ]:
        abilities_body = re.sub(pattern, "", abilities_body, flags=re.DOTALL)
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
        "traits": ", ".join(traits),
        "stealthDc": stealth_dc,
        "stealthNote": stealth_note.strip("()") if stealth_note else None,
        "description": description,
        "disableText": disable_text,
        "ac": ac,
        "fortitude": fort,
        "reflex": reflex,
        "hardness": hardness,
        "hp": hp,
        "immunities": immunities,
        "abilitiesText": abilities_text,
        "resetText": reset_text,
        "source": "GM Core / Core Rulebook hazard appendix",
    }


def main() -> None:
    repo_dir = download_and_extract()
    hazards_file = repo_dir / "source" / "game_mastering" / "hazards.rst"
    text = hazards_file.read_text(encoding="utf-8")

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

    out_path = DATA / "pf2e-hazards.json"
    out_path.write_text(json.dumps(results, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"Записано {len(results)} опасностей в {out_path}")


if __name__ == "__main__":
    main()
