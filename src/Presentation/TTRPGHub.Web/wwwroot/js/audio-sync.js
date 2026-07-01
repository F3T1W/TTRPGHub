window.tableAudio = {
    _el: null,

    _ensure: function () {
        if (!this._el) {
            this._el = document.createElement('audio');
            this._el.id = 'tableAudioPlayer';
            this._el.style.display = 'none';
            document.body.appendChild(this._el);
        }
        return this._el;
    },

    sync: function (url, isPlaying, targetPositionSeconds) {
        const el = this._ensure();
        const isNewTrack = !el.src || !el.src.endsWith(url);

        if (isNewTrack) {
            el.src = url;
            el.currentTime = Math.max(0, targetPositionSeconds);
        } else if (Math.abs(el.currentTime - targetPositionSeconds) > 1.5) {
            el.currentTime = Math.max(0, targetPositionSeconds);
        }

        if (isPlaying) {
            el.play().catch(() => { /* autoplay blocked until user interacts */ });
        } else {
            el.pause();
        }
    },

    stop: function () {
        if (this._el) {
            this._el.pause();
            this._el.removeAttribute('src');
        }
    },

    getCurrentTime: function () {
        return this._el ? this._el.currentTime : 0;
    },

    setVolume: function (volume) {
        this._ensure().volume = volume;
    }
};
