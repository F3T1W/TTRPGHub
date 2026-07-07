#!/usr/bin/env python3
"""Генерирует RU-overlay для PF2e (L.6): conditions + glossary + частичные spells/monsters."""

from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "src/Infrastructure/TTRPGHub.Persistence/Seeding/Data"
OUT = ROOT / "src/Presentation/TTRPGHub.Web/wwwroot/locale/pf2e"

# Официальные/устоявшиеся русские названия состояний PF2e (Player Core).
CONDITION_NAMES_RU: dict[str, str] = {
    "blinded": "Ослеплён",
    "broken": "Сломан",
    "clumsy": "Неуклюжесть",
    "concealed": "Укрыт",
    "confused": "Замешательство",
    "controlled": "Подчинён",
    "cursebound": "Связан проклятием",
    "dazzled": "Ослеплён ярким светом",
    "deafened": "Оглох",
    "doomed": "Обречён",
    "drained": "Истощён",
    "dying": "Умирающий",
    "encumbered": "Перегружен",
    "enfeebled": "Ослаблен",
    "fascinated": "Очарован",
    "fatigued": "Усталость",
    "fleeing": "Бегство",
    "friendly": "Дружелюбный",
    "frightened": "Испуган",
    "grabbed": "Схвачен",
    "helpful": "Готов помочь",
    "hidden": "Скрыт",
    "hostile": "Враждебный",
    "immobilized": "Обездвижен",
    "indifferent": "Безразличный",
    "invisible": "Невидим",
    "observed": "Замечен",
    "off-guard": "Застигнут врасплох",
    "paralyzed": "Парализован",
    "persistent-damage": "Продолжительный урон",
    "petrified": "Окаменён",
    "prone": "Сбит с ног",
    "quickened": "Ускорен",
    "restrained": "Скован",
    "sickened": "Тошнота",
    "slowed": "Замедлен",
    "stunned": "Ошеломлён",
    "stupefied": "Ступор",
    "unconscious": "Без сознания",
    "undetected": "Не обнаружен",
    "unfriendly": "Недружелюбный",
    "unnoticed": "Незамечен",
    "wounded": "Ранен",
}

GLOSSARY_RU: dict[str, str] = {
    # Традиции
    "arcane": "аркана",
    "divine": "божественная",
    "occult": "оккультная",
    "primal": "первобытная",
    # Частые свойства
    "attack": "атака",
    "cantrip": "заговор",
    "concentrate": "концентрация",
    "manipulate": "манипуляция",
    "mental": "ментальное",
    "emotion": "эмоция",
    "fear": "страх",
    "fire": "огонь",
    "cold": "холод",
    "electricity": "электричество",
    "sonic": "звук",
    "acid": "кислота",
    "poison": "яд",
    "healing": "исцеление",
    "necromancy": "некромантия",
    "evocation": "эвокация",
    "abjuration": "абъюрация",
    "conjuration": "конъюрация",
    "divination": "прорицание",
    "enchantment": "очарование",
    "illusion": "иллюзия",
    "transmutation": "трансмутация",
    "uncommon": "необычное",
    "rare": "редкое",
    "universal": "универсальное",
    # Размеры
    "tiny": "крошечный",
    "small": "маленький",
    "medium": "средний",
    "large": "большой",
    "huge": "огромный",
    "gargantuan": "гигантский",
    # Типы урона
    "bludgeoning": "дробящий",
    "piercing": "колющий",
    "slashing": "рубящий",
    "spirit": "дух",
    "holy": "святой",
    "unholy": "нечестивый",
    # Применение
    "1 action": "1 действие",
    "2 actions": "2 действия",
    "3 actions": "3 действия",
    "reaction": "реакция",
    "free-action": "свободное действие",
}

# Часто встречающиеся за столом заклинания/монстры (имя; описания — EN fallback).
SPELLS_RU: dict[str, str] = {
    "lay-on-hands": "Прикосновение исцеления",
    "heal": "Исцеление",
    "magic-missile": "Волшебная стрела",
    "fireball": "Огненный шар",
    "shield": "Щит",
    "bless": "Благословение",
    "charm": "Очарование",
    "grease": "Скользкость",
    "mage-armor": "Магический доспех",
    "lightning-bolt": "Молния",
    "invisibility": "Невидимость",
    "dispel-magic": "Рассеивание магии",
    "haste": "Ускорение",
    "slow": "Замедление",
    "power-attack": "Мощная атака",
}

MONSTERS_RU: dict[str, str] = {
    "goblin-warrior": "Гоблин-воин",
    "goblin-boss": "Гоблин-вожак",
    "orc-warrior": "Орк-воин",
    "skeleton-guard": "Скелет-страж",
    "zombie-shambler": "Шаркающий зомби",
    "wolf": "Волк",
    "giant-spider": "Гигантский паук",
    "ogre-warrior": "Огр-воин",
    "troll": "Тролль",
    "dragon-warden": "Драконий страж",
}


def write_json(path: Path, data: object) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def build_conditions() -> dict[str, dict[str, str]]:
    conditions = json.loads((DATA / "pf2e-conditions.json").read_text(encoding="utf-8"))
    out: dict[str, dict[str, str]] = {}
    for c in conditions:
        slug = c["slug"]
        name_ru = CONDITION_NAMES_RU.get(slug)
        if not name_ru:
            continue
        out[slug] = {"name": name_ru}
    return out


def build_spells() -> dict[str, dict[str, str]]:
    spells = json.loads((DATA / "pf2e-spells.json").read_text(encoding="utf-8"))
    by_slug = {s["slug"].lower(): s for s in spells}
    out: dict[str, dict[str, str]] = {}
    for slug, name_ru in SPELLS_RU.items():
        if slug in by_slug:
            out[slug] = {"name": name_ru}
    return out


def build_monsters() -> dict[str, dict[str, str]]:
    monsters = json.loads((DATA / "pf2e-monsters.json").read_text(encoding="utf-8"))
    slugs = {m["slug"].lower() for m in monsters}
    out: dict[str, dict[str, str]] = {}
    for slug, name_ru in MONSTERS_RU.items():
        if slug in slugs:
            out[slug] = {"name": name_ru}
    return out


def main() -> None:
    write_json(OUT / "glossary.ru.json", GLOSSARY_RU)
    write_json(OUT / "conditions.ru.json", build_conditions())
    write_json(OUT / "spells.ru.json", build_spells())
    write_json(OUT / "monsters.ru.json", build_monsters())
    write_json(OUT / "entries.ru.json", {})
    print(f"Wrote locale files to {OUT}")


if __name__ == "__main__":
    main()
