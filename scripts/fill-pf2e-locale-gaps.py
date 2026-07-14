#!/usr/bin/env python3
"""Дозаполняет RU-overlay машинным переводом (Google Translate, без ключа) для slug'ов,
которых нет в community-слое pf2e_ru_translation или у которых битое/пустое описание.

Запускать ПОСЛЕ scrape-pf2e-ru-translation.py и scrape-pf2e-ru-entries.py.
Кеш: .cache/pf2e-mt/ — повторный прогон не переводит заново.
"""

from __future__ import annotations

import argparse
import json
import re
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
from concurrent.futures import ThreadPoolExecutor, as_completed
from pathlib import Path
from threading import Lock

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "src/Infrastructure/TTRPGHub.Persistence/Seeding/Data"
OUT = ROOT / "src/Presentation/TTRPGHub.Web/wwwroot/locale/pf2e"
CACHE = ROOT / ".cache" / "pf2e-mt"

MAX_CHUNK = 1800
WORKERS = 4
SAVE_EVERY = 40

MACRO_RE = re.compile(
    r"(@\[[^\]]+\]|@\w+\[[^\]]+\]|@UUID\[[^\]]+\]|"
    r"spell--[a-z0-9-]+|action--[a-z0-9-]+|item--[a-z0-9-]+|"
    r"feat--[a-z0-9-]+|material--[a-z0-9-]+|cr_ability--[a-z0-9-]+)"
)

_print_lock = Lock()


def log(msg: str) -> None:
    with _print_lock:
        print(msg, flush=True)


def protect_macros(text: str) -> tuple[str, dict[str, str]]:
    placeholders: dict[str, str] = {}

    def repl(m: re.Match[str]) -> str:
        key = f"__PH{len(placeholders)}__"
        placeholders[key] = m.group(0)
        return key

    return MACRO_RE.sub(repl, text), placeholders


def restore_macros(text: str, placeholders: dict[str, str]) -> str:
    for key, value in placeholders.items():
        text = text.replace(key, value)
    return text


def translate_chunk(text: str, retries: int = 4) -> str:
    if not text or not text.strip():
        return text
    url = (
        "https://translate.googleapis.com/translate_a/single"
        f"?client=gtx&sl=en&tl=ru&dt=t&q={urllib.parse.quote(text)}"
    )
    for attempt in range(retries):
        try:
            with urllib.request.urlopen(url, timeout=45) as resp:
                data = json.loads(resp.read().decode("utf-8"))
            parts = [seg[0] for seg in data[0] if seg[0]]
            return "".join(parts) if parts else text
        except (urllib.error.URLError, TimeoutError, json.JSONDecodeError, ConnectionError, OSError) as ex:
            if attempt == retries - 1:
                log(f"  WARN: translate failed after {retries} tries: {ex}")
                return text
            time.sleep(0.5 * (attempt + 1))
    return text


def translate_text(text: str, cache_key: str) -> str:
    cache_file = CACHE / f"{cache_key}.txt"
    if cache_file.exists():
        try:
            return cache_file.read_text(encoding="utf-8")
        except UnicodeDecodeError:
            cache_file.unlink(missing_ok=True)

    protected, ph = protect_macros(text)
    if len(protected) <= MAX_CHUNK:
        result = translate_chunk(protected)
    else:
        paragraphs = protected.split("\n")
        buf: list[str] = []
        chunk = ""
        for para in paragraphs:
            if len(chunk) + len(para) + 1 > MAX_CHUNK and chunk:
                buf.append(translate_chunk(chunk))
                chunk = ""
            chunk += para + "\n"
        if chunk:
            buf.append(translate_chunk(chunk.rstrip("\n")))
        result = "\n".join(buf)

    result = restore_macros(result, ph)
    cache_file.parent.mkdir(parents=True, exist_ok=True)
    cache_file.write_text(result, encoding="utf-8")
    return result


def load_json(path: Path) -> dict | list:
    return json.loads(path.read_text(encoding="utf-8"))


def write_json(path: Path, data: object) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(
        json.dumps(data, ensure_ascii=False, indent=2, sort_keys=True) + "\n",
        encoding="utf-8",
    )


def is_bad_description(desc: str | None, en_desc: str | None) -> bool:
    if not en_desc or not en_desc.strip():
        return False
    if not desc or not desc.strip():
        return True
    d = desc.strip()
    if re.match(r"^/\s*\d+", d):
        return True
    if len(d) < 40 and len(en_desc) > 120:
        return True
    if d == en_desc:
        return True
    return False


