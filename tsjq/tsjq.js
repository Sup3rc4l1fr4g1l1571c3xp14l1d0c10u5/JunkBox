"use strict";
Array.prototype.shuffle = function () {
    const self = this.slice();
    for (let i = self.length - 1; i > 0; i--) {
        const r = Math.floor(Math.random() * (i + 1));
        const tmp = self[i];
        self[i] = self[r];
        self[r] = tmp;
    }
    return self;
};
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
            for (var i = 0, len = temp.length; i < len; ++i) {
                temp[i].apply(this, args);
            }
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
                    var msg = `�t�@�C�� ${asserts[x]}�̃��[�h�Ɏ��s�B`;
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
                        var msg = `�t�@�C�� ${file}�̃��[�h�Ɏ��s�B`;
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
                        var msg = `�t�@�C�� ${file}��decodeAudioData�Ɏ��s�B`;
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
                            throw new Error("createBufferSource�Ɏ��s�B");
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
        class InputDispatcher extends Dispatcher.EventDispatcher {
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
                if (document.body["pointermove"]) {
                    consolere.log("pointer event is implemented");
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
            }
            checkEvent(e) {
                e.preventDefault();
                const istouch = e.type.indexOf("touch") === 0;
                const ismouse = e.type.indexOf("mouse") === 0;
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
                    const singleFinger = evt["mouse"] || (evt["touch"] && e.touches.length === 1);
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
        Input.InputDispatcher = InputDispatcher;
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
                this.update = null;
                this.draw = null;
                this.leave = null;
                this.suspend = null;
                this.resume = null;
            }
            next(...data) {
                return this.state.next.apply(this.state, data);
            }
            push(id, param = {}) { this.manager.push(id, param); }
            pop() { this.manager.pop(); }
            // virtual methods
            enter(...data) {
                this.state = this.init.apply(this, data);
                this.next(null);
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
                return this;
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
                return this;
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
                if (this.sceneStack.length === 0) {
                    throw new Error("there is no scene.");
                }
                this.peek().update.apply(this.peek(), args);
                return this;
            }
            draw() {
                if (this.sceneStack.length === 0) {
                    throw new Error("there is no scene.");
                }
                this.peek().draw.apply(this.peek());
                return this;
            }
        }
        Scene_1.SceneManager = SceneManager;
    })(Scene = Game.Scene || (Game.Scene = {}));
})(Game || (Game = {}));
var Dungeon;
(function (Dungeon) {
    // マップ描画時の視点・視野情報
    class Camera {
    }
    Dungeon.Camera = Camera;
    // マス目
    class Matrix {
        constructor(width, height, fill) {
            this.matrixWidth = width;
            this.matrixHeight = height;
            if (fill == undefined) {
                this.matrixBuffer = new Array(width * height);
            }
            else {
                this.matrixBuffer = new Array(width * height).fill(fill);
            }
        }
        get width() {
            return this.matrixWidth;
        }
        get height() {
            return this.matrixHeight;
        }
        value(x, y, value) {
            if (0 > x || x >= this.matrixWidth || 0 > y || y >= this.matrixHeight) {
                return 0;
            }
            if (value != undefined) {
                this.matrixBuffer[y * this.matrixWidth + x] = value;
            }
            return this.matrixBuffer[y * this.matrixWidth + x];
        }
        fill(value) {
            this.matrixBuffer.fill(value);
            return this;
        }
        dup() {
            const m = new Matrix(this.width, this.height);
            m.matrixBuffer = this.matrixBuffer.slice();
            return m;
        }
        static createFromArray(array, fill) {
            const h = array.length;
            const w = Math.max.apply(Math, array.map(x => x.length));
            var matrix = new Matrix(w, h, fill);
            array.forEach((vy, y) => vy.forEach((vx, x) => matrix.value(x, y, vx)));
            return matrix;
        }
        toString() {
            const lines = [];
            for (let y = 0; y < this.height; y++) {
                lines[y] = `|${this.matrixBuffer.slice((y + 0) * this.matrixWidth, (y + 1) * this.matrixWidth).join(", ")}|`;
            }
            return lines.join("\r\n");
        }
        // 基点からの重み距離算出
        propagation(sx, sy, value, costs, opts) {
            opts = Object.assign({ left: 0, top: 0, right: this.width, bottom: this.height, timeout: 1000, topology: 8 }, opts);
            const temp = new Matrix(this.width, this.height, 0);
            const topology = opts.topology;
            var dirs;
            if (topology === 4) {
                dirs = Matrix.dir4;
            }
            else if (topology === 8) {
                dirs = Matrix.dir8;
            }
            else {
                throw new Error("Illegal topology");
            }
            temp.value(sx, sy, value);
            const request = dirs.map(([ox, oy]) => [sx + ox, sy + oy, value]);
            var start = Date.now();
            while (request.length !== 0 && (Date.now() - start) < opts.timeout) {
                var [x, y, currentValue] = request.shift();
                if (opts.top > y || y >= opts.bottom || opts.left > x || x >= opts.right) {
                    continue;
                }
                const cost = costs(this.value(x, y));
                if (cost < 0 || currentValue < cost) {
                    continue;
                }
                currentValue -= cost;
                const targetPower = temp.value(x, y);
                if (currentValue <= targetPower) {
                    continue;
                }
                temp.value(x, y, currentValue);
                Array.prototype.push.apply(request, dirs.map(([ox, oy]) => [x + ox, y + oy, currentValue]));
            }
            return temp;
        }
        // A*での経路探索
        pathfind(fromX, fromY, toX, toY, costs, opts) {
            opts = Object.assign({ topology: 8 }, opts);
            var topology = opts.topology;
            var dirs;
            if (topology === 4) {
                dirs = Matrix.dir4;
            }
            else if (topology === 8) {
                dirs = Matrix.dir8;
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
        // 重み距離を使ったA*
        pathfindByPropergation(fromX, fromY, toX, toY, propagation, opts) {
            opts = Object.assign({ topology: 8 }, opts);
            const topology = opts.topology;
            let dirs;
            if (topology === 4) {
                dirs = Matrix.dir4;
            }
            else if (topology === 8) {
                dirs = Matrix.dir8;
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
    }
    Matrix.dir4 = [
        [0, -1],
        [1, 0],
        [0, 1],
        [-1, 0]
    ];
    Matrix.dir8 = [
        [0, -1],
        [1, 0],
        [0, 1],
        [-1, 0]
    ];
    Dungeon.Matrix = Matrix;
    // ダンジョンデータ
    class DungeonData {
        constructor(config) {
            this.width = config.width;
            this.height = config.height;
            this.gridsize = config.gridsize;
            this.layer = config.layer;
            this.camera = new Camera();
            this.lighting = new Matrix(this.width, this.height, 0);
            this.visibled = new Matrix(this.width, this.height, 0);
        }
        clearLighting() {
            this.lighting.fill(0);
            return this;
        }
        // update camera
        update(param) {
            var mapWidth = this.width * this.gridsize.width;
            var mapHeight = this.height * this.gridsize.height;
            // マップ上でのカメラの注視点
            var mapPx = param.viewpoint.x;
            var mapPy = param.viewpoint.y;
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
            // 視野の左上位置に対応するマップチップ座標を算出
            this.camera.chipX = ~~(this.camera.left / this.gridsize.width);
            this.camera.chipY = ~~(this.camera.top / this.gridsize.height);
            // 視野の左上位置をにマップチップをおいた場合のスクロールによるズレ量を算出
            this.camera.chipOffX = -(this.camera.left % this.gridsize.width);
            this.camera.chipOffY = -(this.camera.top % this.gridsize.height);
        }
        draw(layerDrawHook) {
            // 描画開始
            var gridw = this.gridsize.width;
            var gridh = this.gridsize.height;
            var yy = ~~(this.camera.height / gridh + 1);
            var xx = ~~(this.camera.width / gridw + 1);
            Object.keys(this.layer).forEach((key) => {
                var l = ~~key;
                for (let y = -1; y < yy; y++) {
                    for (let x = -1; x < xx; x++) {
                        const chipid = this.layer[l].chips.value(x + this.camera.chipX, y + this.camera.chipY) || 0;
                        if (this.layer[l].chip[chipid]) {
                            Game.getScreen().drawImage(Game.getScreen().texture(this.layer[l].texture), this.layer[l].chip[chipid].x, this.layer[l].chip[chipid].y, gridw, gridh, 0 + x * gridw + this.camera.chipOffX + gridw / 2, 0 + y * gridh + this.camera.chipOffY + gridh / 2, gridw, gridh);
                        }
                    }
                }
                // レイヤー描画フック
                layerDrawHook(l, this.camera.localPx, this.camera.localPy);
            });
            // 照明描画
            for (let y = -1; y < yy; y++) {
                for (let x = -1; x < xx; x++) {
                    let light = this.lighting.value(x + this.camera.chipX, y + this.camera.chipY) / 100;
                    if (light > 1) {
                        light = 1;
                    }
                    else if (light < 0) {
                        light = 0;
                    }
                    Game.getScreen().fillStyle = `rgba(0,0,0,${1 - light})`;
                    Game.getScreen().fillRect(0 + x * gridw + this.camera.chipOffX + gridw / 2, 0 + y * gridh + this.camera.chipOffY + gridh / 2, gridw, gridh);
                }
            }
        }
    }
    Dungeon.DungeonData = DungeonData;
    //ダンジョン自動生成
    let Generator;
    (function (Generator) {
        class Feature {
            isValid(isWallCallback, canBeDugCallback) { }
            ;
            create(digCallback) { }
            ;
            debug() { }
            ;
            static createRandomAt(x, y, dx, dy, options) { }
            ;
        }
        function getUniformInt(lowerBound, upperBound) {
            const max = Math.max(lowerBound, upperBound);
            const min = Math.min(lowerBound, upperBound);
            return Math.floor(Math.random() * (max - min + 1)) + min;
        }
        function getWeightedValue(data) {
            let total = 0;
            for (var id in data) {
                total += data[id];
            }
            const random = Math.random() * total;
            let part = 0;
            for (var id in data) {
                part += data[id];
                if (random < part) {
                    return id;
                }
            }
            return id;
        }
        class Room extends Feature {
            constructor(x1, y1, x2, y2, doorX, doorY) {
                super();
                this._x1 = x1;
                this._y1 = y1;
                this._x2 = x2;
                this._y2 = y2;
                this._doors = {};
                if (arguments.length > 4) {
                    this.addDoor(doorX, doorY);
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
                    let y2 = y - Math.floor(Math.random() * height);
                    return new Room(x + 1, y2, x + width, y2 + height - 1, x, y);
                }
                if (dx === -1) {
                    let y2 = y - Math.floor(Math.random() * height);
                    return new Room(x - width, y2, x - 1, y2 + height - 1, x, y);
                }
                if (dy === 1) {
                    let x2 = x - Math.floor(Math.random() * width);
                    return new Room(x2, y + 1, x2 + width - 1, y + height, x, y);
                }
                if (dy === -1) {
                    let x2 = x - Math.floor(Math.random() * width);
                    return new Room(x2, y - height, x2 + width - 1, y - 1, x, y);
                }
                throw new Error("dx or dy must be 1 or -1");
            }
            ;
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
            ;
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
            ;
            addDoor(x, y) {
                this._doors[x + "," + y] = 1;
                return this;
            }
            ;
            getDoors(callback) {
                for (let key in this._doors) {
                    const parts = key.split(",");
                    callback(parseInt(parts[0]), parseInt(parts[1]));
                }
                return this;
            }
            ;
            clearDoors() {
                this._doors = {};
                return this;
            }
            ;
            addDoors(isWallCallback) {
                const left = this._x1 - 1;
                const right = this._x2 + 1;
                const top = this._y1 - 1;
                const bottom = this._y2 + 1;
                for (let x = left; x <= right; x++) {
                    for (let y = top; y <= bottom; y++) {
                        if (x != left && x != right && y != top && y != bottom) {
                            continue;
                        }
                        if (isWallCallback(x, y)) {
                            continue;
                        }
                        this.addDoor(x, y);
                    }
                }
                return this;
            }
            ;
            debug() {
                consolere.log("room", this._x1, this._y1, this._x2, this._y2);
            }
            ;
            isValid(isWallCallback, canBeDugCallback) {
                const left = this._x1 - 1;
                const right = this._x2 + 1;
                const top = this._y1 - 1;
                const bottom = this._y2 + 1;
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
            ;
            /**
             * @param {function} digCallback Dig callback with a signature (x, y, value). Values: 0 = empty, 1 = wall, 2 = door. Multiple doors are allowed.
             */
            create(digCallback) {
                const left = this._x1 - 1;
                const right = this._x2 + 1;
                const top = this._y1 - 1;
                const bottom = this._y2 + 1;
                let value = 0;
                for (let x = left; x <= right; x++) {
                    for (let y = top; y <= bottom; y++) {
                        if (x + "," + y in this._doors) {
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
            ;
            getCenter() {
                return [Math.round((this._x1 + this._x2) / 2), Math.round((this._y1 + this._y2) / 2)];
            }
            ;
            getLeft() {
                return this._x1;
            }
            ;
            getRight() {
                return this._x2;
            }
            ;
            getTop() {
                return this._y1;
            }
            ;
            getBottom() {
                return this._y2;
            }
            ;
        }
        ;
        class Corridor extends Feature {
            constructor(startX, startY, endX, endY) {
                super();
                this._startX = startX;
                this._startY = startY;
                this._endX = endX;
                this._endY = endY;
                this._endsWithAWall = true;
            }
            static createRandomAt(x, y, dx, dy, options) {
                const min = options.corridorLength[0];
                const max = options.corridorLength[1];
                const length = getUniformInt(min, max);
                return new Corridor(x, y, x + dx * length, y + dy * length);
            }
            ;
            debug() {
                consolere.log("corridor", this._startX, this._startY, this._endX, this._endY);
            }
            ;
            isValid(isWallCallback, canBeDugCallback) {
                const sx = this._startX;
                const sy = this._startY;
                let dx = this._endX - sx;
                let dy = this._endY - sy;
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
                        this._endX = x - dx;
                        this._endY = y - dy;
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
                if (length === 1 && isWallCallback(this._endX + dx, this._endY + dy)) {
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
                const firstCornerBad = !isWallCallback(this._endX + dx + nx, this._endY + dy + ny);
                const secondCornerBad = !isWallCallback(this._endX + dx - nx, this._endY + dy - ny);
                this._endsWithAWall = isWallCallback(this._endX + dx, this._endY + dy);
                if ((firstCornerBad || secondCornerBad) && this._endsWithAWall) {
                    return false;
                }
                return true;
            }
            ;
            create(digCallback) {
                const sx = this._startX;
                const sy = this._startY;
                let dx = this._endX - sx;
                let dy = this._endY - sy;
                const length = 1 + Math.max(Math.abs(dx), Math.abs(dy));
                if (dx) {
                    dx = dx / Math.abs(dx);
                }
                if (dy) {
                    dy = dy / Math.abs(dy);
                }
                //const nx = dy;
                //const ny = -dx;
                for (let i = 0; i < length; i++) {
                    const x = sx + i * dx;
                    const y = sy + i * dy;
                    digCallback(x, y, 0);
                }
                return true;
            }
            ;
            createPriorityWalls(priorityWallCallback) {
                if (!this._endsWithAWall) {
                    return;
                }
                const sx = this._startX;
                const sy = this._startY;
                let dx = this._endX - sx;
                let dy = this._endY - sy;
                if (dx) {
                    dx = dx / Math.abs(dx);
                }
                if (dy) {
                    dy = dy / Math.abs(dy);
                }
                const nx = dy;
                const ny = -dx;
                priorityWallCallback(this._endX + dx, this._endY + dy);
                priorityWallCallback(this._endX + nx, this._endY + ny);
                priorityWallCallback(this._endX - nx, this._endY - ny);
            }
            ;
        }
        class Map {
            constructor(width, height, option) {
                this.FeatureClass = { Room: Room, Corridor: Corridor };
                this._ROTDIRS4 = [
                    [0, -1],
                    [1, 0],
                    [0, 1],
                    [-1, 0]
                ];
                this._width = width;
                this._height = height;
                this._rooms = []; /* list of all rooms */
                this._corridors = [];
                this._options = {
                    roomWidth: [3, 9],
                    roomHeight: [3, 5],
                    corridorLength: [3, 10],
                    dugPercentage: 0.2,
                    timeLimit: 1000 /* we stop after this much time has passed (msec) */
                };
                Object.assign(this._options, option);
                this._features = {
                    Room: 4,
                    Corridor: 4
                };
                this._featureAttempts = 20; /* how many times do we try to create a feature on a suitable wall */
                this._walls = {}; /* these are available for digging */
                this._digCallback = this._digCallback.bind(this);
                this._canBeDugCallback = this._canBeDugCallback.bind(this);
                this._isWallCallback = this._isWallCallback.bind(this);
                this._priorityWallCallback = this._priorityWallCallback.bind(this);
            }
            create(callback) {
                this._rooms = [];
                this._corridors = [];
                this._map = this._fillMap(1);
                this._walls = {};
                this._dug = 0;
                const area = (this._width - 2) * (this._height - 2);
                this._firstRoom();
                const t1 = Date.now();
                do {
                    const t2 = Date.now();
                    if (t2 - t1 > this._options.timeLimit) {
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
                    //		consolere.log("wall", x, y);
                    /* try adding a feature */
                    let featureAttempts = 0;
                    do {
                        featureAttempts++;
                        if (this._tryFeature(x, y, dir[0], dir[1])) {
                            //if (this._rooms.length + this._corridors.length == 2) { this._rooms[0].addDoor(x, y); } /* first room oficially has doors */
                            this._removeSurroundingWalls(x, y);
                            this._removeSurroundingWalls(x - dir[0], y - dir[1]);
                            break;
                        }
                    } while (featureAttempts < this._featureAttempts);
                    var priorityWalls = 0;
                    for (let id in this._walls) {
                        if (this._walls[id] > 1) {
                            priorityWalls++;
                        }
                    }
                } while (this._dug / area < this._options.dugPercentage || priorityWalls); /* fixme number of priority walls */
                this._addDoors();
                if (callback) {
                    for (let i = 0; i < this._width; i++) {
                        for (let j = 0; j < this._height; j++) {
                            callback(i, j, this._map[i][j]);
                        }
                    }
                }
                this._walls = {};
                this._map = null;
                return this;
            }
            ;
            _digCallback(x, y, value) {
                if (value == 0 || value == 2) {
                    this._map[x][y] = 0;
                    this._dug++;
                }
                else {
                    this._walls[x + "," + y] = 1;
                }
            }
            ;
            _isWallCallback(x, y) {
                if (x < 0 || y < 0 || x >= this._width || y >= this._height) {
                    return false;
                }
                return (this._map[x][y] == 1);
            }
            ;
            _canBeDugCallback(x, y) {
                if (x < 1 || y < 1 || x + 1 >= this._width || y + 1 >= this._height) {
                    return false;
                }
                return (this._map[x][y] == 1);
            }
            ;
            _priorityWallCallback(x, y) {
                this._walls[x + "," + y] = 2;
            }
            ;
            _findWall() {
                const prio1 = [];
                const prio2 = [];
                for (let id in this._walls) {
                    const prio = this._walls[id];
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
                delete this._walls[id2];
                return id2;
            }
            ;
            _firstRoom() {
                const cx = Math.floor(this._width / 2);
                const cy = Math.floor(this._height / 2);
                const room = Room.createRandomCenter(cx, cy, this._options);
                this._rooms.push(room);
                room.create(this._digCallback);
            }
            ;
            _fillMap(value) {
                const map = [];
                for (let i = 0; i < this._width; i++) {
                    map.push([]);
                    for (let j = 0; j < this._height; j++) {
                        map[i].push(value);
                    }
                }
                return map;
            }
            ;
            _tryFeature(x, y, dx, dy) {
                const featureType = getWeightedValue(this._features);
                const feature = this.FeatureClass[featureType].createRandomAt(x, y, dx, dy, this._options);
                if (!feature.isValid(this._isWallCallback, this._canBeDugCallback)) {
                    //		consolere.log("not valid");
                    //		feature.debug();
                    return false;
                }
                feature.create(this._digCallback);
                //	feature.debug();
                if (feature instanceof Room) {
                    this._rooms.push(feature);
                }
                if (feature instanceof Corridor) {
                    feature.createPriorityWalls(this._priorityWallCallback);
                    this._corridors.push(feature);
                }
                return true;
            }
            ;
            _removeSurroundingWalls(cx, cy) {
                const deltas = this._ROTDIRS4;
                for (let i = 0; i < deltas.length; i++) {
                    const delta = deltas[i];
                    var x = cx + delta[0];
                    var y = cy + delta[1];
                    delete this._walls[x + "," + y];
                    var x = cx + 2 * delta[0];
                    var y = cy + 2 * delta[1];
                    delete this._walls[x + "," + y];
                }
            }
            ;
            _getDiggingDirection(cx, cy) {
                if (cx <= 0 || cy <= 0 || cx >= this._width - 1 || cy >= this._height - 1) {
                    return null;
                }
                let result = null;
                const deltas = this._ROTDIRS4;
                for (let i = 0; i < deltas.length; i++) {
                    const delta = deltas[i];
                    const x = cx + delta[0];
                    const y = cy + delta[1];
                    if (!this._map[x][y]) {
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
            ;
            _addDoors() {
                var data = this._map;
                const isWallCallback = (x, y) => {
                    return (data[x][y] == 1);
                };
                for (let i = 0; i < this._rooms.length; i++) {
                    const room = this._rooms[i];
                    room.clearDoors();
                    room.addDoors(isWallCallback);
                }
            }
            ;
            getRooms() {
                return this._rooms;
            }
            ;
            getCorridors() {
                return this._corridors;
            }
            ;
        }
        function create(w, h, callback) {
            return new Map(w, h, {}).create(callback);
        }
        Generator.create = create;
    })(Generator = Dungeon.Generator || (Dungeon.Generator = {}));
})(Dungeon || (Dungeon = {}));
/*
//
//interface IteratorResult<T> {
//    //done: boolean;
//    //value: T;
//}

//interface Iterator<T> {
//    next(value?: any): IteratorResult<T>;
//    return?(value?: any): IteratorResult<T>;
//    throw?(e?: any): IteratorResult<T>;
//}

//interface Generator extends Iterator<any> { }

//interface GeneratorFunction {
//    /**
//     * Creates a new Generator object.
//     * @param args A list of arguments the function accepts.
//     */
//    new(...args: any[]): Generator;
//    /**
//     * Creates a new Generator object.
//     * @param args A list of arguments the function accepts.
//     */
//    (...args: any[]): Generator;
//    /**
//     * The length of the arguments.
//     */
//    length: number;
//    /**
//     * Returns the name of the function.
//     */
//    name: string;
//    /**
//     * A reference to the prototype.
//     */
//    prototype: Generator;
//}
//interface GeneratorFunctionConstructor {
//    /**
//     * Creates a new Generator function.
//     * @param args A list of arguments the function accepts.
//     */
//    new(...args: string[]): GeneratorFunction;
//    /**
//     * Creates a new Generator function.
//     * @param args A list of arguments the function accepts.
//     */
//    (...args: string[]): GeneratorFunction;
//    /**
//     * The length of the arguments.
//     */
//    length: number;
//    /**
//     * Returns the name of the function.
//     */
//    name: string;
//    /**
//     * A reference to the prototype.
//     */
//    //prototype: GeneratorFunction;
//}
//interface CanvasRenderingContext2D {
//    mozImageSmoothingEnabled: boolean;
//    imageSmoothingEnabled: boolean;
//    webkitImageSmoothingEnabled: boolean;
//    ellipse: (x: number, y: number, radiusX: number, radiusY: number, rotation: number, startAngle: number, endAngle: number, anticlockwise?: boolean) => void;
//}
//*/
var consolere;
var Game;
(function (Game) {
    consolere.log("remote log start");
    // Global Variables
    var video = null;
    var sceneManager = null;
    var input = null;
    var timer = null;
    var sound = null;
    //
    function create(config) {
        return new Promise((resolve, reject) => {
            try {
                document.title = config.title;
                video = new Game.Video(config.screen.id);
                video.imageSmoothingEnabled = false;
                sceneManager = new Game.Scene.SceneManager(config.scene);
                timer = new Game.Timer.AnimationTimer();
                input = new Game.Input.InputDispatcher();
                sound = new Game.Sound.SoundManager();
                resolve();
            }
            catch (e) {
                reject(e);
            }
        });
    }
    Game.create = create;
    ;
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
        return input;
    }
    Game.getInput = getInput;
    function getSound() {
        return sound;
    }
    Game.getSound = getSound;
})(Game || (Game = {}));
class Player {
    constructor(config) {
        this.charactor = config.charactor;
        this.x = config.x;
        this.y = config.y;
        this.offx = 0;
        this.offy = 0;
        this.dir = "down";
        this.movemode = "idle";
        this.movems = 0;
        this.anim = 0;
        // 移動時間とアニメーション時間(どちらもms単位)
        // ダッシュ相当の設定
        //this.movestep = 150;
        //this.animstep = 150;
        // 通常方向の設定
        this.movestep = 250;
        this.animstep = 250;
        this.changeCharactor(this.charactor);
    }
    changeCharactor(charactor) {
        this.charactor = charactor;
        var psbasex = (this.charactor % 2) * 752;
        var psbasey = ~~(this.charactor / 2) * 47;
        this._sprite_width = 47;
        this._sprite_height = 47;
        this._sprite = {
            down: [[0, 0], [1, 0], [2, 0], [3, 0]].map(xy => [
                psbasex + this._sprite_width * xy[0], psbasey + this._sprite_height * xy[1]
            ]),
            left: [[4, 0], [5, 0], [6, 0], [7, 0]].map(xy => [
                psbasex + this._sprite_width * xy[0], psbasey + this._sprite_height * xy[1]
            ]),
            up: [[8, 0], [9, 0], [10, 0], [11, 0]].map(xy => [
                psbasex + this._sprite_width * xy[0], psbasey + this._sprite_height * xy[1]
            ]),
            right: [[12, 0], [13, 0], [14, 0], [15, 0]].map(xy => [
                psbasex + this._sprite_width * xy[0], psbasey + this._sprite_height * xy[1]
            ]),
        };
    }
    update(delta, ms, opts) {
        if (this.movemode == "idle") {
            switch (opts.moveDir) {
                case "left":
                    this.dir = "left";
                    if (opts.moveCheckCallback(this, this.x - 1, this.y)) {
                        this.movemode = "move-left";
                        this.movems = this.movems == 0 ? this.movestep : this.movems;
                    }
                    else {
                        this.anim = 0;
                        this.movems = 0;
                    }
                    break;
                case "up":
                    this.dir = "up";
                    if (opts.moveCheckCallback(this, this.x, this.y - 1)) {
                        this.movemode = "move-up";
                        this.movems = this.movems == 0 ? this.movestep : this.movems;
                    }
                    else {
                        this.anim = 0;
                        this.movems = 0;
                    }
                    break;
                case "right":
                    this.dir = "right";
                    if (opts.moveCheckCallback(this, this.x + 1, this.y)) {
                        this.movemode = "move-right";
                        this.movems = this.movems == 0 ? this.movestep : this.movems;
                    }
                    else {
                        this.anim = 0;
                        this.movems = 0;
                    }
                    break;
                case "down":
                    this.dir = "down";
                    if (opts.moveCheckCallback(this, this.x, this.y + 1)) {
                        this.movemode = "move-down";
                        this.movems = this.movems == 0 ? this.movestep : this.movems;
                    }
                    else {
                        this.anim = 0;
                        this.movems = 0;
                    }
                    break;
                default:
                    this.movemode = "idle";
                    this.anim = 0;
                    this.movems = 0;
                    return true;
            }
        }
        else if (this.movemode == "move-right") {
            this.movems -= delta;
            this.anim += delta;
            if (this.movems <= 0) {
                this.x += 1;
                this.movemode = "idle";
                this.movems += this.movestep;
            }
            this.offx = 24 * (1 - this.movems / this.movestep);
        }
        else if (this.movemode == "move-left") {
            this.movems -= delta;
            this.anim += delta;
            if (this.movems <= 0) {
                this.x -= 1;
                this.movemode = "idle";
                this.movems += this.movestep;
            }
            this.offx = -24 * (1 - this.movems / this.movestep);
        }
        else if (this.movemode == "move-down") {
            this.movems -= delta;
            this.anim += delta;
            if (this.movems <= 0) {
                this.y += 1;
                this.movemode = "idle";
                this.movems += this.movestep;
            }
            this.offy = 24 * (1 - this.movems / this.movestep);
        }
        else if (this.movemode == "move-up") {
            this.movems -= delta;
            this.anim += delta;
            if (this.movems <= 0) {
                this.y -= 1;
                this.movemode = "idle";
                this.movems += this.movestep;
            }
            this.offy = -24 * (1 - this.movems / this.movestep);
        }
        if (this.anim >= this.animstep * 4) {
            this.anim -= this.animstep * 4;
        }
    }
    getAnimFrame() {
        return ~~(((~~this.anim) + this.animstep - 1) / this.animstep) % 4;
    }
}
class Monster {
    constructor(config) {
        this.x = config.x;
        this.y = config.y;
        this.anim = config.anim;
        this.startms = config.startms;
        this.draw = config.draw.bind(this);
        this.update = config.update.bind(this);
    }
}
class FadeOut {
    constructor(w, h) {
        this.startTime = -1;
        this.started = false;
        this.w = w;
        this.h = h;
    }
    start() {
        this.started = true;
        this.startTime = -1;
        this.rate = 0;
    }
    stop() {
        this.started = false;
        this.startTime = -1;
        this.rate = 0;
    }
    update(ms) {
        if (this.started == false) {
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
    }
    draw() {
        if (this.started) {
            Game.getScreen().fillStyle = `rgba(0,0,0,${this.rate})`;
            Game.getScreen().fillRect(0, 0, this.w, this.h);
        }
    }
}
class FadeIn {
    constructor(w, h) {
        this.startTime = -1;
        this.started = false;
        this.w = w;
        this.h = h;
    }
    start() {
        this.started = true;
        this.startTime = -1;
        this.rate = 0;
    }
    stop() {
        this.started = false;
        this.startTime = -1;
        this.rate = 0;
    }
    update(ms) {
        if (this.started == false) {
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
    }
    draw() {
        if (this.started) {
            Game.getScreen().fillStyle = `rgba(0,0,0,${1 - this.rate})`;
            Game.getScreen().fillRect(0, 0, this.w, this.h);
        }
    }
}
window.onload = () => {
    function waitTimeout(param) {
        var startTime = -1;
        return (delta, ms) => {
            if (startTime === -1) {
                startTime = ms;
            }
            const elapsed = ms - startTime;
            if (elapsed >= param.timeout) {
                param.onend(elapsed);
            }
            else {
                param.action(elapsed);
            }
        };
    }
    ;
    Game.create({
        title: "TSJQ",
        screen: {
            id: "glcanvas",
            scale: 2,
        },
        scene: {
            title: function* (data) {
                console.log("state start", data);
                // setup 
                var show_click_or_tap = false;
                var fadeOut = new FadeOut(Game.getScreen().width / 2, Game.getScreen().height / 2);
                this.draw = () => {
                    const w = Game.getScreen().width / 2;
                    const h = Game.getScreen().height / 2;
                    Game.getScreen().save();
                    Game.getScreen().scale(2, 2);
                    Game.getScreen().clearRect(0, 0, w, h);
                    Game.getScreen().fillStyle = "rgb(255,255,255)";
                    Game.getScreen().fillRect(0, 0, w, h);
                    Game.getScreen().drawImage(Game.getScreen().texture("title"), 0, 0, 192, 72, w / 2 - 192 / 2, 50, 192, 72);
                    if (show_click_or_tap) {
                        Game.getScreen().drawImage(Game.getScreen().texture("title"), 0, 72, 168, 24, w / 2 - 168 / 2, h - 50, 168, 24);
                    }
                    fadeOut.draw();
                    Game.getScreen().restore();
                };
                {
                    this.update = (delta, ms) => {
                        show_click_or_tap = (~~(ms / 500) % 2) === 0;
                    };
                    var pointerclick = (ev) => {
                        if (Game.getScreen().pagePointContainScreen(ev.pageX, ev.pageY)) {
                            Game.getInput().off("pointerclick", pointerclick);
                            this.next();
                        }
                    };
                    Game.getInput().on("pointerclick", pointerclick);
                    yield;
                    Game.getSound().reqPlayChannel(0);
                    Game.getSound().playChannel();
                }
                {
                    this.update = waitTimeout({
                        timeout: 1000,
                        action: (e) => { show_click_or_tap = (~~(e / 50) % 2) === 0; },
                        onend: (e) => this.next()
                    });
                    yield;
                }
                {
                    fadeOut.start();
                    this.update = waitTimeout({
                        timeout: 500,
                        action: (e) => { fadeOut.update(e); show_click_or_tap = (~~(e / 50) % 2) === 0; },
                        onend: (e) => {
                            Game.getSceneManager().push("classroom");
                            this.next();
                        }
                    });
                    yield;
                }
            },
            classroom: function* (data) {
                var selectedCharactor = -1;
                var selectedCharactorDir = 0;
                var selectedCharactorOffY = 0;
                var fadeIn = new FadeIn(Game.getScreen().width / 2, Game.getScreen().height / 2);
                var fadeOut = new FadeOut(Game.getScreen().width / 2, Game.getScreen().height / 2);
                this.draw = () => {
                    var w = Game.getScreen().width / 2;
                    var h = Game.getScreen().height / 2;
                    Game.getScreen().save();
                    Game.getScreen().scale(2, 2);
                    Game.getScreen().clearRect(0, 0, w, h);
                    Game.getScreen().fillStyle = "rgb(255,255,255)";
                    Game.getScreen().fillRect(0, 0, w, h);
                    // 床
                    for (var y = 0; y < ~~((w + 23) / 24); y++) {
                        for (var x = 0; x < ~~((w + 23) / 24); x++) {
                            Game.getScreen().drawImage(Game.getScreen().texture("mapchip"), 0, 0, 24, 24, x * 24, y * 24, 24, 24);
                        }
                    }
                    // 壁
                    for (var y = 0; y < 2; y++) {
                        for (var x = 0; x < ~~((w + 23) / 24); x++) {
                            Game.getScreen().drawImage(Game.getScreen().texture("mapchip"), 120, 96, 24, 24, x * 24, y * 24 - 23, 24, 24);
                        }
                    }
                    // 黒板
                    Game.getScreen().drawImage(Game.getScreen().texture("mapchip"), 0, 204, 72, 36, 90, -12, 72, 36);
                    // 各キャラと机
                    for (var y = 0; y < 5; y++) {
                        for (var x = 0; x < 6; x++) {
                            var id = y * 6 + x;
                            Game.getScreen().drawImage(Game.getScreen().texture("charactor"), 752 * (id % 2) +
                                ((selectedCharactor != id) ? 0 : (188 * (selectedCharactorDir % 4))), 47 * ~~(id / 2), 47, 47, 12 + x * 36, 24 + y * (48 - 7) - ((selectedCharactor != id) ? 0 : (selectedCharactorOffY)), 47, 47);
                            Game.getScreen().drawImage(Game.getScreen().texture("mapchip"), 72, 180, 24, 24, 24 + x * 36, 48 + y * (48 - 7), 24, 24);
                        }
                    }
                    fadeOut.draw();
                    fadeIn.draw();
                    Game.getScreen().restore();
                };
                {
                    fadeIn.start();
                    Game.getSound().reqPlayChannel(2, true);
                    Game.getSound().playChannel();
                    this.update = waitTimeout({
                        timeout: 500,
                        action: (e) => { fadeIn.update(e); },
                        onend: (e) => {
                            fadeIn.stop();
                            this.next();
                        }
                    });
                    yield;
                }
                var onpointerclick = (ev) => {
                    if (Game.getScreen().pagePointContainScreen(ev.pageX, ev.pageY)) {
                        const pos = Game.getScreen().pagePointToScreenPoint(ev.pageX, ev.pageY);
                        const xx = ~~((pos[0] / 2 - 12) / 36);
                        const yy = ~~((pos[1] / 2 - 24) / (48 - 7));
                        if (0 <= xx && xx < 6 && 0 <= yy && yy < 5) {
                            selectedCharactor = yy * 6 + xx;
                            this.next();
                        }
                    }
                };
                Game.getInput().on("pointerclick", onpointerclick);
                this.update = (delta, ms) => { };
                yield;
                selectedCharactorDir = 0;
                selectedCharactorOffY = 0;
                Game.getInput().off("pointerclick", onpointerclick);
                Game.getSound().reqPlayChannel(0);
                Game.getSound().playChannel();
                this.update = waitTimeout({
                    timeout: 1800,
                    action: (e) => {
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
                    onend: (e) => { this.next(); }
                });
                yield;
                fadeOut.start();
                this.update = waitTimeout({
                    timeout: 500,
                    action: (e) => { fadeOut.update(e); },
                    onend: (e) => { this.next(); }
                });
                yield;
                const player = new Player({
                    charactor: selectedCharactor,
                    x: 0,
                    y: 0,
                });
                Game.getSound().reqStopChannel(2);
                Game.getSound().playChannel();
                Game.getSceneManager().pop();
                Game.getSceneManager().push("dungeon", { player: player, floor: 1 });
            },
            dungeon: function* (param) {
                // マップサイズ算出
                const mapChipW = 30 + param.floor * 3;
                const mapChipH = 30 + param.floor * 3;
                // マップ自動生成
                const mapchipsL1 = new Dungeon.Matrix(mapChipW, mapChipH);
                const dungeon = Dungeon.Generator.create(mapChipW, mapChipH, (x, y, v) => { mapchipsL1.value(x, y, v ? 0 : 1); });
                // 装飾
                for (let y = 1; y < mapChipH; y++) {
                    for (let x = 0; x < mapChipW; x++) {
                        mapchipsL1.value(x, y - 1, mapchipsL1.value(x, y) === 1 && mapchipsL1.value(x, y - 1) === 0
                            ? 2
                            : mapchipsL1.value(x, y - 1));
                    }
                }
                const mapchipsL2 = new Dungeon.Matrix(mapChipW, mapChipH);
                for (let y = 0; y < mapChipH; y++) {
                    for (let x = 0; x < mapChipW; x++) {
                        mapchipsL2.value(x, y, (mapchipsL1.value(x, y) === 0) ? 0 : 1);
                    }
                }
                // 部屋シャッフル
                var rooms = dungeon._rooms.shuffle();
                // 開始位置
                var startPos = rooms[0].getCenter();
                param.player.x = startPos[0];
                param.player.y = startPos[1];
                // 階段位置
                var stairsPos = rooms[1].getCenter();
                mapchipsL1.value(stairsPos[0], stairsPos[1], 10);
                // モンスター配置
                var monsters = rooms.splice(2).map(x => {
                    return new Monster({
                        x: x.getLeft(),
                        y: x.getTop(),
                        anim: 0,
                        startms: -1,
                        update(delta, ms) {
                            if (this.startms == -1) {
                                this.startms = ms;
                            }
                            this.anim = ~~((ms - this.startms) / 160) % 4;
                        },
                        draw(x, y, offx, offy) {
                            const xx = this.x - x;
                            const yy = this.y - y;
                            if (0 <= xx &&
                                xx < Game.getScreen().width / 24 &&
                                0 <= yy &&
                                yy < Game.getScreen().height / 24) {
                                Game.getScreen().drawImage(Game.getScreen().texture("monster"), this.anim * 24, 0, 24, 24, xx * 24 + offx + 12 + this.dx, yy * 24 + offy + 12 + this.dy, 24, 24);
                            }
                        }
                    });
                });
                var map = new Dungeon.DungeonData({
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
                var scale = 2;
                // カメラを更新
                map.update({
                    viewpoint: {
                        x: (param.player.x * 24 + param.player.offx) + param.player._sprite_width / 2,
                        y: (param.player.y * 24 + param.player.offy) + param.player._sprite_height / 2
                    },
                    viewwidth: Game.getScreen().width / scale,
                    viewheight: Game.getScreen().height / scale,
                });
                Game.getSound().reqPlayChannel(1, true);
                Game.getSound().playChannel();
                // assign virtual pad
                var pad = new Game.Input.VirtualStick();
                var pointerdown = (ev) => {
                    if (pad.onpointingstart(ev.pointerId)) {
                        const pos = Game.getScreen().pagePointToScreenPoint(ev.pageX, ev.pageY);
                        pad.x = pos[0];
                        pad.y = pos[1];
                    }
                };
                var pointermove = (ev) => {
                    const pos = Game.getScreen().pagePointToScreenPoint(ev.pageX, ev.pageY);
                    pad.onpointingmove(ev.pointerId, pos[0], pos[1]);
                };
                var pointerup = (ev) => {
                    pad.onpointingend(ev.pointerId);
                };
                var pointerclick = (ev) => {
                    if (Game.getScreen().pagePointContainScreen(ev.pageX, ev.pageY)) {
                        Game.getInput().off("pointerclick", pointerclick);
                        Game.getSceneManager().push("mapview", { map: map, player: param.player });
                    }
                };
                var onPointerHook = () => {
                    Game.getInput().on("pointerdown", pointerdown);
                    Game.getInput().on("pointermove", pointermove);
                    Game.getInput().on("pointerup", pointerup);
                    Game.getInput().on("pointerleave", pointerup);
                    Game.getInput().on("pointerclick", pointerclick);
                };
                var offPointerHook = () => {
                    Game.getInput().off("pointerdown", pointerdown);
                    Game.getInput().off("pointermove", pointermove);
                    Game.getInput().off("pointerup", pointerup);
                    Game.getInput().off("pointerleave", pointerup);
                    Game.getInput().off("pointerclick", pointerclick);
                };
                this.suspend = () => {
                    offPointerHook();
                    Game.getSound().reqStopChannel(1);
                    Game.getSound().playChannel();
                };
                this.resume = () => {
                    onPointerHook();
                    Game.getSound().reqPlayChannel(1, true);
                    Game.getSound().playChannel();
                };
                this.leave = () => {
                    offPointerHook();
                    Game.getSound().reqStopChannel(1);
                    Game.getSound().playChannel();
                };
                var update_lighting = (iswalkable) => {
                    var calc_lighting = (x, y, power, dec, dec2, setted) => {
                        if (0 > x || x >= map.width) {
                            return;
                        }
                        if (0 > y || y >= map.height) {
                            return;
                        }
                        if (power <= map.lighting.value(x, y)) {
                            return;
                        }
                        setted[x + "," + y] = true;
                        map.lighting.value(x, y, Math.max(map.lighting.value(x, y), power));
                        map.visibled.value(x, y, Math.max(map.lighting.value(x, y), map.visibled.value(x, y)));
                        if (!iswalkable(x, y)) {
                            power -= dec2;
                        }
                        else {
                            power -= dec;
                        }
                        calc_lighting(x + 0, y - 1, power, dec, dec2, setted);
                        calc_lighting(x - 1, y + 0, power, dec, dec2, setted);
                        calc_lighting(x + 1, y + 0, power, dec, dec2, setted);
                        calc_lighting(x + 0, y + 1, power, dec, dec2, setted);
                    };
                    map.clearLighting();
                    calc_lighting(param.player.x, param.player.y, 140, 20, 50, {});
                };
                var start_ms = -1;
                var fade_rate = 1;
                var scale = 2;
                this.draw = () => {
                    Game.getScreen().save();
                    Game.getScreen().scale(scale, scale);
                    Game.getScreen().clearRect(0, 0, Game.getScreen().width / 2, Game.getScreen().height / 2);
                    Game.getScreen().fillStyle = "rgb(255,255,255)";
                    Game.getScreen().fillRect(0, 0, Game.getScreen().width / 2, Game.getScreen().height / 2);
                    map.draw((l, cameraLocalPx, cameraLocalPy) => {
                        if (l == 0) {
                            const animf = param.player.getAnimFrame();
                            // 影
                            Game.getScreen().fillStyle = "rgba(0,0,0,0.25)";
                            Game.getScreen().beginPath();
                            Game.getScreen().ellipse(cameraLocalPx, cameraLocalPy + 7, 12, 3, 0, 0, Math.PI * 2);
                            Game.getScreen().fill();
                            // モンスター
                            var camera = map.camera;
                            monsters.forEach((x) => x.draw(camera.chipX, camera.chipY, camera.chipOffX, camera.chipOffY));
                            // キャラクター
                            Game.getScreen().drawImage(Game.getScreen().texture("charactor"), param.player._sprite[param.player.dir][animf][0], param.player._sprite[param.player.dir][animf][1], param.player._sprite_width, param.player._sprite_height, cameraLocalPx - param.player._sprite_width / 2, cameraLocalPy - param.player._sprite_width / 2 - 12, param.player._sprite_width, param.player._sprite_height);
                        }
                    });
                    // フェード
                    if (fade_rate > 0) {
                        Game.getScreen().fillStyle = `rgba(0,0,0,${fade_rate})`;
                        Game.getScreen().fillRect(0, 0, Game.getScreen().width / scale, Game.getScreen().height / scale);
                    }
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
                this.update = (delta, ms) => {
                    if (start_ms === -1) {
                        start_ms = ms;
                    }
                    update_lighting((x, y) => ((map.layer[0].chips.value(x, y) === 1) ||
                        (map.layer[0].chips.value(x, y) === 10)));
                    const elapsed = ms - start_ms;
                    if (elapsed <= 500) {
                        fade_rate = 1 - elapsed / 500;
                    }
                    else {
                        this.next();
                    }
                };
                yield;
                onPointerHook();
                fade_rate = 0;
                start_ms = -1;
                // キャラクターキュー
                var matrix_move = new Dungeon.Matrix(map.width, map.height, 0);
                var move_queue = [];
                move_queue.push({
                    owner: param.player,
                    movedir: "",
                    get_input() {
                        if (pad.isTouching && pad.distance > 0.4) {
                            const dirToAngle = { 0: "left", 1: "up", 2: "right", 3: "down" };
                            const xy = [[-1, 0], [0, -1], [1, 0], [0, 1]];
                            var d = ~~((pad.angle + 180 + 45) / 90) % 4;
                            this.movedir = dirToAngle[d];
                            matrix_move.fill(0);
                            matrix_move.value(param.player.x + xy[d][0], param.player.y + xy[d][1], 1);
                        }
                        else {
                            this.movedir = "";
                        }
                    },
                    moving(delta, ms) {
                        param.player.update(delta, ms, {
                            moveDir: this.movedir,
                            moveCheckCallback: (p, x, y) => (map.layer[0].chips.value(x, y) == 1) ||
                                (map.layer[0].chips.value(x, y) == 10)
                        });
                        return (param.player.movemode == "idle");
                    },
                    update(delta, ms) {
                        param.player.update(delta, ms, {
                            moveDir: "idle",
                            moveCheckCallback: (p, x, y) => (map.layer[0].chips.value(x, y) == 1) ||
                                (map.layer[0].chips.value(x, y) == 10)
                        });
                    },
                });
                monsters.forEach((monster) => {
                    move_queue.push({
                        owner: monster,
                        start: -1,
                        movedir: "",
                        get_input() {
                            this.start = -1;
                            var s = [["left", -1, 0], ["up", 0, -1], ["right", 1, 0], ["down", 0, 1], ["idle", 0, 0]].filter(x => map.layer[0].chips.value(monster.x + x[1], monster.y + x[2]) == 1 && matrix_move.value(monster.x + x[1], monster.y + x[2]) != 1).shuffle()[0];
                            matrix_move.value(monster.x + s[1], monster.y + s[2], 1);
                            this.movedir = s[0];
                        },
                        moving(delta, ms) {
                            if (this.start == -1) {
                                this.start = ms;
                            }
                            var e = ms - this.start;
                            var dx = 0;
                            var dy = 0;
                            switch (this.movedir) {
                                case "left":
                                    dx = -1;
                                    break;
                                case "up":
                                    dy = -1;
                                    break;
                                case "right":
                                    dx = 1;
                                    break;
                                case "down":
                                    dy = 1;
                                    break;
                            }
                            if (e >= 250) {
                                monster.x += dx;
                                monster.y += dy;
                                monster.dx = 0;
                                monster.dy = 0;
                                monster.update(delta, ms);
                                return true;
                            }
                            else {
                                monster.dx = dx * 24 * e / 250;
                                monster.dy = dy * 24 * e / 250;
                                monster.update(delta, ms);
                                return false;
                            }
                        },
                        update(delta, ms, dir) {
                            monster.update(delta, ms);
                        },
                    });
                });
                var move_queue_state = "wait_input";
                var move_dir = "";
                this.update = (delta, ms) => {
                    /*
                                        // プレイヤーを更新
                                        let dir = "idle";
                                        if (pad.isTouching && pad.distance > 0.4) {
                                            const dirToAngle = { 0: "left", 1: "up", 2: "right", 3: "down" };
                                            dir = dirToAngle[~~((pad.angle + 180 + 45) / 90) % 4];
                                        }
                    
                                        param.player.update(delta,
                                            ms,
                                            {
                                                moveDir: dir,
                                                moveCheckCallback: (p, x, y) => (map.layer[0].chips.value(x, y) == 1) ||
                                                    (map.layer[0].chips.value(x, y) == 10)
                                            });
                    
                                        // モンスターを更新
                                        monsters.forEach((x) => x.update(delta, ms));
                    */
                    switch (move_queue_state) {
                        case "wait_input":
                            move_queue[0].get_input.call(move_queue[0]);
                            if (move_queue[0].movedir != "") {
                                for (var i = 1; i < move_queue.length; i++) {
                                    move_queue[i].get_input.call(move_queue[i]);
                                }
                                move_queue_state = "moving";
                            }
                            else {
                                for (var i = 0; i < move_queue.length; i++) {
                                    move_queue[i].update.call(move_queue[i], delta, ms);
                                }
                            }
                            break;
                        case "moving":
                            console.log("--");
                            for (var i = 0; i < move_queue.length; i++) {
                                if (move_queue[i].movedir != "") {
                                    if (move_queue[i].moving.call(move_queue[i], delta, ms)) {
                                        move_queue[i].movedir = "";
                                        console.log(i, "finish");
                                    }
                                    console.log(i, "move");
                                }
                                else {
                                    move_queue[i].update.call(move_queue[i], delta, ms);
                                    console.log(i, "idle");
                                }
                            }
                            if (move_queue.every(x => x.movedir == "")) {
                                move_queue_state = "wait_input";
                                // 現在位置のマップチップを取得
                                const chip = map.layer[0].chips.value(~~param.player.x, ~~param.player.y);
                                if (chip === 10) {
                                    // 階段なので次の階層に移動させる。
                                    this.next();
                                }
                                // プレイヤー位置のモンスターを破壊
                                monsters = monsters.filter((monster) => {
                                    if ((monster.x === param.player.x) && (monster.y === param.player.y)) {
                                        consolere.log(param.player.x, param.player.y, monster.x, monster.y);
                                        var n = move_queue.findIndex((x) => x.owner == monster);
                                        move_queue.splice(n, 1);
                                        return false;
                                    }
                                    else {
                                        return true;
                                    }
                                });
                            }
                            else {
                                for (var i = 1; i < move_queue.length; i++) {
                                    move_queue[i].update.call(move_queue[i], delta, ms);
                                }
                            }
                            break;
                    }
                    const scale = 2;
                    // カメラを更新
                    map.update({
                        viewpoint: {
                            x: (param.player.x * 24 + param.player.offx) +
                                param.player._sprite_width / 2,
                            y: (param.player.y * 24 + param.player.offy) +
                                param.player._sprite_height / 2
                        },
                        viewwidth: Game.getScreen().width / scale,
                        viewheight: Game.getScreen().height / scale,
                    });
                    update_lighting((x, y) => (map.layer[0].chips.value(x, y) === 1) ||
                        (map.layer[0].chips.value(x, y) === 10));
                };
                yield;
                Game.getSound().reqPlayChannel(3);
                Game.getSound().playChannel();
                start_ms = -1;
                this.update = (delta, ms) => {
                    const scale = 2;
                    if (start_ms === -1) {
                        start_ms = ms;
                    }
                    const elapsed = ms - start_ms;
                    update_lighting((x, y) => (map.layer[0].chips.value(x, y) === 1) ||
                        (map.layer[0].chips.value(x, y) === 10));
                    if (elapsed <= 500) {
                        fade_rate = (elapsed / 500);
                    }
                    if (elapsed >= 1000) {
                        param.floor++;
                        this.next();
                    }
                };
                yield;
                Game.getSceneManager().pop();
                Game.getSceneManager().push("dungeon", param);
            },
            mapview: function* (data) {
                var pointerclick = (ev) => {
                    if (Game.getScreen().pagePointContainScreen(ev.pageX, ev.pageY)) {
                        this.next();
                    }
                };
                this.draw = () => {
                    Game.getScreen().save();
                    Game.getScreen().clearRect(0, 0, Game.getScreen().width, Game.getScreen().height);
                    Game.getScreen().fillStyle = "rgb(0,0,0)";
                    Game.getScreen().fillRect(0, 0, Game.getScreen().width, Game.getScreen().height);
                    var offx = ~~((Game.getScreen().width - data.map.width * 5) / 2);
                    var offy = ~~((Game.getScreen().height - data.map.height * 5) / 2);
                    // ミニマップを描画
                    for (var y = 0; y < data.map.height; y++) {
                        for (var x = 0; x < data.map.width; x++) {
                            var chip = data.map.layer[0].chips.value(x, y);
                            var color = "rgb(52,12,0)";
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
                            var light = 1 - data.map.visibled.value(x, y) / 100;
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
                Game.getInput().on("pointerclick", pointerclick);
                this.update = (delta, ms) => { };
                yield;
                Game.getInput().off("pointerclick", pointerclick);
                Game.getSceneManager().pop();
            },
        }
    }).then(() => {
        var anim = 0;
        var update = (ms) => {
            Game.getScreen().save();
            Game.getScreen().clearRect(0, 0, Game.getScreen().width, Game.getScreen().height);
            Game.getScreen().fillStyle = "rgb(255,255,255)";
            Game.getScreen().fillRect(0, 0, Game.getScreen().width, Game.getScreen().height);
            var n = ~(ms / 200);
            Game.getScreen().translate(Game.getScreen().width / 2, Game.getScreen().height / 2);
            Game.getScreen().rotate(n * Math.PI / 4);
            for (let i = 0; i < 8; i++) {
                const g = (i * 32);
                Game.getScreen().save();
                Game.getScreen().rotate(i * Math.PI / 4);
                Game.getScreen().fillStyle = `rgb(${g},${g},${g})`;
                Game.getScreen().fillRect(-10, -100, 20, 50);
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
            Game.getSceneManager().update(delta, now);
            Game.getSceneManager().draw();
        });
        Game.getTimer().start();
    });
};
//# sourceMappingURL=tsjq.js.map