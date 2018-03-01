var consolere;
var m3;
(function (m3) {
    function projection(width, height) {
        // Note: This matrix flips the Y axis so that 0 is at the top.
        return [
            2 / width, 0, 0,
            0, -2 / height, 0,
            -1, 1, 1
        ];
    }
    m3.projection = projection;
    function identity() {
        return [
            1, 0, 0,
            0, 1, 0,
            0, 0, 1,
        ];
    }
    m3.identity = identity;
    function translation(tx, ty) {
        return [
            1, 0, 0,
            0, 1, 0,
            tx, ty, 1,
        ];
    }
    m3.translation = translation;
    function rotation(angleInRadians) {
        var c = Math.cos(angleInRadians);
        var s = Math.sin(angleInRadians);
        return [
            c, -s, 0,
            s, c, 0,
            0, 0, 1,
        ];
    }
    m3.rotation = rotation;
    function scaling(sx, sy) {
        return [
            sx, 0, 0,
            0, sy, 0,
            0, 0, 1,
        ];
    }
    m3.scaling = scaling;
    function multiply(a, b) {
        var a00 = a[0 * 3 + 0];
        var a01 = a[0 * 3 + 1];
        var a02 = a[0 * 3 + 2];
        var a10 = a[1 * 3 + 0];
        var a11 = a[1 * 3 + 1];
        var a12 = a[1 * 3 + 2];
        var a20 = a[2 * 3 + 0];
        var a21 = a[2 * 3 + 1];
        var a22 = a[2 * 3 + 2];
        var b00 = b[0 * 3 + 0];
        var b01 = b[0 * 3 + 1];
        var b02 = b[0 * 3 + 2];
        var b10 = b[1 * 3 + 0];
        var b11 = b[1 * 3 + 1];
        var b12 = b[1 * 3 + 2];
        var b20 = b[2 * 3 + 0];
        var b21 = b[2 * 3 + 1];
        var b22 = b[2 * 3 + 2];
        return [
            b00 * a00 + b01 * a10 + b02 * a20,
            b00 * a01 + b01 * a11 + b02 * a21,
            b00 * a02 + b01 * a12 + b02 * a22,
            b10 * a00 + b11 * a10 + b12 * a20,
            b10 * a01 + b11 * a11 + b12 * a21,
            b10 * a02 + b11 * a12 + b12 * a22,
            b20 * a00 + b21 * a10 + b22 * a20,
            b20 * a01 + b21 * a11 + b22 * a21,
            b20 * a02 + b21 * a12 + b22 * a22,
        ];
    }
    m3.multiply = multiply;
})(m3 || (m3 = {}));
var Game;
(function (Game) {
    consolere.log('remote log start');
    // Global Variables
    var canvas;
    var gl;
    var textures = {};
    class EventDispatcher {
        constructor() {
            this._listeners = {};
        }
        on(type, listener) {
            if (this._listeners[type] === undefined) {
                this._listeners[type] = [];
            }
            this._listeners[type].push(listener);
            return this;
        }
        off(type, listener) {
            var listeners = this._listeners[type];
            var index = listeners.indexOf(listener);
            if (index != -1) {
                listeners.splice(index, 1);
            }
            return this;
        }
        fire(eventName, param) {
            var listeners = this._listeners[eventName];
            if (listeners) {
                var temp = listeners.slice();
                for (var i = 0, len = temp.length; i < len; ++i) {
                    temp[i].call(this, param);
                }
            }
            return this;
        }
        one(type, listener) {
            var func = () => {
                var result = listener.apply(this, arguments);
                this.off(type, func);
                return result;
            };
            this.on(type, func);
            return this;
        }
        hasEventListener(type) {
            if (this._listeners[type] === undefined && !this["on" + type]) {
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
    let Shader;
    (function (Shader) {
        class ShaderBase {
            constructor(type, source) {
                this.shader = gl.createShader(type);
                gl.shaderSource(this.shader, source);
                gl.compileShader(this.shader);
                var success = gl.getShaderParameter(this.shader, gl.COMPILE_STATUS);
                if (!success) {
                    var log = gl.getShaderInfoLog(this.shader);
                    gl.deleteShader(this.shader);
                    this.shader = null;
                    throw new Error(log);
                }
            }
        }
        Shader.ShaderBase = ShaderBase;
        class VertexShader extends ShaderBase {
            constructor(source) {
                super(gl.VERTEX_SHADER, source);
            }
        }
        Shader.VertexShader = VertexShader;
        class FragmentShader extends ShaderBase {
            constructor(source) {
                super(gl.FRAGMENT_SHADER, source);
            }
        }
        Shader.FragmentShader = FragmentShader;
        class ShaderProgram {
            constructor(...shaders) {
                this.program = gl.createProgram();
                shaders.forEach(x => gl.attachShader(this.program, x.shader));
                gl.linkProgram(this.program);
                var success = gl.getProgramParameter(this.program, gl.LINK_STATUS);
                if (!success) {
                    var log = gl.getProgramInfoLog(this.program);
                    gl.deleteProgram(this.program);
                    this.program = null;
                    throw new Error(log);
                }
            }
            getAttribLocation(name) {
                return gl.getAttribLocation(this.program, name);
            }
            getUniformLocation(name) {
                return gl.getUniformLocation(this.program, name);
            }
        }
        Shader.ShaderProgram = ShaderProgram;
    })(Shader || (Shader = {}));
    class Texture {
        constructor(image) {
            this.texture = gl.createTexture();
            this.image = image;
            gl.activeTexture(gl.TEXTURE0);
            gl.bindTexture(gl.TEXTURE_2D, this.texture);
            // Set the parameters so we can render any size image.
            gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
            gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
            gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.NEAREST);
            gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.NEAREST);
            // Upload the image into the texture.
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, image);
        }
    }
    const vertexShaderSource = `
attribute vec2 a_position;
attribute vec4 a_color;
attribute vec3 a_texCoord;

uniform mat3 u_matrix;

varying vec3 v_texCoord;
varying vec4 v_color;

void main() {

    // Multiply the position by the matrix.
    gl_Position = vec4((u_matrix * vec3(a_position, 1)).xy, 0, 1);

    // pass the texCoord to the fragment shader
    // The GPU will interpolate this value between points.
    v_texCoord = a_texCoord;
    v_color = a_color;

}
`;
    const fragmentShaderSource = `
precision mediump float;

// our texture
uniform sampler2D u_image;

// the texCoords passed in from the vertex shader.
varying vec3 v_texCoord;
varying vec4 v_color;

void main() {
    gl_FragColor = (texture2D(u_image, v_texCoord.xy) * v_color * v_texCoord.z) + (v_color * (1.0 - v_texCoord.z));
}
`;
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
    class Display {
        constructor(shaderProgram) {
            this.program = shaderProgram;
            // Create a buffer and put three 2d clip space points in it
            this.positionBuffer = gl.createBuffer();
        }
        start() {
            // look up where the vertex data needs to go.
            var positionAttributeLocation = this.program.getAttribLocation("a_position");
            var colorAttributeLocation = this.program.getAttribLocation("a_color");
            var texCoordAttributeLocation = this.program.getAttribLocation("a_texCoord");
            // look up uniform locations
            var matrixUniformLocation = this.program.getUniformLocation("u_matrix");
            // Bind it to ARRAY_BUFFER (think of it as ARRAY_BUFFER = positionBuffer)
            gl.bindBuffer(gl.ARRAY_BUFFER, this.positionBuffer);
            // Tell WebGL how to convert from clip space to pixels
            gl.viewport(0, 0, gl.canvas.width, gl.canvas.height);
            // Clear the canvas
            gl.clearColor(0, 0, 0, 0);
            gl.clear(gl.COLOR_BUFFER_BIT);
            // Tell it to use our program (pair of shaders)
            gl.useProgram(this.program.program);
            // Turn on the attribute
            gl.enableVertexAttribArray(positionAttributeLocation);
            gl.enableVertexAttribArray(colorAttributeLocation);
            gl.enableVertexAttribArray(texCoordAttributeLocation);
            // Bind the position buffer.
            gl.bindBuffer(gl.ARRAY_BUFFER, this.positionBuffer);
            // Tell the attribute how to get data out of positionBuffer (ARRAY_BUFFER)
            gl.vertexAttribPointer(positionAttributeLocation, 2, gl.FLOAT, false, 9 * 4, 0 * 4);
            gl.vertexAttribPointer(colorAttributeLocation, 4, gl.FLOAT, false, 9 * 4, 2 * 4);
            gl.vertexAttribPointer(texCoordAttributeLocation, 3, gl.FLOAT, false, 9 * 4, 6 * 4);
            var projectionMatrix = m3.projection(gl.canvas.clientWidth, gl.canvas.clientHeight);
            // set the matrix
            gl.uniformMatrix3fv(matrixUniformLocation, false, projectionMatrix);
            // enable alpha blend
            gl.enable(gl.BLEND);
            gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);
        }
        end() {
        }
        width() {
            return gl.canvas.width;
        }
        height() {
            return gl.canvas.height;
        }
        setTexture(id) {
            var texture = textures[id];
            if (texture != null) {
                gl.activeTexture(gl.TEXTURE0);
                gl.bindTexture(gl.TEXTURE_2D, texture.texture);
                return true;
            }
            else {
                return false;
            }
        }
        rect({ left, top, width, height, color = [1, 1, 1, 1], texture = null, uv = [0, 0, 1, 1] }) {
            var x1 = left;
            var y1 = top;
            var x2 = left + width;
            var y2 = top + height;
            var tw = 1.0;
            var th = 1.0;
            if (textures[texture] != null) {
                tw = textures[texture].image.width * 1.0;
                th = textures[texture].image.height * 1.0;
            }
            var u1 = uv[0] / tw;
            var v1 = uv[1] / th;
            var u2 = (uv[0] + uv[2]) / tw;
            var v2 = (uv[1] + uv[3]) / th;
            var flag = texture == null ? 0.0 : 1.0;
            var vertexBuffer = [
                // x, y, r, g, b, a, u, v, flag
                x1, y1, color[0], color[1], color[2], color[3], u1, v1, flag,
                x2, y1, color[0], color[1], color[2], color[3], u2, v1, flag,
                x1, y2, color[0], color[1], color[2], color[3], u1, v2, flag,
                x1, y2, color[0], color[1], color[2], color[3], u1, v2, flag,
                x2, y1, color[0], color[1], color[2], color[3], u2, v1, flag,
                x2, y2, color[0], color[1], color[2], color[3], u2, v2, flag,
            ];
            // draw
            this.setTexture(texture);
            gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(vertexBuffer), gl.STREAM_DRAW);
            gl.drawArrays(gl.TRIANGLES, 0, 6);
        }
        circle({ x, y, radius, color = [1, 1, 1, 1] /*, texture = null, uv = [0, 0, 1, 1]*/ }) {
            var num_segments = ~~(10 * Math.sqrt(radius));
            var theta = Math.PI * 2 / num_segments;
            var c = Math.cos(theta); //precalculate the sine and cosine
            var s = Math.sin(theta);
            var x1 = radius; //we start at angle = 0 
            var y1 = 0;
            var vertexBuffer = [];
            for (var ii = 0; ii < num_segments; ii++) {
                //apply the rotation matrix
                var x2 = c * x1 - s * y1;
                var y2 = s * x1 + c * y1;
                Array.prototype.push.apply(vertexBuffer, [
                    x, y, color[0], color[1], color[2], color[3], 0, 0, 0,
                    x1 + x, y1 + y, color[0], color[1], color[2], color[3], 0, 0, 0,
                    x2 + x, y2 + y, color[0], color[1], color[2], color[3], 0, 0, 0,
                ]);
                x1 = x2;
                y1 = y2;
            }
            gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(vertexBuffer), gl.STREAM_DRAW);
            gl.drawArrays(gl.TRIANGLES, 0, num_segments * 3);
        }
        toDisplayPos(x, y) {
            var cr = canvas.getBoundingClientRect();
            var scaleScreen = window.innerWidth / document.body.clientWidth;
            var scaleCanvas = canvas.width * 1.0 / cr.width;
            var sx = (x - (cr.left + window.pageXOffset));
            var sy = (y - (cr.top + window.pageYOffset));
            return [sx, sy];
        }
    }
    var display = null;
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
    var timer = new AnimationTimer();
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
    class Scene {
        constructor(manager, obj = {}) {
            Object.assign(this, dup(obj));
            this.manager = manager;
        }
        // virtual methods
        enter(param) { }
        leave() { }
        suspend() { }
        resume() { }
        update() { consolere.log("original"); }
        push(id, param = {}) { this.manager.push(id, param); }
        pop() { this.manager.pop(); }
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
    }
    var sceneManager = null;
    function Create(config) {
        return new Promise((resolve, reject) => {
            canvas = document.getElementById(config.display.id);
            if (!canvas) {
                throw new Error("your browser is not support canvas.");
            }
            gl = canvas.getContext("webgl");
            if (!gl) {
                throw new Error("your browser is not support webgl.");
            }
            // create GLSL shaders, upload the GLSL source, compile the shaders
            var vertexShader = new Shader.VertexShader(vertexShaderSource);
            var fragmentShader = new Shader.FragmentShader(fragmentShaderSource);
            // Link the two shaders into a program
            var program = new Shader.ShaderProgram(vertexShader, fragmentShader);
            display = new Display(program);
            sceneManager = new SceneManager(config.scene);
            hookInputEvent();
            resolve();
        });
    }
    Game.Create = Create;
    ;
    function getDisplay() {
        return display;
    }
    Game.getDisplay = getDisplay;
    function getTimer() {
        return timer;
    }
    Game.getTimer = getTimer;
    function getSceneManager() {
        return sceneManager;
    }
    Game.getSceneManager = getSceneManager;
    function hookInputEvent() {
        if (document.body['pointermove']) {
            consolere.log('pointer event is implemented');
            document.body.addEventListener('touchmove', function (evt) { evt.preventDefault(); }, false);
            document.body.addEventListener('touchdown', function (evt) { evt.preventDefault(); }, false);
            document.body.addEventListener('touchup', function (evt) { evt.preventDefault(); }, false);
            document.body.addEventListener('mousemove', function (evt) { evt.preventDefault(); }, false);
            document.body.addEventListener('mousedown', function (evt) { evt.preventDefault(); }, false);
            document.body.addEventListener('mouseup', function (evt) { evt.preventDefault(); }, false);
        }
        else {
            consolere.log('pointer event is not implemented');
            class Input {
                constructor() {
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
                    var body = document.body;
                    body.addEventListener('mousedown', this._pointerDown.bind(this));
                    body.addEventListener('touchstart', this._pointerDown.bind(this));
                    body.addEventListener('mouseup', this._pointerUp.bind(this));
                    body.addEventListener('touchend', this._pointerUp.bind(this));
                    body.addEventListener('mousemove', this._pointerMove.bind(this));
                    body.addEventListener('touchmove', this._pointerMove.bind(this));
                    body.addEventListener('mouseout', this._pointerLeave.bind(this));
                    body.addEventListener('touchcancel', this._pointerLeave.bind(this));
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
                // 'pointerdown' event, triggered by mousedown/touchstart.
                _pointerDown(e) {
                    if (this._checkEvent(e)) {
                        var evt = this._makePointerEvent('down', e);
                        // don't maybeClick if more than one touch is active.
                        var singleFinger = evt['mouse'] || (evt['touch'] && e.touches.length === 1);
                        if (!this._isScrolling && singleFinger) {
                            this._maybeClick = true;
                            this._maybeClickX = evt.pageX;
                            this._maybeClickY = evt.pageY;
                        }
                    }
                    return false;
                }
                // 'pointerdown' event, triggered by mouseout/touchleave.
                _pointerLeave(e) {
                    if (this._checkEvent(e)) {
                        this._maybeClick = false;
                        this._makePointerEvent('leave', e);
                    }
                    return false;
                }
                // 'pointermove' event, triggered by mousemove/touchmove.
                _pointerMove(e) {
                    if (this._checkEvent(e)) {
                        var evt = this._makePointerEvent('move', e);
                    }
                    return false;
                }
                // 'pointerup' event, triggered by mouseup/touchend.
                _pointerUp(e) {
                    if (this._checkEvent(e)) {
                        var evt = this._makePointerEvent('up', e);
                        // Does our target have maybeClick set by pointerdown?
                        if (this._maybeClick) {
                            // Have we moved too much?
                            if (Math.abs(this._maybeClickX - evt.pageX) < 5 && Math.abs(this._maybeClickY - evt.pageY) < 5) {
                                // Have we scrolled too much?
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
                    evt.initCustomEvent('pointer' + type, true, true, {});
                    evt.touch = e.type.indexOf('touch') === 0;
                    evt.mouse = e.type.indexOf('mouse') === 0;
                    if (evt.touch) {
                        evt.pageX = e.changedTouches[0].pageX;
                        evt.pageY = e.changedTouches[0].pageY;
                    }
                    if (evt.mouse) {
                        evt.pageX = e.clientX + window.pageXOffset;
                        evt.pageY = e.clientY + window.pageYOffset;
                    }
                    evt.maskedEvent = e;
                    document.body.dispatchEvent(evt);
                    return evt;
                }
            }
            new Input();
        }
    }
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
    }
    isHit(x, y) {
        var dx = x - this.x;
        var dy = y - this.y;
        return ((dx * dx) + (dy * dy)) <= this.radius * this.radius;
    }
    onpointingstart() {
        this.isTouching = true;
        this.cx = 0;
        this.cy = 0;
        this.angle = 0;
        this.distance = 0;
    }
    onpointingend() {
        this.isTouching = false;
        this.cx = 0;
        this.cy = 0;
        this.angle = 0;
        this.distance = 0;
    }
    onpointingmove(x, y) {
        if (this.isTouching == false) {
            return;
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
    }
}
class MapData {
    constructor(config) {
        this.config = config;
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
            console.log("room", this._x1, this._y1, this._x2, this._y2);
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
            console.log("corridor", this._startX, this._startY, this._endX, this._endY);
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
            this._ROTDIRS4 = [
                [0, -1],
                [1, 0],
                [0, 1],
                [-1, 0]
            ];
            this._getDiggingDirection = function (cx, cy) {
                if (cx <= 0 || cy <= 0 || cx >= this._width - 1 || cy >= this._height - 1) {
                    return null;
                }
                var result = null;
                var deltas = this._ROTDIRS4[4];
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
            };
            this._addDoors = function () {
                var data = this._map;
                var isWallCallback = function (x, y) {
                    return (data[x][y] == 1);
                };
                for (var i = 0; i < this._rooms.length; i++) {
                    var room = this._rooms[i];
                    room.clearDoors();
                    room.addDoors(isWallCallback);
                }
            };
            this.getRooms = function () {
                return this._rooms;
            };
            this.getCorridors = function () {
                return this._corridors;
            };
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
                //		console.log("wall", x, y);
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
        digCallback(x, y, value) {
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
            var feature = Feature[featureType].createRandomAt(x, y, dx, dy, this._options);
            if (!feature.isValid(this._isWallCallback, this._canBeDugCallback)) {
                //		console.log("not valid");
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
    }
})(DungeonGenerator || (DungeonGenerator = {}));
window.onload = () => {
    Game.Create({
        display: {
            id: 'glcanvas',
        },
        scene: {
            dungeon: {
                enter: function (data) {
                    var mapchips = [];
                    for (var y = 0; y < 60; y++) {
                        mapchips[y] = [];
                        for (var x = 0; x < 60; x++) {
                            mapchips[y][x] = 0;
                        }
                    }
                    for (var y = 60 / 2 - 5; y <= 60 / 2 + 5; y++) {
                        for (var x = ~~(60 / 2) - 5; x <= ~~(60 / 2) + 5; x++) {
                            mapchips[y][x] = 1;
                        }
                    }
                    this.map = new MapData({
                        width: 60,
                        height: 60,
                        gridsize: { width: 24, height: 24 },
                        layer: {
                            0: {
                                chipsize: { width: 24, height: 24 },
                                renderoffset: { x: 0, y: 0 },
                                texture: "mapchip",
                                chip: {
                                    1: { x: 48, y: 0 },
                                }
                            },
                            1: {
                                chipsize: { width: 24, height: 36 },
                                renderoffset: { x: 0, y: -12 },
                                texture: "mapchip",
                                chip: {
                                    0: { x: 96, y: 96 },
                                }
                            },
                            2: {
                                chipsize: { width: 24, height: 24 },
                                renderoffset: { x: 0, y: -36 },
                                texture: "mapchip",
                                chip: {
                                    0: { x: 96, y: 72 },
                                }
                            },
                        },
                        chips: mapchips
                    });
                    var charactor = ~~(Math.random() * 30);
                    var psbasex = (charactor % 2) * 752;
                    var psbasey = ~~(charactor / 2) * 47;
                    this.psprite = {
                        down: [[0, 0], [1, 0], [2, 0], [3, 0]].map(xy => [psbasex + 47 * xy[0], psbasey + 47 * xy[1]]),
                        left: [[4, 0], [5, 0], [6, 0], [7, 0]].map(xy => [psbasex + 47 * xy[0], psbasey + 47 * xy[1]]),
                        up: [[8, 0], [9, 0], [10, 0], [11, 0]].map(xy => [psbasex + 47 * xy[0], psbasey + 47 * xy[1]]),
                        right: [[12, 0], [13, 0], [14, 0], [15, 0]].map(xy => [psbasex + 47 * xy[0], psbasey + 47 * xy[1]]),
                    };
                    this.changed = true;
                    this.px = 0;
                    this.py = 0;
                    this.offx = 0;
                    this.offy = 0;
                    this.pdir = 'down';
                    this.movemode = 'idle';
                    this.movems = 0;
                    this.anim = 0;
                    this.pad = new Pad();
                    this.inputId1 = document.addEventListener("pointerdown", (ev) => {
                        var pos = Game.getDisplay().toDisplayPos(ev.pageX, ev.pageY);
                        this.pad.x = pos[0];
                        this.pad.y = pos[1];
                        this.pad.onpointingstart();
                        this.changed = true;
                    });
                    this.inputId2 = document.addEventListener("pointermove", (ev) => {
                        var pos = Game.getDisplay().toDisplayPos(ev.pageX, ev.pageY);
                        this.pad.onpointingmove(pos[0], pos[1]);
                        this.changed = true;
                    });
                    this.inputId3 = document.addEventListener("pointerup", (ev) => {
                        this.pad.onpointingend();
                        this.changed = true;
                    });
                },
                update: function (delta, ms) {
                    if (!this.changed) {
                        return;
                    }
                    var movestep = 150;
                    var animstep = 150;
                    if (this.movemode == 'idle') {
                        if (this.pad.isTouching && this.pad.distance > 0.4) {
                            var angle = ~~((this.pad.angle + 180 + 45) / 90) % 4;
                            switch (angle) {
                                case 0:
                                    this.pdir = 'left';
                                    this.movemode = 'move-left';
                                    this.movems = this.movems == 0 ? movestep : this.movems;
                                    break;
                                case 1:
                                    this.pdir = 'up';
                                    this.movemode = 'move-up';
                                    this.movems = this.movems == 0 ? movestep : this.movems;
                                    break;
                                case 2:
                                    this.pdir = 'right';
                                    this.movemode = 'move-right';
                                    this.movems = this.movems == 0 ? movestep : this.movems;
                                    break;
                                case 3:
                                    this.pdir = 'down';
                                    this.movemode = 'move-down';
                                    this.movems = this.movems == 0 ? movestep : this.movems;
                                    break;
                            }
                        }
                        else {
                            this.movemode = "idle";
                            this.anim = 0;
                            this.movems = 0;
                            this.changed = false;
                        }
                    }
                    else if (this.movemode == "move-right") {
                        this.movems -= delta;
                        this.anim += delta;
                        if (this.movems <= 0) {
                            this.px += 1;
                            this.movemode = 'idle';
                            this.movems += movestep;
                        }
                        this.offx = 12 * (1 - this.movems / movestep);
                    }
                    else if (this.movemode == "move-left") {
                        this.movems -= delta;
                        this.anim += delta;
                        if (this.movems <= 0) {
                            this.px -= 1;
                            this.movemode = 'idle';
                            this.movems += movestep;
                        }
                        this.offx = -12 * (1 - this.movems / movestep);
                    }
                    else if (this.movemode == "move-down") {
                        this.movems -= delta;
                        this.anim += delta;
                        if (this.movems <= 0) {
                            this.py += 1;
                            this.movemode = 'idle';
                            this.movems += movestep;
                        }
                        this.offy = 12 * (1 - this.movems / movestep);
                    }
                    else if (this.movemode == "move-up") {
                        this.movems -= delta;
                        this.anim += delta;
                        if (this.movems <= 0) {
                            this.py -= 1;
                            this.movemode = 'idle';
                            this.movems += movestep;
                        }
                        this.offy = -12 * (1 - this.movems / movestep);
                    }
                    if (this.anim >= animstep * 4) {
                        this.anim -= animstep * 4;
                    }
                    Game.getDisplay().start();
                    // 
                    for (var l = 0; l < 3; l++) {
                        var gridw = this.map.config.gridsize.width;
                        var gridh = this.map.config.gridsize.height;
                        var lw = this.map.config.layer[l].chipsize.width;
                        var lh = this.map.config.layer[l].chipsize.height;
                        var lox = this.map.config.layer[l].renderoffset.x;
                        var loy = this.map.config.layer[l].renderoffset.y;
                        for (var y = 0; y < ~~(Game.getDisplay().height() / gridh); y++) {
                            for (var x = 0; x < ~~(Game.getDisplay().width() / gridw); x++) {
                                var chipid = this.map.config.chips[y][x];
                                if (this.map.config.layer[l].chip[chipid]) {
                                    var uv = [this.map.config.layer[l].chip[chipid].x, this.map.config.layer[l].chip[chipid].y, lw, lh];
                                    Game.getDisplay().rect({ left: 0 + x * gridw + lox, top: 0 + y * gridh + loy, width: lw, height: lh, texture: 'mapchip', uv: uv });
                                }
                            }
                        }
                        if (l == 2) {
                            var animf = ~~(((~~this.anim) + animstep - 1) / animstep) % 4;
                            Game.getDisplay().rect({ left: 0 + this.px * 12 + this.offx, top: 0 + this.py * 12 + this.offy, width: 47, height: 47, texture: 'charactor', uv: [this.psprite[this.pdir][animf][0], this.psprite[this.pdir][animf][1], 47, 47] });
                        }
                    }
                    Game.getDisplay().circle({ x: this.pad.x, y: this.pad.y, radius: this.pad.radius * 1.2, color: [1, 1, 1, 0.25] });
                    Game.getDisplay().circle({ x: this.pad.x + this.pad.cx, y: this.pad.y + this.pad.cy, radius: this.pad.radius, color: [1, 1, 1, 0.25] });
                    Game.getDisplay().end();
                },
                leave: function () {
                    document.removeEventListener("pointrwdown", this.inputId1);
                    document.removeEventListener("pointermove", this.inputId2);
                    document.removeEventListener("pointerup", this.inputId3);
                }
            }
        },
    }).then(() => Game.loadTextures({
        mapchip: './assets/mapchip.png',
        charactor: './assets/charactor.png'
    })).then(() => {
        Game.getSceneManager().push("dungeon");
        Game.getTimer().on((delta, now, id) => {
            Game.getSceneManager().update(delta, now);
        });
        Game.getTimer().start();
    });
};
//# sourceMappingURL=app.js.map