def needs_name(existing_name: str | None, en_name: str) -> bool:
    if not existing_name or not existing_name.strip():
        return True
    return existing_name.strip() == en_name.strip()


def build_entry(
    current: dict,
    slug: str,
    en_name: str,
    en_desc: str | None,
    en_heightened: str | None,
    *,
    force: bool,
) -> dict | None:
    updated = dict(current)
    changed = False

    if force or needs_name(updated.get("name"), en_name):
        updated["name"] = translate_text(en_name, f"name-{slug}")
        changed = True

    if en_desc and (force or is_bad_description(updated.get("description"), en_desc)):
        updated["description"] = translate_text(en_desc, f"desc-{slug}")
        changed = True

    if en_heightened and (force or is_bad_description(updated.get("heightened"), en_heightened)):
        updated["heightened"] = translate_text(en_heightened, f"heightened-{slug}")
        changed = True

    return updated if changed else None


def fill_spells(overlay: dict, *, force: bool, limit: int | None) -> int:
    items = load_json(DATA / "pf2e-spells.json")
    tasks = []
    for item in items:
        slug = item["slug"].lower()
        current = overlay.get(slug, {})
        if not force and not needs_name(current.get("name"), item["name"]) and not is_bad_description(
            current.get("description"), item.get("description")
        ) and not is_bad_description(current.get("heightened"), item.get("heightened")):
            continue
        tasks.append((slug, item))

    if limit is not None:
        tasks = tasks[:limit]

    count = 0
    with ThreadPoolExecutor(max_workers=WORKERS) as pool:
        futures = {
            pool.submit(
                build_entry, overlay.get(slug, {}), slug,
                item["name"], item.get("description"), item.get("heightened"), force=force,
            ): slug
            for slug, item in tasks
        }
        for fut in as_completed(futures):
            slug = futures[fut]
            try:
                result = fut.result()
            except Exception as ex:
                log(f"  WARN: skip {slug}: {ex}")
                continue
            if result:
                overlay[slug] = result
                count += 1
                if count % SAVE_EVERY == 0:
                    write_json(OUT / "spells.ru.json", overlay)
                    log(f"  spells: {count}/{len(tasks)}...")
    return count


def fill_monsters(overlay: dict, *, force: bool, limit: int | None) -> int:
    items = load_json(DATA / "pf2e-monsters.json")
    tasks = []
    for item in items:
        slug = item["slug"].lower()
        en_desc = item.get("abilities") or item.get("description")
        current = overlay.get(slug, {})
        if not force and not needs_name(current.get("name"), item["name"]) and not is_bad_description(
            current.get("description"), en_desc
        ):
            continue
        tasks.append((slug, item, en_desc))

    if limit is not None:
        tasks = tasks[:limit]

    count = 0
    with ThreadPoolExecutor(max_workers=WORKERS) as pool:
        futures = {
            pool.submit(
                build_entry, overlay.get(slug, {}), slug,
                item["name"], en_desc, None, force=force,
            ): slug
            for slug, item, en_desc in tasks
        }
        for fut in as_completed(futures):
            slug = futures[fut]
            try:
                result = fut.result()
            except Exception as ex:
                log(f"  WARN: skip {slug}: {ex}")
                continue
            if result:
                overlay[slug] = result
                count += 1
                if count % SAVE_EVERY == 0:
                    write_json(OUT / "monsters.ru.json", overlay)
                    log(f"  monsters: {count}/{len(tasks)}...")
    return count


def fill_entries(overlay: dict, *, force: bool, limit: int | None) -> int:
    tasks = []
    for filename in ("pf2e-feats.json", "pf2e-equipment.json"):
        for item in load_json(DATA / filename):
            slug = item["slug"].lower()
            current = overlay.get(slug, {})
            if not force and not needs_name(current.get("name"), item["name"]) and not is_bad_description(
                current.get("description"), item.get("description")
            ):
                continue
            tasks.append((slug, item))

    if limit is not None:
        tasks = tasks[:limit]

    count = 0
    with ThreadPoolExecutor(max_workers=WORKERS) as pool:
        futures = {
            pool.submit(
                build_entry, overlay.get(slug, {}), slug,
                item["name"], item.get("description"), None, force=force,
            ): slug
            for slug, item in tasks
        }
        for fut in as_completed(futures):
            slug = futures[fut]
            try:
                result = fut.result()
            except Exception as ex:
                log(f"  WARN: skip {slug}: {ex}")
                continue
            if result:
                overlay[slug] = result
                count += 1
                if count % SAVE_EVERY == 0:
                    write_json(OUT / "entries.ru.json", overlay)
                    log(f"  entries: {count}/{len(tasks)}...")
    return count


