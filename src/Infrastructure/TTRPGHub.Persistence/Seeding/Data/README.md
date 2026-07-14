# Источник данных

Файлы в этой папке извлечены из открытых data-паков системы **pf2e** для Foundry VTT
(https://github.com/foundryvtt/pf2e), которые распространяются под лицензией **ORC**
(Open RPG Creative License) — официальной открытой лицензией Paizo, разрешающей
переиспользование игровых правил и характеристик Pathfinder 2e.

Контент **на английском** — не переведён (см. ROADMAP.md, раздел I.6, за статусом перевода
и обоснованием, почему это отдельная задача). `source` каждой записи указывает конкретную
книгу-первоисточник (`system.publication.title` из исходных данных Foundry).

Формат файлов — распакованный (flattened) JSON-массив, а не оригинальная схема Foundry:
скрипт-экстрактор (не входит в репозиторий) прошёл по `packs/pf2e/<category>/**/*.json`
исходного репозитория, отфильтровал записи нужного `type`, снял HTML-разметку с описаний
и сохранил только поля, нужные `Pf2eImporter`/`Pf2eContentSeeder`.

## Файлы

- `pf2e-spells.json` — 1144 заклинания (`packs/pf2e/spells`); поля `damageJson` /
  `heighteningJson` / `defenseJson` добавляются скриптом `scripts/build-pf2e-spell-automation.py`
- `pf2e-feats.json` — 6044 фита всех категорий: ancestry/class/classfeature/skill/general/bonus
  (`packs/pf2e/feats`)
- `pf2e-equipment.json` — 5672 предмета снаряжения (оружие, доспехи, щиты, расходники,
  боеприпасы, ценности, контейнеры, наборы) (`packs/pf2e/equipment`)
- `pf2e-monsters.json` — 2308 монстров (npc-акторы) из всех 62 бестиарных паков (основные
  бестиарии, приключения, PFS-сезоны). `license` в каждой записи — `ORC` или `OGL`.
  Включает `attacksJson`, `resistancesJson`/`weaknessesJson`, а также `immunitiesJson` /
  `aurasJson` / `modifiersJson` — последние три заполняются скриптом
  `scripts/build-pf2e-monster-automation.py` из Foundry `system.attributes` и rule elements.
