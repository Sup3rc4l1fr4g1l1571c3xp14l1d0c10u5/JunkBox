"use strict";
var consolere;
class EventDispatcher {
    constructor() {
        this._listeners = {};
    }
    on(eventName, listener) {
        if (this._listeners[eventName] === undefined) {
            this._listeners[eventName] = [];
        }
        this._listeners[eventName].push(listener);
        return this;
    }
    off(eventName, listener) {
        var listeners = this._listeners[eventName];
        var index = listeners.indexOf(listener);
        if (index != -1) {
            listeners.splice(index, 1);
        }
        return this;
    }
    fire(eventName, ...args) {
        var listeners = this._listeners[eventName];
        if (listeners) {
            var temp = listeners.slice();
            for (var i = 0, len = temp.length; i < len; ++i) {
                temp[i].apply(this, args);
            }
        }
        return this;
    }
    one(eventName, listener) {
        var func = (...args) => {
            var result = listener.apply(this, args);
            this.off(eventName, func);
            return result;
        };
        this.on(eventName, func);
        return this;
    }
    hasEventListener(eventName) {
        if (this._listeners[eventName] === undefined && !this["on" + eventName]) {
            return false;
        }
        else {
            return true;
        }
    }
    clearEventListener(type) {
        var oldEventName = 'on' + type;
        if (this[oldEventName]) {
            delete this[oldEventName];
        }
        this._listeners[type] = [];
        return this;
    }
}
var Game;
(function (Game) {
    consolere.log('remote log start');
    // Global Variables
    var canvas = null;
    var gl = null;
    var screen = null;
    var textures = {};
    var sceneManager = null;
    var input = null;
    var timer = null;
    var sound = null;
    class Texture {
        constructor(image) {
            this.image = image;
        }
        width() {
            return this.image.width;
        }
        height() {
            return this.image.height;
        }
    }
    function loadTextures(asserts) {
        return Promise.all(Object.keys(asserts).map((x) => new Promise((resolve, reject) => {
            let img = new Image();
            img.onload = () => { textures[x] = new Texture(img); resolve(); };
            img.src = asserts[x];
        }))).then((x) => {
            return true;
        });
    }
    Game.loadTextures = loadTextures;
    class Screen {
        width() {
            return gl.canvas.width;
        }
        height() {
            return gl.canvas.height;
        }
        context() {
            return gl;
        }
        texture(id) {
            return textures[id].image;
        }
        PagePosToScreenPos(x, y) {
            var cr = canvas.getBoundingClientRect();
            var scaleScreen = window.innerWidth / document.body.clientWidth;
            var scaleCanvas = canvas.width * 1.0 / cr.width;
            var sx = (x - (cr.left + window.pageXOffset));
            var sy = (y - (cr.top + window.pageYOffset));
            return [sx, sy];
        }
        PagePosContainScreen(x, y) {
            var pos = this.PagePosToScreenPos(x, y);
            return 0 <= pos[0] && pos[0] < Game.getScreen().width() && 0 <= pos[1] && pos[1] < Game.getScreen().height();
        }
    }
    class AnimationTimer {
        constructor() {
            this.id = NaN;
            this.prevTime = NaN;
            this.listenerId = 0;
            this.listeners = {};
        }
        start() {
            if (!isNaN(this.id)) {
                stop();
            }
            this.id = requestAnimationFrame(this.tick.bind(this));
            return !isNaN(this.id);
        }
        stop() {
            if (!isNaN(this.id)) {
                cancelAnimationFrame(this.id);
                this.id = NaN;
            }
        }
        tick(ts) {
            requestAnimationFrame(this.tick.bind(this));
            if (!isNaN(this.prevTime)) {
                var delta = ts - this.prevTime;
                for (var key in this.listeners) {
                    if (this.listeners.hasOwnProperty(key)) {
                        this.listeners[key].call(this.listeners[key], delta, ts, key);
                    }
                }
            }
            this.prevTime = ts;
        }
        on(listener) {
            var id = this.listenerId;
            while (this.listeners[id] != null) {
                id++;
            }
            this.listenerId = id + 1;
            this.listeners[id] = listener;
            return id;
        }
        off(id) {
            if (this.listeners[id] != null) {
                delete this.listeners[id];
                return true;
            }
            else {
                return false;
            }
        }
    }
    class Scene {
        constructor(manager, obj = {}) {
            Object.assign(this, dup(obj));
            this.manager = manager;
        }
        push(id, param = {}) { this.manager.push(id, param); }
        pop() { this.manager.pop(); }
        // virtual methods
        enter(data) { }
        update(delta, now) { }
        draw() { }
        leave() { }
        suspend() { }
        resume() { }
    }
    class SceneManager {
        constructor(scenes = {}) {
            this.scenes = dup(scenes);
            this.sceneStack = [];
        }
        push(id, param = {}) {
            var sceneDef = this.scenes[id];
            if (sceneDef === undefined) {
                throw new Error("scene " + id + " is not defined.");
            }
            if (this.peek() != null) {
                this.peek().suspend();
            }
            this.sceneStack.push(new Scene(this, sceneDef));
            this.peek().enter(param);
            return this;
        }
        pop() {
            if (this.sceneStack.length == 0) {
                throw new Error("there is no scene.");
            }
            this.sceneStack.pop().leave();
            if (this.peek() != null) {
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
            if (this.sceneStack.length == 0) {
                throw new Error("there is no scene.");
            }
            this.peek().update.apply(this.peek(), args);
            return this;
        }
        draw() {
            if (this.sceneStack.length == 0) {
                throw new Error("there is no scene.");
            }
            this.peek().draw.apply(this.peek());
            return this;
        }
    }
    class Input extends EventDispatcher {
        constructor() {
            super();
            this._isScrolling = false;
            this._timeout = 0;
            this._sDistX = 0;
            this._sDistY = 0;
            this._maybeClick = false;
            this._maybeClickX = 0;
            this._maybeClickY = 0;
            this._prevTimeStamp = 0;
            this._prevInputType = "none";
            window.addEventListener('scroll', () => {
                if (!this._isScrolling) {
                    this._sDistX = window.pageXOffset;
                    this._sDistY = window.pageYOffset;
                }
                this._isScrolling = true;
                clearTimeout(this._timeout);
                this._timeout = setTimeout(() => {
                    this._isScrolling = false;
                    this._sDistX = 0;
                    this._sDistY = 0;
                }, 100);
            });
            // add event listener to body
            document.onselectstart = () => false;
            if (document.body['pointermove']) {
                consolere.log('pointer event is implemented');
                document.body.addEventListener('touchmove', function (evt) { evt.preventDefault(); }, false);
                document.body.addEventListener('touchdown', function (evt) { evt.preventDefault(); }, false);
                document.body.addEventListener('touchup', function (evt) { evt.preventDefault(); }, false);
                document.body.addEventListener('mousemove', function (evt) { evt.preventDefault(); }, false);
                document.body.addEventListener('mousedown', function (evt) { evt.preventDefault(); }, false);
                document.body.addEventListener('mouseup', function (evt) { evt.preventDefault(); }, false);
                document.body.addEventListener("pointerdown", (ev) => this.fire("pointerdown", ev));
                document.body.addEventListener("pointermove", (ev) => this.fire("pointermove", ev));
                document.body.addEventListener("pointerup", (ev) => this.fire("pointerup", ev));
                document.body.addEventListener("pointerleave", (ev) => this.fire("pointerleave", ev));
            }
            else {
                consolere.log('pointer event is not implemented');
                document.body.addEventListener('mousedown', this._pointerDown.bind(this), false);
                document.body.addEventListener('touchstart', this._pointerDown.bind(this), false);
                document.body.addEventListener('mouseup', this._pointerUp.bind(this), false);
                document.body.addEventListener('touchend', this._pointerUp.bind(this), false);
                document.body.addEventListener('mousemove', this._pointerMove.bind(this), false);
                document.body.addEventListener('touchmove', this._pointerMove.bind(this), false);
                document.body.addEventListener('mouseleave', this._pointerLeave.bind(this), false);
                document.body.addEventListener('touchleave', this._pointerLeave.bind(this), false);
                document.body.addEventListener('touchcancel', this._pointerUp.bind(this), false);
            }
        }
        _checkEvent(e) {
            e.preventDefault();
            var istouch = e.type.indexOf('touch') === 0;
            var ismouse = e.type.indexOf('mouse') === 0;
            if (istouch && this._prevInputType != 'touch') {
                if (e.timeStamp - this._prevTimeStamp >= 500) {
                    this._prevInputType = 'touch';
                    this._prevTimeStamp = e.timeStamp;
                    return true;
                }
                else {
                    return false;
                }
            }
            else if (ismouse && this._prevInputType != 'mouse') {
                if (e.timeStamp - this._prevTimeStamp >= 500) {
                    this._prevInputType = 'mouse';
                    this._prevTimeStamp = e.timeStamp;
                    return true;
                }
                else {
                    return false;
                }
            }
            else {
                this._prevInputType = istouch ? 'touch' : ismouse ? 'mouse' : 'none';
                this._prevTimeStamp = e.timeStamp;
                return istouch || ismouse;
            }
        }
        _pointerDown(e) {
            if (this._checkEvent(e)) {
                var evt = this._makePointerEvent('down', e);
                var singleFinger = evt['mouse'] || (evt['touch'] && e.touches.length === 1);
                if (!this._isScrolling && singleFinger) {
                    this._maybeClick = true;
                    this._maybeClickX = evt.pageX;
                    this._maybeClickY = evt.pageY;
                }
            }
            return false;
        }
        _pointerLeave(e) {
            if (this._checkEvent(e)) {
                this._maybeClick = false;
                this._makePointerEvent('leave', e);
            }
            return false;
        }
        _pointerMove(e) {
            if (this._checkEvent(e)) {
                var evt = this._makePointerEvent('move', e);
            }
            return false;
        }
        _pointerUp(e) {
            if (this._checkEvent(e)) {
                var evt = this._makePointerEvent('up', e);
                if (this._maybeClick) {
                    if (Math.abs(this._maybeClickX - evt.pageX) < 5 && Math.abs(this._maybeClickY - evt.pageY) < 5) {
                        if (!this._isScrolling || (Math.abs(this._sDistX - window.pageXOffset) < 5 && Math.abs(this._sDistY - window.pageYOffset) < 5)) {
                            this._makePointerEvent('click', e);
                        }
                    }
                }
                this._maybeClick = false;
            }
            return false;
        }
        _makePointerEvent(type, e) {
            var evt = document.createEvent('CustomEvent');
            var eventType = 'pointer' + type;
            evt.initCustomEvent(eventType, true, true, {});
            evt.touch = e.type.indexOf('touch') === 0;
            evt.mouse = e.type.indexOf('mouse') === 0;
            if (evt.touch) {
                evt.pointerId = e.changedTouches[0].identifier;
                evt.pageX = e.changedTouches[0].pageX;
                evt.pageY = e.changedTouches[0].pageY;
            }
            if (evt.mouse) {
                evt.pointerId = 0;
                evt.pageX = e.clientX + window.pageXOffset;
                evt.pageY = e.clientY + window.pageYOffset;
            }
            evt.maskedEvent = e;
            this.fire(eventType, evt);
            return evt;
        }
    }
    //
    function Create(config) {
        return new Promise((resolve, reject) => {
            document.title = config.title;
            canvas = document.getElementById(config.screen.id);
            if (!canvas) {
                throw new Error("your browser is not support canvas.");
            }
            gl = canvas.getContext("2d");
            if (!gl) {
                throw new Error("your browser is not support CanvasRenderingContext2D.");
            }
            gl.mozImageSmoothingEnabled = false;
            gl.imageSmoothingEnabled = false;
            gl.webkitImageSmoothingEnabled = false;
            screen = new Screen();
            sceneManager = new SceneManager(config.scene);
            timer = new AnimationTimer();
            input = new Input();
            sound = new Sound();
            resolve();
        });
    }
    Game.Create = Create;
    ;
    function getScreen() {
        return screen;
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
    function shuffle(array) {
        for (var i = array.length - 1; i > 0; i--) {
            var r = Math.floor(Math.random() * (i + 1));
            var tmp = array[i];
            array[i] = array[r];
            array[r] = tmp;
        }
        return array;
    }
    Game.shuffle = shuffle;
    function dup(data) {
        function getDataType(data) { return Object.prototype.toString.call(data).slice(8, -1); }
        function isCyclic(data) {
            let seenObjects = [];
            function detect(data) {
                if (data && getDataType(data) === "Object") {
                    if (seenObjects.indexOf(data) !== -1) {
                        return true;
                    }
                    seenObjects.push(data);
                    return Object.keys(data).some((key) => data.hasOwnProperty(key) === true && detect(data[key]));
                }
                return false;
            }
            return detect(data);
        }
        if (data === null || data === undefined) {
            return undefined;
        }
        const dataType = getDataType(data);
        if (dataType === "Date") {
            let clonedDate = new Date();
            clonedDate.setTime(data.getTime());
            return clonedDate;
        }
        else if (dataType === "Object") {
            if (isCyclic(data) === true) {
                return data;
            }
            return Object.keys(data).reduce((s, key) => { s[key] = dup(data[key]); return s; }, {});
        }
        else if (dataType === "Array") {
            return data.map(dup);
        }
        else {
            return data;
        }
    }
    Game.dup = dup;
})(Game || (Game = {}));
class Pad {
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
        var dx = x - this.x;
        var dy = y - this.y;
        return ((dx * dx) + (dy * dy)) <= this.radius * this.radius;
    }
    onpointingstart(id) {
        if (this.id != -1) {
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
        if (this.id != id) {
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
        if (this.isTouching == false) {
            return false;
        }
        if (id != this.id) {
            return false;
        }
        var dx = x - this.x;
        var dy = y - this.y;
        var len = Math.sqrt((dx * dx) + (dy * dy));
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
class Camera {
    constructor() {
    }
}
class MapData {
    constructor(config) {
        Object.assign(this, config);
        this.camera = new Camera();
        this.lighting = [];
        for (var y = 0; y < this.height; y++) {
            this.lighting[y] = [];
            for (var x = 0; x < this.width; x++) {
                this.lighting[y][x] = 0;
            }
        }
        this.visibled = [];
        for (var y = 0; y < this.height; y++) {
            this.visibled[y] = [];
            for (var x = 0; x < this.width; x++) {
                this.visibled[y][x] = 0;
            }
        }
    }
    clear_lighting() {
        for (var y = 0; y < this.height; y++) {
            for (var x = 0; x < this.width; x++) {
                this.lighting[y][x] = 0;
            }
        }
    }
    // update camera
    update(param) {
        var map_width = this.width * this.gridsize.width;
        var map_height = this.height * this.gridsize.height;
        // マップ上でのカメラの注視点
        var map_px = param.viewpoint.x;
        var map_py = param.viewpoint.y;
        // カメラの視野の幅・高さ
        this.camera.width = param.viewwidth;
        this.camera.height = param.viewheight;
        // カメラの注視点が中心となるようなカメラの視野
        this.camera.left = ~~(map_px - this.camera.width / 2);
        this.camera.top = ~~(map_py - this.camera.height / 2);
        this.camera.right = this.camera.left + this.camera.width;
        this.camera.bottom = this.camera.top + this.camera.height;
        // 視野をマップ内に補正
        if ((this.camera.left < 0) && (this.camera.right - this.camera.left < map_width)) {
            this.camera.right -= this.camera.left;
            this.camera.left = 0;
        }
        else if ((this.camera.right >= map_width) && (this.camera.left - (this.camera.right - map_width) >= 0)) {
            this.camera.left -= (this.camera.right - map_width);
            this.camera.right = map_width - 1;
        }
        if ((this.camera.top < 0) && (this.camera.bottom - this.camera.top < map_height)) {
            this.camera.bottom -= this.camera.top;
            this.camera.top = 0;
        }
        else if ((this.camera.bottom >= map_height) && (this.camera.top - (this.camera.bottom - map_height) >= 0)) {
            this.camera.top -= (this.camera.bottom - map_height);
            this.camera.bottom = map_height - 1;
        }
        // 視野の左上位置を原点とした注視点を算出
        this.camera.local_px = map_px - this.camera.left;
        this.camera.local_py = map_py - this.camera.top;
        // 視野の左上位置に対応するマップチップ座標を算出
        this.camera.chip_x = ~~(this.camera.left / this.gridsize.width);
        this.camera.chip_y = ~~(this.camera.top / this.gridsize.height);
        // 視野の左上位置をにマップチップをおいた場合のスクロールによるズレ量を算出
        this.camera.chip_offx = -(this.camera.left % this.gridsize.width);
        this.camera.chip_offy = -(this.camera.top % this.gridsize.height);
    }
    draw(layerDrawHook) {
        // 描画開始
        var gridw = this.gridsize.width;
        var gridh = this.gridsize.height;
        var yy = ~~(this.camera.height / gridh + 1);
        var xx = ~~(this.camera.width / gridw + 1);
        Object.keys(this.layer).forEach((key) => {
            var l = ~~key;
            for (var y = -1; y < yy; y++) {
                for (var x = -1; x < xx; x++) {
                    var chipid = (this.layer[l].chips[y + this.camera.chip_y] != null && this.layer[l].chips[y + this.camera.chip_y][x + this.camera.chip_x] != null) ? this.layer[l].chips[y + this.camera.chip_y][x + this.camera.chip_x] : 0;
                    if (this.layer[l].chip[chipid]) {
                        var uv = [];
                        Game.getScreen().context().drawImage(Game.getScreen().texture('mapchip'), this.layer[l].chip[chipid].x, this.layer[l].chip[chipid].y, gridw, gridh, 0 + x * gridw + this.camera.chip_offx + gridw / 2, 0 + y * gridh + this.camera.chip_offy + gridh / 2, gridw, gridh);
                    }
                }
            }
            // レイヤー描画フック
            layerDrawHook(l, this.camera.local_px, this.camera.local_py);
        });
        // 照明描画
        for (var y = -1; y < yy; y++) {
            for (var x = -1; x < xx; x++) {
                var light = 0;
                if (this.lighting[y + this.camera.chip_y] != null && this.lighting[y + this.camera.chip_y][x + this.camera.chip_x] != null) {
                    light = this.lighting[y + this.camera.chip_y][x + this.camera.chip_x] / 100;
                    if (light > 1) {
                        light = 1;
                    }
                    else if (light < 0) {
                        light = 0;
                    }
                }
                Game.getScreen().context().fillStyle = "rgba(0,0,0," + (1 - light) + ")";
                Game.getScreen().context().fillRect(0 + x * gridw + this.camera.chip_offx + gridw / 2, 0 + y * gridh + this.camera.chip_offy + gridh / 2, gridw, gridh);
            }
        }
    }
}
var DungeonGenerator;
(function (DungeonGenerator) {
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
    function getUniform() {
        return Math.random();
    }
    function getUniformInt(lowerBound, upperBound) {
        var max = Math.max(lowerBound, upperBound);
        var min = Math.min(lowerBound, upperBound);
        return Math.floor(getUniform() * (max - min + 1)) + min;
    }
    function getWeightedValue(data) {
        var total = 0;
        for (var id in data) {
            total += data[id];
        }
        var random = getUniform() * total;
        var part = 0;
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
            var min = options.roomWidth[0];
            var max = options.roomWidth[1];
            var width = getUniformInt(min, max);
            var min = options.roomHeight[0];
            var max = options.roomHeight[1];
            var height = getUniformInt(min, max);
            if (dx == 1) {
                var y2 = y - Math.floor(getUniform() * height);
                return new Room(x + 1, y2, x + width, y2 + height - 1, x, y);
            }
            if (dx == -1) {
                var y2 = y - Math.floor(getUniform() * height);
                return new Room(x - width, y2, x - 1, y2 + height - 1, x, y);
            }
            if (dy == 1) {
                var x2 = x - Math.floor(getUniform() * width);
                return new Room(x2, y + 1, x2 + width - 1, y + height, x, y);
            }
            if (dy == -1) {
                var x2 = x - Math.floor(getUniform() * width);
                return new Room(x2, y - height, x2 + width - 1, y - 1, x, y);
            }
            throw new Error("dx or dy must be 1 or -1");
        }
        ;
        static createRandomCenter(cx, cy, options) {
            var min = options.roomWidth[0];
            var max = options.roomWidth[1];
            var width = getUniformInt(min, max);
            var min = options.roomHeight[0];
            var max = options.roomHeight[1];
            var height = getUniformInt(min, max);
            var x1 = cx - Math.floor(getUniform() * width);
            var y1 = cy - Math.floor(getUniform() * height);
            var x2 = x1 + width - 1;
            var y2 = y1 + height - 1;
            return new Room(x1, y1, x2, y2);
        }
        ;
        static createRandom(availWidth, availHeight, options) {
            var min = options.roomWidth[0];
            var max = options.roomWidth[1];
            var width = getUniformInt(min, max);
            var min = options.roomHeight[0];
            var max = options.roomHeight[1];
            var height = getUniformInt(min, max);
            var left = availWidth - width - 1;
            var top = availHeight - height - 1;
            var x1 = 1 + Math.floor(getUniform() * left);
            var y1 = 1 + Math.floor(getUniform() * top);
            var x2 = x1 + width - 1;
            var y2 = y1 + height - 1;
            return new Room(x1, y1, x2, y2);
        }
        ;
        addDoor(x, y) {
            this._doors[x + "," + y] = 1;
            return this;
        }
        ;
        getDoors(callback) {
            for (var key in this._doors) {
                var parts = key.split(",");
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
            var left = this._x1 - 1;
            var right = this._x2 + 1;
            var top = this._y1 - 1;
            var bottom = this._y2 + 1;
            for (var x = left; x <= right; x++) {
                for (var y = top; y <= bottom; y++) {
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
            var left = this._x1 - 1;
            var right = this._x2 + 1;
            var top = this._y1 - 1;
            var bottom = this._y2 + 1;
            for (var x = left; x <= right; x++) {
                for (var y = top; y <= bottom; y++) {
                    if (x == left || x == right || y == top || y == bottom) {
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
            var left = this._x1 - 1;
            var right = this._x2 + 1;
            var top = this._y1 - 1;
            var bottom = this._y2 + 1;
            var value = 0;
            for (var x = left; x <= right; x++) {
                for (var y = top; y <= bottom; y++) {
                    if (x + "," + y in this._doors) {
                        value = 2;
                    }
                    else if (x == left || x == right || y == top || y == bottom) {
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
        ;
        static createRandomAt(x, y, dx, dy, options) {
            var min = options.corridorLength[0];
            var max = options.corridorLength[1];
            var length = getUniformInt(min, max);
            return new Corridor(x, y, x + dx * length, y + dy * length);
        }
        ;
        debug() {
            consolere.log("corridor", this._startX, this._startY, this._endX, this._endY);
        }
        ;
        isValid(isWallCallback, canBeDugCallback) {
            var sx = this._startX;
            var sy = this._startY;
            var dx = this._endX - sx;
            var dy = this._endY - sy;
            var length = 1 + Math.max(Math.abs(dx), Math.abs(dy));
            if (dx) {
                dx = dx / Math.abs(dx);
            }
            if (dy) {
                dy = dy / Math.abs(dy);
            }
            var nx = dy;
            var ny = -dx;
            var ok = true;
            for (var i = 0; i < length; i++) {
                var x = sx + i * dx;
                var y = sy + i * dy;
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
            if (length == 0) {
                return false;
            }
            /* length 1 allowed only if the next space is empty */
            if (length == 1 && isWallCallback(this._endX + dx, this._endY + dy)) {
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
            var firstCornerBad = !isWallCallback(this._endX + dx + nx, this._endY + dy + ny);
            var secondCornerBad = !isWallCallback(this._endX + dx - nx, this._endY + dy - ny);
            this._endsWithAWall = isWallCallback(this._endX + dx, this._endY + dy);
            if ((firstCornerBad || secondCornerBad) && this._endsWithAWall) {
                return false;
            }
            return true;
        }
        ;
        create(digCallback) {
            var sx = this._startX;
            var sy = this._startY;
            var dx = this._endX - sx;
            var dy = this._endY - sy;
            var length = 1 + Math.max(Math.abs(dx), Math.abs(dy));
            if (dx) {
                dx = dx / Math.abs(dx);
            }
            if (dy) {
                dy = dy / Math.abs(dy);
            }
            var nx = dy;
            var ny = -dx;
            for (var i = 0; i < length; i++) {
                var x = sx + i * dx;
                var y = sy + i * dy;
                digCallback(x, y, 0);
            }
            return true;
        }
        ;
        createPriorityWalls(priorityWallCallback) {
            if (!this._endsWithAWall) {
                return;
            }
            var sx = this._startX;
            var sy = this._startY;
            var dx = this._endX - sx;
            var dy = this._endY - sy;
            if (dx) {
                dx = dx / Math.abs(dx);
            }
            if (dy) {
                dy = dy / Math.abs(dy);
            }
            var nx = dy;
            var ny = -dx;
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
        ;
        create(callback) {
            this._rooms = [];
            this._corridors = [];
            this._map = this._fillMap(1);
            this._walls = {};
            this._dug = 0;
            var area = (this._width - 2) * (this._height - 2);
            this._firstRoom();
            var t1 = Date.now();
            do {
                var t2 = Date.now();
                if (t2 - t1 > this._options.timeLimit) {
                    break;
                }
                /* find a good wall */
                var wall = this._findWall();
                if (!wall) {
                    break;
                } /* no more walls */
                var parts = wall.split(",");
                var x = parseInt(parts[0]);
                var y = parseInt(parts[1]);
                var dir = this._getDiggingDirection(x, y);
                if (!dir) {
                    continue;
                } /* this wall is not suitable */
                //		consolere.log("wall", x, y);
                /* try adding a feature */
                var featureAttempts = 0;
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
                for (var id in this._walls) {
                    if (this._walls[id] > 1) {
                        priorityWalls++;
                    }
                }
            } while (this._dug / area < this._options.dugPercentage || priorityWalls); /* fixme number of priority walls */
            this._addDoors();
            if (callback) {
                for (var i = 0; i < this._width; i++) {
                    for (var j = 0; j < this._height; j++) {
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
            var prio1 = [];
            var prio2 = [];
            for (var id in this._walls) {
                var prio = this._walls[id];
                if (prio == 2) {
                    prio2.push(id);
                }
                else {
                    prio1.push(id);
                }
            }
            var arr = (prio2.length ? prio2 : prio1);
            if (!arr.length) {
                return null;
            } /* no walls :/ */
            var id2 = arr.sort()[Math.floor(Math.random() * arr.length)]; // sort to make the order deterministic
            delete this._walls[id2];
            return id2;
        }
        ;
        _firstRoom() {
            var cx = Math.floor(this._width / 2);
            var cy = Math.floor(this._height / 2);
            var room = Room.createRandomCenter(cx, cy, this._options);
            this._rooms.push(room);
            room.create(this._digCallback);
        }
        ;
        _fillMap(value) {
            var map = [];
            for (var i = 0; i < this._width; i++) {
                map.push([]);
                for (var j = 0; j < this._height; j++) {
                    map[i].push(value);
                }
            }
            return map;
        }
        ;
        _tryFeature(x, y, dx, dy) {
            var featureType = getWeightedValue(this._features);
            var feature = this.FeatureClass[featureType].createRandomAt(x, y, dx, dy, this._options);
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
            var deltas = this._ROTDIRS4;
            for (var i = 0; i < deltas.length; i++) {
                var delta = deltas[i];
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
            var result = null;
            var deltas = this._ROTDIRS4;
            for (var i = 0; i < deltas.length; i++) {
                var delta = deltas[i];
                var x = cx + delta[0];
                var y = cy + delta[1];
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
            var isWallCallback = (x, y) => {
                return (data[x][y] == 1);
            };
            for (var i = 0; i < this._rooms.length; i++) {
                var room = this._rooms[i];
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
    DungeonGenerator.create = create;
})(DungeonGenerator || (DungeonGenerator = {}));
class Player {
    constructor(param) {
        Object.assign(this, Game.dup(param));
        this.offx = 0;
        this.offy = 0;
        this.dir = 'down';
        this.movemode = 'idle';
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
            down: [[0, 0], [1, 0], [2, 0], [3, 0]].map(xy => [psbasex + this._sprite_width * xy[0], psbasey + this._sprite_height * xy[1]]),
            left: [[4, 0], [5, 0], [6, 0], [7, 0]].map(xy => [psbasex + this._sprite_width * xy[0], psbasey + this._sprite_height * xy[1]]),
            up: [[8, 0], [9, 0], [10, 0], [11, 0]].map(xy => [psbasex + this._sprite_width * xy[0], psbasey + this._sprite_height * xy[1]]),
            right: [[12, 0], [13, 0], [14, 0], [15, 0]].map(xy => [psbasex + this._sprite_width * xy[0], psbasey + this._sprite_height * xy[1]]),
        };
    }
    update(delta, ms, opts) {
        if (this.movemode == 'idle') {
            switch (opts.moveDir) {
                case 'left':
                    this.dir = 'left';
                    if (opts.moveCheckCallback(this, this.x - 1, this.y)) {
                        this.movemode = 'move-left';
                        this.movems = this.movems == 0 ? this.movestep : this.movems;
                    }
                    else {
                        this.anim = 0;
                        this.movems = 0;
                    }
                    break;
                case 'up':
                    this.dir = 'up';
                    if (opts.moveCheckCallback(this, this.x, this.y - 1)) {
                        this.movemode = 'move-up';
                        this.movems = this.movems == 0 ? this.movestep : this.movems;
                    }
                    else {
                        this.anim = 0;
                        this.movems = 0;
                    }
                    break;
                case 'right':
                    this.dir = 'right';
                    if (opts.moveCheckCallback(this, this.x + 1, this.y)) {
                        this.movemode = 'move-right';
                        this.movems = this.movems == 0 ? this.movestep : this.movems;
                    }
                    else {
                        this.anim = 0;
                        this.movems = 0;
                    }
                    break;
                case 'down':
                    this.dir = 'down';
                    if (opts.moveCheckCallback(this, this.x, this.y + 1)) {
                        this.movemode = 'move-down';
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
                this.movemode = 'idle';
                this.movems += this.movestep;
            }
            this.offx = 24 * (1 - this.movems / this.movestep);
        }
        else if (this.movemode == "move-left") {
            this.movems -= delta;
            this.anim += delta;
            if (this.movems <= 0) {
                this.x -= 1;
                this.movemode = 'idle';
                this.movems += this.movestep;
            }
            this.offx = -24 * (1 - this.movems / this.movestep);
        }
        else if (this.movemode == "move-down") {
            this.movems -= delta;
            this.anim += delta;
            if (this.movems <= 0) {
                this.y += 1;
                this.movemode = 'idle';
                this.movems += this.movestep;
            }
            this.offy = 24 * (1 - this.movems / this.movestep);
        }
        else if (this.movemode == "move-up") {
            this.movems -= delta;
            this.anim += delta;
            if (this.movems <= 0) {
                this.y -= 1;
                this.movemode = 'idle';
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
class Channel {
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
        this.bufferSource = this.sound.audioContext.createBufferSource();
        this.bufferSource.buffer = this.buffer;
        this.bufferSource.connect(this.sound.audioContext.destination);
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
class Sound {
    constructor() {
        this.SOUND_CHANNEL_MAX = 36 * 36;
        this.channels = new Array(this.SOUND_CHANNEL_MAX);
        this.bufferSourceIdCount = 0;
        this.playingBufferSources = {};
        this.audioContext = new AudioContext();
        this.channels = new Array(this.SOUND_CHANNEL_MAX);
        this.bufferSourceIdCount = 0;
        this.playingBufferSources = {};
        this.reset();
    }
    _loadSound(file) {
        return new Promise((resolve, reject) => {
            var xhr = new XMLHttpRequest();
            xhr.responseType = "arraybuffer";
            xhr.open("GET", file, true);
            xhr.onerror = () => {
                var msg = "ファイル " + file + "のロードに失敗。";
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
                var msg = 'file:' + file + 'decodeAudioDataに失敗。';
                reject(msg);
            });
        }));
    }
    loadSoundToChannel(file, channel) {
        return this._loadSound(file).then((audioBufferNode) => { this.channels[channel].audioBufferNode = audioBufferNode; });
    }
    loadSoundsToChannel(config) {
        return Promise.all(Object.keys(config)
            .map((x) => ~~x)
            .map((channel) => this._loadSound(config[channel]).then((audioBufferNode) => { this.channels[channel].audioBufferNode = audioBufferNode; }))).then(() => { });
    }
    createUnmanagedSoundChannel(file) {
        return this._loadSound(file).then((audioBufferNode) => new UnmanagedSoundChannel(this, audioBufferNode));
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
                for (var key /* number */ in this.playingBufferSources) {
                    var bufferid = ~~key;
                    if (this.playingBufferSources[bufferid].id == i) {
                        var srcNode = this.playingBufferSources[bufferid].buffer;
                        srcNode.stop();
                        srcNode.disconnect();
                        this.playingBufferSources[bufferid] = null;
                        delete this.playingBufferSources[bufferid];
                    }
                }
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
                this.playingBufferSources[bufferid] = { id: i, buffer: src };
                src.buffer = c.audioBufferNode;
                src.loop = c.loopPlay;
                src.connect(this.audioContext.destination);
                src.onended = ((id) => {
                    var srcNode = src; // this.playingBufferSources[id];
                    //if (!!srcNode && srcNode.playbackState === PlaybackState.FINISHED_STATE) {
                    srcNode.stop(0);
                    srcNode.disconnect();
                    this.playingBufferSources[bufferid] = null;
                    delete this.playingBufferSources[bufferid];
                    //}
                }).bind(null, bufferid);
                src.start(0);
            }
        });
    }
    stop() {
        var oldPlayingBufferSources = this.playingBufferSources;
        this.playingBufferSources = {};
        for (var bufferid /* number */ in oldPlayingBufferSources) {
            if (oldPlayingBufferSources.hasOwnProperty(bufferid)) {
                var s = oldPlayingBufferSources[bufferid].buffer;
                if (s != null) {
                    //if (s.playbackState === PlaybackState.PLAYING_STATE) {
                    s.stop(0);
                    s.disconnect();
                    oldPlayingBufferSources[bufferid] = null;
                    //}
                }
            }
        }
    }
    reset() {
        for (var i = 0; i < this.SOUND_CHANNEL_MAX; i++) {
            this.channels[i] = this.channels[i] || (new Channel());
            this.channels[i].reset();
            this.bufferSourceIdCount = 0;
        }
        this.playingBufferSources = {};
    }
}
class Monster {
    constructor(param = {}) {
        Object.assign(this, param);
    }
    draw() { }
    update() { }
}
window.onload = () => {
    Game.Create({
        title: "TSJQ",
        screen: {
            id: 'glcanvas',
            scale: 2,
        },
        scene: {
            title: {
                enter(data) {
                    this.onpointerclick = this.onpointerclick.bind(this);
                    Game.getInput().on("pointerclick", this.onpointerclick);
                    this.wait_click = this.wait_click.bind(this);
                    this.clicked = this.clicked.bind(this);
                    this.update = this.wait_click;
                    this.fade_rate = 0;
                },
                suspend() {
                    Game.getInput().off("pointerclick", this.onpointerclick);
                },
                resume() {
                    Game.getInput().on("pointerclick", this.onpointerclick);
                },
                leave() {
                    Game.getInput().off("pointerclick", this.onpointerclick);
                },
                wait_click(delta, ms) {
                    this.show_click_or_tap = (~~(ms / 500) % 2) == 0;
                    this.fade_rate = 0;
                },
                clicked(delta, ms) {
                    if (this.start_ms == -1) {
                        this.start_ms = ms;
                    }
                    var elapsed = ms - this.start_ms;
                    this.show_click_or_tap = (~~(elapsed / 50) % 2) == 0;
                    if (elapsed >= 1000) {
                        this.fade_rate = ((elapsed - 1000) / 500);
                    }
                    if (elapsed >= 1500) {
                        Game.getSceneManager().push("classroom");
                        this.update = this.wait_click;
                    }
                },
                draw() {
                    var w = Game.getScreen().width() / 2;
                    var h = Game.getScreen().height() / 2;
                    Game.getScreen().context().save();
                    Game.getScreen().context().scale(2, 2);
                    Game.getScreen().context().clearRect(0, 0, w, h);
                    Game.getScreen().context().fillStyle = "rgb(255,255,255)";
                    Game.getScreen().context().fillRect(0, 0, w, h);
                    Game.getScreen().context().drawImage(Game.getScreen().texture('title'), 0, 0, 192, 72, w / 2 - 192 / 2, 50, 192, 72);
                    if (this.show_click_or_tap) {
                        Game.getScreen().context().drawImage(Game.getScreen().texture('title'), 0, 72, 168, 24, w / 2 - 168 / 2, h - 50, 168, 24);
                    }
                    if (this.fade_rate > 0) {
                        Game.getScreen().context().fillStyle = "rgba(0,0,0," + this.fade_rate + ")";
                        Game.getScreen().context().fillRect(0, 0, w, h);
                    }
                    Game.getScreen().context().restore();
                },
                onpointerclick(ev) {
                    if (Game.getScreen().PagePosContainScreen(ev.pageX, ev.pageY)) {
                        Game.getInput().off("pointerclick", this.onpointerclick);
                        Game.getSound().reqPlayChannel(0);
                        Game.getSound().playChannel();
                        this.start_ms = -1;
                        this.update = this.clicked;
                    }
                },
            },
            classroom: {
                enter(data) {
                    this.onpointerclick = this.onpointerclick.bind(this);
                    this.wait_fadein = this.wait_fadein.bind(this);
                    this.wait_click = this.wait_click.bind(this);
                    this.clicked = this.clicked.bind(this);
                    this.update = this.wait_fadein;
                    this.fade_rate = 1;
                    this.start_ms = -1;
                    this.selectedCharactor = -1;
                    this.selectedCharactorDir = 0;
                    this.selectedCharactorOffY = 0;
                    Game.getSound().reqPlayChannel(2, true);
                    Game.getSound().playChannel();
                },
                suspend() {
                    Game.getInput().off("pointerclick", this.onpointerclick);
                },
                resume() {
                    Game.getInput().on("pointerclick", this.onpointerclick);
                },
                leave() {
                    Game.getInput().off("pointerclick", this.onpointerclick);
                    Game.getSound().reqStopChannel(2);
                    Game.getSound().playChannel();
                },
                wait_fadein(delta, ms) {
                    if (this.start_ms == -1) {
                        this.start_ms = ms;
                    }
                    var elapsed = ms - this.start_ms;
                    if (elapsed <= 500) {
                        this.fade_rate = 1 - elapsed / 500;
                    }
                    else {
                        this.fade_rate = 0;
                        this.start_ms = -1;
                        this.update = this.wait_click;
                        Game.getInput().on("pointerclick", this.onpointerclick);
                    }
                },
                wait_click(delta, ms) {
                },
                clicked(delta, ms) {
                    if (this.start_ms == -1) {
                        this.start_ms = ms;
                    }
                    var elapsed = ms - this.start_ms;
                    if (0 <= elapsed && elapsed < 1600) {
                        // くるくる
                        this.selectedCharactorDir = ~~(elapsed / 100);
                        this.selectedCharactorOffY = 0;
                    }
                    else if (1600 <= elapsed && elapsed < 1800) {
                        // ぴょん
                        this.selectedCharactorDir = 0;
                        this.selectedCharactorOffY = Math.sin((elapsed - 1600) * Math.PI / 200) * 20;
                    }
                    else if (1800 <= elapsed) {
                        this.fade_rate = (((ms - this.start_ms) - 1800) / 500);
                        if (elapsed >= 2300) {
                            var player = new Player({
                                charactor: this.selectedCharactor,
                                x: 0,
                                y: 0,
                            });
                            Game.getSceneManager().pop();
                            Game.getSceneManager().push("dungeon", { player: player, floor: 1 });
                            this.update = this.elapsed;
                        }
                    }
                },
                draw() {
                    var w = Game.getScreen().width() / 2;
                    var h = Game.getScreen().height() / 2;
                    Game.getScreen().context().save();
                    Game.getScreen().context().scale(2, 2);
                    Game.getScreen().context().clearRect(0, 0, w, h);
                    Game.getScreen().context().fillStyle = "rgb(255,255,255)";
                    Game.getScreen().context().fillRect(0, 0, w, h);
                    // 床
                    for (var y = 0; y < ~~((w + 23) / 24); y++) {
                        for (var x = 0; x < ~~((w + 23) / 24); x++) {
                            Game.getScreen().context().drawImage(Game.getScreen().texture('mapchip'), 0, 0, 24, 24, x * 24, y * 24, 24, 24);
                        }
                    }
                    // 壁
                    for (var y = 0; y < 2; y++) {
                        for (var x = 0; x < ~~((w + 23) / 24); x++) {
                            Game.getScreen().context().drawImage(Game.getScreen().texture('mapchip'), 120, 96, 24, 24, x * 24, y * 24 - 23, 24, 24);
                        }
                    }
                    // 黒板
                    Game.getScreen().context().drawImage(Game.getScreen().texture('mapchip'), 0, 204, 72, 36, 90, -12, 72, 36);
                    // 各キャラと机
                    for (var y = 0; y < 5; y++) {
                        for (var x = 0; x < 6; x++) {
                            var id = y * 6 + x;
                            Game.getScreen().context().drawImage(Game.getScreen().texture('charactor'), 752 * (id % 2) + ((this.selectedCharactor != id) ? 0 : (188 * (this.selectedCharactorDir % 4))), 47 * ~~(id / 2), 47, 47, 12 + x * 36, 24 + y * (48 - 7) - ((this.selectedCharactor != id) ? 0 : (this.selectedCharactorOffY)), 47, 47);
                            Game.getScreen().context().drawImage(Game.getScreen().texture('mapchip'), 72, 180, 24, 24, 24 + x * 36, 48 + y * (48 - 7), 24, 24);
                        }
                    }
                    if (this.fade_rate > 0) {
                        Game.getScreen().context().fillStyle = "rgba(0,0,0," + this.fade_rate + ")";
                        Game.getScreen().context().fillRect(0, 0, w, h);
                    }
                    Game.getScreen().context().restore();
                },
                onpointerclick(ev) {
                    if (Game.getScreen().PagePosContainScreen(ev.pageX, ev.pageY)) {
                        var pos = Game.getScreen().PagePosToScreenPos(ev.pageX, ev.pageY);
                        var xx = ~~((pos[0] / 2 - 12) / 36);
                        var yy = ~~((pos[1] / 2 - 24) / (48 - 7));
                        if (0 <= xx && xx < 6 && 0 <= yy && yy < 5) {
                            this.selectedCharactor = yy * 6 + xx;
                            this.selectedCharactorDir = 0;
                            this.selectedCharactorOffY = 0;
                            Game.getInput().off("pointerclick", this.onpointerclick);
                            Game.getSound().reqPlayChannel(0);
                            Game.getSound().playChannel();
                            this.start_ms = -1;
                            this.update = this.clicked;
                        }
                    }
                },
            },
            dungeon: {
                enter(param) {
                    // マップサイズ算出
                    var map_chip_w = 30 + param.floor * 3;
                    var map_chip_h = 30 + param.floor * 3;
                    // マップ自動生成
                    var mapchips_l1 = [];
                    var dungeon = DungeonGenerator.create(map_chip_w, map_chip_w, (x, y, v) => {
                        mapchips_l1[y] = mapchips_l1[y] || [];
                        mapchips_l1[y][x] = v ? 0 : 1;
                    });
                    // 装飾
                    for (var y = 1; y < map_chip_h; y++) {
                        for (var x = 0; x < map_chip_w; x++) {
                            mapchips_l1[y - 1][x] = mapchips_l1[y][x] == 1 && mapchips_l1[y - 1][x] == 0 ? 2 : mapchips_l1[y - 1][x];
                        }
                    }
                    var mapchips_l2 = [];
                    for (var y = 0; y < map_chip_h; y++) {
                        mapchips_l2[y] = [];
                        for (var x = 0; x < map_chip_w; x++) {
                            mapchips_l2[y][x] = (mapchips_l1[y] != null && mapchips_l1[y][x] == 0) ? 0 : 1;
                        }
                    }
                    // 部屋シャッフル
                    var rooms = new Array(dungeon._rooms.length);
                    for (var i = 0; i < rooms.length; i++) {
                        rooms[i] = dungeon._rooms[i];
                    }
                    rooms = Game.shuffle(rooms);
                    // 開始位置
                    var startPos = rooms[0].getCenter();
                    param.player.x = startPos[0];
                    param.player.y = startPos[1];
                    // 階段位置
                    var stairsPos = rooms[1].getCenter();
                    mapchips_l1[stairsPos[1]][stairsPos[0]] = 10;
                    // モンスター配置
                    this.monsters = rooms.splice(2).map(x => {
                        var pos = x.getCenter();
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
                                var xx = this.x - x;
                                var yy = this.y - y;
                                if (0 <= xx && xx < Game.getScreen().width() / 24 && 0 <= yy && yy < Game.getScreen().height() / 24) {
                                    Game.getScreen().context().drawImage(Game.getScreen().texture('monster'), this.anim * 24, 0, 24, 24, xx * 24 + offx + 12, yy * 24 + offy + 12, 24, 24);
                                }
                            }
                        });
                    });
                    this.map = new MapData({
                        width: map_chip_w,
                        height: map_chip_w,
                        gridsize: { width: 24, height: 24 },
                        layer: {
                            0: {
                                texture: "mapchip",
                                chip: {
                                    1: { x: 48, y: 0 },
                                    2: { x: 96, y: 96 },
                                    10: { x: 96, y: 0 },
                                },
                                chips: mapchips_l1
                            },
                            1: {
                                texture: "mapchip",
                                chip: {
                                    0: { x: 96, y: 72 },
                                },
                                chips: mapchips_l2
                            },
                        },
                    });
                    var scale = 2;
                    // カメラを更新
                    this.map.update({
                        viewpoint: {
                            x: (param.player.x * 24 + param.player.offx) + param.player._sprite_width / 2,
                            y: (param.player.y * 24 + param.player.offy) + param.player._sprite_height / 2
                        },
                        viewwidth: Game.getScreen().width() / scale,
                        viewheight: Game.getScreen().height() / scale,
                    });
                    Game.getSound().reqPlayChannel(1, true);
                    Game.getSound().playChannel();
                    // assign virtual pad
                    this.pad = new Pad();
                    this.onpointerdown = this.onpointerdown.bind(this);
                    this.onpointermove = this.onpointermove.bind(this);
                    this.onpointerup = this.onpointerup.bind(this);
                    this.onpointerclick = this.onpointerclick.bind(this);
                    this.wait_fadein = this.wait_fadein.bind(this);
                    this.update_main = this.update_main.bind(this);
                    this.update_kaidan = this.update_kaidan.bind(this);
                    this.update = this.wait_fadein;
                    this.start_ms = -1;
                    this.fade_rate = 1;
                    this.param = param;
                    this.changed = true;
                },
                suspend() {
                    Game.getInput().off("pointerdown", this.onpointerdown);
                    Game.getInput().off("pointermove", this.onpointermove);
                    Game.getInput().off("pointerup", this.onpointerup);
                    Game.getInput().off("pointerleave", this.onpointerup);
                    Game.getInput().off("pointerclick", this.onpointerclick);
                    Game.getSound().reqStopChannel(1);
                    Game.getSound().playChannel();
                },
                resume() {
                    Game.getInput().on("pointerdown", this.onpointerdown);
                    Game.getInput().on("pointermove", this.onpointermove);
                    Game.getInput().on("pointerup", this.onpointerup);
                    Game.getInput().off("pointerleave", this.onpointerup);
                    Game.getInput().on("pointerclick", this.onpointerclick);
                    this.changed = true;
                    Game.getSound().reqPlayChannel(1, true);
                    Game.getSound().playChannel();
                },
                leave() {
                    Game.getInput().off("pointerdown", this.onpointerdown);
                    Game.getInput().off("pointermove", this.onpointermove);
                    Game.getInput().off("pointerup", this.onpointerup);
                    Game.getInput().off("pointerleave", this.onpointerup);
                    Game.getInput().off("pointerclick", this.onpointerclick);
                    Game.getSound().reqStopChannel(1);
                    Game.getSound().playChannel();
                },
                calc_lighting(x, y, power, dec, dec2, iswalkable, setted) {
                    if (0 > x || x >= this.map.width) {
                        return;
                    }
                    if (0 > y || y >= this.map.height) {
                        return;
                    }
                    if (power <= this.map.lighting[y][x]) {
                        return;
                    }
                    setted[x + "," + y] = true;
                    this.map.lighting[y][x] = Math.max(this.map.lighting[y][x], power);
                    this.map.visibled[y][x] = Math.max(this.map.lighting[y][x], this.map.visibled[y][x]);
                    if (!iswalkable(x, y)) {
                        power -= dec2;
                    }
                    else {
                        power -= dec;
                    }
                    this.calc_lighting(x + 0, y - 1, power, dec, dec2, iswalkable, setted);
                    this.calc_lighting(x - 1, y + 0, power, dec, dec2, iswalkable, setted);
                    this.calc_lighting(x + 1, y + 0, power, dec, dec2, iswalkable, setted);
                    this.calc_lighting(x + 0, y + 1, power, dec, dec2, iswalkable, setted);
                },
                update_lighting(iswalkable) {
                    this.map.clear_lighting();
                    this.calc_lighting(this.param.player.x, this.param.player.y, 140, 20, 50, iswalkable, {});
                },
                wait_fadein(delta, ms) {
                    if (this.start_ms == -1) {
                        this.start_ms = ms;
                    }
                    var elapsed = ms - this.start_ms;
                    if (elapsed <= 500) {
                        this.fade_rate = 1 - elapsed / 500;
                    }
                    else {
                        this.fade_rate = 0;
                        this.start_ms = -1;
                        this.update = this.update_main;
                        Game.getInput().on("pointerdown", this.onpointerdown);
                        Game.getInput().on("pointermove", this.onpointermove);
                        Game.getInput().on("pointerup", this.onpointerup);
                        Game.getInput().on("pointerleave", this.onpointerup);
                        Game.getInput().on("pointerclick", this.onpointerclick);
                    }
                    this.update_lighting((x, y) => ((this.map.layer[0].chips[y][x] == 1) || (this.map.layer[0].chips[y][x] == 10)));
                },
                update_main(delta, ms) {
                    // プレイヤーを更新
                    var dir = 'idle;';
                    const dir_to_angle = { 0: 'left', 1: 'up', 2: 'right', 3: 'down' };
                    if (this.pad.isTouching && this.pad.distance > 0.4) {
                        dir = dir_to_angle[~~((this.pad.angle + 180 + 45) / 90) % 4];
                    }
                    this.param.player.update(delta, ms, {
                        moveDir: dir,
                        moveCheckCallback: (p, x, y) => (this.map.layer[0].chips[y][x] == 1) || (this.map.layer[0].chips[y][x] == 10)
                    });
                    // モンスターを更新
                    this.monsters.forEach((x) => x.update(delta, ms));
                    var scale = 2;
                    // カメラを更新
                    this.map.update({
                        viewpoint: {
                            x: (this.param.player.x * 24 + this.param.player.offx) + this.param.player._sprite_width / 2,
                            y: (this.param.player.y * 24 + this.param.player.offy) + this.param.player._sprite_height / 2
                        },
                        viewwidth: Game.getScreen().width() / scale,
                        viewheight: Game.getScreen().height() / scale,
                    });
                    // 現在位置のマップチップを取得
                    var chip = this.map.layer[0].chips[~~this.param.player.y][~~this.param.player.x];
                    if (chip == 10) {
                        // 階段なので次の階層に移動させる。
                        this.update = this.update_kaidan;
                        this.start_ms = -1;
                    }
                    // プレイヤー位置のモンスターを破壊
                    this.monsters = this.monsters.filter((x) => {
                        if ((x.x == this.param.player.x) && (x.y == this.param.player.y)) {
                            consolere.log(this.param.player.x, this.param.player.y, x.x, x.y);
                            return false;
                        }
                        else {
                            return true;
                        }
                    });
                    this.update_lighting((x, y) => (this.map.layer[0].chips[y][x] == 1) || (this.map.layer[0].chips[y][x] == 10));
                },
                update_kaidan(delta, ms) {
                    var scale = 2;
                    if (this.start_ms == -1) {
                        this.start_ms = ms;
                        Game.getSound().reqPlayChannel(3);
                        Game.getSound().playChannel();
                    }
                    var elapsed = ms - this.start_ms;
                    if (elapsed <= 500) {
                        this.fade_rate = (elapsed / 500);
                    }
                    if (elapsed >= 1000) {
                        this.param.floor++;
                        Game.getSceneManager().pop();
                        Game.getSceneManager().push("dungeon", this.param);
                    }
                    this.update_lighting((x, y) => (this.map.layer[0].chips[y][x] == 1) || (this.map.layer[0].chips[y][x] == 10));
                },
                draw() {
                    var scale = 2;
                    Game.getScreen().context().save();
                    Game.getScreen().context().scale(scale, scale);
                    Game.getScreen().context().clearRect(0, 0, Game.getScreen().width() / 2, Game.getScreen().height() / 2);
                    Game.getScreen().context().fillStyle = "rgb(255,255,255)";
                    Game.getScreen().context().fillRect(0, 0, Game.getScreen().width() / 2, Game.getScreen().height() / 2);
                    this.map.draw((l, camera_local_px, camera_local_py) => {
                        if (l == 0) {
                            var animf = this.param.player.getAnimFrame();
                            // 影
                            Game.getScreen().context().fillStyle = "rgba(0,0,0,0.25)";
                            Game.getScreen().context().beginPath();
                            Game.getScreen().context().ellipse(camera_local_px, camera_local_py + 7, 12, 3, 0, 0, Math.PI * 2);
                            Game.getScreen().context().fill();
                            // モンスター
                            var camera = this.map.camera;
                            this.monsters.forEach((x) => x.draw(camera.chip_x, camera.chip_y, camera.chip_offx, camera.chip_offy));
                            // キャラクター
                            Game.getScreen().context().drawImage(Game.getScreen().texture('charactor'), this.param.player._sprite[this.param.player.dir][animf][0], this.param.player._sprite[this.param.player.dir][animf][1], this.param.player._sprite_width, this.param.player._sprite_height, camera_local_px - this.param.player._sprite_width / 2, camera_local_py - this.param.player._sprite_width / 2 - 12, this.param.player._sprite_width, this.param.player._sprite_height);
                        }
                        20;
                    });
                    // フェード
                    if (this.fade_rate > 0) {
                        Game.getScreen().context().fillStyle = "rgba(0,0,0," + this.fade_rate + ")";
                        Game.getScreen().context().fillRect(0, 0, Game.getScreen().width() / scale, Game.getScreen().height() / scale);
                    }
                    Game.getScreen().context().restore();
                    // バーチャルジョイスティックの描画
                    if (this.pad.isTouching) {
                        Game.getScreen().context().fillStyle = "rgba(255,255,255,0.25)";
                        Game.getScreen().context().beginPath();
                        Game.getScreen().context().ellipse(this.pad.x, this.pad.y, this.pad.radius * 1.2, this.pad.radius * 1.2, 0, 0, Math.PI * 2);
                        Game.getScreen().context().fill();
                        Game.getScreen().context().beginPath();
                        Game.getScreen().context().ellipse(this.pad.x + this.pad.cx, this.pad.y + this.pad.cy, this.pad.radius, this.pad.radius, 0, 0, Math.PI * 2);
                        Game.getScreen().context().fill();
                    }
                },
                onpointerdown(ev) {
                    if (this.pad.onpointingstart(ev.pointerId)) {
                        var pos = Game.getScreen().PagePosToScreenPos(ev.pageX, ev.pageY);
                        this.pad.x = pos[0];
                        this.pad.y = pos[1];
                    }
                    this.changed = true;
                },
                onpointermove(ev) {
                    var pos = Game.getScreen().PagePosToScreenPos(ev.pageX, ev.pageY);
                    this.pad.onpointingmove(ev.pointerId, pos[0], pos[1]);
                    this.changed = true;
                },
                onpointerup(ev) {
                    this.pad.onpointingend(ev.pointerId);
                    this.changed = true;
                },
                onpointerclick(ev) {
                    if (Game.getScreen().PagePosContainScreen(ev.pageX, ev.pageY)) {
                        Game.getInput().off("pointerclick", this.onpointerclick);
                        Game.getSceneManager().push("mapview", { map: this.map, player: this.param.player });
                    }
                },
            },
            mapview: {
                enter(data) {
                    this.onpointerclick = this.onpointerclick.bind(this);
                    this.map = data.map;
                    this.player = data.player;
                    Game.getInput().on("pointerclick", this.onpointerclick);
                },
                suspend() {
                    Game.getInput().off("pointerclick", this.onpointerclick);
                },
                resume() {
                    Game.getInput().on("pointerclick", this.onpointerclick);
                },
                leave() {
                    Game.getInput().off("pointerclick", this.onpointerclick);
                },
                update(delta, ms) {
                },
                draw() {
                    Game.getScreen().context().save();
                    Game.getScreen().context().clearRect(0, 0, Game.getScreen().width(), Game.getScreen().height());
                    Game.getScreen().context().fillStyle = "rgb(0,0,0)";
                    Game.getScreen().context().fillRect(0, 0, Game.getScreen().width(), Game.getScreen().height());
                    var offx = ~~((Game.getScreen().width() - this.map.width * 5) / 2);
                    var offy = ~~((Game.getScreen().height() - this.map.height * 5) / 2);
                    // ミニマップを描画
                    for (var y = 0; y < this.map.height; y++) {
                        for (var x = 0; x < this.map.width; x++) {
                            var chip = this.map.layer[0].chips[y][x];
                            var color = "rgb(52,12,0)";
                            switch (chip) {
                                case 1:
                                    color = "rgb(179,116,39)";
                                    break;
                                case 10:
                                    color = "rgb(255,0,0)";
                                    break;
                            }
                            Game.getScreen().context().fillStyle = color;
                            Game.getScreen().context().fillRect(offx + x * 5, offy + y * 5, 5, 5);
                            var light = 1 - this.map.visibled[y][x] / 100;
                            if (light > 1) {
                                light = 1;
                            }
                            else if (light < 0) {
                                light = 0;
                            }
                            Game.getScreen().context().fillStyle = "rgba(0,0,0," + light + ")";
                            Game.getScreen().context().fillRect(offx + x * 5, offy + y * 5, 5, 5);
                        }
                    }
                    Game.getScreen().context().fillStyle = "rgb(0,255,0)";
                    Game.getScreen().context().fillRect(offx + this.player.x * 5, offy + this.player.y * 5, 5, 5);
                    Game.getScreen().context().restore();
                },
                onpointerclick(ev) {
                    if (Game.getScreen().PagePosContainScreen(ev.pageX, ev.pageY)) {
                        Game.getInput().off("pointerclick", this.onpointerclick);
                        Game.getSceneManager().pop();
                    }
                },
            },
        },
    }).then(() => {
        var anim = 0;
        var update = (ms) => {
            Game.getScreen().context().save();
            Game.getScreen().context().clearRect(0, 0, Game.getScreen().width(), Game.getScreen().height());
            Game.getScreen().context().fillStyle = "rgb(255,255,255)";
            Game.getScreen().context().fillRect(0, 0, Game.getScreen().width(), Game.getScreen().height());
            var n = ~(ms / 200);
            Game.getScreen().context().translate(Game.getScreen().width() / 2, Game.getScreen().height() / 2);
            Game.getScreen().context().rotate(n * Math.PI / 4);
            for (var i = 0; i < 8; i++) {
                var g = (i * 32);
                Game.getScreen().context().save();
                Game.getScreen().context().rotate(i * Math.PI / 4);
                Game.getScreen().context().fillStyle = "rgb(" + g + "," + g + "," + g + ")";
                Game.getScreen().context().fillRect(-10, -100, 20, 50);
                Game.getScreen().context().restore();
            }
            Game.getScreen().context().restore();
            anim = requestAnimationFrame(update.bind(this));
        };
        anim = requestAnimationFrame(update.bind(this));
        return Promise.all([
            Game.loadTextures({
                title: './assets/title.png',
                mapchip: './assets/mapchip.png',
                charactor: './assets/charactor.png',
                monster: './assets/monster.png'
            }),
            Game.getSound().loadSoundsToChannel({
                0: './assets/title.mp3',
                1: './assets/dungeon.mp3',
                2: './assets/classroom.mp3',
                3: './assets/kaidan.mp3'
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
//# sourceMappingURL=app.js.map