SEEDER_ENTRIES: dict[str, dict[str, str]] = {
    "investigator": {"name": "Следователь", "description": "Методичный детектив, который изучает ситуацию прежде чем действовать."},
    "magus": {"name": "Магус", "description": "Заклинатель-воин, сливающий удары оружием и арканные заклинания в одно действие."},
    "oracle": {"name": "Оракул", "description": "Спонтанный божественный заклинатель, связанный с тайной и проклятием."},
    "swashbuckler": {"name": "Сорвиголова", "description": "Эффектный дуэлянт, накапливающий Панаш за смелые поступки."},
    "thaumaturge": {"name": "Тауматург", "description": "Оккультный исследователь с связанным артефактом против сверхъестественных угроз."},
    "witch": {"name": "Ведьма", "description": "Заклинатель с покровителем и фамильяром, несущим его волю."},
    "gunslinger": {"name": "Стрелок", "description": "Специалист по огнестрельному и арбалетному оружию."},
    "inventor": {"name": "Изобретатель", "description": "Создатель фирменного изобретения и импровизатор прорывов в бою."},
    "summoner": {"name": "Призыватель", "description": "Навсегда связан с эйдолоном, сражающимся рядом."},
    "psychic": {"name": "Психик", "description": "Оккультный заклинатель с состоянием «Высвободить психику»."},
    "kineticist": {"name": "Кинетик", "description": "Направляет стихийную силу через тело вместо заклинаний."},
    "guardian": {"name": "Страж", "description": "Защитник передовой линии с оборонительной стойкой."},
    "exemplar": {"name": "Экземпляр", "description": "Смертный с божественной искрой и личными иконами."},
    "catfolk": {"name": "Котолюд", "description": "Любопытный народ с кошачьими чертами и быстрыми рефлексами."},
    "kobold": {"name": "Кобольд", "description": "Маленькие драконорождённые с талантом к ловушкам и туннелям."},
    "lizardfolk": {"name": "Ящеролюд", "description": "Рептилоидный народ болот и джунглей."},
    "ratfolk": {"name": "Крысолюд", "description": "Находчивый общинный народ с острыми чувствами."},
    "leshy": {"name": "Леший", "description": "Растительное существо из магического семени."},
    "fetchling": {"name": "Фетчлинг", "description": "Гуманоиды, коснувшиеся Теневого плана."},
    "automaton": {"name": "Автоматон", "description": "Живой конструкт с душой в механическом теле."},
    "kitsune": {"name": "Кицунэ", "description": "Меняющий облик народ лисичьих духов."},
    "grippli": {"name": "Гриппли", "description": "Лягушеподобный народ болот и крон джунглей."},
    "azarketi": {"name": "Азаркети", "description": "Амфибийный прибрежный народ."},
    "weapon-potency-1": {"name": "Могущество оружия (+1)", "description": "Выгравирована на оружии; даёт предметный бонус +1 к броскам атаки."},
    "weapon-potency-2": {"name": "Могущество оружия (+2)", "description": "Выгравирована на оружии; даёт предметный бонус +2 к броскам атаки."},
    "weapon-potency-3": {"name": "Могущество оружия (+3)", "description": "Выгравирована на оружии; даёт предметный бонус +3 к броскам атаки."},
    "striking": {"name": "Руна разящего удара", "description": "Оружие наносит два кубика урона вместо одного."},
    "greater-striking": {"name": "Руна великого разящего удара", "description": "Оружие наносит три кубика урона вместо одного."},
    "major-striking": {"name": "Руна величайшего разящего удара", "description": "Оружие наносит четыре кубика урона вместо одного."},
    "armor-potency-1": {"name": "Могущество доспеха (+1)", "description": "Выгравирована на доспехе; даёт предметный бонус +1 к КБ."},
    "armor-potency-2": {"name": "Могущество доспеха (+2)", "description": "Выгравирована на доспехе; даёт предметный бонус +2 к КБ."},
    "armor-potency-3": {"name": "Могущество доспеха (+3)", "description": "Выгравирована на доспехе; даёт предметный бонус +3 к КБ."},
    "resilient": {"name": "Руна стойкости", "description": "Даёт предметный бонус +1 к спасброскам, пока доспех надет."},
    "greater-resilient": {"name": "Руна великой стойкости", "description": "Даёт предметный бонус +2 к спасброскам, пока доспех надет."},
    "major-resilient": {"name": "Руна величайшей стойкости", "description": "Даёт предметный бонус +3 к спасброскам, пока доспех надет."},
    "flaming": {"name": "Руна пламени", "description": "Дополнительный урон огнём при попадании и продолжительный урон огнём при крите."},
    "frost": {"name": "Руна мороза", "description": "Дополнительный урон холодом при попадании и продолжительный урон холодом при крите."},
    "shock": {"name": "Руна разряда", "description": "Дополнительный урон электричеством при попадании."},
    "thundering": {"name": "Руна грома", "description": "Дополнительный урон звуком; при крите цель может оглохнуть."},
    "corrosive": {"name": "Руна коррозии", "description": "Дополнительный урон кислотой при попадании."},
    "keen": {"name": "Руна остроты", "description": "Расширяет диапазон критического попадания оружия."},
    "returning": {"name": "Руна возврата", "description": "Метательное оружие возвращается в руку после атаки."},
    "ghost-touch": {"name": "Руна призрачного касания", "description": "Оружие поражает бестелесных существ нормально."},
    "bane": {"name": "Руна гибели", "description": "Бонус к атакам и урону против выбранного существа."},
    "wounding": {"name": "Руна ранения", "description": "Дополнительный продолжительный урон кровотечением."},
    "disrupting": {"name": "Руна изгнания", "description": "Дополнительный урон нежити; при крите — уничтожение или ошеломление."},
    "fortification-rune": {"name": "Руна укрепления", "description": "Шанс превратить крит по носителю в обычное попадание."},
}


