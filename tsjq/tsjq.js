"use strict";
Object.defineProperties(Array.prototype, {
    "shuffle": {
        enumerable: false,
        configurable: false,
        writable: false,
        value: function () {
            const self = this.slice();
            for (let i = self.length - 1; i > 0; i--) {
                const r = Math.floor(Math.random() * (i + 1));
                const tmp = self[i];
                self[i] = self[r];
                self[r] = tmp;
            }
            return self;
        }
    }
});
Object.defineProperties(Number.prototype, {
    "times": {
        enumerable: false,
        configurable: false,
        writable: false,
        value: function () {
            for (var i = 0; i < this; i++) {
            }
        }
    }
});
var Dispatcher;
(function (Dispatcher) {
    class SingleDispatcher {
        constructor() {
            this.listeners = [];
        }
        clear() {
            this.listeners.length = 0;
            return this;
        }
        on(listener) {
            this.listeners.push(listener);
            return this;
        }
        off(listener) {
            const index = this.listeners.indexOf(listener);
            if (index !== -1) {
                this.listeners.splice(index, 1);
            }
            return this;
        }
        fire(...args) {
            const temp = this.listeners.slice();
            temp.forEach((dispatcher) => dispatcher.apply(this, args));
            return this;
        }
        one(listener) {
            var func = (...args) => {
                var result = listener.apply(this, args);
                this.off(func);
                return result;
            };
            this.on(func);
            return this;
        }
    }
    Dispatcher.SingleDispatcher = SingleDispatcher;
    class EventDispatcher {
        constructor() {
            this.listeners = new Map();
        }
        on(eventName, listener) {
            if (!this.listeners.has(eventName)) {
                this.listeners.set(eventName, new SingleDispatcher());
            }
            this.listeners.get(eventName).on(listener);
            return this;
        }
        off(eventName, listener) {
            this.listeners.get(eventName).off(listener);
            return this;
        }
        fire(eventName, ...args) {
            if (this.listeners.has(eventName)) {
                const dispatcher = this.listeners.get(eventName);
                dispatcher.fire.apply(dispatcher, args);
            }
            return this;
        }
        one(eventName, listener) {
            if (!this.listeners.has(eventName)) {
                this.listeners.set(eventName, new SingleDispatcher());
            }
            this.listeners.get(eventName).one(listener);
            return this;
        }
        hasEventListener(eventName) {
            return this.listeners.has(eventName);
        }
        clearEventListener(eventName) {
            if (this.listeners.has(eventName)) {
                this.listeners.get(eventName).clear();
            }
            return this;
        }
    }
    Dispatcher.EventDispatcher = EventDispatcher;
})(Dispatcher || (Dispatcher = {}));
var Game;
(function (Game) {
    class Video {
        constructor(id) {
            this.canvasElement = document.getElementById(id);
            if (!this.canvasElement) {
                throw new Error("your browser is not support canvas.");
            }
            this.canvasRenderingContext2D = this.canvasElement.getContext("2d");
            if (!this.canvasRenderingContext2D) {
                throw new Error("your browser is not support CanvasRenderingContext2D.");
            }
            this.images = new Map();
            this.arc = this.canvasRenderingContext2D.arc.bind(this.canvasRenderingContext2D);
            this.arcTo = this.canvasRenderingContext2D.arcTo.bind(this.canvasRenderingContext2D);
            this.beginPath = this.canvasRenderingContext2D.beginPath.bind(this.canvasRenderingContext2D);
            this.bezierCurveTo = this.canvasRenderingContext2D.bezierCurveTo.bind(this.canvasRenderingContext2D);
            this.clearRect = this.canvasRenderingContext2D.clearRect.bind(this.canvasRenderingContext2D);
            this.clip = this.canvasRenderingContext2D.clip.bind(this.canvasRenderingContext2D);
            this.closePath = this.canvasRenderingContext2D.closePath.bind(this.canvasRenderingContext2D);
            this.createImageData = this.canvasRenderingContext2D.createImageData.bind(this.canvasRenderingContext2D);
            this.createLinearGradient = this.canvasRenderingContext2D.createLinearGradient.bind(this.canvasRenderingContext2D);
            this.createPattern = this.canvasRenderingContext2D.createPattern.bind(this.canvasRenderingContext2D);
            this.createRadialGradient = this.canvasRenderingContext2D.createRadialGradient.bind(this.canvasRenderingContext2D);
            this.drawImage = this.canvasRenderingContext2D.drawImage.bind(this.canvasRenderingContext2D);
            this.fill = this.canvasRenderingContext2D.fill.bind(this.canvasRenderingContext2D);
            this.fillRect = this.canvasRenderingContext2D.fillRect.bind(this.canvasRenderingContext2D);
            this.fillText = this.canvasRenderingContext2D.fillText.bind(this.canvasRenderingContext2D);
            this.getImageData = this.canvasRenderingContext2D.getImageData.bind(this.canvasRenderingContext2D);
            this.getLineDash = this.canvasRenderingContext2D.getLineDash.bind(this.canvasRenderingContext2D);
            this.isPointInPath = this.canvasRenderingContext2D.isPointInPath.bind(this.canvasRenderingContext2D);
            this.lineTo = this.canvasRenderingContext2D.lineTo.bind(this.canvasRenderingContext2D);
            this.measureText = this.canvasRenderingContext2D.measureText.bind(this.canvasRenderingContext2D);
            this.moveTo = this.canvasRenderingContext2D.moveTo.bind(this.canvasRenderingContext2D);
            this.putImageData = this.canvasRenderingContext2D.putImageData.bind(this.canvasRenderingContext2D);
            this.quadraticCurveTo = this.canvasRenderingContext2D.quadraticCurveTo.bind(this.canvasRenderingContext2D);
            this.rect = this.canvasRenderingContext2D.rect.bind(this.canvasRenderingContext2D);
            this.restore = this.canvasRenderingContext2D.restore.bind(this.canvasRenderingContext2D);
            this.rotate = this.canvasRenderingContext2D.rotate.bind(this.canvasRenderingContext2D);
            this.save = this.canvasRenderingContext2D.save.bind(this.canvasRenderingContext2D);
            this.scale = this.canvasRenderingContext2D.scale.bind(this.canvasRenderingContext2D);
            this.setLineDash = this.canvasRenderingContext2D.setLineDash.bind(this.canvasRenderingContext2D);
            this.setTransform = this.canvasRenderingContext2D.setTransform.bind(this.canvasRenderingContext2D);
            this.stroke = this.canvasRenderingContext2D.stroke.bind(this.canvasRenderingContext2D);
            this.strokeRect = this.canvasRenderingContext2D.strokeRect.bind(this.canvasRenderingContext2D);
            this.strokeText = this.canvasRenderingContext2D.strokeText.bind(this.canvasRenderingContext2D);
            this.transform = this.canvasRenderingContext2D.transform.bind(this.canvasRenderingContext2D);
            this.translate = this.canvasRenderingContext2D.translate.bind(this.canvasRenderingContext2D);
            this.ellipse = this.canvasRenderingContext2D.ellipse.bind(this.canvasRenderingContext2D);
        }
        //
        get canvas() { return this.canvasRenderingContext2D.canvas; }
        get fillStyle() { return this.canvasRenderingContext2D.fillStyle; }
        set fillStyle(value) { this.canvasRenderingContext2D.fillStyle = value; }
        get font() { return this.canvasRenderingContext2D.font; }
        set font(value) { this.canvasRenderingContext2D.font = value; }
        get globalAlpha() { return this.canvasRenderingContext2D.globalAlpha; }
        set globalAlpha(value) { this.canvasRenderingContext2D.globalAlpha = value; }
        get globalCompositeOperation() { return this.canvasRenderingContext2D.globalCompositeOperation; }
        set globalCompositeOperation(value) { this.canvasRenderingContext2D.globalCompositeOperation = value; }
        get lineCap() { return this.canvasRenderingContext2D.lineCap; }
        set lineCap(value) { this.canvasRenderingContext2D.lineCap = value; }
        get lineDashOffset() { return this.canvasRenderingContext2D.lineDashOffset; }
        set lineDashOffset(value) { this.canvasRenderingContext2D.lineDashOffset = value; }
        get lineJoin() { return this.canvasRenderingContext2D.lineJoin; }
        set lineJoin(value) { this.canvasRenderingContext2D.lineJoin = value; }
        get lineWidth() { return this.canvasRenderingContext2D.lineWidth; }
        set lineWidth(value) { this.canvasRenderingContext2D.lineWidth = value; }
        get miterLimit() { return this.canvasRenderingContext2D.miterLimit; }
        set miterLimit(value) { this.canvasRenderingContext2D.miterLimit = value; }
        //get msFillRule(): string { return this.context.msFillRule; }
        //set msFillRule(value: string) { this.context.msFillRule = value; }
        get shadowBlur() { return this.canvasRenderingContext2D.shadowBlur; }
        set shadowBlur(value) { this.canvasRenderingContext2D.shadowBlur = value; }
        get shadowColor() { return this.canvasRenderingContext2D.shadowColor; }
        set shadowColor(value) { this.canvasRenderingContext2D.shadowColor = value; }
        get shadowOffsetX() { return this.canvasRenderingContext2D.shadowOffsetX; }
        set shadowOffsetX(value) { this.canvasRenderingContext2D.shadowOffsetX = value; }
        get shadowOffsetY() { return this.canvasRenderingContext2D.shadowOffsetY; }
        set shadowOffsetY(value) { this.canvasRenderingContext2D.shadowOffsetY = value; }
        get strokeStyle() { return this.canvasRenderingContext2D.strokeStyle; }
        set strokeStyle(value) { this.canvasRenderingContext2D.strokeStyle = value; }
        get textAlign() { return this.canvasRenderingContext2D.textAlign; }
        set textAlign(value) { this.canvasRenderingContext2D.textAlign = value; }
        get textBaseline() { return this.canvasRenderingContext2D.textBaseline; }
        set textBaseline(value) { this.canvasRenderingContext2D.textBaseline = value; }
        get imageSmoothingEnabled() {
            if ('imageSmoothingEnabled' in this.canvasRenderingContext2D) {
                return this.canvasRenderingContext2D.imageSmoothingEnabled;
            }
            if ('mozImageSmoothingEnabled' in this.canvasRenderingContext2D) {
                return this.canvasRenderingContext2D.mozImageSmoothingEnabled;
            }
            if ('webkitImageSmoothingEnabled' in this.canvasRenderingContext2D) {
                return this.canvasRenderingContext2D.webkitImageSmoothingEnabled;
            }
            return false;
        }
        set imageSmoothingEnabled(value) {
            if ('imageSmoothingEnabled' in this.canvasRenderingContext2D) {
                this.canvasRenderingContext2D.imageSmoothingEnabled = value;
                return;
            }
            if ('mozImageSmoothingEnabled' in this.canvasRenderingContext2D) {
                this.canvasRenderingContext2D.mozImageSmoothingEnabled = value;
                return;
            }
            if ('webkitImageSmoothingEnabled' in this.canvasRenderingContext2D) {
                this.canvasRenderingContext2D.webkitImageSmoothingEnabled = value;
                return;
            }
        }
        //
        drawTile(image, offsetX, offsetY, sprite, spritesize, tile) {
            for (var y = 0; y < tile.height; y++) {
                for (var x = 0; x < tile.width; x++) {
                    var chip = tile.value(x, y);
                    this.drawImage(image, sprite[chip][0] * spritesize[0], sprite[chip][1] * spritesize[1], spritesize[0], spritesize[1], offsetX + x * spritesize[0], offsetY + y * spritesize[1], spritesize[0], spritesize[1]);
                }
            }
        }
        //
        get width() {
            return this.canvasRenderingContext2D.canvas.width;
        }
        get height() {
            return this.canvasRenderingContext2D.canvas.height;
        }
        loadImage(asserts) {
            return Promise.all(Object.keys(asserts).map((x) => new Promise((resolve, reject) => {
                const img = new Image();
                img.onload = () => {
                    this.images.set(x, img);
                    resolve();
                };
                img.onerror = () => {
                    var msg = `ファイル ${asserts[x]}のロードに失敗。`;
                    consolere.error(msg);
                    reject(msg);
                };
                img.src = asserts[x];
            }))).then(() => {
                return true;
            });
        }
        texture(id) {
            return this.images.get(id);
        }
        pagePointToScreenPoint(x, y) {
            const cr = this.canvasRenderingContext2D.canvas.getBoundingClientRect();
            const sx = (x - (cr.left + window.pageXOffset));
            const sy = (y - (cr.top + window.pageYOffset));
            return [sx, sy];
        }
        pagePointContainScreen(x, y) {
            const pos = this.pagePointToScreenPoint(x, y);
            return 0 <= pos[0] && pos[0] < this.width && 0 <= pos[1] && pos[1] < this.height;
        }
    }
    Game.Video = Video;
})(Game || (Game = {}));
var Game;
(function (Game) {
    let Sound;
    (function (Sound) {
        class ManagedSoundChannel {
            constructor() {
                this.audioBufferNode = null;
                this.playRequest = false;
                this.stopRequest = false;
                this.loopPlay = false;
            }
            reset() {
                this.audioBufferNode = null;
                this.playRequest = false;
                this.stopRequest = false;
                this.loopPlay = false;
            }
        }
        class UnmanagedSoundChannel {
            constructor(sound, buffer) {
                this.isEnded = true;
                this.bufferSource = null;
                this.buffer = null;
                this.sound = null;
                this.buffer = buffer;
                this.sound = sound;
                this.reset();
            }
            reset() {
                this.stop();
                this.bufferSource = this.sound.createBufferSource(this.buffer);
                this.bufferSource.onended = () => this.isEnded = true;
            }
            loopplay() {
                if (this.isEnded) {
                    this.bufferSource.loop = true;
                    this.bufferSource.start(0);
                    this.isEnded = false;
                }
            }
            play() {
                if (this.isEnded) {
                    this.bufferSource.loop = false;
                    this.bufferSource.start(0);
                    this.isEnded = false;
                }
            }
            stop() {
                if (!this.isEnded) {
                    this.bufferSource.stop(0);
                    this.bufferSource.disconnect();
                    this.isEnded = true;
                }
            }
        }
        class SoundManager {
            constructor() {
                this.channels = new Array(SoundManager.soundChannelMax);
                this.bufferSourceIdCount = 0;
                this.audioContext = new AudioContext();
                this.channels = new Array(SoundManager.soundChannelMax);
                this.bufferSourceIdCount = 0;
                this.playingBufferSources = new Map();
                this.reset();
                let touchEventHooker = () => {
                    // A small hack to unlock AudioContext on mobile safari.
                    const buffer = this.audioContext.createBuffer(1, (this.audioContext.sampleRate / 100), this.audioContext.sampleRate);
                    const channel = buffer.getChannelData(0);
                    channel.fill(0);
                    const src = this.audioContext.createBufferSource();
                    src.buffer = buffer;
                    src.connect(this.audioContext.destination);
                    src.start(this.audioContext.currentTime);
                    document.body.removeEventListener('touchstart', touchEventHooker);
                };
                document.body.addEventListener('touchstart', touchEventHooker);
            }
            createBufferSource(buffer) {
                var bufferSource = this.audioContext.createBufferSource();
                bufferSource.buffer = buffer;
                bufferSource.connect(this.audioContext.destination);
                return bufferSource;
            }
            loadSound(file) {
                return new Promise((resolve, reject) => {
                    var xhr = new XMLHttpRequest();
                    xhr.responseType = "arraybuffer";
                    xhr.open("GET", file, true);
                    xhr.onerror = () => {
                        var msg = `ファイル ${file}のロードに失敗。`;
                        consolere.error(msg);
                        reject(msg);
                    };
                    xhr.onload = () => {
                        resolve(xhr);
                    };
                    xhr.send();
                })
                    .then((xhr) => new Promise((resolve, reject) => {
                    this.audioContext.decodeAudioData(xhr.response, (audioBufferNode) => {
                        resolve(audioBufferNode);
                    }, () => {
                        var msg = `ファイル ${file}のdecodeAudioDataに失敗。`;
                        reject(msg);
                    });
                }));
            }
            loadSoundToChannel(file, channel) {
                return this.loadSound(file).then((audioBufferNode) => {
                    this.channels[channel].audioBufferNode = audioBufferNode;
                });
            }
            loadSoundsToChannel(config) {
                return Promise.all(Object.keys(config)
                    .map((x) => ~~x)
                    .map((channel) => this.loadSound(config[channel]).then((audioBufferNode) => {
                    this.channels[channel].audioBufferNode = audioBufferNode;
                }))).then(() => { });
            }
            createUnmanagedSoundChannel(file) {
                return this.loadSound(file)
                    .then((audioBufferNode) => new UnmanagedSoundChannel(this, audioBufferNode));
            }
            reqPlayChannel(channel, loop = false) {
                this.channels[channel].playRequest = true;
                this.channels[channel].loopPlay = loop;
            }
            reqStopChannel(channel) {
                this.channels[channel].stopRequest = true;
            }
            playChannel() {
                this.channels.forEach((c, i) => {
                    if (c.stopRequest) {
                        c.stopRequest = false;
                        if (c.audioBufferNode == null) {
                            return;
                        }
                        this.playingBufferSources.forEach((value, key) => {
                            if (value.id === i) {
                                const srcNode = value.buffer;
                                srcNode.stop();
                                srcNode.disconnect();
                                this.playingBufferSources.set(key, null);
                                this.playingBufferSources.delete(key);
                            }
                        });
                    }
                    if (c.playRequest) {
                        c.playRequest = false;
                        if (c.audioBufferNode == null) {
                            return;
                        }
                        var src = this.audioContext.createBufferSource();
                        if (src == null) {
                            throw new Error("createBufferSourceに失敗。");
                        }
                        var bufferid = this.bufferSourceIdCount++;
                        this.playingBufferSources.set(bufferid, { id: i, buffer: src });
                        src.buffer = c.audioBufferNode;
                        src.loop = c.loopPlay;
                        src.connect(this.audioContext.destination);
                        src.onended = (() => {
                            var srcNode = src;
                            srcNode.stop(0);
                            srcNode.disconnect();
                            this.playingBufferSources.set(bufferid, null);
                            this.playingBufferSources.delete(bufferid);
                        }).bind(null, bufferid);
                        src.start(0);
                    }
                });
            }
            stop() {
                const oldPlayingBufferSources = this.playingBufferSources;
                this.playingBufferSources = new Map();
                oldPlayingBufferSources.forEach((value, key) => {
                    const s = value.buffer;
                    if (s != null) {
                        s.stop(0);
                        s.disconnect();
                        oldPlayingBufferSources.set(key, null);
                        oldPlayingBufferSources.delete(key);
                    }
                });
            }
            reset() {
                for (let i = 0; i < SoundManager.soundChannelMax; i++) {
                    this.channels[i] = this.channels[i] || (new ManagedSoundChannel());
                    this.channels[i].reset();
                    this.bufferSourceIdCount = 0;
                }
                this.playingBufferSources = new Map();
            }
        }
        SoundManager.soundChannelMax = 36 * 36;
        Sound.SoundManager = SoundManager;
    })(Sound = Game.Sound || (Game.Sound = {}));
})(Game || (Game = {}));
var Game;
(function (Game) {
    let Input;
    (function (Input) {
        class CustomPointerEvent extends CustomEvent {
        }
        let PointerChangeStatus;
        (function (PointerChangeStatus) {
            PointerChangeStatus[PointerChangeStatus["Down"] = 0] = "Down";
            PointerChangeStatus[PointerChangeStatus["Up"] = 1] = "Up";
            PointerChangeStatus[PointerChangeStatus["Leave"] = 2] = "Leave";
        })(PointerChangeStatus || (PointerChangeStatus = {}));
        class InputManager extends Dispatcher.EventDispatcher {
            constructor() {
                super();
                this.isScrolling = false;
                this.timeout = 0;
                this.sDistX = 0;
                this.sDistY = 0;
                this.maybeClick = false;
                this.maybeClickX = 0;
                this.maybeClickY = 0;
                this.prevTimeStamp = 0;
                this.prevInputType = "none";
                window.addEventListener("scroll", () => {
                    if (!this.isScrolling) {
                        this.sDistX = window.pageXOffset;
                        this.sDistY = window.pageYOffset;
                    }
                    this.isScrolling = true;
                    clearTimeout(this.timeout);
                    this.timeout = setTimeout(() => {
                        this.isScrolling = false;
                        this.sDistX = 0;
                        this.sDistY = 0;
                    }, 100);
                });
                // add event listener to body
                document.onselectstart = () => false;
                if (document.body["pointermove"] !== undefined) {
                    document.body.addEventListener('touchmove', evt => { evt.preventDefault(); }, false);
                    document.body.addEventListener('touchdown', evt => { evt.preventDefault(); }, false);
                    document.body.addEventListener('touchup', evt => { evt.preventDefault(); }, false);
                    document.body.addEventListener('mousemove', evt => { evt.preventDefault(); }, false);
                    document.body.addEventListener('mousedown', evt => { evt.preventDefault(); }, false);
                    document.body.addEventListener('mouseup', evt => { evt.preventDefault(); }, false);
                    document.body.addEventListener('pointerdown', (ev) => this.fire('pointerdown', ev));
                    document.body.addEventListener('pointermove', (ev) => this.fire('pointermove', ev));
                    document.body.addEventListener('pointerup', (ev) => this.fire('pointerup', ev));
                    document.body.addEventListener('pointerleave', (ev) => this.fire('pointerleave', ev));
                }
                else {
                    consolere.log('pointer event is not implemented');
                    document.body.addEventListener('mousedown', this.pointerDown.bind(this), false);
                    document.body.addEventListener('touchstart', this.pointerDown.bind(this), false);
                    document.body.addEventListener('mouseup', this.pointerUp.bind(this), false);
                    document.body.addEventListener('touchend', this.pointerUp.bind(this), false);
                    document.body.addEventListener('mousemove', this.pointerMove.bind(this), false);
                    document.body.addEventListener('touchmove', this.pointerMove.bind(this), false);
                    document.body.addEventListener('mouseleave', this.pointerLeave.bind(this), false);
                    document.body.addEventListener('touchleave', this.pointerLeave.bind(this), false);
                    document.body.addEventListener('touchcancel', this.pointerUp.bind(this), false);
                }
                this.capture = false;
                this.lastPageX = 0;
                this.lastPageY = 0;
                this.downup = 0;
                this.status = PointerChangeStatus.Leave;
                this.clicked = false;
                this.lastDownPageX = 0;
                this.lastDownPageY = 0;
                this.draglen = 0;
                this.captureHandler = this.captureHandler.bind(this);
                this.on('pointerdown', this.captureHandler);
                this.on('pointermove', this.captureHandler);
                this.on('pointerup', this.captureHandler);
                this.on('pointerleave', this.captureHandler);
            }
            get pageX() {
                return this.lastPageX;
            }
            get pageY() {
                return this.lastPageY;
            }
            isDown() {
                return this.downup == 1;
            }
            isPush() {
                return this.downup > 1;
            }
            isUp() {
                return this.downup == -1;
            }
            isClick() {
                return this.clicked;
            }
            isRelease() {
                return this.downup < -1;
            }
            startCapture() {
                this.capture = true;
            }
            endCapture() {
                this.capture = false;
                if (this.status == PointerChangeStatus.Down) {
                    if (this.downup < 1) {
                        this.downup = 1;
                    }
                    else {
                        this.downup += 1;
                    }
                }
                else if (this.status == PointerChangeStatus.Up) {
                    if (this.downup > -1) {
                        this.downup = -1;
                    }
                    else {
                        this.downup -= 1;
                    }
                }
                else {
                    this.downup = 0;
                }
                this.clicked = false;
                if (this.downup == -1) {
                    if (this.draglen < 5) {
                        this.clicked = true;
                    }
                }
                else if (this.downup == 1) {
                    this.lastDownPageX = this.lastPageX;
                    this.lastDownPageY = this.lastPageY;
                    this.draglen = 0;
                }
                else if (this.downup > 1) {
                    this.draglen = Math.max(this.draglen, Math.sqrt((this.lastDownPageX - this.lastPageX) * (this.lastDownPageX - this.lastPageX) + (this.lastDownPageY - this.lastPageY) * (this.lastDownPageY - this.lastPageY)));
                }
            }
            captureHandler(e) {
                if (this.capture == false) {
                    return;
                }
                switch (e.type) {
                    case "pointerdown":
                        this.status = PointerChangeStatus.Down;
                        break;
                    case "pointerup":
                        this.status = PointerChangeStatus.Up;
                        break;
                    case "pointerleave":
                        this.status = PointerChangeStatus.Leave;
                        break;
                    case "pointermove":
                        break;
                }
                this.lastPageX = e.pageX;
                this.lastPageY = e.pageY;
            }
            checkEvent(e) {
                e.preventDefault();
                const istouch = e instanceof TouchEvent || (e instanceof PointerEvent && e.pointerType == "touch");
                const ismouse = e instanceof MouseEvent || ((e instanceof PointerEvent && (e.pointerType == "mouse" || e.pointerType == "pen")));
                if (istouch && this.prevInputType !== "touch") {
                    if (e.timeStamp - this.prevTimeStamp >= 500) {
                        this.prevInputType = "touch";
                        this.prevTimeStamp = e.timeStamp;
                        return true;
                    }
                    else {
                        return false;
                    }
                }
                else if (ismouse && this.prevInputType !== "mouse") {
                    if (e.timeStamp - this.prevTimeStamp >= 500) {
                        this.prevInputType = "mouse";
                        this.prevTimeStamp = e.timeStamp;
                        return true;
                    }
                    else {
                        return false;
                    }
                }
                else {
                    this.prevInputType = istouch ? "touch" : ismouse ? "mouse" : "none";
                    this.prevTimeStamp = e.timeStamp;
                    return istouch || ismouse;
                }
            }
            pointerDown(e) {
                if (this.checkEvent(e)) {
                    const evt = this.makePointerEvent("down", e);
                    const singleFinger = (e instanceof MouseEvent) || (e instanceof TouchEvent && e.touches.length === 1);
                    if (!this.isScrolling && singleFinger) {
                        this.maybeClick = true;
                        this.maybeClickX = evt.pageX;
                        this.maybeClickY = evt.pageY;
                    }
                }
                return false;
            }
            pointerLeave(e) {
                if (this.checkEvent(e)) {
                    this.maybeClick = false;
                    this.makePointerEvent("leave", e);
                }
                return false;
            }
            pointerMove(e) {
                if (this.checkEvent(e)) {
                    this.makePointerEvent("move", e);
                }
                return false;
            }
            pointerUp(e) {
                if (this.checkEvent(e)) {
                    const evt = this.makePointerEvent("up", e);
                    if (this.maybeClick) {
                        if (Math.abs(this.maybeClickX - evt.pageX) < 5 && Math.abs(this.maybeClickY - evt.pageY) < 5) {
                            if (!this.isScrolling ||
                                (Math.abs(this.sDistX - window.pageXOffset) < 5 &&
                                    Math.abs(this.sDistY - window.pageYOffset) < 5)) {
                                this.makePointerEvent("click", e);
                            }
                        }
                    }
                    this.maybeClick = false;
                }
                return false;
            }
            makePointerEvent(type, e) {
                const evt = document.createEvent("CustomEvent");
                const eventType = `pointer${type}`;
                evt.initCustomEvent(eventType, true, true, {});
                evt.touch = e.type.indexOf("touch") === 0;
                evt.mouse = e.type.indexOf("mouse") === 0;
                if (evt.touch) {
                    const touchEvent = e;
                    evt.pointerId = touchEvent.changedTouches[0].identifier;
                    evt.pageX = touchEvent.changedTouches[0].pageX;
                    evt.pageY = touchEvent.changedTouches[0].pageY;
                }
                if (evt.mouse) {
                    const mouseEvent = e;
                    evt.pointerId = 0;
                    evt.pageX = mouseEvent.clientX + window.pageXOffset;
                    evt.pageY = mouseEvent.clientY + window.pageYOffset;
                }
                evt.maskedEvent = e;
                this.fire(eventType, evt);
                return evt;
            }
        }
        Input.InputManager = InputManager;
        class VirtualStick {
            constructor(x = 120, y = 120, radius = 40) {
                this.isTouching = false;
                this.x = x;
                this.y = y;
                this.cx = 0;
                this.cy = 0;
                this.radius = radius;
                this.distance = 0;
                this.angle = 0;
                this.id = -1;
            }
            get dir4() {
                switch (~~((this.angle + 360 + 45) / 90) % 4) {
                    case 0: return 6; // left
                    case 1: return 2; // up
                    case 2: return 4; // right
                    case 3: return 8; // down
                }
                return 5; // neutral
            }
            get dir8() {
                var d = ~~((this.angle + 360 + 22.5) / 45) % 8;
                switch (d) {
                    case 0: return 6; // right
                    case 1: return 3; // right-down
                    case 2: return 2; // down
                    case 3: return 1; // left-down
                    case 4: return 4; // left
                    case 5: return 7; // left-up
                    case 6: return 8; // up
                    case 7: return 9; // right-up
                }
                return 5; // neutral
            }
            isHit(x, y) {
                const dx = x - this.x;
                const dy = y - this.y;
                return ((dx * dx) + (dy * dy)) <= this.radius * this.radius;
            }
            onpointingstart(id) {
                if (this.id !== -1) {
                    return false;
                }
                this.isTouching = true;
                this.cx = 0;
                this.cy = 0;
                this.angle = 0;
                this.distance = 0;
                this.id = id;
                return true;
            }
            onpointingend(id) {
                if (this.id !== id) {
                    return false;
                }
                this.isTouching = false;
                this.cx = 0;
                this.cy = 0;
                this.angle = 0;
                this.distance = 0;
                this.id = -1;
                return true;
            }
            onpointingmove(id, x, y) {
                if (this.isTouching === false) {
                    return false;
                }
                if (id !== this.id) {
                    return false;
                }
                let dx = x - this.x;
                let dy = y - this.y;
                let len = Math.sqrt((dx * dx) + (dy * dy));
                if (len > 0) {
                    dx /= len;
                    dy /= len;
                    if (len > this.radius) {
                        len = this.radius;
                    }
                    this.angle = Math.atan2(dy, dx) * 180 / Math.PI;
                    this.distance = len * 1.0 / this.radius;
                    this.cx = dx * len;
                    this.cy = dy * len;
                }
                else {
                    this.cx = 0;
                    this.cy = 0;
                    this.angle = 0;
                    this.distance = 0;
                }
                return true;
            }
        }
        Input.VirtualStick = VirtualStick;
    })(Input = Game.Input || (Game.Input = {}));
})(Game || (Game = {}));
var Game;
(function (Game) {
    let Timer;
    (function (Timer) {
        class AnimationTimer extends Dispatcher.SingleDispatcher {
            constructor() {
                super();
                this.animationFrameId = NaN;
                this.prevTime = NaN;
            }
            start() {
                if (!isNaN(this.animationFrameId)) {
                    this.stop();
                }
                this.animationFrameId = requestAnimationFrame(this.tick.bind(this));
                return !isNaN(this.animationFrameId);
            }
            stop() {
                if (!isNaN(this.animationFrameId)) {
                    cancelAnimationFrame(this.animationFrameId);
                    this.animationFrameId = NaN;
                }
            }
            tick(ts) {
                requestAnimationFrame(this.tick.bind(this));
                if (!isNaN(this.prevTime)) {
                    const delta = ts - this.prevTime;
                    this.fire(delta, ts);
                }
                this.prevTime = ts;
            }
        }
        Timer.AnimationTimer = AnimationTimer;
    })(Timer = Game.Timer || (Game.Timer = {}));
})(Game || (Game = {}));
var Game;
(function (Game) {
    let Scene;
    (function (Scene_1) {
        class Scene {
            constructor(manager, init) {
                this.manager = manager;
                this.state = null;
                this.init = init;
                this.update = () => { };
                this.draw = () => { };
                this.leave = () => { };
                this.suspend = () => { };
                this.resume = () => { };
            }
            next(...args) {
                this.update = this.state.next.apply(this.state, args).value;
            }
            push(id, param = {}) { this.manager.push(id, param); }
            pop() { this.manager.pop(); }
            enter(...data) {
                this.state = this.init.apply(this, data);
                this.next();
            }
        }
        class SceneManager {
            constructor(scenes) {
                this.scenes = new Map();
                this.sceneStack = [];
                Object.keys(scenes).forEach((key) => this.scenes.set(key, scenes[key]));
            }
            push(id, ...param) {
                const sceneDef = this.scenes.get(id);
                if (this.scenes.has(id) === false) {
                    throw new Error(`scene ${id} is not defined.`);
                }
                if (this.peek() != null && this.peek().suspend != null) {
                    this.peek().suspend();
                }
                this.sceneStack.push(new Scene(this, sceneDef));
                this.peek().enter.apply(this.peek(), param);
            }
            pop() {
                if (this.sceneStack.length === 0) {
                    throw new Error("there is no scene.");
                }
                if (this.peek() != null) {
                    var p = this.sceneStack.pop();
                    if (p.leave != null) {
                        p.leave();
                    }
                }
                if (this.peek() != null && this.peek().resume != null) {
                    this.peek().resume();
                }
            }
            peek() {
                if (this.sceneStack.length > 0) {
                    return this.sceneStack[this.sceneStack.length - 1];
                }
                else {
                    return null;
                }
            }
            update(...args) {
                if (this.sceneStack.length !== 0) {
                    this.peek().update.apply(this.peek(), args);
                }
                return this;
            }
            draw() {
                if (this.sceneStack.length !== 0) {
                    this.peek().draw.apply(this.peek());
                }
                return this;
            }
        }
        Scene_1.SceneManager = SceneManager;
    })(Scene = Game.Scene || (Game.Scene = {}));
})(Game || (Game = {}));
class Array2D {
    constructor(width, height, fill) {
        this.arrayWidth = width;
        this.arrayHeight = height;
        if (fill == undefined) {
            this.matrixBuffer = new Array(width * height);
        }
        else {
            this.matrixBuffer = new Array(width * height).fill(fill);
        }
    }
    get width() {
        return this.arrayWidth;
    }
    get height() {
        return this.arrayHeight;
    }
    value(x, y, value) {
        if (0 > x || x >= this.arrayWidth || 0 > y || y >= this.arrayHeight) {
            return 0;
        }
        if (value != undefined) {
            this.matrixBuffer[y * this.arrayWidth + x] = value;
        }
        return this.matrixBuffer[y * this.arrayWidth + x];
    }
    fill(value) {
        this.matrixBuffer.fill(value);
        return this;
    }
    dup() {
        const m = new Array2D(this.width, this.height);
        m.matrixBuffer = this.matrixBuffer.slice();
        return m;
    }
    static createFromArray(array, fill) {
        const h = array.length;
        const w = Math.max.apply(Math, array.map(x => x.length));
        var matrix = new Array2D(w, h, fill);
        array.forEach((vy, y) => vy.forEach((vx, x) => matrix.value(x, y, vx)));
        return matrix;
    }
    toString() {
        const lines = [];
        for (let y = 0; y < this.height; y++) {
            lines[y] = `|${this.matrixBuffer.slice((y + 0) * this.arrayWidth, (y + 1) * this.arrayWidth).join(", ")}|`;
        }
        return lines.join("\r\n");
    }
}
Array2D.DIR8 = [
    [+Number.MAX_SAFE_INTEGER, +Number.MAX_SAFE_INTEGER],
    [-1, +1],
    [+0, +1],
    [+1, +1],
    [-1, +0],
    [+0, +0],
    [+1, +0],
    [-1, -1],
    [+0, -1],
    [+1, -1]
];
var PathFinder;
(function (PathFinder) {
    const dir4 = [
        [0, -1],
        [1, 0],
        [0, 1],
        [-1, 0]
    ];
    const dir8 = [
        [0, -1],
        [1, 0],
        [0, 1],
        [-1, 0],
        [1, -1],
        [1, 1],
        [-1, 1],
        [-1, -1]
    ];
    // 基点からの重み距離算出
    function propagation({ array2D = null, sx = null, sy = null, value = null, costs = null, left = 0, top = 0, right = undefined, bottom = undefined, timeout = 1000, topology = 8, output = null }) {
        if (left === undefined || left < 0) {
            right == 0;
        }
        if (top === undefined || top < 0) {
            bottom == 0;
        }
        if (right === undefined || right > array2D.width) {
            right == array2D.width;
        }
        if (bottom === undefined || bottom > array2D.height) {
            bottom == array2D.height;
        }
        if (output === null) {
            output = new Array2D(array2D.width, array2D.height);
        }
        const dirs = (topology === 8) ? dir8 : dir4;
        output.value(sx, sy, value);
        const request = dirs.map(([ox, oy]) => [sx + ox, sy + oy, value]);
        var start = Date.now();
        while (request.length !== 0 && (Date.now() - start) < timeout) {
            var [x, y, currentValue] = request.shift();
            if (top > y || y >= bottom || left > x || x >= right) {
                continue;
            }
            const cost = costs(array2D.value(x, y));
            if (cost < 0 || currentValue < cost) {
                continue;
            }
            currentValue -= cost;
            const targetPower = output.value(x, y);
            if (currentValue <= targetPower) {
                continue;
            }
            output.value(x, y, currentValue);
            Array.prototype.push.apply(request, dirs.map(([ox, oy]) => [x + ox, y + oy, currentValue]));
        }
        return output;
    }
    PathFinder.propagation = propagation;
    // A*での経路探索
    function pathfind(array2D, fromX, fromY, toX, toY, costs, opts) {
        opts = Object.assign({ topology: 8 }, opts);
        var topology = opts.topology;
        var dirs;
        if (topology === 4) {
            dirs = dir4;
        }
        else if (topology === 8) {
            dirs = dir8;
        }
        else {
            throw new Error("Illegal topology");
        }
        var todo = [];
        const add = ((x, y, prev) => {
            // distance
            var distance;
            switch (topology) {
                case 4:
                    distance = (Math.abs(x - fromX) + Math.abs(y - fromY));
                    break;
                case 8:
                    distance = Math.min(Math.abs(x - fromX), Math.abs(y - fromY));
                    break;
                default:
                    throw new Error("Illegal topology");
            }
            var obj = {
                x: x,
                y: y,
                prev: prev,
                g: (prev ? prev.g + 1 : 0),
                distance: distance
            };
            /* insert into priority queue */
            var f = obj.g + obj.distance;
            for (let i = 0; i < todo.length; i++) {
                const item = todo[i];
                const itemF = item.g + item.distance;
                if (f < itemF || (f === itemF && distance < item.distance)) {
                    todo.splice(i, 0, obj);
                    return;
                }
            }
            todo.push(obj);
        });
        // set start position 
        add(toX, toY, null);
        const done = new Map();
        while (todo.length) {
            let item = todo.shift();
            {
                const id = item.x + "," + item.y;
                if (done.has(id)) {
                    /* 探索済みなので探索しない */
                    continue;
                }
                done.set(id, item);
            }
            if (item.x === fromX && item.y === fromY) {
                /* 始点に到達したので経路を生成して返す */
                const result = [];
                while (item) {
                    result.push([item.x, item.y]);
                    item = item.prev;
                }
                return result;
            }
            else {
                /* 隣接地点から移動可能地点を探す */
                for (let i = 0; i < dirs.length; i++) {
                    const dir = dirs[i];
                    const x = item.x + dir[0];
                    const y = item.y + dir[1];
                    const cost = costs[this.value(x, y)];
                    if (cost < 0) {
                        /* 侵入不可能 */
                        continue;
                    }
                    else {
                        /* 移動可能地点が探索済みでないなら探索キューに追加 */
                        const id = x + "," + y;
                        if (done.has(id)) {
                            continue;
                        }
                        add(x, y, item);
                    }
                }
            }
        }
        /* 始点に到達しなかったので空の経路を返す */
        return [];
    }
    PathFinder.pathfind = pathfind;
    // 重み距離を使ったA*
    function pathfindByPropergation(array2D, fromX, fromY, toX, toY, propagation, { topology = 8 }) {
        let dirs;
        if (topology === 4) {
            dirs = dir4;
        }
        else if (topology === 8) {
            dirs = dir8;
        }
        else {
            throw new Error("Illegal topology");
        }
        var todo = [];
        const add = ((x, y, prev) => {
            // distance
            var distance = Math.abs(propagation.value(x, y) - propagation.value(fromX, fromY));
            var obj = {
                x: x,
                y: y,
                prev: prev,
                g: (prev ? prev.g + 1 : 0),
                distance: distance
            };
            /* insert into priority queue */
            var f = obj.g + obj.distance;
            for (let i = 0; i < todo.length; i++) {
                const item = todo[i];
                const itemF = item.g + item.distance;
                if (f < itemF || (f === itemF && distance < item.distance)) {
                    todo.splice(i, 0, obj);
                    return;
                }
            }
            todo.push(obj);
        });
        // set start position 
        add(toX, toY, null);
        const done = new Map();
        while (todo.length) {
            let item = todo.shift();
            {
                const id = item.x + "," + item.y;
                if (done.has(id)) {
                    /* 探索済みなので探索しない */
                    continue;
                }
                done.set(id, item);
            }
            if (item.x === fromX && item.y === fromY) {
                /* 始点に到達したので経路を生成して返す */
                const result = [];
                while (item) {
                    result.push([item.x, item.y]);
                    item = item.prev;
                }
                return result;
            }
            else {
                /* 隣接地点から移動可能地点を探す */
                dirs.forEach((dir) => {
                    const x = item.x + dir[0];
                    const y = item.y + dir[1];
                    const pow = propagation.value(x, y);
                    if (pow === 0) {
                        /* 侵入不可能 */
                        return;
                    }
                    else {
                        /* 移動可能地点が探索済みでないなら探索キューに追加 */
                        var id = x + "," + y;
                        if (done.has(id)) {
                            return;
                        }
                        else {
                            add(x, y, item);
                        }
                    }
                });
            }
        }
        /* 始点に到達しなかったので空の経路を返す */
        return [];
    }
    PathFinder.pathfindByPropergation = pathfindByPropergation;
})(PathFinder || (PathFinder = {}));
var Dungeon;
(function (Dungeon) {
    // マップ描画時の視点・視野情報
    class Camera {
    }
    Dungeon.Camera = Camera;
    // ダンジョンデータ
    class DungeonData {
        constructor(config) {
            this.width = config.width;
            this.height = config.height;
            this.gridsize = config.gridsize;
            this.layer = config.layer;
            this.camera = new Camera();
            this.lighting = new Array2D(this.width, this.height, 0);
            this.visibled = new Array2D(this.width, this.height, 0);
        }
        clearLighting() {
            this.lighting.fill(0);
            return this;
        }
        // update camera
        update(param) {
            const mapWidth = this.width * this.gridsize.width;
            const mapHeight = this.height * this.gridsize.height;
            // マップ上でのカメラの注視点
            const mapPx = param.viewpoint.x;
            const mapPy = param.viewpoint.y;
            // カメラの視野の幅・高さ
            this.camera.width = param.viewwidth;
            this.camera.height = param.viewheight;
            // カメラの注視点が中心となるようなカメラの視野
            this.camera.left = ~~(mapPx - this.camera.width / 2);
            this.camera.top = ~~(mapPy - this.camera.height / 2);
            this.camera.right = this.camera.left + this.camera.width;
            this.camera.bottom = this.camera.top + this.camera.height;
            // 視野をマップ内に補正
            if ((this.camera.left < 0) && (this.camera.right - this.camera.left < mapWidth)) {
                this.camera.right -= this.camera.left;
                this.camera.left = 0;
            }
            else if ((this.camera.right >= mapWidth) && (this.camera.left - (this.camera.right - mapWidth) >= 0)) {
                this.camera.left -= (this.camera.right - mapWidth);
                this.camera.right = mapWidth - 1;
            }
            if ((this.camera.top < 0) && (this.camera.bottom - this.camera.top < mapHeight)) {
                this.camera.bottom -= this.camera.top;
                this.camera.top = 0;
            }
            else if ((this.camera.bottom >= mapHeight) && (this.camera.top - (this.camera.bottom - mapHeight) >= 0)) {
                this.camera.top -= (this.camera.bottom - mapHeight);
                this.camera.bottom = mapHeight - 1;
            }
            // 視野の左上位置を原点とした注視点を算出
            this.camera.localPx = mapPx - this.camera.left;
            this.camera.localPy = mapPy - this.camera.top;
            // 視野の四隅位置に対応するマップチップ座標を算出
            this.camera.chipLeft = ~~(this.camera.left / this.gridsize.width);
            this.camera.chipTop = ~~(this.camera.top / this.gridsize.height);
            this.camera.chipRight = ~~((this.camera.right + (this.gridsize.width - 1)) / this.gridsize.width);
            this.camera.chipBottom = ~~((this.camera.bottom + (this.gridsize.height - 1)) / this.gridsize.height);
            // 視野の左上位置をにマップチップをおいた場合のスクロールによるズレ量を算出
            this.camera.chipOffX = -(this.camera.left % this.gridsize.width);
            this.camera.chipOffY = -(this.camera.top % this.gridsize.height);
        }
        draw(layerDrawHook) {
            // 描画開始
            const gridw = this.gridsize.width;
            const gridh = this.gridsize.height;
            Object.keys(this.layer).forEach((key) => {
                const l = ~~key;
                for (let y = this.camera.chipTop; y <= this.camera.chipBottom; y++) {
                    for (let x = this.camera.chipLeft; x <= this.camera.chipRight; x++) {
                        const chipid = this.layer[l].chips.value(x, y) || 0;
                        if ((chipid === 1 || chipid === 10) && this.layer[l].chip[chipid]) {
                            const xx = (x - this.camera.chipLeft) * gridw;
                            const yy = (y - this.camera.chipTop) * gridh;
                            if (!Game.pmode) {
                                Game.getScreen().drawImage(Game.getScreen().texture(this.layer[l].texture), this.layer[l].chip[chipid].x, this.layer[l].chip[chipid].y, gridw, gridh, 0 + xx + this.camera.chipOffX, 0 + yy + this.camera.chipOffY, gridw, gridh);
                            }
                            else {
                                Game.getScreen().fillStyle = `rgba(185,122,87,1)`;
                                Game.getScreen().strokeStyle = `rgba(0,0,0,1)`;
                                Game.getScreen().fillRect(0 + xx + this.camera.chipOffX, 0 + yy + this.camera.chipOffY, gridw, gridh);
                                Game.getScreen().strokeRect(0 + xx + this.camera.chipOffX, 0 + yy + this.camera.chipOffY, gridw, gridh);
                            }
                        }
                    }
                }
                // レイヤー描画フック
                layerDrawHook(l, this.camera.localPx, this.camera.localPy);
            });
            // 照明描画
            for (let y = this.camera.chipTop; y <= this.camera.chipBottom; y++) {
                for (let x = this.camera.chipLeft; x <= this.camera.chipRight; x++) {
                    let light = this.lighting.value(x, y) / 100;
                    if (light > 1) {
                        light = 1;
                    }
                    else if (light < 0) {
                        light = 0;
                    }
                    const xx = (x - this.camera.chipLeft) * gridw;
                    const yy = (y - this.camera.chipTop) * gridh;
                    Game.getScreen().fillStyle = `rgba(0,0,0,${1 - light})`;
                    Game.getScreen().fillRect(0 + xx + this.camera.chipOffX, 0 + yy + this.camera.chipOffY, gridw, gridh);
                }
            }
        }
    }
    Dungeon.DungeonData = DungeonData;
    // ダンジョン構成要素基底クラス
    class Feature {
    }
    function getUniformInt(lowerBound, upperBound) {
        const max = Math.max(lowerBound, upperBound);
        const min = Math.min(lowerBound, upperBound);
        return Math.floor(Math.random() * (max - min + 1)) + min;
    }
    function getWeightedValue(data) {
        const keys = Object.keys(data);
        const total = keys.reduce((s, x) => s + data[x], 0);
        const random = Math.random() * total;
        let part = 0;
        for (const id of keys) {
            part += data[id];
            if (random < part) {
                return id;
            }
        }
        return keys[keys.length - 1];
    }
    // 部屋
    class Room extends Feature {
        constructor(left, top, right, bottom, door) {
            super();
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
            this.doors = {};
            if (door !== undefined) {
                this.addDoor(door);
            }
        }
        static createRandomAt(x, y, dx, dy, options) {
            const minw = options.roomWidth[0];
            const maxw = options.roomWidth[1];
            const width = getUniformInt(minw, maxw);
            const minh = options.roomHeight[0];
            const maxh = options.roomHeight[1];
            const height = getUniformInt(minh, maxh);
            if (dx === 1) {
                const y2 = y - Math.floor(Math.random() * height);
                return new Room(x + 1, y2, x + width, y2 + height - 1, [x, y]);
            }
            if (dx === -1) {
                const y2 = y - Math.floor(Math.random() * height);
                return new Room(x - width, y2, x - 1, y2 + height - 1, [x, y]);
            }
            if (dy === 1) {
                const x2 = x - Math.floor(Math.random() * width);
                return new Room(x2, y + 1, x2 + width - 1, y + height, [x, y]);
            }
            if (dy === -1) {
                const x2 = x - Math.floor(Math.random() * width);
                return new Room(x2, y - height, x2 + width - 1, y - 1, [x, y]);
            }
            throw new Error("dx or dy must be 1 or -1");
        }
        static createRandomCenter(cx, cy, options) {
            const minw = options.roomWidth[0];
            const maxw = options.roomWidth[1];
            const width = getUniformInt(minw, maxw);
            const minh = options.roomHeight[0];
            const maxh = options.roomHeight[1];
            const height = getUniformInt(minh, maxh);
            const x1 = cx - Math.floor(Math.random() * width);
            const y1 = cy - Math.floor(Math.random() * height);
            const x2 = x1 + width - 1;
            const y2 = y1 + height - 1;
            return new Room(x1, y1, x2, y2);
        }
        static createRandom(availWidth, availHeight, options) {
            const minw = options.roomWidth[0];
            const maxw = options.roomWidth[1];
            const width = getUniformInt(minw, maxw);
            const minh = options.roomHeight[0];
            const maxh = options.roomHeight[1];
            const height = getUniformInt(minh, maxh);
            const left = availWidth - width - 1;
            const top = availHeight - height - 1;
            const x1 = 1 + Math.floor(Math.random() * left);
            const y1 = 1 + Math.floor(Math.random() * top);
            const x2 = x1 + width - 1;
            const y2 = y1 + height - 1;
            return new Room(x1, y1, x2, y2);
        }
        addDoor([x, y]) {
            this.doors[x + "," + y] = 1;
            return this;
        }
        getDoors(callback) {
            for (const key of Object.keys(this.doors)) {
                const parts = key.split(",");
                callback([parseInt(parts[0], 10), parseInt(parts[1], 10)]);
            }
            return this;
        }
        clearDoors() {
            this.doors = {};
            return this;
        }
        addDoors(isWallCallback) {
            const left = this.left - 1;
            const right = this.right + 1;
            const top = this.top - 1;
            const bottom = this.bottom + 1;
            for (let x = left; x <= right; x++) {
                for (let y = top; y <= bottom; y++) {
                    if (x !== left && x !== right && y !== top && y !== bottom) {
                        continue;
                    }
                    if (isWallCallback([x, y])) {
                        continue;
                    }
                    this.addDoor([x, y]);
                }
            }
            return this;
        }
        debug() {
            consolere.log("room", this.left, this.top, this.right, this.bottom);
        }
        isValid(isWallCallback, canBeDugCallback) {
            const left = this.left - 1;
            const right = this.right + 1;
            const top = this.top - 1;
            const bottom = this.bottom + 1;
            for (let x = left; x <= right; x++) {
                for (let y = top; y <= bottom; y++) {
                    if (x === left || x === right || y === top || y === bottom) {
                        if (!isWallCallback(x, y)) {
                            return false;
                        }
                    }
                    else {
                        if (!canBeDugCallback(x, y)) {
                            return false;
                        }
                    }
                }
            }
            return true;
        }
        /**
         * @param {function} digCallback Dig callback with a signature (x, y, value). Values: 0 = empty, 1 = wall, 2 = door. Multiple doors are allowed.
         */
        create(digCallback) {
            const left = this.left - 1;
            const right = this.right + 1;
            const top = this.top - 1;
            const bottom = this.bottom + 1;
            for (let x = left; x <= right; x++) {
                for (let y = top; y <= bottom; y++) {
                    let value = 0;
                    if (x + "," + y in this.doors) {
                        value = 2;
                    }
                    else if (x === left || x === right || y === top || y === bottom) {
                        value = 1;
                    }
                    else {
                        value = 0;
                    }
                    digCallback(x, y, value);
                }
            }
        }
        getCenter() {
            return [Math.round((this.left + this.right) / 2), Math.round((this.top + this.bottom) / 2)];
        }
        getLeft() {
            return this.left;
        }
        getRight() {
            return this.right;
        }
        getTop() {
            return this.top;
        }
        getBottom() {
            return this.bottom;
        }
    }
    // 通路
    class Corridor extends Feature {
        constructor(startX, startY, endX, endY) {
            super();
            this.startX = startX;
            this.startY = startY;
            this.endX = endX;
            this.endY = endY;
            this.endsWithAWall = true;
        }
        static createRandomAt(x, y, dx, dy, options) {
            const min = options.corridorLength[0];
            const max = options.corridorLength[1];
            const length = getUniformInt(min, max);
            return new Corridor(x, y, x + dx * length, y + dy * length);
        }
        debug() {
            consolere.log("corridor", this.startX, this.startY, this.endX, this.endY);
        }
        isValid(isWallCallback, canBeDugCallback) {
            const sx = this.startX;
            const sy = this.startY;
            let dx = this.endX - sx;
            let dy = this.endY - sy;
            let length = 1 + Math.max(Math.abs(dx), Math.abs(dy));
            if (dx) {
                dx = dx / Math.abs(dx);
            }
            if (dy) {
                dy = dy / Math.abs(dy);
            }
            const nx = dy;
            const ny = -dx;
            let ok = true;
            for (let i = 0; i < length; i++) {
                const x = sx + i * dx;
                const y = sy + i * dy;
                if (!canBeDugCallback(x, y)) {
                    ok = false;
                }
                if (!isWallCallback(x + nx, y + ny)) {
                    ok = false;
                }
                if (!isWallCallback(x - nx, y - ny)) {
                    ok = false;
                }
                if (!ok) {
                    length = i;
                    this.endX = x - dx;
                    this.endY = y - dy;
                    break;
                }
            }
            /**
             * If the length degenerated, this corridor might be invalid
             */
            /* not supported */
            if (length === 0) {
                return false;
            }
            /* length 1 allowed only if the next space is empty */
            if (length === 1 && isWallCallback(this.endX + dx, this.endY + dy)) {
                return false;
            }
            /**
             * We do not want the corridor to crash into a corner of a room;
             * if any of the ending corners is empty, the N+1th cell of this corridor must be empty too.
             *
             * Situation:
             * #######1
             * .......?
             * #######2
             *
             * The corridor was dug from left to right.
             * 1, 2 - problematic corners, ? = N+1th cell (not dug)
             */
            const firstCornerBad = !isWallCallback(this.endX + dx + nx, this.endY + dy + ny);
            const secondCornerBad = !isWallCallback(this.endX + dx - nx, this.endY + dy - ny);
            this.endsWithAWall = isWallCallback(this.endX + dx, this.endY + dy);
            if ((firstCornerBad || secondCornerBad) && this.endsWithAWall) {
                return false;
            }
            return true;
        }
        create(digCallback) {
            const sx = this.startX;
            const sy = this.startY;
            let dx = this.endX - sx;
            let dy = this.endY - sy;
            const length = 1 + Math.max(Math.abs(dx), Math.abs(dy));
            if (dx) {
                dx = dx / Math.abs(dx);
            }
            if (dy) {
                dy = dy / Math.abs(dy);
            }
            for (let i = 0; i < length; i++) {
                const x = sx + i * dx;
                const y = sy + i * dy;
                digCallback(x, y, 0);
            }
            return true;
        }
        createPriorityWalls(priorityWallCallback) {
            if (!this.endsWithAWall) {
                return;
            }
            const sx = this.startX;
            const sy = this.startY;
            let dx = this.endX - sx;
            let dy = this.endY - sy;
            if (dx) {
                dx = dx / Math.abs(dx);
            }
            if (dy) {
                dy = dy / Math.abs(dy);
            }
            const nx = dy;
            const ny = -dx;
            priorityWallCallback(this.endX + dx, this.endY + dy);
            priorityWallCallback(this.endX + nx, this.endY + ny);
            priorityWallCallback(this.endX - nx, this.endY - ny);
        }
    }
    class Generator {
        constructor(width, height, option = {
                roomWidth: [3, 9],
                roomHeight: [3, 5],
                corridorLength: [3, 10],
                dugPercentage: 0.2,
                timeLimit: 1000,
            }) {
            this.width = width;
            this.height = height;
            this.rooms = []; /* list of all rooms */
            this.corridors = [];
            this.options = option;
            this.features = {
                Room: 4,
                Corridor: 4,
            };
            this.featureAttempts = 20; /* how many times do we try to create a feature on a suitable wall */
            this.walls = {}; /* these are available for digging */
            this._digCallback = this._digCallback.bind(this);
            this._canBeDugCallback = this._canBeDugCallback.bind(this);
            this._isWallCallback = this._isWallCallback.bind(this);
            this._priorityWallCallback = this._priorityWallCallback.bind(this);
        }
        /*@*/ create(callback) {
            this.rooms = [];
            this.corridors = [];
            this.map = this._fillMap(1);
            this.walls = {};
            this.dug = 0;
            const area = (this.width - 2) * (this.height - 2);
            this._firstRoom();
            const t1 = Date.now();
            let priorityWalls = 0;
            do {
                const t2 = Date.now();
                if (t2 - t1 > this.options.timeLimit) {
                    break;
                }
                /* find a good wall */
                const wall = this._findWall();
                if (!wall) {
                    break;
                } /* no more walls */
                const parts = wall.split(",");
                const x = parseInt(parts[0]);
                const y = parseInt(parts[1]);
                const dir = this._getDiggingDirection(x, y);
                if (!dir) {
                    continue;
                } /* this wall is not suitable */
                // consolere.log("wall", x, y);
                /* try adding a feature */
                let featureAttempts = 0;
                do {
                    featureAttempts++;
                    if (this._tryFeature(x, y, dir[0], dir[1])) {
                        // if (this._rooms.length + this._corridors.length === 2) { this._rooms[0].addDoor(x, y); } /* first room oficially has doors */
                        this._removeSurroundingWalls(x, y);
                        this._removeSurroundingWalls(x - dir[0], y - dir[1]);
                        break;
                    }
                } while (featureAttempts < this.featureAttempts);
                for (const id in this.walls) {
                    if (this.walls[id] > 1) {
                        priorityWalls++;
                    }
                }
            } while (this.dug / area < this.options.dugPercentage || priorityWalls); /* fixme number of priority walls */
            this._addDoors();
            if (callback) {
                for (let i = 0; i < this.width; i++) {
                    for (let j = 0; j < this.height; j++) {
                        callback(i, j, this.map[i][j]);
                    }
                }
            }
            this.walls = {};
            this.map = null;
            return this;
        }
        _digCallback(x, y, value) {
            if (value === 0 || value === 2) {
                this.map[x][y] = 0;
                this.dug++;
            }
            else {
                this.walls[x + "," + y] = 1;
            }
        }
        _isWallCallback(x, y) {
            if (x < 0 || y < 0 || x >= this.width || y >= this.height) {
                return false;
            }
            return (this.map[x][y] === 1);
        }
        _canBeDugCallback(x, y) {
            if (x < 1 || y < 1 || x + 1 >= this.width || y + 1 >= this.height) {
                return false;
            }
            return (this.map[x][y] === 1);
        }
        _priorityWallCallback(x, y) {
            this.walls[x + "," + y] = 2;
        }
        _findWall() {
            const prio1 = [];
            const prio2 = [];
            for (const id in this.walls) {
                const prio = this.walls[id];
                if (prio === 2) {
                    prio2.push(id);
                }
                else {
                    prio1.push(id);
                }
            }
            const arr = (prio2.length ? prio2 : prio1);
            if (!arr.length) {
                return null;
            } /* no walls :/ */
            const id2 = arr.sort()[Math.floor(Math.random() * arr.length)]; // sort to make the order deterministic
            delete this.walls[id2];
            return id2;
        }
        _firstRoom() {
            const cx = Math.floor(this.width / 2);
            const cy = Math.floor(this.height / 2);
            const room = Room.createRandomCenter(cx, cy, this.options);
            this.rooms.push(room);
            room.create(this._digCallback);
        }
        _fillMap(value) {
            const map = [];
            for (let i = 0; i < this.width; i++) {
                map.push([]);
                for (let j = 0; j < this.height; j++) {
                    map[i].push(value);
                }
            }
            return map;
        }
        _tryFeature(x, y, dx, dy) {
            const featureType = getWeightedValue(this.features);
            const feature = Generator.FeatureCreateMethod[featureType](x, y, dx, dy, this.options);
            if (!feature.isValid(this._isWallCallback, this._canBeDugCallback)) {
                return false;
            }
            feature.create(this._digCallback);
            if (feature instanceof Room) {
                this.rooms.push(feature);
            }
            if (feature instanceof Corridor) {
                feature.createPriorityWalls(this._priorityWallCallback);
                this.corridors.push(feature);
            }
            return true;
        }
        _removeSurroundingWalls(cx, cy) {
            const deltas = Generator._ROTDIRS4;
            for (const delta of deltas) {
                const x1 = cx + delta[0];
                const y1 = cy + delta[1];
                delete this.walls[x1 + "," + y1];
                const x2 = cx + 2 * delta[0];
                const y2 = cy + 2 * delta[1];
                delete this.walls[x2 + "," + y2];
            }
        }
        _getDiggingDirection(cx, cy) {
            if (cx <= 0 || cy <= 0 || cx >= this.width - 1 || cy >= this.height - 1) {
                return null;
            }
            let result = null;
            const deltas = Generator._ROTDIRS4;
            for (const delta of deltas) {
                const x = cx + delta[0];
                const y = cy + delta[1];
                if (!this.map[x][y]) {
                    if (result) {
                        return null;
                    }
                    result = delta;
                }
            }
            /* no empty neighbor */
            if (!result) {
                return null;
            }
            return [-result[0], -result[1]];
        }
        _addDoors() {
            const data = this.map;
            const isWallCallback = ([x, y]) => {
                return (data[x][y] === 1);
            };
            for (const room of this.rooms) {
                room.clearDoors();
                room.addDoors(isWallCallback);
            }
        }
        getRooms() {
            return this.rooms;
        }
        getCorridors() {
            return this.corridors;
        }
    }
    Generator.FeatureCreateMethod = { "Room": Room.createRandomAt, "Corridor": Corridor.createRandomAt };
    Generator._ROTDIRS4 = [
        [0, -1],
        [1, 0],
        [0, 1],
        [-1, 0],
    ];
    function generate(w, h, callback) {
        return new Generator(w, h).create(callback);
    }
    Dungeon.generate = generate;
})(Dungeon || (Dungeon = {}));
// <reference path="C:/Program Files/Microsoft Visual Studio 14.0/Common7/IDE/CommonExtensions/Microsoft/TypeScript/lib.es6.d.ts" />
var TurnState;
(function (TurnState) {
    TurnState[TurnState["WaitInput"] = 0] = "WaitInput";
    TurnState[TurnState["PlayerAction"] = 1] = "PlayerAction";
    TurnState[TurnState["PlayerActionRunning"] = 2] = "PlayerActionRunning";
    TurnState[TurnState["EnemyAI"] = 3] = "EnemyAI";
    TurnState[TurnState["EnemyAction"] = 4] = "EnemyAction";
    TurnState[TurnState["EnemyActionRunning"] = 5] = "EnemyActionRunning";
    TurnState[TurnState["Move"] = 6] = "Move";
    TurnState[TurnState["MoveRunning"] = 7] = "MoveRunning";
    TurnState[TurnState["TurnEnd"] = 8] = "TurnEnd";
})(TurnState || (TurnState = {}));
var consolere;
var Game;
(function (Game) {
    Game.pmode = false;
    consolere.log("remote log start");
    // Global Variables
    let video = null;
    let sceneManager = null;
    let inputDispacher = null;
    let timer = null;
    let soundManager = null;
    //
    function create(config) {
        return new Promise((resolve, reject) => {
            try {
                document.title = config.title;
                video = new Game.Video(config.screen.id);
                video.imageSmoothingEnabled = false;
                sceneManager = new Game.Scene.SceneManager(config.scene);
                timer = new Game.Timer.AnimationTimer();
                inputDispacher = new Game.Input.InputManager();
                soundManager = new Game.Sound.SoundManager();
                resolve();
            }
            catch (e) {
                reject(e);
            }
        });
    }
    Game.create = create;
    function getScreen() {
        return video;
    }
    Game.getScreen = getScreen;
    function getTimer() {
        return timer;
    }
    Game.getTimer = getTimer;
    function getSceneManager() {
        return sceneManager;
    }
    Game.getSceneManager = getSceneManager;
    function getInput() {
        return inputDispacher;
    }
    Game.getInput = getInput;
    function getSound() {
        return soundManager;
    }
    Game.getSound = getSound;
})(Game || (Game = {}));
class Animator {
    constructor(sprite, spriteWidth, spriteHeight) {
        this.sprite = sprite;
        this.spriteWidth = spriteWidth;
        this.spriteHeight = spriteHeight;
        this.offx = 0;
        this.offy = 0;
        this.dir = 5;
        this.animDir = 2;
        this.animFrame = 0;
    }
    setDir(dir) {
        if (dir === 0) {
            return;
        }
        this.dir = dir;
        switch (dir) {
            case 1: {
                if (this.animDir === 4) {
                    this.animDir = 4;
                }
                else if (this.animDir === 2) {
                    this.animDir = 2;
                }
                else if (this.animDir === 8) {
                    this.animDir = 2;
                }
                else if (this.animDir === 6) {
                    this.animDir = 4;
                }
                break;
            }
            case 3: {
                if (this.animDir === 4) {
                    this.animDir = 6;
                }
                else if (this.animDir === 2) {
                    this.animDir = 2;
                }
                else if (this.animDir === 8) {
                    this.animDir = 2;
                }
                else if (this.animDir === 6) {
                    this.animDir = 2;
                }
                break;
            }
            case 9: {
                if (this.animDir === 4) {
                    this.animDir = 6;
                }
                else if (this.animDir === 2) {
                    this.animDir = 8;
                }
                else if (this.animDir === 8) {
                    this.animDir = 8;
                }
                else if (this.animDir === 6) {
                    this.animDir = 6;
                }
                break;
            }
            case 7: {
                if (this.animDir === 4) {
                    this.animDir = 4;
                }
                else if (this.animDir === 2) {
                    this.animDir = 8;
                }
                else if (this.animDir === 8) {
                    this.animDir = 8;
                }
                else if (this.animDir === 6) {
                    this.animDir = 4;
                }
                break;
            }
            case 5: {
                break;
            }
            default: {
                this.animDir = dir;
                break;
            }
        }
    }
    setAnimation(type, rate) {
        if (rate > 1) {
            rate = 1;
        }
        if (rate < 0) {
            rate = 0;
        }
        if (type === "move") {
            this.offx = ~~(Array2D.DIR8[this.dir][0] * 24 * rate);
            this.offy = ~~(Array2D.DIR8[this.dir][1] * 24 * rate);
        }
        else if (type === "action") {
            this.offx = ~~(Array2D.DIR8[this.dir][0] * 12 * Math.sin(rate * Math.PI));
            this.offy = ~~(Array2D.DIR8[this.dir][1] * 12 * Math.sin(rate * Math.PI));
        }
        this.animFrame = ~~(rate * this.sprite[this.animDir].length) % this.sprite[this.animDir].length;
    }
}
class Player extends Animator {
    constructor(config) {
        if (!Game.pmode) {
            super([], 47, 47);
        }
        else {
            super([], 24, 24);
        }
        this.charactor = config.charactor;
        this.x = config.x;
        this.y = config.y;
        this.changeCharactor(this.charactor);
    }
    changeCharactor(charactor) {
        this.charactor = charactor;
        if (!Game.pmode) {
            const psbasex = (this.charactor % 2) * 752;
            const psbasey = ~~(this.charactor / 2) * 47;
            this.sprite[2] = [[0, 0], [1, 0], [2, 0], [3, 0]].map(xy => [
                psbasex + this.spriteWidth * xy[0], psbasey + this.spriteHeight * xy[1]
            ]);
            this.sprite[4] = [[4, 0], [5, 0], [6, 0], [7, 0]].map(xy => [
                psbasex + this.spriteWidth * xy[0], psbasey + this.spriteHeight * xy[1]
            ]);
            this.sprite[8] = [[8, 0], [9, 0], [10, 0], [11, 0]].map(xy => [
                psbasex + this.spriteWidth * xy[0], psbasey + this.spriteHeight * xy[1]
            ]);
            this.sprite[6] = [[12, 0], [13, 0], [14, 0], [15, 0]].map(xy => [
                psbasex + this.spriteWidth * xy[0], psbasey + this.spriteHeight * xy[1]
            ]);
        }
        else {
            const animdir = [4, 8, 6, 2];
            const sprites = [];
            for (let i = 0; i < 4; i++) {
                const spr = [];
                for (let j = 0; j < 4; j++) {
                    spr[j] = [j * 24, i * 24];
                }
                sprites[animdir[i]] = spr;
            }
            this.sprite = sprites;
        }
    }
}
class Monster extends Animator {
    constructor(config) {
        super(config._sprite, config._sprite_width, config._sprite_height);
        this.x = config.x;
        this.y = config.y;
    }
}
class Fade {
    constructor(w, h) {
        this.startTime = -1;
        this.started = false;
        this.w = w;
        this.h = h;
        this.mode = "";
    }
    startFadeOut() {
        this.started = true;
        this.startTime = -1;
        this.rate = 0;
        this.mode = "fadeout";
    }
    startFadeIn() {
        this.started = true;
        this.startTime = -1;
        this.rate = 1;
        this.mode = "fadein";
    }
    stop() {
        this.started = false;
        this.startTime = -1;
    }
    update(ms) {
        if (this.started === false) {
            return;
        }
        if (this.startTime === -1) {
            this.startTime = ms;
        }
        this.rate = (ms - this.startTime) / 500;
        if (this.rate < 0) {
            this.rate = 0;
        }
        else if (this.rate > 1) {
            this.rate = 1;
        }
        if (this.mode === "fadein") {
            this.rate = 1 - this.rate;
        }
    }
    draw() {
        if (this.started) {
            Game.getScreen().fillStyle = `rgba(0,0,0,${this.rate})`;
            Game.getScreen().fillRect(0, 0, this.w, this.h);
        }
    }
}
window.onload = () => {
    function waitTimeout({ timeout, init = () => { }, start = () => { }, update = () => { }, end = () => { } }) {
        let startTime = -1;
        init();
        return (delta, ms) => {
            if (startTime === -1) {
                startTime = ms;
                start(0, ms);
            }
            const elapsed = ms - startTime;
            if (elapsed >= timeout) {
                end(elapsed, ms);
            }
            else {
                update(elapsed, ms);
            }
        };
    }
    function waitClick({ update = () => { }, start = () => { }, check = () => true, end = () => { } }) {
        let startTime = -1;
        return (delta, ms) => {
            if (startTime === -1) {
                startTime = ms;
                start(0, ms);
            }
            const elapsed = ms - startTime;
            if (Game.getInput().isClick()) {
                const pX = Game.getInput().pageX;
                const pY = Game.getInput().pageY;
                if (Game.getScreen().pagePointContainScreen(pX, pY)) {
                    const pos = Game.getScreen().pagePointToScreenPoint(pX, pY);
                    const xx = pos[0];
                    const yy = pos[1];
                    if (check(xx, yy, elapsed, ms)) {
                        end(xx, yy, elapsed, ms);
                        return;
                    }
                }
            }
            update(elapsed, ms);
        };
    }
    Game.create({
        title: "TSJQ",
        screen: {
            id: "glcanvas",
        },
        scene: {
            title: function* (data) {
                // setup
                let showClickOrTap = false;
                const fade = new Fade(Game.getScreen().width, Game.getScreen().height);
                this.draw = () => {
                    const w = Game.getScreen().width;
                    const h = Game.getScreen().height;
                    Game.getScreen().save();
                    Game.getScreen().clearRect(0, 0, w, h);
                    Game.getScreen().fillStyle = "rgb(255,255,255)";
                    Game.getScreen().fillRect(0, 0, w, h);
                    Game.getScreen().drawImage(Game.getScreen().texture("title"), 0, 0, 192, 72, w / 2 - 192 / 2, 50, 192, 72);
                    if (showClickOrTap) {
                        Game.getScreen().drawImage(Game.getScreen().texture("title"), 0, 72, 168, 24, w / 2 - 168 / 2, h - 50, 168, 24);
                    }
                    fade.draw();
                    Game.getScreen().restore();
                };
                yield waitClick({
                    update: (e, ms) => { showClickOrTap = (~~(ms / 500) % 2) === 0; },
                    check: () => true,
                    end: () => {
                        Game.getSound().reqPlayChannel(0);
                        this.next();
                    }
                });
                yield waitTimeout({
                    timeout: 1000,
                    update: (e, ms) => { showClickOrTap = (~~(ms / 50) % 2) === 0; },
                    end: () => this.next()
                });
                yield waitTimeout({
                    timeout: 500,
                    init: () => { fade.startFadeOut(); },
                    update: (e, ms) => { fade.update(e); showClickOrTap = (~~(ms / 50) % 2) === 0; },
                    end: () => {
                        Game.getSceneManager().push("classroom");
                        this.next();
                    }
                });
            },
            classroom: function* () {
                let selectedCharactor = -1;
                let selectedCharactorDir = 0;
                let selectedCharactorOffY = 0;
                const fade = new Fade(Game.getScreen().width, Game.getScreen().height);
                this.draw = () => {
                    const w = Game.getScreen().width;
                    const h = Game.getScreen().height;
                    Game.getScreen().save();
                    Game.getScreen().clearRect(0, 0, w, h);
                    Game.getScreen().fillStyle = "rgb(255,255,255)";
                    Game.getScreen().fillRect(0, 0, w, h);
                    // 床
                    for (let y = 0; y < ~~((w + 23) / 24); y++) {
                        for (let x = 0; x < ~~((w + 23) / 24); x++) {
                            Game.getScreen().drawImage(Game.getScreen().texture("mapchip"), 0, 0, 24, 24, x * 24, y * 24, 24, 24);
                        }
                    }
                    // 壁
                    for (let y = 0; y < 2; y++) {
                        for (let x = 0; x < ~~((w + 23) / 24); x++) {
                            Game.getScreen().drawImage(Game.getScreen().texture("mapchip"), 120, 96, 24, 24, x * 24, y * 24 - 23, 24, 24);
                        }
                    }
                    // 黒板
                    Game.getScreen().drawImage(Game.getScreen().texture("mapchip"), 0, 204, 72, 36, 90, -12, 72, 36);
                    // 各キャラと机
                    for (let y = 0; y < 5; y++) {
                        for (let x = 0; x < 6; x++) {
                            const id = y * 6 + x;
                            Game.getScreen().drawImage(Game.getScreen().texture("charactor"), 752 * (id % 2) +
                                ((selectedCharactor !== id) ? 0 : (188 * (selectedCharactorDir % 4))), 47 * ~~(id / 2), 47, 47, 12 + x * 36, 24 + y * (48 - 7) - ((selectedCharactor !== id) ? 0 : (selectedCharactorOffY)), 47, 47);
                            Game.getScreen().drawImage(Game.getScreen().texture("mapchip"), 72, 180, 24, 24, 24 + x * 36, 48 + y * (48 - 7), 24, 24);
                        }
                    }
                    fade.draw();
                    Game.getScreen().restore();
                };
                {
                    Game.getSound().reqPlayChannel(2, true);
                    yield waitTimeout({
                        timeout: 500,
                        init: () => { fade.startFadeIn(); },
                        update: (e) => { fade.update(e); },
                        end: () => {
                            fade.stop();
                            this.next();
                        }
                    });
                }
                yield waitClick({
                    check: (x, y) => {
                        const xx = ~~((x - 12) / 36);
                        const yy = ~~((y - 24) / (48 - 7));
                        return (0 <= xx && xx < 6 && 0 <= yy && yy < 5);
                    },
                    end: (x, y) => {
                        Game.getSound().reqPlayChannel(0);
                        const xx = ~~((x - 12) / 36);
                        const yy = ~~((y - 24) / (48 - 7));
                        selectedCharactor = yy * 6 + xx;
                        this.next();
                    }
                });
                yield waitTimeout({
                    timeout: 1800,
                    init: () => {
                        selectedCharactorDir = 0;
                        selectedCharactorOffY = 0;
                    },
                    update: (e) => {
                        if (0 <= e && e < 1600) {
                            // くるくる
                            selectedCharactorDir = ~~(e / 100);
                            selectedCharactorOffY = 0;
                        }
                        else if (1600 <= e && e < 1800) {
                            // ぴょん
                            selectedCharactorDir = 0;
                            selectedCharactorOffY = Math.sin((e - 1600) * Math.PI / 200) * 20;
                        }
                    },
                    end: (e) => { this.next(); }
                });
                yield waitTimeout({
                    timeout: 500,
                    init: () => { fade.startFadeOut(); },
                    update: (e) => { fade.update(e); },
                    end: (e) => { this.next(); }
                });
                const player = new Player({
                    charactor: selectedCharactor,
                    x: 0,
                    y: 0,
                });
                Game.getSound().reqStopChannel(2);
                Game.getSceneManager().pop();
                Game.getSceneManager().push("dungeon", { player: player, floor: 1 });
            },
            dungeon: function* ({ player = null, floor = 0 }) {
                // マップサイズ算出
                const mapChipW = 30 + floor * 3;
                const mapChipH = 30 + floor * 3;
                // マップ自動生成
                const mapchipsL1 = new Array2D(mapChipW, mapChipH);
                const dungeon = Dungeon.generate(mapChipW, mapChipH, (x, y, v) => { mapchipsL1.value(x, y, v ? 0 : 1); });
                // 装飾
                for (let y = 1; y < mapChipH; y++) {
                    for (let x = 0; x < mapChipW; x++) {
                        mapchipsL1.value(x, y - 1, mapchipsL1.value(x, y) === 1 && mapchipsL1.value(x, y - 1) === 0
                            ? 2
                            : mapchipsL1.value(x, y - 1));
                    }
                }
                const mapchipsL2 = new Array2D(mapChipW, mapChipH);
                for (let y = 0; y < mapChipH; y++) {
                    for (let x = 0; x < mapChipW; x++) {
                        mapchipsL2.value(x, y, (mapchipsL1.value(x, y) === 0) ? 0 : 1);
                    }
                }
                // 部屋シャッフル
                const rooms = dungeon.rooms.shuffle();
                // 開始位置
                const startPos = rooms[0].getCenter();
                player.x = startPos[0];
                player.y = startPos[1];
                // 階段位置
                const stairsPos = rooms[1].getCenter();
                mapchipsL1.value(stairsPos[0], stairsPos[1], 10);
                // モンスター配置
                const monsters = rooms.splice(2).map(x => {
                    const sprites = [];
                    if (!Game.pmode) {
                        const animdir = [2, 4, 8, 6];
                        for (let i = 0; i < 4; i++) {
                            const spr = [];
                            for (let j = 0; j < 4; j++) {
                                spr[j] = [((i + 1) * 4 + j) * 24, 0];
                            }
                            sprites[animdir[i]] = spr;
                        }
                        this._sprite = sprites;
                    }
                    else {
                        const animdir = [4, 8, 6, 2];
                        for (let i = 0; i < 4; i++) {
                            const spr = [];
                            for (let j = 0; j < 4; j++) {
                                spr[j] = [j * 24, i * 24];
                            }
                            sprites[animdir[i]] = spr;
                        }
                        this._sprite = sprites;
                    }
                    return new Monster({
                        _sprite: sprites,
                        _sprite_width: 24,
                        _sprite_height: 24,
                        x: x.getLeft(),
                        y: x.getTop(),
                    });
                });
                const map = new Dungeon.DungeonData({
                    width: mapChipW,
                    height: mapChipW,
                    gridsize: { width: 24, height: 24 },
                    layer: {
                        0: {
                            texture: "mapchip",
                            chip: {
                                1: { x: 48, y: 0 },
                                2: { x: 96, y: 96 },
                                10: { x: 96, y: 0 },
                            },
                            chips: mapchipsL1
                        },
                        1: {
                            texture: "mapchip",
                            chip: {
                                0: { x: 96, y: 72 },
                            },
                            chips: mapchipsL2
                        },
                    },
                });
                // カメラを更新
                map.update({
                    viewpoint: {
                        x: (player.x * map.gridsize.width + player.offx) + map.gridsize.width / 2,
                        y: (player.y * map.gridsize.height + player.offy) + map.gridsize.height / 2
                    },
                    viewwidth: Game.getScreen().width,
                    viewheight: Game.getScreen().height,
                });
                Game.getSound().reqPlayChannel(1, true);
                // assign virtual pad
                const pad = new Game.Input.VirtualStick();
                const pointerdown = (ev) => {
                    if (pad.onpointingstart(ev.pointerId)) {
                        const pos = Game.getScreen().pagePointToScreenPoint(ev.pageX, ev.pageY);
                        pad.x = pos[0];
                        pad.y = pos[1];
                    }
                };
                const pointermove = (ev) => {
                    const pos = Game.getScreen().pagePointToScreenPoint(ev.pageX, ev.pageY);
                    pad.onpointingmove(ev.pointerId, pos[0], pos[1]);
                };
                const pointerup = (ev) => {
                    pad.onpointingend(ev.pointerId);
                };
                const onPointerHook = () => {
                    Game.getInput().on("pointerdown", pointerdown);
                    Game.getInput().on("pointermove", pointermove);
                    Game.getInput().on("pointerup", pointerup);
                    Game.getInput().on("pointerleave", pointerup);
                };
                const offPointerHook = () => {
                    Game.getInput().off("pointerdown", pointerdown);
                    Game.getInput().off("pointermove", pointermove);
                    Game.getInput().off("pointerup", pointerup);
                    Game.getInput().off("pointerleave", pointerup);
                };
                this.suspend = () => {
                    offPointerHook();
                    Game.getSound().reqStopChannel(1);
                };
                this.resume = () => {
                    onPointerHook();
                    Game.getSound().reqPlayChannel(1, true);
                };
                this.leave = () => {
                    offPointerHook();
                    Game.getSound().reqStopChannel(1);
                };
                const updateLighting = (iswalkable) => {
                    map.clearLighting();
                    PathFinder.propagation({
                        array2D: map.layer[0].chips,
                        sx: player.x,
                        sy: player.y,
                        value: 140,
                        costs: (v) => iswalkable(v) ? 20 : 50,
                        output: map.lighting
                    });
                };
                const fade = new Fade(Game.getScreen().width, Game.getScreen().height);
                this.draw = () => {
                    Game.getScreen().save();
                    Game.getScreen().clearRect(0, 0, Game.getScreen().width, Game.getScreen().height);
                    Game.getScreen().fillStyle = "rgb(255,255,255)";
                    Game.getScreen().fillRect(0, 0, Game.getScreen().width, Game.getScreen().height);
                    map.draw((l, cameraLocalPx, cameraLocalPy) => {
                        if (l === 0) {
                            // 影
                            Game.getScreen().fillStyle = "rgba(0,0,0,0.25)";
                            Game.getScreen().beginPath();
                            Game.getScreen().ellipse(cameraLocalPx, cameraLocalPy + 7, 12, 3, 0, 0, Math.PI * 2);
                            Game.getScreen().fill();
                            // モンスター
                            const camera = map.camera;
                            monsters.forEach((monster) => {
                                const xx = monster.x - camera.chipLeft;
                                const yy = monster.y - camera.chipTop;
                                if (0 <= xx &&
                                    xx < Game.getScreen().width / 24 &&
                                    0 <= yy &&
                                    yy < Game.getScreen().height / 24) {
                                    const adir = monster.animDir;
                                    const af = monster.animFrame;
                                    Game.getScreen().drawImage(Game.getScreen().texture("monster"), monster.sprite[adir][af][0], monster.sprite[adir][af][1], monster.spriteWidth, monster.spriteHeight, xx * monster.spriteWidth + camera.chipOffX + monster.offx, yy * monster.spriteWidth + camera.chipOffY + monster.offy, monster.spriteWidth, monster.spriteHeight);
                                }
                            });
                            const animf = player.animFrame;
                            const playersprite = player.sprite[player.animDir];
                            // キャラクター
                            Game.getScreen().drawImage(Game.getScreen().texture("charactor"), playersprite[animf][0], playersprite[animf][1], player.spriteWidth, player.spriteHeight, cameraLocalPx - player.spriteWidth / 2, cameraLocalPy - player.spriteHeight / 2 - 12, player.spriteWidth, player.spriteHeight);
                        }
                    });
                    // フェード
                    fade.draw();
                    Game.getScreen().restore();
                    // バーチャルジョイスティックの描画
                    if (pad.isTouching) {
                        Game.getScreen().fillStyle = "rgba(255,255,255,0.25)";
                        Game.getScreen().beginPath();
                        Game.getScreen().ellipse(pad.x, pad.y, pad.radius * 1.2, pad.radius * 1.2, 0, 0, Math.PI * 2);
                        Game.getScreen().fill();
                        Game.getScreen().beginPath();
                        Game.getScreen().ellipse(pad.x + pad.cx, pad.y + pad.cy, pad.radius, pad.radius, 0, 0, Math.PI * 2);
                        Game.getScreen().fill();
                    }
                };
                yield waitTimeout({
                    timeout: 500,
                    init: () => { fade.startFadeIn(); },
                    update: (e) => { fade.update(e); updateLighting((v) => v === 1 || v === 10); },
                    end: (e) => { this.next(); }
                });
                onPointerHook();
                // ターンの状態（フェーズ）
                const turnStateStack = [[TurnState.WaitInput, null]];
                let playerTactics = {};
                let monstersTactics = [];
                yield (delta, ms) => {
                    switch (turnStateStack[0][0]) {
                        case TurnState.WaitInput:
                            {
                                // キー入力待ち
                                if (pad.isTouching === false || pad.distance <= 0.4) {
                                    player.setAnimation('idle', 0);
                                    break;
                                }
                                // キー入力されたのでプレイヤーの移動方向(5)は移動しない。
                                const playerMoveDir = pad.dir8;
                                // 「行動(Action)」と「移動(Move)」の識別を行う
                                // 移動先が侵入不可能の場合は待機とする
                                const [ox, oy] = Array2D.DIR8[playerMoveDir];
                                if (map.layer[0].chips.value(player.x + ox, player.y + oy) !== 1 && map.layer[0].chips.value(player.x + ox, player.y + oy) !== 10) {
                                    player.setDir(playerMoveDir);
                                    break;
                                }
                                // 移動先に敵がいる場合は「行動(Action)」、いない場合は「移動(Move)」
                                const targetMonster = monsters.findIndex((monster) => (monster.x === player.x + ox) &&
                                    (monster.y === player.y + oy));
                                if (targetMonster !== -1) {
                                    // 移動先に敵がいる＝「行動(Action)」
                                    playerTactics = {
                                        type: "action",
                                        moveDir: playerMoveDir,
                                        targetMonster: playerMoveDir,
                                        startTime: ms,
                                        actionTime: 250
                                    };
                                    // プレイヤーの行動、敵の行動の決定、敵の行動処理、移動実行の順で行う
                                    turnStateStack.unshift([TurnState.PlayerAction, null], [TurnState.EnemyAI, null], [TurnState.EnemyAction, 0], [TurnState.Move, null], [TurnState.TurnEnd, null]);
                                    break;
                                }
                                else {
                                    // 移動先に敵はいない＝「移動(Move)」
                                    playerTactics = {
                                        type: "move",
                                        moveDir: playerMoveDir,
                                        startTime: ms,
                                        actionTime: 250
                                    };
                                    // 敵の行動の決定、移動実行、敵の行動処理、の順で行う。
                                    turnStateStack.unshift([TurnState.EnemyAI, null], [TurnState.Move, null], [TurnState.EnemyAction, 0], [TurnState.TurnEnd, null]);
                                    break;
                                }
                            }
                        case TurnState.PlayerAction: {
                            // プレイヤーの行動開始
                            turnStateStack[0][0] = TurnState.PlayerActionRunning;
                            player.setDir(playerTactics.moveDir);
                            player.setAnimation('action', 0);
                            break;
                        }
                        case TurnState.PlayerActionRunning: {
                            // プレイヤーの行動中
                            const rate = (ms - playerTactics.startTime) / playerTactics.actionTime;
                            player.setAnimation('action', rate);
                            if (rate >= 1) {
                                // プレイヤーの行動終了
                                turnStateStack.shift();
                            }
                            break;
                        }
                        case TurnState.EnemyAI: {
                            // 敵の行動の決定
                            // プレイヤーが移動する場合、移動先にいると想定して敵の行動を決定する
                            let px = player.x;
                            let py = player.y;
                            if (playerTactics.type === "move") {
                                const off = Array2D.DIR8[playerTactics.moveDir];
                                px += off[0];
                                py += off[1];
                            }
                            monstersTactics = monsters.map((monster) => {
                                const dx = px - monster.x;
                                const dy = py - monster.y;
                                if (Math.abs(dx) <= 1 && Math.abs(dy) <= 1) {
                                    // 移動先のプレイヤー位置は現在位置に隣接しているので、行動(Action)を選択
                                    const dir = Array2D.DIR8.findIndex((x) => x[0] === dx && x[1] === dy);
                                    return {
                                        type: "action",
                                        moveDir: dir,
                                        startTime: 0,
                                        actionTime: 250
                                    };
                                }
                                else {
                                    // 移動先のプレイヤー位置は現在位置に隣接していないので、移動(Move)を選択
                                    // とりあえず軸合わせで動く
                                    const cands = [
                                        [Math.sign(dx), Math.sign(dy)],
                                        (Math.abs(dx) > Math.abs(dy)) ? [0, Math.sign(dy)] : [Math.sign(dx), 0],
                                        (Math.abs(dx) > Math.abs(dy)) ? [Math.sign(dx), 0] : [0, Math.sign(dy)],
                                    ];
                                    for (let i = 0; i < 3; i++) {
                                        const [cx, cy] = cands[i];
                                        if (map.layer[0].chips.value(monster.x + cx, monster.y + cy) === 1 || map.layer[0].chips.value(monster.x + cx, monster.y + cy) === 10) {
                                            const dir = Array2D.DIR8.findIndex((x) => x[0] === cx && x[1] === cy);
                                            return {
                                                type: "move",
                                                moveDir: dir,
                                                startTime: ms,
                                                actionTime: 250
                                            };
                                        }
                                    }
                                    return {
                                        type: "idle",
                                        moveDir: 5,
                                        startTime: ms,
                                        actionTime: 250
                                    };
                                }
                            });
                            // 敵の行動の決定の終了
                            turnStateStack.shift();
                            break;
                        }
                        case TurnState.EnemyAction: {
                            // 敵の行動開始
                            let enemyId = turnStateStack[0][1];
                            while (enemyId < monstersTactics.length) {
                                if (monstersTactics[enemyId].type !== "action") {
                                    enemyId++;
                                }
                                else {
                                    break;
                                }
                            }
                            if (enemyId < monstersTactics.length) {
                                monstersTactics[enemyId].startTime = ms;
                                monsters[enemyId].setDir(monstersTactics[enemyId].moveDir);
                                monsters[enemyId].setAnimation('action', 0);
                                turnStateStack[0][0] = TurnState.EnemyActionRunning;
                                turnStateStack[0][1] = enemyId;
                            }
                            else {
                                // もう動かす敵がいない
                                turnStateStack.shift();
                            }
                            break;
                        }
                        case TurnState.EnemyActionRunning: {
                            // 敵の行動中
                            const enemyId = turnStateStack[0][1];
                            const rate = (ms - monstersTactics[enemyId].startTime) / monstersTactics[enemyId].actionTime;
                            monsters[enemyId].setAnimation('action', rate);
                            if (rate >= 1) {
                                // 行動終了。次の敵へ
                                turnStateStack[0][0] = TurnState.EnemyAction;
                                turnStateStack[0][1] = enemyId + 1;
                            }
                            break;
                        }
                        case TurnState.Move: {
                            // 移動開始
                            turnStateStack[0][0] = TurnState.MoveRunning;
                            monstersTactics.forEach((monsterTactic, i) => {
                                if (monsterTactic.type === "move") {
                                    monsters[i].setDir(monsterTactic.moveDir);
                                    monsters[i].setAnimation('move', 0);
                                    monstersTactics[i].startTime = ms;
                                }
                            });
                            if (playerTactics.type === "move") {
                                player.setDir(playerTactics.moveDir);
                                player.setAnimation('move', 0);
                                playerTactics.startTime = ms;
                            }
                            break;
                        }
                        case TurnState.MoveRunning: {
                            // 移動実行
                            let finish = true;
                            monstersTactics.forEach((monsterTactic, i) => {
                                if (monsterTactic.type === "move") {
                                    const rate = (ms - monsterTactic.startTime) / monsterTactic.actionTime;
                                    monsters[i].setDir(monsterTactic.moveDir);
                                    monsters[i].setAnimation('move', rate);
                                    if (rate < 1) {
                                        finish = false; // 行動終了していないフラグをセット
                                    }
                                }
                            });
                            if (playerTactics.type === "move") {
                                const rate = (ms - playerTactics.startTime) / playerTactics.actionTime;
                                player.setDir(playerTactics.moveDir);
                                player.setAnimation('move', rate);
                                if (rate < 1) {
                                    finish = false; // 行動終了していないフラグをセット
                                }
                            }
                            if (finish) {
                                // 行動終了
                                turnStateStack.shift();
                                monstersTactics.forEach((monsterTactic, i) => {
                                    if (monsterTactic.type === "move") {
                                        monsters[i].x += Array2D.DIR8[monsterTactic.moveDir][0];
                                        monsters[i].y += Array2D.DIR8[monsterTactic.moveDir][1];
                                        monsters[i].offx = 0;
                                        monsters[i].offy = 0;
                                    }
                                });
                                if (playerTactics.type === "move") {
                                    player.x += Array2D.DIR8[playerTactics.moveDir][0];
                                    player.y += Array2D.DIR8[playerTactics.moveDir][1];
                                    player.offx = 0;
                                    player.offy = 0;
                                }
                                // 現在位置のマップチップを取得
                                const chip = map.layer[0].chips.value(~~player.x, ~~player.y);
                                if (chip === 10) {
                                    // 階段なので次の階層に移動させる。
                                    this.next("nextfloor");
                                }
                            }
                            break;
                        }
                        case TurnState.TurnEnd: {
                            // ターン終了
                            turnStateStack.shift();
                            break;
                        }
                    }
                    // カメラを更新
                    map.update({
                        viewpoint: {
                            x: (player.x * map.gridsize.width + player.offx) + map.gridsize.width / 2,
                            y: (player.y * map.gridsize.height + player.offy) + map.gridsize.height / 2
                        },
                        viewwidth: Game.getScreen().width,
                        viewheight: Game.getScreen().height,
                    });
                    updateLighting((v) => v === 1 || v === 10);
                    if (Game.getInput().isClick() && Game.getScreen().pagePointContainScreen(Game.getInput().pageX, Game.getInput().pageY)) {
                        Game.getSceneManager().push("mapview", { map: map, player: player });
                    }
                };
                Game.getSound().reqPlayChannel(3);
                yield waitTimeout({
                    timeout: 500,
                    init: () => { fade.startFadeOut(); },
                    update: (e) => { fade.update(e); updateLighting((v) => v === 1 || v === 10); },
                    end: (e) => { this.next(); }
                });
                yield waitTimeout({
                    timeout: 500,
                    end: (e) => { floor++; this.next(); }
                });
                Game.getSceneManager().pop();
                Game.getSceneManager().push("dungeon", { player: player, floor: floor });
            },
            mapview: function* (data) {
                this.draw = () => {
                    Game.getScreen().save();
                    Game.getScreen().clearRect(0, 0, Game.getScreen().width, Game.getScreen().height);
                    Game.getScreen().fillStyle = "rgb(0,0,0)";
                    Game.getScreen().fillRect(0, 0, Game.getScreen().width, Game.getScreen().height);
                    const offx = ~~((Game.getScreen().width - data.map.width * 5) / 2);
                    const offy = ~~((Game.getScreen().height - data.map.height * 5) / 2);
                    // ミニマップを描画
                    for (let y = 0; y < data.map.height; y++) {
                        for (let x = 0; x < data.map.width; x++) {
                            const chip = data.map.layer[0].chips.value(x, y);
                            let color = "rgb(52,12,0)";
                            switch (chip) {
                                case 1:
                                    color = "rgb(179,116,39)";
                                    break;
                                case 10:
                                    color = "rgb(255,0,0)";
                                    break;
                            }
                            Game.getScreen().fillStyle = color;
                            Game.getScreen().fillRect(offx + x * 5, offy + y * 5, 5, 5);
                            let light = 1 - data.map.visibled.value(x, y) / 100;
                            if (light > 1) {
                                light = 1;
                            }
                            else if (light < 0) {
                                light = 0;
                            }
                            Game.getScreen().fillStyle = `rgba(0,0,0,${light})`;
                            Game.getScreen().fillRect(offx + x * 5, offy + y * 5, 5, 5);
                        }
                    }
                    Game.getScreen().fillStyle = "rgb(0,255,0)";
                    Game.getScreen().fillRect(offx + data.player.x * 5, offy + data.player.y * 5, 5, 5);
                    Game.getScreen().restore();
                };
                yield waitClick({ end: () => this.next() });
                Game.getSceneManager().pop();
            },
        }
    }).then(() => {
        let anim = 0;
        const update = (ms) => {
            Game.getScreen().save();
            Game.getScreen().clearRect(0, 0, Game.getScreen().width, Game.getScreen().height);
            Game.getScreen().fillStyle = "rgb(255,255,255)";
            Game.getScreen().fillRect(0, 0, Game.getScreen().width, Game.getScreen().height);
            const n = ~(ms / 50);
            Game.getScreen().translate(Game.getScreen().width / 2, Game.getScreen().height / 2);
            Game.getScreen().rotate(n * Math.PI / 4);
            for (let i = 0; i < 8; i++) {
                const g = (i * 32);
                Game.getScreen().save();
                Game.getScreen().rotate(i * Math.PI / 4);
                Game.getScreen().fillStyle = `rgb(${g},${g},${g})`;
                Game.getScreen().fillRect(-5, -50, 10, 25);
                Game.getScreen().restore();
            }
            Game.getScreen().restore();
            anim = requestAnimationFrame(update.bind(this));
        };
        anim = requestAnimationFrame(update.bind(this));
        return Promise.all([
            Game.getScreen().loadImage({
                title: "./assets/title.png",
                mapchip: "./assets/mapchip.png",
                charactor: "./assets/charactor.png",
                monster: "./assets/monster.png"
            }),
            Game.getSound().loadSoundsToChannel({
                0: "./assets/title.mp3",
                1: "./assets/dungeon.mp3",
                2: "./assets/classroom.mp3",
                3: "./assets/kaidan.mp3"
            }),
            new Promise((resolve, reject) => setTimeout(() => resolve(), 5000))
        ]).then(() => {
            cancelAnimationFrame(anim);
        });
    }).then(() => {
        Game.getSceneManager().push("title");
        Game.getTimer().on((delta, now, id) => {
            Game.getInput().endCapture();
            Game.getSceneManager().update(delta, now);
            Game.getInput().startCapture();
            Game.getSound().playChannel();
            Game.getSceneManager().draw();
        });
        Game.getTimer().start();
    });
};
//# sourceMappingURL=tsjq.js.map