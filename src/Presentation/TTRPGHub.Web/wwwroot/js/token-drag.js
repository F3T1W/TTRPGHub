window.tableDrag = {
    _dotNetRef: null,
    _containerEl: null,
    _dragging: null,
    _bound: false,
    _cellSizePx: 50,
    _measureMode: false,
    _measureOrigin: null,
    _measureLineEl: null,
    _measureLabelEl: null,
    _pingMode: false,
    _templateMode: null,
    _templateOrigin: null,
    _templateEl: null,
    _wallMode: false,
    _wallOrigin: null,
    _wallPreviewEl: null,
    _lightMode: false,
    _fogSvgEl: null,
    _fogShapesEl: null,
    _fogDefsEl: null,
    _audioCtx: null,

    // K.4 — звук броска кубиков синтезируется через Web Audio (короткие шумовые щелчки убывающей
    // громкости, имитация стука костей), а не проигрывается из аудиофайла: не нужно тащить в
    // проект бинарный ассет и решать вопрос лицензии на звук ради 300-миллисекундного эффекта.
    playDiceSound: function () {
        try {
            if (!this._audioCtx) this._audioCtx = new (window.AudioContext || window.webkitAudioContext)();
            const ctx = this._audioCtx;
            if (ctx.state === 'suspended') ctx.resume();

            const clickCount = 4 + Math.floor(Math.random() * 3);
            for (let i = 0; i < clickCount; i++) {
                const t = ctx.currentTime + i * (0.03 + Math.random() * 0.04);
                const bufferSize = ctx.sampleRate * 0.03;
                const buffer = ctx.createBuffer(1, bufferSize, ctx.sampleRate);
                const data = buffer.getChannelData(0);
                for (let j = 0; j < bufferSize; j++) data[j] = (Math.random() * 2 - 1) * (1 - j / bufferSize);

                const noise = ctx.createBufferSource();
                noise.buffer = buffer;
                const filter = ctx.createBiquadFilter();
                filter.type = 'bandpass';
                filter.frequency.value = 800 + Math.random() * 800;
                const gain = ctx.createGain();
                gain.gain.value = 0.25 * (1 - i / (clickCount + 1));

                noise.connect(filter);
                filter.connect(gain);
                gain.connect(ctx.destination);
                noise.start(t);
            }
        } catch { /* Web Audio недоступен (например, старый браузер) — просто без звука */ }
    },

    init: function (containerId, dotNetRef, cellSizePx) {
        this._dotNetRef = dotNetRef;
        this._containerEl = document.getElementById(containerId);
        this._cellSizePx = cellSizePx || 50;
        if (!this._containerEl) return;

        this._containerEl.addEventListener('pointerdown', this._onPointerDown.bind(this));
        if (!this._bound) {
            document.addEventListener('pointermove', this._onPointerMove.bind(this));
            document.addEventListener('pointerup', this._onPointerUp.bind(this));
            this._bound = true;
        }
    },

    // ГМ меняет размер клетки на лету (число в поле "Сетка") — обновляем без переинициализации drag.
    setCellSize: function (cellSizePx) {
        this._cellSizePx = cellSizePx || 50;
    },

    // L.7 — PF2e 5-10-5-10: чередование стоимости диагоналей при измерении по сетке.
    pf2eGridDistanceFeet: function (cellsX, cellsY) {
        const diagonals = Math.min(cellsX, cellsY);
        const straights = Math.max(cellsX, cellsY) - diagonals;
        let diagCost = 0;
        for (let i = 0; i < diagonals; i++) diagCost += (i % 2 === 0) ? 5 : 10;
        return diagCost + straights * 5;
    },

    _gridCellsBetween: function (originPx, cursorPx) {
        const cellsX = Math.round(Math.abs(cursorPx.x - originPx.x) / this._cellSizePx);
        const cellsY = Math.round(Math.abs(cursorPx.y - originPx.y) / this._cellSizePx);
        return { cellsX, cellsY };
    },

    // Линейка: клетка = 5 футов, диагонали PF2e 5-10-5-10; измерение синхронизируется через SignalR.
    setMeasureMode: function (enabled) {
        this._measureMode = !!enabled;
        if (enabled) this._pingMode = false;
        this._measureOrigin = null;
        this._clearMeasureOverlay();
    },

    setPingMode: function (enabled) {
        this._pingMode = !!enabled;
        if (enabled) this._measureMode = false;
        this._measureOrigin = null;
        this._clearMeasureOverlay();
    },

    _ensureMeasureOverlay: function () {
        if (!this._containerEl) return;
        if (!this._measureLineEl) {
            this._measureLineEl = document.createElement('div');
            this._measureLineEl.className = 'ta-measure-line';
            this._containerEl.appendChild(this._measureLineEl);
        }
        if (!this._measureLabelEl) {
            this._measureLabelEl = document.createElement('div');
            this._measureLabelEl.className = 'ta-measure-label';
            this._containerEl.appendChild(this._measureLabelEl);
        }
    },

    _clearMeasureOverlay: function () {
        if (this._measureLineEl) { this._measureLineEl.remove(); this._measureLineEl = null; }
        if (this._measureLabelEl) { this._measureLabelEl.remove(); this._measureLabelEl = null; }
    },

    _updateMeasureOverlay: function (originPx, cursorPx, feet) {
        this._ensureMeasureOverlay();
        const dx = cursorPx.x - originPx.x;
        const dy = cursorPx.y - originPx.y;
        const length = Math.sqrt(dx * dx + dy * dy);
        const angle = Math.atan2(dy, dx) * (180 / Math.PI);

        this._measureLineEl.style.left = originPx.x + 'px';
        this._measureLineEl.style.top = originPx.y + 'px';
        this._measureLineEl.style.width = length + 'px';
        this._measureLineEl.style.transform = `rotate(${angle}deg)`;

        this._measureLabelEl.style.left = cursorPx.x + 'px';
        this._measureLabelEl.style.top = cursorPx.y + 'px';
        this._measureLabelEl.textContent = feet + ' фт.';
    },

    // L.7 — удалённая линейка другого участника (эфемерная, 4 с).
    showRemoteMeasure: function (x1, y1, x2, y2, feet) {
        if (!this._containerEl) return;
        const originPx = { x: x1 * this._cellSizePx, y: y1 * this._cellSizePx };
        const cursorPx = { x: x2 * this._cellSizePx, y: y2 * this._cellSizePx };
        this._updateMeasureOverlay(originPx, cursorPx, feet);
        setTimeout(() => this._clearMeasureOverlay(), 4000);
    },

    // L.7 — пинг: пульсирующий круг в клетках сетки.
    showPing: function (x, y) {
        if (!this._containerEl) return;
        const el = document.createElement('div');
        el.className = 'ta-map-ping';
        el.style.left = (x * this._cellSizePx) + 'px';
        el.style.top = (y * this._cellSizePx) + 'px';
        this._containerEl.appendChild(el);
        setTimeout(() => el.remove(), 2000);
    },

    // J.5 — шаблоны зоны поражения: раньше это была чисто локальная прикидка (даже не видная
    // другим игрокам, исчезающая при выходе из режима рисования). Теперь после отпускания кнопки
    // шаблон остаётся на месте (снапнутый к сетке) и рассылается остальным участникам напрямую
    // через TableHub (BroadcastTemplate/ClearTemplate — без персистентности в БД: шаблон по
    // природе эфемерный, ГМ ставит его, чтобы обсудить зону, и убирает после разрешения эффекта).
    // Подсветку задетых токенов считает Table.razor.cs (геометрия в футах), не эта функция —
    // здесь только визуальный прямоугольник/круг/треугольник. Геометрия сознательно упрощена:
    // конус — равнобедренный треугольник через clip-path (не точная дуга PF2e, ~90° раствора),
    // сфера — круг диаметром 2×радиус, линия — прямоугольник шириной в одну клетку (5 фт).
    setTemplateMode: function (type, feet) {
        this._templateMode = type ? { type: type, feet: feet || 15 } : null;
        this._templateOrigin = null;
    },

    clearPlacedTemplate: function () {
        if (this._templateEl) { this._templateEl.remove(); this._templateEl = null; }
    },

    _ensureTemplateOverlay: function (type) {
        if (!this._containerEl) return;
        if (this._templateEl && this._templateEl.dataset.shape !== type) this.clearPlacedTemplate();
        if (!this._templateEl) {
            this._templateEl = document.createElement('div');
            this._templateEl.className = 'ta-template ta-template-' + type;
            this._templateEl.dataset.shape = type;
            this._containerEl.appendChild(this._templateEl);
        }
    },

    _updateTemplateOverlay: function (originPx, cursorPx) {
        const { type, feet } = this._templateMode;
        const dx = cursorPx.x - originPx.x;
        const dy = cursorPx.y - originPx.y;
        const angleDeg = Math.atan2(dy, dx) * (180 / Math.PI);
        this._renderTemplateShape(type, feet, originPx, angleDeg);
    },

    // originPx — фиксированная позиция (уже снапнутая к сетке при размещении), angleDeg —
    // направление конуса/линии в градусах. Используется и для живого превью при рисовании, и
    // для отрисовки уже размещённого/полученного от другого клиента шаблона (showTemplate).
    // animate=true (K.4) — проиграть анимацию появления (не при каждом кадре живого превью во
    // время рисования, только когда шаблон зафиксирован — свой или пришедший по SignalR).
    _renderTemplateShape: function (type, feet, originPx, angleDeg, animate) {
        const lengthPx = (feet / 5) * this._cellSizePx;
        this._ensureTemplateOverlay(type);

        if (animate) {
            this._templateEl.classList.remove('ta-template-pop');
            void this._templateEl.offsetWidth; // форсируем reflow, чтобы анимация запустилась заново
            this._templateEl.classList.add('ta-template-pop');
        }

        if (type === 'burst') {
            this._templateEl.style.left = (originPx.x - lengthPx) + 'px';
            this._templateEl.style.top = (originPx.y - lengthPx) + 'px';
            this._templateEl.style.width = (lengthPx * 2) + 'px';
            this._templateEl.style.height = (lengthPx * 2) + 'px';
            this._templateEl.style.transform = '';
            return;
        }

        this._templateEl.style.left = originPx.x + 'px';
        this._templateEl.style.width = lengthPx + 'px';
        if (type === 'line') {
            this._templateEl.style.top = (originPx.y - this._cellSizePx / 2) + 'px';
            this._templateEl.style.height = this._cellSizePx + 'px';
        } else {
            this._templateEl.style.top = (originPx.y - lengthPx) + 'px';
            this._templateEl.style.height = (lengthPx * 2) + 'px';
        }
        this._templateEl.style.transform = `rotate(${angleDeg}deg)`;
        this._templateEl.style.transformOrigin = 'left center';
    },

    // Отрисовка шаблона по координатам сетки (клетки) — свой (после размещения) и полученный по
    // SignalR от ГМ/другого игрока рендерятся одной и той же функцией.
    showTemplate: function (type, feet, originXCells, originYCells, angleDeg) {
        const originPx = { x: originXCells * this._cellSizePx, y: originYCells * this._cellSizePx };
        this._renderTemplateShape(type, feet, originPx, angleDeg, true);
    },

    // Стены сцены (I.4) — GM рисует отрезки кликом-протяжкой, как линейка/шаблон. Координаты
    // отдаются в C# в клетках сетки (не px), чтобы не зависеть от размера клетки при её смене.
    setWallMode: function (enabled) {
        this._wallMode = !!enabled;
        this._wallOrigin = null;
        this._clearWallPreview();
    },

    // J.3 — режим размещения источника света: один клик по сцене ставит источник в этой точке
    // (радиус/цвет потом настраиваются в списке источников на панели, не через drag — в отличие
    // от стен, у света нет "направления", только позиция).
    setLightMode: function (enabled) {
        this._lightMode = !!enabled;
    },

    // Косметический слой освещения — мягкое радиальное свечение, видно всем (GM и игрокам)
    // независимо от тумана: у источника света это единственная цель, видимость по нему считает
    // updateFog отдельно (см. ниже). Пересоздаём с нуля при каждом вызове — источников на сцене
    // немного (единицы-десятки), разница с diff-обновлением не оправдывает сложность.
    renderLights: function (lightsGrid) {
        if (!this._containerEl) return;
        this._containerEl.querySelectorAll('.ta-light-glow').forEach(el => el.remove());

        for (const l of lightsGrid) {
            const cx = l.x * this._cellSizePx, cy = l.y * this._cellSizePx;
            const dimPx = (l.dimRadiusFeet / 5) * this._cellSizePx;
            const el = document.createElement('div');
            el.className = 'ta-light-glow';
            el.style.left = (cx - dimPx) + 'px';
            el.style.top = (cy - dimPx) + 'px';
            el.style.width = (dimPx * 2) + 'px';
            el.style.height = (dimPx * 2) + 'px';
            const color = l.color || '#f59e0b';
            el.style.background = `radial-gradient(circle, ${color}55 0%, ${color}22 60%, transparent 100%)`;
            this._containerEl.appendChild(el);
        }
    },

    _clearWallPreview: function () {
        if (this._wallPreviewEl) { this._wallPreviewEl.remove(); this._wallPreviewEl = null; }
    },

    _ensureWallPreview: function () {
        if (!this._containerEl) return;
        if (!this._wallPreviewEl) {
            this._wallPreviewEl = document.createElement('div');
            this._wallPreviewEl.className = 'ta-wall-preview';
            this._containerEl.appendChild(this._wallPreviewEl);
        }
    },

    _updateWallPreview: function (originPx, cursorPx) {
        this._ensureWallPreview();
        const dx = cursorPx.x - originPx.x;
        const dy = cursorPx.y - originPx.y;
        const length = Math.sqrt(dx * dx + dy * dy);
        const angle = Math.atan2(dy, dx) * (180 / Math.PI);

        this._wallPreviewEl.style.left = originPx.x + 'px';
        this._wallPreviewEl.style.top = originPx.y + 'px';
        this._wallPreviewEl.style.width = length + 'px';
        this._wallPreviewEl.style.transform = `rotate(${angle}deg)`;
    },

    // Отрисовка уже сохранённых стен — вызывается вместе с обновлением тумана (updateFog),
    // видна только GM (в режиме рисования и вообще при активном тумане, чтобы понимать
    // геометрию), игрокам сами линии стен не показываем — только их эффект (туман).
    renderWalls: function (wallsGrid, showLines) {
        if (!this._containerEl) return;
        this._containerEl.querySelectorAll('.ta-wall-line').forEach(el => el.remove());
        if (!showLines) return;

        for (const w of wallsGrid) {
            const x1 = w.x1 * this._cellSizePx, y1 = w.y1 * this._cellSizePx;
            const x2 = w.x2 * this._cellSizePx, y2 = w.y2 * this._cellSizePx;
            const dx = x2 - x1, dy = y2 - y1;
            const length = Math.sqrt(dx * dx + dy * dy);
            const angle = Math.atan2(dy, dx) * (180 / Math.PI);

            const el = document.createElement('div');
            const isDoor = !!w.isDoor;
            const isOpen = !!w.isOpen;
            el.className = 'ta-wall-line' + (isDoor ? (isOpen ? ' ta-wall-door-open' : ' ta-wall-door') : '');
            el.style.left = x1 + 'px';
            el.style.top = y1 + 'px';
            el.style.width = length + 'px';
            el.style.transform = `rotate(${angle}deg)`;
            this._containerEl.appendChild(el);
        }
    },

    _wallsForRaycast: function (wallsGrid) {
        return (wallsGrid || [])
            .filter(w => !(w.isDoor && w.isOpen))
            .map(w => ({
                x1: w.x1 * this._cellSizePx, y1: w.y1 * this._cellSizePx,
                x2: w.x2 * this._cellSizePx, y2: w.y2 * this._cellSizePx
            }));
    },

    // Видимость с учётом стен — стандартный алгоритм 2D visibility polygon: луч к каждому концу
    // стены (± небольшой угол, чтобы захватить край тени) и по кругу по 32 базовым углам (даёт
    // ровный круг, если стен рядом нет). Ближайшее пересечение луча со стеной обрезает дальность,
    // иначе луч идёт до радиуса зрения. Не point-in-polygon с учётом полупрозрачных
    // укрытий/приоткрытых дверей — только сплошные стены "видно/не видно".
    _intersectRaySegment: function (originX, originY, dirX, dirY, ax, ay, bx, by) {
        const sdx = bx - ax, sdy = by - ay;
        const denom = dirX * sdy - dirY * sdx;
        if (Math.abs(denom) < 1e-10) return null;

        // t2 — параметр вдоль отрезка стены: решение origin + t1·dir = a + t2·s через векторное
        // произведение даёт t2 = dir × (origin − a) / (dir × s). Знак важен: с (a − origin)
        // пересечения зеркалились в t2 ∈ [−1, 0] и отбрасывались (K.1 — свет и тени сквозь стены).
        const t2 = (dirX * (originY - ay) - dirY * (originX - ax)) / denom;
        if (t2 < 0 || t2 > 1) return null;

        let t1;
        if (Math.abs(dirX) > 1e-10) t1 = (ax + sdx * t2 - originX) / dirX;
        else t1 = (ay + sdy * t2 - originY) / dirY;
        if (t1 < 0) return null;

        return t1;
    },

    _castRay: function (originX, originY, angle, wallsPx, maxDist) {
        const dirX = Math.cos(angle), dirY = Math.sin(angle);
        let dist = maxDist;
        for (const w of wallsPx) {
            const t = this._intersectRaySegment(originX, originY, dirX, dirY, w.x1, w.y1, w.x2, w.y2);
            if (t !== null && t < dist) dist = t;
        }
        return { x: originX + dirX * dist, y: originY + dirY * dist, angle: angle };
    },

    _computeVisibilityPolygon: function (originX, originY, wallsPx, maxDist) {
        const EPS = 0.00001;
        const angles = [];
        for (let i = 0; i < 32; i++) angles.push((i / 32) * Math.PI * 2);
        for (const w of wallsPx) {
            for (const pt of [[w.x1, w.y1], [w.x2, w.y2]]) {
                const a = Math.atan2(pt[1] - originY, pt[0] - originX);
                angles.push(a - EPS, a, a + EPS);
            }
        }
        const points = angles.map(a => this._castRay(originX, originY, a, wallsPx, maxDist));
        points.sort((a, b) => a.angle - b.angle);
        return points;
    },

    _ensureFogOverlay: function () {
        if (!this._containerEl) return;
        if (this._fogSvgEl) return;

        const ns = 'http://www.w3.org/2000/svg';
        this._fogSvgEl = document.createElementNS(ns, 'svg');
        this._fogSvgEl.setAttribute('class', 'ta-fog-overlay');
        this._fogSvgEl.setAttribute('width', '100%');
        this._fogSvgEl.setAttribute('height', '100%');

        this._fogDefsEl = document.createElementNS(ns, 'defs');
        const mask = document.createElementNS(ns, 'mask');
        mask.setAttribute('id', 'ta-fog-mask');
        const baseRect = document.createElementNS(ns, 'rect');
        baseRect.setAttribute('width', '100%');
        baseRect.setAttribute('height', '100%');
        baseRect.setAttribute('fill', 'white');
        this._fogShapesEl = document.createElementNS(ns, 'g');
        mask.appendChild(baseRect);
        mask.appendChild(this._fogShapesEl);

        // J.3 — маска освещения: белое = освещено (ярко/тускло), чёрное = темнота. Используется
        // как ВТОРАЯ, вложенная маска для origin-ов без тёмного зрения — последовательное
        // наложение двух SVG-масок на один и тот же элемент перемножает альфа-каналы, что даёт
        // пересечение "видно по стенам" ∩ "освещено", без ручной геометрии клиппинга полигон-круг.
        const lightMask = document.createElementNS(ns, 'mask');
        lightMask.setAttribute('id', 'ta-light-mask');
        const lightBase = document.createElementNS(ns, 'rect');
        lightBase.setAttribute('width', '100%');
        lightBase.setAttribute('height', '100%');
        lightBase.setAttribute('fill', 'black');
        this._lightShapesEl = document.createElementNS(ns, 'g');
        lightMask.appendChild(lightBase);
        lightMask.appendChild(this._lightShapesEl);

        this._fogDefsEl.appendChild(mask);
        this._fogDefsEl.appendChild(lightMask);

        const fogRect = document.createElementNS(ns, 'rect');
        fogRect.setAttribute('width', '100%');
        fogRect.setAttribute('height', '100%');
        fogRect.setAttribute('fill', 'black');
        fogRect.setAttribute('mask', 'url(#ta-fog-mask)');

        this._fogSvgEl.appendChild(this._fogDefsEl);
        this._fogSvgEl.appendChild(fogRect);
        this._containerEl.appendChild(this._fogSvgEl);
    },

    clearFog: function () {
        if (this._fogSvgEl) { this._fogSvgEl.remove(); this._fogSvgEl = null; this._fogShapesEl = null; this._lightShapesEl = null; this._fogDefsEl = null; }
    },

    // originsGrid: [{x,y,darkvision,lowLight}], wallsGrid: [{x1,y1,x2,y2,isDoor,isOpen}],
    // ambientLighting: bright | dim-light | darkness
    updateFog: function (enabled, originsGrid, wallsGrid, radiusFeet, lightsGrid, ambientLighting) {
        if (!enabled || !this._containerEl) { this.clearFog(); return; }

        this._ensureFogOverlay();
        const radiusPx = (radiusFeet / 5) * this._cellSizePx;
        const wallsPx = this._wallsForRaycast(wallsGrid);
        const ambient = ambientLighting || 'bright';

        const ns = 'http://www.w3.org/2000/svg';
        while (this._fogShapesEl.firstChild) this._fogShapesEl.removeChild(this._fogShapesEl.firstChild);
        while (this._lightShapesEl.firstChild) this._lightShapesEl.removeChild(this._lightShapesEl.firstChild);

        // K.1 — свет обрезается стенами: в маску кладём не круг, а полигон видимости от позиции
        // источника (тот же raycasting, что для зрения токенов — без стен рядом он даёт ровный
        // круг радиуса dimRadiusFeet, у стены — тень за ней).
        for (const l of (lightsGrid || [])) {
            const poly = this._computeVisibilityPolygon(
                l.x * this._cellSizePx, l.y * this._cellSizePx,
                wallsPx, (l.dimRadiusFeet / 5) * this._cellSizePx);
            const polygon = document.createElementNS(ns, 'polygon');
            polygon.setAttribute('points', poly.map(p => p.x + ',' + p.y).join(' '));
            polygon.setAttribute('fill', 'white');
            this._lightShapesEl.appendChild(polygon);
        }

        // Если на сцене нет ни одного источника света — освещение не задействуем вообще (только
        // стены/радиус, как раньше до J.3): иначе GM, ещё не настроивший свет, увидел бы, что все
        // игроки внезапно ничего не видят, хотя ничего не менял в тумане/стенах.
        const hasLights = (lightsGrid || []).length > 0;

        for (const origin of originsGrid) {
            const originX = origin.x * this._cellSizePx, originY = origin.y * this._cellSizePx;
            const poly = this._computeVisibilityPolygon(originX, originY, wallsPx, radiusPx);
            const points = poly.map(p => p.x + ',' + p.y).join(' ');
            const polygon = document.createElementNS(ns, 'polygon');
            polygon.setAttribute('points', points);
            polygon.setAttribute('fill', 'black');

            if (hasLights && !origin.darkvision && !(origin.lowLight && ambient === 'dim-light')) {
                const g = document.createElementNS(ns, 'g');
                g.setAttribute('mask', 'url(#ta-light-mask)');
                g.appendChild(polygon);
                this._fogShapesEl.appendChild(g);
            } else {
                this._fogShapesEl.appendChild(polygon);
            }
        }
    },

    _onPointerDown: function (e) {
        if (this._lightMode) {
            if (!this._containerEl) return;
            const rect = this._containerEl.getBoundingClientRect();
            const x = (e.clientX - rect.left) / this._cellSizePx;
            const y = (e.clientY - rect.top) / this._cellSizePx;
            this._dotNetRef.invokeMethodAsync('OnLightPlaced', x, y);
            e.preventDefault();
            return;
        }

        if (this._wallMode) {
            if (!this._containerEl) return;
            const rect = this._containerEl.getBoundingClientRect();
            this._wallOrigin = { x: e.clientX - rect.left, y: e.clientY - rect.top };
            this._clearWallPreview();
            e.preventDefault();
            return;
        }

        if (this._pingMode) {
            if (!this._containerEl) return;
            const rect = this._containerEl.getBoundingClientRect();
            const x = Math.round((e.clientX - rect.left) / this._cellSizePx);
            const y = Math.round((e.clientY - rect.top) / this._cellSizePx);
            this.showPing(x, y);
            this._dotNetRef.invokeMethodAsync('OnPingPlaced', x, y);
            e.preventDefault();
            return;
        }

        if (this._measureMode) {
            if (!this._containerEl) return;
            const rect = this._containerEl.getBoundingClientRect();
            this._measureOrigin = { x: e.clientX - rect.left, y: e.clientY - rect.top };
            this._clearMeasureOverlay();
            e.preventDefault();
            return;
        }

        if (this._templateMode) {
            if (!this._containerEl) return;
            const rect = this._containerEl.getBoundingClientRect();
            // Снапаем происхождение шаблона к ближайшей клетке сразу при нажатии — как токены при
            // перетаскивании, чтобы итоговая позиция всегда была привязана к сетке.
            const rawX = e.clientX - rect.left, rawY = e.clientY - rect.top;
            const snappedX = Math.round(rawX / this._cellSizePx) * this._cellSizePx;
            const snappedY = Math.round(rawY / this._cellSizePx) * this._cellSizePx;
            this._templateOrigin = { x: snappedX, y: snappedY };
            this._updateTemplateOverlay(this._templateOrigin, this._templateOrigin);
            e.preventDefault();
            return;
        }

        if (e.target.closest('.ta-token-remove') || e.target.closest('.ta-token-hp-controls')) return;
        const tokenEl = e.target.closest('.ta-token');
        if (!tokenEl || tokenEl.dataset.canMove !== 'true') return;

        this._dragging = { tokenId: tokenEl.dataset.tokenId, el: tokenEl, lastX: undefined, lastY: undefined, moved: false };
        tokenEl.classList.add('dragging');
        e.preventDefault();
    },

    _onPointerMove: function (e) {
        if (this._wallMode && this._wallOrigin && this._containerEl) {
            const rect = this._containerEl.getBoundingClientRect();
            const cursor = { x: e.clientX - rect.left, y: e.clientY - rect.top };
            this._updateWallPreview(this._wallOrigin, cursor);
            return;
        }

        if (this._measureMode && this._measureOrigin && this._containerEl) {
            const rect = this._containerEl.getBoundingClientRect();
            const cursor = { x: e.clientX - rect.left, y: e.clientY - rect.top };
            const { cellsX, cellsY } = this._gridCellsBetween(this._measureOrigin, cursor);
            const feet = this.pf2eGridDistanceFeet(cellsX, cellsY);
            this._updateMeasureOverlay(this._measureOrigin, cursor, feet);
            return;
        }

        if (this._templateMode && this._templateOrigin && this._containerEl) {
            const rect = this._containerEl.getBoundingClientRect();
            const cursor = { x: e.clientX - rect.left, y: e.clientY - rect.top };
            this._updateTemplateOverlay(this._templateOrigin, cursor);
            return;
        }

        if (!this._dragging || !this._containerEl) return;
        const rect = this._containerEl.getBoundingClientRect();

        // Координаты — в клетках сетки (не доля [0,1] контейнера), считаем от размера клетки в px,
        // клетка "занята" токеном может быть дробной во время перетаскивания — округляем только на drop.
        let x = (e.clientX - rect.left) / this._cellSizePx;
        let y = (e.clientY - rect.top) / this._cellSizePx;
        x = Math.max(0, x);
        y = Math.max(0, y);

        this._dragging.el.style.left = (x * this._cellSizePx) + 'px';
        this._dragging.el.style.top = (y * this._cellSizePx) + 'px';
        this._dragging.lastX = x;
        this._dragging.lastY = y;
        this._dragging.moved = true;
    },

    _onPointerUp: function (e) {
        if (this._measureMode && this._measureOrigin && this._containerEl) {
            const rect = this._containerEl.getBoundingClientRect();
            const cursor = { x: e.clientX - rect.left, y: e.clientY - rect.top };
            const { cellsX, cellsY } = this._gridCellsBetween(this._measureOrigin, cursor);
            const feet = this.pf2eGridDistanceFeet(cellsX, cellsY);
            const x1 = this._measureOrigin.x / this._cellSizePx;
            const y1 = this._measureOrigin.y / this._cellSizePx;
            const x2 = cursor.x / this._cellSizePx;
            const y2 = cursor.y / this._cellSizePx;
            this._measureOrigin = null;
            if (cellsX > 0 || cellsY > 0) {
                this._dotNetRef.invokeMethodAsync('OnMeasureDrawn', x1, y1, x2, y2, feet);
            } else {
                this._clearMeasureOverlay();
            }
            return;
        }

        if (this._templateMode && this._templateOrigin) {
            const rect = this._containerEl.getBoundingClientRect();
            const cursor = { x: e.clientX - rect.left, y: e.clientY - rect.top };
            const dx = cursor.x - this._templateOrigin.x, dy = cursor.y - this._templateOrigin.y;
            const angleDeg = Math.atan2(dy, dx) * (180 / Math.PI);
            const originXCells = this._templateOrigin.x / this._cellSizePx;
            const originYCells = this._templateOrigin.y / this._cellSizePx;
            const { type, feet } = this._templateMode;
            this._templateOrigin = null;
            // K.4 — у себя шаблон уже нарисован живым превью во время протяжки (_updateTemplateOverlay),
            // здесь только доигрываем анимацию появления на зафиксированной форме.
            if (this._templateEl) {
                this._templateEl.classList.remove('ta-template-pop');
                void this._templateEl.offsetWidth;
                this._templateEl.classList.add('ta-template-pop');
            }
            this._dotNetRef.invokeMethodAsync('OnTemplatePlaced', type, feet, originXCells, originYCells, angleDeg);
            return;
        }

        if (this._wallMode && this._wallOrigin) {
            const rect = this._containerEl.getBoundingClientRect();
            const cursor = { x: e.clientX - rect.left, y: e.clientY - rect.top };
            const x1 = this._wallOrigin.x / this._cellSizePx, y1 = this._wallOrigin.y / this._cellSizePx;
            const x2 = cursor.x / this._cellSizePx, y2 = cursor.y / this._cellSizePx;
            this._wallOrigin = null;
            this._clearWallPreview();
            // Игнорируем клик без протяжки (случайное нажатие вместо рисования стены).
            if (Math.abs(x2 - x1) > 0.1 || Math.abs(y2 - y1) > 0.1) {
                this._dotNetRef.invokeMethodAsync('OnWallDrawn', x1, y1, x2, y2);
            }
            return;
        }

        if (!this._dragging) return;
        const { tokenId, el, lastX, lastY, moved } = this._dragging;
        el.classList.remove('dragging');
        this._dragging = null;

        if (moved && lastX !== undefined && this._dotNetRef) {
            // Округляем до целой клетки при отпускании — во время перетаскивания движение плавное (px),
            // итоговая позиция всегда привязана к сетке, как в Foundry.
            const snappedX = Math.round(lastX);
            const snappedY = Math.round(lastY);
            el.style.left = (snappedX * this._cellSizePx) + 'px';
            el.style.top = (snappedY * this._cellSizePx) + 'px';
            this._dotNetRef.invokeMethodAsync('OnTokenDragEnd', tokenId, snappedX, snappedY);
        }
    }
};