def fill_seeder_entries(overlay: dict) -> int:
    count = 0
    for slug, entry in SEEDER_ENTRIES.items():
        current = overlay.get(slug, {})
        if current.get("name") != entry["name"] or current.get("description") != entry["description"]:
            overlay[slug] = {**current, **entry}
            count += 1
    return count


def main() -> None:
    parser = argparse.ArgumentParser(description="MT-дозаполнение RU-overlay PF2e")
    parser.add_argument(
        "--category", choices=["spells", "monsters", "entries", "seeder", "all"],
        default="all",
    )
    parser.add_argument("--force", action="store_true", help="Перевести заново даже существующие")
    parser.add_argument("--limit", type=int, default=None, help="Лимит новых переводов на категорию")
    args = parser.parse_args()

    if args.category in ("spells", "all"):
        spells_path = OUT / "spells.ru.json"
        spells = load_json(spells_path) if spells_path.exists() else {}
        log("Заклинания...")
        n = fill_spells(spells, force=args.force, limit=args.limit)
        write_json(spells_path, spells)
        log(f"  spells.ru.json: +{n} (всего {len(spells)})")

    if args.category in ("monsters", "all"):
        monsters_path = OUT / "monsters.ru.json"
        monsters = load_json(monsters_path) if monsters_path.exists() else {}
        log("Монстры...")
        n = fill_monsters(monsters, force=args.force, limit=args.limit)
        write_json(monsters_path, monsters)
        log(f"  monsters.ru.json: +{n} (всего {len(monsters)})")

    if args.category in ("entries", "all"):
        entries_path = OUT / "entries.ru.json"
        entries = load_json(entries_path) if entries_path.exists() else {}
        log("Фиты и снаряжение...")
        n = fill_entries(entries, force=args.force, limit=args.limit)
        write_json(entries_path, entries)
        log(f"  entries.ru.json: +{n} (всего {len(entries)})")

    if args.category in ("seeder", "all"):
        entries_path = OUT / "entries.ru.json"
        entries = load_json(entries_path) if entries_path.exists() else {}
        log("Классы/предки/руны из сидера...")
        n = fill_seeder_entries(entries)
        write_json(entries_path, entries)
        log(f"  entries.ru.json: +{n} seeder slugs")

    log("Готово.")


if __name__ == "__main__":
    main()
