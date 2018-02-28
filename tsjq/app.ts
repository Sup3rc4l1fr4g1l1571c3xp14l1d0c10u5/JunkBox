module Game {

    // Global Variables
    var canvas: HTMLCanvasElement;
    var gl: WebGLRenderingContext;
    var textures: { [key: string]: Texture } = {}

    module Shader {
        export class ShaderBase {
            shader: WebGLShader;
            constructor(type: number, source: string) {
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
        export class VertexShader extends ShaderBase {
            shader: WebGLShader;
            constructor(source: string) {
                super(gl.VERTEX_SHADER, source);
            }
        }
        export class FragmentShader extends ShaderBase {
            shader: WebGLShader;
            constructor(source: string) {
                super(gl.FRAGMENT_SHADER, source);
            }
        }
        export class ShaderProgram {
            // Link the two shaders into a program
            program: WebGLProgram;

            constructor(...shaders: ShaderBase[]) {
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

            getAttribLocation(name: string): number {
                return gl.getAttribLocation(this.program, name);
            }

            getUniformLocation(name: string): WebGLUniformLocation {
                return gl.getUniformLocation(this.program, name);
            }
        }
    }

    class Texture {
        texture: WebGLTexture;
        image: HTMLImageElement;

        constructor(image: HTMLImageElement) {
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

    const vertexShaderSource: string = `
attribute vec2 a_position;
attribute vec4 a_color;
attribute vec3 a_texCoord;

uniform vec2 u_resolution;

varying vec3 v_texCoord;
varying vec4 v_color;

void main() {
// convert the rectangle from pixels to 0.0 to 1.0
vec2 zeroToOne = a_position / u_resolution;

// convert from 0->1 to 0->2
vec2 zeroToTwo = zeroToOne * 2.0;

// convert from 0->2 to -1->+1 (clipspace)
vec2 clipSpace = zeroToTwo - 1.0;

gl_Position = vec4(clipSpace * vec2(1, -1), 0, 1);

// pass the texCoord to the fragment shader
// The GPU will interpolate this value between points.
v_texCoord = a_texCoord;
v_color = a_color;

}
`;

    const fragmentShaderSource: string = `
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

    export function loadTextures(asserts: { [id: string]: string }): Promise<boolean> {
        return Promise.all(
            Object.keys(asserts).map((x) => new Promise<void>((resolve, reject) => {
                let img = new Image();
                img.onload = () => { textures[x] = new Texture(img); resolve(); };
                img.src = asserts[x];
            }))
        ).then((x) => {
            return true;
        });
    }

    class Display {
        program: Shader.ShaderProgram;
        positionBuffer: WebGLBuffer;

        constructor(shaderProgram: Shader.ShaderProgram) {
            this.program = shaderProgram;
            // Create a buffer and put three 2d clip space points in it
            this.positionBuffer = gl.createBuffer();
        }
        start(): void {
            // look up where the vertex data needs to go.
            var positionAttributeLocation = this.program.getAttribLocation("a_position");
            var colorAttributeLocation = this.program.getAttribLocation("a_color");
            var texCoordAttributeLocation = this.program.getAttribLocation("a_texCoord");

            // look up uniform locations
            var resolutionUniformLocation = this.program.getUniformLocation("u_resolution");

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
            gl.vertexAttribPointer(   colorAttributeLocation, 4, gl.FLOAT, false, 9 * 4, 2 * 4);
            gl.vertexAttribPointer(texCoordAttributeLocation, 3, gl.FLOAT, false, 9 * 4, 6 * 4);

            // set the resolution
            gl.uniform2f(resolutionUniformLocation, gl.canvas.width, gl.canvas.height);

            // enable alpha blend
            gl.enable(gl.BLEND);
            gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);
        }
        end(): void {

        }


        setTexture(id: string): boolean {
            var texture = textures[id];
            if (texture != null) {
                gl.activeTexture(gl.TEXTURE0);
                gl.bindTexture(gl.TEXTURE_2D, texture.texture);
                return true;
            } else {
                return false;
            }
        }

        rect({ left, top, width, height, color = [1, 1, 1, 1], texture = null, uv = [0, 0, 1, 1] }: { left: number, top: number, width: number, height: number, color?: number[], texture?: string, uv?: number[] }): void {

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
        circle({ x, y, radius, color = [1, 1, 1, 1]/*, texture = null, uv = [0, 0, 1, 1]*/ }: { x: number, y: number, radius: number, color?: number[] /*, texture?: string, uv?: number[]*/ }): void {
            var num_segments = ~~(10 * Math.sqrt(radius));
            var theta = Math.PI * 2 / num_segments;
            var c = Math.cos(theta);    //precalculate the sine and cosine
            var s = Math.sin(theta);

            var x1 = radius;  //we start at angle = 0 
            var y1 = 0;

            var vertexBuffer = [];
            for (var ii = 0; ii < num_segments; ii++) 
            {
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
            gl.drawArrays(gl.TRIANGLES, 0, num_segments*3);

        }

        toDisplayPos(x: number, y: number): number[] {
            var cr = canvas.getBoundingClientRect();
            return [x - cr.left, y - cr.top];
        }
    }

    var display: Display = null;

    class AnimationTimer {
        id: number;
        prevTime: number;
        listenerId: number;
        listeners: { [id: number]: ((delta: number, now: number, id: number) => void) };

        constructor() {
            this.id = NaN;
            this.prevTime = NaN;
            this.listenerId = 0;
            this.listeners = {};
        }
        start(): boolean {
            if (!isNaN(this.id)) {
                stop();
            }
            this.id = requestAnimationFrame(this.tick.bind(this));
            return !isNaN(this.id);
        }
        stop(): void {
            if (!isNaN(this.id)) {
                cancelAnimationFrame(this.id);
                this.id = NaN;
            }
        }
        tick(ts: number) {
            requestAnimationFrame(this.tick.bind(this))
            if (!isNaN(this.prevTime)) {
                var delta = ts - this.prevTime;
                for (var key in this.listeners) {
                    if (this.listeners.hasOwnProperty(key)) {
                        this.listeners[key].call(this.listeners[key], delta, ts, key)
                    }
                }
            }
            this.prevTime = ts;
        }
        on(listener: ((delta: number, now: number, id: number) => void)): number {
            var id = this.listenerId;
            while (this.listeners[id] != null) {
                id++;
            }
            this.listenerId = id + 1;
            this.listeners[id] = listener;
            return id;
        }
        off(id: number): boolean {
            if (this.listeners[id] != null) {
                delete this.listeners[id];
                return true;
            } else {
                return false;
            }
        }
    }

    var timer = new AnimationTimer();


    class Input {
        listenerId: number;
        listeners: { [event: string]: { [id: number]: ((ev: Event, evtype: string, id: number) => void) } };

        constructor() {
            this.listenerId = 0;
            this.listeners = {};
            this.setup();
            canvas.addEventListener('pointerdown', (ev) => this._dispatch(ev));
            canvas.addEventListener('pointermove', (ev) => this._dispatch(ev));
            canvas.addEventListener('pointerup', (ev) => this._dispatch(ev));
            canvas.addEventListener('pointerleave', (ev) => this._dispatch(ev));
        }
        setup() {
            var body = document.body;

            var isScrolling = false;
            var timeout = 0;
            var sDistX = 0;
            var sDistY = 0;

            window.addEventListener('scroll', function () {
                if (!isScrolling) {
                    sDistX = window.pageXOffset;
                    sDistY = window.pageYOffset;
                }
                isScrolling = true;
                clearTimeout(timeout);
                timeout = setTimeout(function () {
                    isScrolling = false;
                    sDistX = 0;
                    sDistY = 0;
                }, 100);
            });

            body.addEventListener('mousedown', pointerDown);
            body.addEventListener('touchstart', pointerDown);
            body.addEventListener('mouseup', pointerUp);
            body.addEventListener('touchend', pointerUp);
            body.addEventListener('mousemove', pointerMove);
            body.addEventListener('touchmove', pointerMove);
            body.addEventListener('mouseout', pointerLeave);
            body.addEventListener('touchcancel', pointerLeave);

            // 'pointerdown' event, triggered by mousedown/touchstart.
            function pointerDown(e) {
                var evt = makePointerEvent('down', e);
                // don't maybeClick if more than one touch is active.
                var singleFinger = evt['mouse'] || (evt['touch'] && e.touches.length === 1);
                if (!isScrolling && singleFinger) {
                    e.target.maybeClick = true;
                    e.target.maybeClickX = evt['x'];
                    e.target.maybeClickY = evt['y'];
                }
                e.preventDefault();

            }

            // 'pointerdown' event, triggered by mouseout/touchleave.
            function pointerLeave(e) {
                e.target.maybeClick = false;
                makePointerEvent('leave', e);
            }

            // 'pointermove' event, triggered by mousemove/touchmove.
            function pointerMove(e) {
                var evt = makePointerEvent('move', e);
            }

            // 'pointerup' event, triggered by mouseup/touchend.
            function pointerUp(e) {
                var evt = makePointerEvent('up', e);
                // Does our target have maybeClick set by pointerdown?
                if (e.target.maybeClick) {
                    // Have we moved too much?
                    if (Math.abs(e.target.maybeClickX - evt['x']) < 5 &&
                        Math.abs(e.target.maybeClickY - evt['y']) < 5) {
                        // Have we scrolled too much?
                        if (!isScrolling ||
                            (Math.abs(sDistX - window.pageXOffset) < 5 &&
                                Math.abs(sDistY - window.pageYOffset) < 5)) {
                            makePointerEvent('click', e);
                        }
                    }
                }
                e.target.maybeClick = false;
            }

            function makePointerEvent(type, e) {
                var tgt = e.target;
                var evt = document.createEvent('CustomEvent');
                evt.initCustomEvent('pointer' + type, true, true, {});
                evt['touch'] = e.type.indexOf('touch') === 0;
                evt['mouse'] = e.type.indexOf('mouse') === 0;
                if (evt['touch']) {
                    evt['x'] = e.changedTouches[0].pageX;
                    evt['y'] = e.changedTouches[0].pageY;
                    console.log(["makePointerEvent::touch", evt['x'], evt['y']]);
                }
                if (evt['mouse']) {
                    evt['x'] = e.clientX + window.pageXOffset;
                    evt['y'] = e.clientY + window.pageYOffset;
                    console.log(["makePointerEvent::mouse", evt['x'], evt['y']]);
                }
                evt['maskedEvent'] = e;
                console.log(["makePointerEvent",type,evt]);
                tgt.dispatchEvent(evt);
                return evt;
            }
        }
        _dispatch(ev: PointerEvent) {
            for (var key in this.listeners[ev.type]) {
                if (this.listeners[ev.type].hasOwnProperty(key)) {
                    console.log(["dispatch", ev.type, key, ev]);
                    this.listeners[ev.type][key](ev, ev.type, ~~key);
                    if (ev.defaultPrevented) {
                        return;
                    }
                }
            }
            ev.preventDefault();
        }
        on(event: string, listener: ((ev: Event, evtype: string, id: number) => void)): number {
            var id = this.listenerId;
            if (this.listeners[event] == null) {
                this.listeners[event] = {};
            }
            while (this.listeners[event][id] != null) {
                id++;
            }
            this.listenerId = id + 1;
            this.listeners[event][id] = listener;
            return id;
        }
        off(event: string, id: number): boolean {
            if (this.listeners[event] != null && this.listeners[event][id] != null) {
                delete this.listeners[event][id];
                return true;
            } else {
                return false;
            }
        }

    }

    type SceneConfigParam = {
        enter?: (data:any) => void;
        update?: (delta:number, now:number) => void;
        leave?: () => void;
        suspend?: () => void;
        resume?: () => void;
    };

    class Scene {

        name: string;

        update: () => void;
        enter: (any) => void;
        leave: () => void;
        suspend: () => void;
        resume: () => void;

        parentScene: Scene;

        constructor(name: string, config: SceneConfigParam = {}) {
            this.name = name;
            this.update = config.update == null ? () => { } : config.update.bind(this);
            this.enter = config.enter == null ? () => { } : config.enter.bind(this);
            this.leave = config.leave == null ? () => { } : config.leave.bind(this);
            this.suspend = config.suspend == null ? () => { } : config.suspend.bind(this);
            this.resume = config.resume == null ? () => { } : config.resume.bind(this);
            this.parentScene = null;
        }
    }
    class SceneManager {
        scenes: { [name: string]: (() => Scene) };
        currentScene: Scene;
        constructor(config) {
            this.scenes = {};
            this.currentScene = null;
            Object.keys(config).forEach((key) => {
                this.scenes[key] = () => new Scene(key, config[key]);
            })
        }
        pushScene(name: string, data: any) {
            var newScene = this.scenes[name]();
            newScene.parentScene = this.currentScene;
            if (this.currentScene != null) {
                this.currentScene.suspend();
            }
            this.currentScene = newScene;
            newScene.enter(data);
        }
        popScene() {
            var oldScene = this.currentScene;
            this.currentScene = oldScene.parentScene;
            oldScene.leave();
            if (this.currentScene != null) {
                this.currentScene.resume();
            }
        }
        update(delta, ms) {
            this.currentScene.update.call(this.currentScene, delta, ms);
        }
    }

    var input: Input = null;
    var sceneManager: SceneManager = null;

    export function Create(config: { display: { width: number; height: number; }, scene: { [name in string]: SceneConfigParam }}) {
        return new Promise<void>((resolve, reject) => {

            var container = document.createElement('div');
            container.style.width = '100vw';
            container.style.height = '100vh';
            container.style.display = 'flex';
            container.style.justifyContent = 'center';
            container.style.alignItems = 'center';
            container.style.padding = '0';
            if (!container) {
                throw new Error("your browser is not support div.");
            }
            document.body.appendChild(container);

            canvas = document.createElement('canvas');
            if (!canvas) {
                throw new Error("your browser is not support canvas.");
            }
            canvas.width = config.display.width || 256;
            canvas.height = config.display.height || 256;
            container.appendChild(canvas);


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

            input = new Input();
            sceneManager = new SceneManager(config.scene);
            resolve();
        });
    };

    export function getDisplay(): Display {
        return display;
    }
    export function getTimer(): AnimationTimer {
        return timer;
    }
    export function getInput(): Input {
        return input;
    }
    export function getSceneManager(): SceneManager {
        return sceneManager;
    }
}

class Pad {
    isTouching: boolean;
    x: number;
    y: number;
    cx: number;
    cy: number;
    innerColor: number[];
    outerColor: number[];
    innerRadius: number;
    outerRadius: number;
    distance: number;
    angle: number;

    constructor(x: number = 120, y: number = 120) {
        this.isTouching = false;
        this.x = x;
        this.y = y;
        this.cx = 0;
        this.cy = 0;
        this.innerColor = [1, 1, 1, 0.75];
        this.outerColor = [0.2, 0.2, 0.2, 0.75];
        this.innerRadius = 40;
        this.outerRadius = 60;
        this.distance = 0;
        this.angle = 0;
    }

    onpointingstart(ev) {
        this.isTouching = true;
        this.cx = 0;
        this.cy = 0;
        this.angle = 0;
        this.distance = 0;
        console.log(["onpointingstart", ev]);
    }

    onpointingend(ev) {
        this.isTouching = false;
        this.cx = 0;
        this.cy = 0;
        this.angle = 0;
        this.distance = 0;
        console.log(["onpointingend", ev]);
    }

    onpointingmove(ev,x:number, y:number) {
        if (this.isTouching == false) {
            return;
        }
        var dx = x - this.x;
        var dy = y - this.y;
        console.log(["onpointingmove", ev, dx, dy]);
        var len = Math.sqrt((dx * dx) + (dy * dy));
        if (len > 0) {
            dx /= len;
            dy /= len;
            if (len > 40) { len = 40; }

            this.angle = Math.atan2(dy, dx) * 180 / Math.PI;
            this.distance = len / 40.0;
            this.cx = dx * len;
            this.cy = dy * len;
        } else {
            this.cx = 0;
            this.cy = 0;
            this.angle = 0;
            this.distance = 0;
        }
    }

}

window.onload = () => {
    Game.Create({
        display: {
            width: 512,
            height: 512,
        },
        scene: {
            dungeon: {
                enter: function (data) {
                    this.layerconfig = {
                        0: { size: [24, 24], offset: [0, 0] },
                        1: { size: [24, 36], offset: [0, -12] },
                        2: { size: [24, 24], offset: [0, -36] },
                    }
                    this.mapchip = {
                        0: {
                            1: [48, 0, 24, 24],
                        },
                        1: {
                            0: [96, 96, 24, 36],
                        },
                        2: {
                            0: [96, 72, 24, 24],
                        },
                    };

                    this.map = [];
                    for (var y = 0; y < ~~(512 / 24); y++) {
                        this.map[y] = []
                        for (var x = 0; x < ~~(512 / 24); x++) {
                            this.map[y][x] = 0;
                        }
                    }

                    for (var y = ~~(512 / 24 / 2) - 5; y <= ~~(512 / 24 / 2) + 5; y++) {
                        for (var x = ~~(512 / 24 / 2) - 5; x <= ~~(512 / 24 / 2) + 5; x++) {
                            this.map[y][x] = 1;
                        }
                    }

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
                    this.inputId1 = Game.getInput().on("pointerdown", (ev, evtype, id) => {
                        var pos = Game.getDisplay().toDisplayPos(ev['x'], ev['y']);
                        console.log(pos);
                        this.pad.x = pos[0];
                        this.pad.y = pos[1];
                        this.pad.onpointingstart(ev);
                        this.changed = true;
                    })
                    this.inputId2 = Game.getInput().on("pointermove", (ev, evtype, id) => {
                        var pos = Game.getDisplay().toDisplayPos(ev['x'], ev['y']);
                        console.log(pos);
                        this.pad.onpointingmove(ev,pos[0], pos[1]);
                        this.changed = true;
                    })
                    this.inputId3 = Game.getInput().on("pointerup", (ev, evtype, id) => {
                        this.pad.onpointingend(ev);
                        this.changed = true;
                    })
                    this.inputId4 = Game.getInput().on("pointerleave", (ev, evtype, id) => {
                        this.pad.onpointingend(ev);
                        this.changed = true;
                    })

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
                                case 0: this.pdir = 'left'; this.movemode = 'move-left'; this.movems = this.movems == 0 ? movestep : this.movems; break;
                                case 1: this.pdir = 'up'; this.movemode = 'move-up'; this.movems = this.movems == 0 ? movestep : this.movems; break;
                                case 2: this.pdir = 'right'; this.movemode = 'move-right'; this.movems = this.movems == 0 ? movestep : this.movems; break;
                                case 3: this.pdir = 'down'; this.movemode = 'move-down'; this.movems = this.movems == 0 ? movestep : this.movems; break;
                            }
                        } else {
                            this.movemode = "idle";
                            this.anim = 0;
                            this.movems = 0;
                            this.changed = false;
                        }
                    } else if (this.movemode == "move-right") {
                        
                        this.movems -= delta;
                        this.anim += delta;
                        if (this.movems <= 0) {
                            this.px += 1;
                            this.movemode = 'idle';
                            this.movems += movestep
                        }
                        this.offx = 12 * (1 - this.movems / movestep);
                    } else if (this.movemode == "move-left") {
                        this.movems -= delta;
                        this.anim += delta;
                        if (this.movems <= 0) {
                            this.px -= 1;
                            this.movemode = 'idle';
                            this.movems += movestep
                        }
                        this.offx = -12 * (1 - this.movems / movestep);
                    } else if (this.movemode == "move-down") {
                        this.movems -= delta;
                        this.anim += delta;
                        if (this.movems <= 0) {
                            this.py += 1;
                            this.movemode = 'idle';
                            this.movems += movestep
                        }
                        this.offy = 12 * (1 - this.movems / movestep);
                    } else if (this.movemode == "move-up") {
                        this.movems -= delta;
                        this.anim += delta;
                        if (this.movems <= 0) {
                            this.py -= 1;
                            this.movemode = 'idle';
                            this.movems += movestep
                        }
                        this.offy = -12 * (1 - this.movems / movestep);
                    }
                    if (this.anim >= animstep * 4) {
                        this.anim -= animstep * 4;
                    }

                    Game.getDisplay().start();
                    for (var l = 0; l < 3; l++) {
                        var lw = this.layerconfig[l].size[0];
                        var lh = this.layerconfig[l].size[1];
                        var lox = this.layerconfig[l].offset[0];
                        var loy = this.layerconfig[l].offset[1];
                        for (var y = 0; y < ~~(512 / 24); y++) {
                            for (var x = 0; x < ~~(512 / 24); x++) {
                                var chipid = this.map[y][x];
                                if (this.mapchip[l][chipid]) {
                                    Game.getDisplay().rect({ left: 0 + x * 24 + lox, top: 0 + y * 24 + loy, width: lw, height: lh, texture: 'mapchip', uv: this.mapchip[l][chipid] });
                                }
                            }
                        }
                        if (l == 2) {
                            var animf = ~~(((~~this.anim) + animstep-1) / animstep) % 4;
                            Game.getDisplay().rect({ left: 0 + this.px * 12 + this.offx, top: 0 + this.py * 12 + this.offy, width: 47, height: 47, texture: 'charactor', uv: [this.psprite[this.pdir][animf][0], this.psprite[this.pdir][animf][1], 47, 47] });
                        }
                    }
                    
                    Game.getDisplay().circle({ x: this.pad.x, y: this.pad.y, radius: this.pad.outerRadius, color: this.pad.outerColor });
                    Game.getDisplay().circle({ x: this.pad.x + this.pad.cx, y: this.pad.y+this.pad.cy, radius: this.pad.innerRadius, color: this.pad.innerColor });

                    Game.getDisplay().end();
                },
                leave: function () {
                    Game.getInput().off("pointerup", this.inputId);
                }
            }
        },
    }).then(() => Game.loadTextures({
        mapchip: './assets/mapchip.png',
        charactor: './assets/charactor.png'
    })).then(() => {
        Game.getSceneManager().pushScene("dungeon", null);
        Game.getTimer().on((delta, now, id) => {
            Game.getSceneManager().update(delta, now);
        });
        Game.getTimer().start();
    });
};