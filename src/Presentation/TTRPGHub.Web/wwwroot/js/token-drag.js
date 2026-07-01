window.tableDrag = {
    _dotNetRef: null,
    _containerEl: null,
    _dragging: null,
    _bound: false,

    init: function (containerId, dotNetRef) {
        this._dotNetRef = dotNetRef;
        this._containerEl = document.getElementById(containerId);
        if (!this._containerEl) return;

        this._containerEl.addEventListener('pointerdown', this._onPointerDown.bind(this));
        if (!this._bound) {
            document.addEventListener('pointermove', this._onPointerMove.bind(this));
            document.addEventListener('pointerup', this._onPointerUp.bind(this));
            this._bound = true;
        }
    },

    _onPointerDown: function (e) {
        if (e.target.closest('.ta-token-remove')) return;
        const tokenEl = e.target.closest('.ta-token');
        if (!tokenEl || tokenEl.dataset.canMove !== 'true') return;

        this._dragging = { tokenId: tokenEl.dataset.tokenId, el: tokenEl, lastX: undefined, lastY: undefined };
        tokenEl.classList.add('dragging');
        e.preventDefault();
    },

    _onPointerMove: function (e) {
        if (!this._dragging || !this._containerEl) return;
        const rect = this._containerEl.getBoundingClientRect();
        let x = (e.clientX - rect.left) / rect.width;
        let y = (e.clientY - rect.top) / rect.height;
        x = Math.min(1, Math.max(0, x));
        y = Math.min(1, Math.max(0, y));

        this._dragging.el.style.left = (x * 100) + '%';
        this._dragging.el.style.top = (y * 100) + '%';
        this._dragging.lastX = x;
        this._dragging.lastY = y;
    },

    _onPointerUp: function () {
        if (!this._dragging) return;
        const { tokenId, el, lastX, lastY } = this._dragging;
        el.classList.remove('dragging');
        this._dragging = null;

        if (lastX !== undefined && this._dotNetRef) {
            this._dotNetRef.invokeMethodAsync('OnTokenDragEnd', tokenId, lastX, lastY);
        }
    }
};
