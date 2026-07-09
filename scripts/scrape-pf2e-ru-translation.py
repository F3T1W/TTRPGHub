#!/usr/bin/env python3
"""M.4 — массовый RU-overlay для spells/monsters из pf2e_ru_translation (GitLab, RST-исходники).

Лицензия: сайт/репозиторий распространяется под Paizo Community Use Policy ("нельзя брать
плату за доступ к этому контенту" — TTRPGHub бесплатен, условие соблюдено) + OGL 1.0a (Open
Game Content, требует атрибуции). Импортируем ТОЛЬКО текстовые name/description как overlay
поверх уже существующих slug'ов из pf2e-spells.json/pf2e-monsters.json (структурные числа —
AC/HP/спасброски и т.д. — остаются из уже импортированного ORC/OGL источника, не отсюда) —
то же архитектурное решение, что и community-overlay L.6, просто теперь заполненное массово
вместо горстки вручную подобранных записей.

Источник исходников — не рендер readthedocs (HTML), а сырые .rst из GitLab-репозитория:
надёжнее для парсинга (хорошо структурированный текст с фиксированными полями), плюс один
запрос архива вместо тысяч отдельных HTTP-запросов к сайту.
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
ARCHIVE_URL = (
    "https://gitlab.com/api/v4/projects/pf2e_ru%2Fpf2e_ru_translation"
    "/repository/archive.tar.gz?sha=master"
)
CACHE_DIR = ROOT / ".cache" / "pf2e-ru-translation"


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


def slugify_from_filename(path: Path) -> str:
    # Файлы уже названы в стиле "Giant-Bat.rst" — ровно тот же слаг-паттерн, что у наших
    # английских pf2e-*.json (name.lower().replace(' ', '-')), просто нужно привести в нижний
    # регистр и убрать расширение.
    return path.stem.lower()


# |д-2| и т.п. — необязательный маркер стоимости действия перед "/", "-го"/"-ого" —
# необязательный порядковый суффикс уровня ("1-го").
NAME_RE_SPELL = re.compile(
    r"^(?P<name>.+?)\s+\(`.+?<.*?>`_\)\s*(?:\|[^|]+\|\s*)?/\s*(?:Закл\.|Чары)\s*"
    r"(?P<level>-?\d+)(?:-?(?:го|ого|й))?\s*$",
    re.MULTILINE,
)
NAME_RE_CREATURE = re.compile(
    r"^(?P<name>.+?)\s+\(`.+?<.*?>`_\)\s*(?:\|[^|]+\|\s*)?/\s*Существо\s*(?P<level>-?\d+)\s*$",
    re.MULTILINE,
)
SEPARATOR_RE = re.compile(r"^-{5,}$", re.MULTILINE)
DIRECTIVE_OR_HEADING_RE = re.compile(r"^(\.\. |={5,}$)", re.MULTILINE)


DIRECTIVE_LINE_RE = re.compile(r"^\.\. .*(\n[ \t]+.*)*$", re.MULTILINE)


def clean_body(text: str) -> str:
    # Отрезаем хвостовые RST-директивы (.. include::, .. versionchanged:: + их
    # tab-indented продолжение) — они не часть читаемого текста, это разметка/служебные пометки.
    text = DIRECTIVE_LINE_RE.sub("", text)
    # Убираем RST-разметку ролей/ссылок (:ref:`...`, :t_xxx:`...`, `Text <url>`_) — оставляем
    # только читаемый русский текст, без синтаксического мусора, который в UI не нужен.
    text = re.sub(r":[a-z_]+:`([^`]+)`", r"\1", text)
    text = re.sub(r"`([^`<]+)\s*<[^>]+>`_", r"\1", text)
    text = re.sub(r"\|[^|]+\|", "", text)  # |д-1|, |treasure| и т.п. inline-подстановки
    # UI не рендерит markdown/RST-жирный (см. Table.razor/SpellDetail — plain text) — уже и
    # английский исходник (pf2e-spells.json) хранит Heightened без **, для консистентности
    # убираем и у нас, а не выводим сырые звёздочки.
    text = text.replace("**", "")
    text = re.sub(r"\n{3,}", "\n\n", text)
    return text.strip()


def parse_spell(text: str) -> dict | None:
    m = NAME_RE_SPELL.search(text)
    if not m:
        return None
    name = m.group("name").strip()

    after_name = text[m.end():]
    sep_matches = list(SEPARATOR_RE.finditer(after_name))
    if not sep_matches:
        return None

    # Основное описание — между первым и последним разделителем "----------".
    # Если разделитель один (нет усиления) — всё после него.
    if len(sep_matches) >= 2:
        description = after_name[sep_matches[0].end():sep_matches[1].start()]
        heightened_block = after_name[sep_matches[1].end():]
    else:
        description = after_name[sep_matches[0].end():]
        heightened_block = ""

    description = clean_body(description)
    heightened = None
    if heightened_block.strip().startswith("**Усиление"):
        heightened = clean_body(heightened_block)

    if not name or not description:
        return None

    entry = {"name": name, "description": description}
    if heightened:
        entry["heightened"] = heightened
    return entry


def parse_creature(text: str) -> dict | None:
    m = NAME_RE_CREATURE.search(text)
    if not m:
        return None
    name = m.group("name").strip()

    # Флейвор-текст (если есть) — блок между заголовком "creature-details" (первая строка
    # файла, RU-имя под "====") и первой директивой (следующий ".. rst-class::"/подраздел).
    # У части монстров (как Kraken) после флейвора идут ещё под-разделы (сокровища/локации) —
    # они НЕ часть описания монстра, отсекаются на первой же директиве.
    flavor_match = re.search(r"={5,}\n(?P<flavor>.*?)(?=\n\.\. )", text, re.DOTALL)
    description = clean_body(flavor_match.group("flavor")) if flavor_match else ""

    if not name:
        return None

    entry: dict = {"name": name}
    if description:
        entry["description"] = description
    return entry


def collect(base: Path, subdir: str, parse_fn, expected_prefix: str) -> tuple[dict[str, dict], int, int]:
    files = sorted((base / subdir).rglob("*.rst"))
    files = [f for f in files if f.name.lower() != "index.rst"]
    parsed: dict[str, dict] = {}
    failed = 0
    for f in files:
        text = f.read_text(encoding="utf-8")
        entry = parse_fn(text)
        if entry is None:
            failed += 1
            continue
        parsed[slugify_from_filename(f)] = entry
    print(f"{expected_prefix}: {len(files)} файлов, распознано {len(parsed)}, не распознано {failed}")
    return parsed, len(files), failed


def merge_with_existing_slugs(parsed: dict[str, dict], known_slugs: set[str]) -> dict[str, dict]:
    # Оверлей имеет смысл только для того, что уже есть в нашей базе (см. модуль docstring) —
    # запись без совпадения по слагу просто отбрасывается, а не создаёт мусорную ссылку в никуда.
    return {slug: entry for slug, entry in parsed.items() if slug in known_slugs}


def write_json(path: Path, data: object) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def main() -> None:
    repo_dir = download_and_extract()
    source = repo_dir / "source"

    known_spell_slugs = {s["slug"].lower() for s in json.loads((DATA / "pf2e-spells.json").read_text(encoding="utf-8"))}
    known_monster_slugs = {m["slug"].lower() for m in json.loads((DATA / "pf2e-monsters.json").read_text(encoding="utf-8"))}

    spells, spell_files, spell_failed = collect(source, "spells", parse_spell, "Заклинания")
    creatures, creature_files, creature_failed = collect(source, "creatures/bestiary", parse_creature, "Существа")

    spells_matched = merge_with_existing_slugs(spells, known_spell_slugs)
    creatures_matched = merge_with_existing_slugs(creatures, known_monster_slugs)

    print(f"Заклинания: сматчено со своей базой {len(spells_matched)} из {len(spells)} распознанных "
          f"({len(known_spell_slugs)} слагов в своей базе)")
    print(f"Существа: сматчено со своей базой {len(creatures_matched)} из {len(creatures)} распознанных "
          f"({len(known_monster_slugs)} слагов в своей базе)")

    write_json(OUT / "spells.ru.json", spells_matched)
    write_json(OUT / "monsters.ru.json", creatures_matched)
    print(f"\nЗаписано в {OUT}")


if __name__ == "__main__":
    main()
