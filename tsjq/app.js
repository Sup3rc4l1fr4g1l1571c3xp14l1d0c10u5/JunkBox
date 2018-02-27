var Game;
(function (Game) {
    var canvas;
    var gl;
    class Shader {
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
    class VertexShader extends Shader {
        constructor(source) {
            super(gl.VERTEX_SHADER, source);
        }
    }
    class FragmentShader extends Shader {
        constructor(source) {
            super(gl.FRAGMENT_SHADER, source);
        }
    }
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
    const vertexShaderSource = `
		attribute vec2 a_position;
		attribute vec4 a_color;
		attribute vec2 a_texCoord;

		uniform vec2 u_resolution;

		varying vec2 v_texCoord;
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
    const fragmentShaderSource = `
		precision mediump float;

		// our texture
		uniform sampler2D u_image;

		// the texCoords passed in from the vertex shader.
		varying vec2 v_texCoord;
		varying vec4 v_color;

		void main() {
			gl_FragColor = texture2D(u_image, v_texCoord) * v_color;
		}
	`;
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
    var textures = {};
    function loadImage(src) {
        return new Promise((resolve, reject) => {
            let img = new Image();
            img.onload = () => { resolve(img); };
            img.src = src;
        });
    }
    function loadTextures(src) {
        return Promise.all(Object.keys(src).map((x) => loadImage(src[x]).then((img) => { textures[x] = new Texture(img); }))).then((x) => {
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
            gl.vertexAttribPointer(positionAttributeLocation, 2, gl.FLOAT, false, 8 * 4, 0 * 4);
            gl.vertexAttribPointer(colorAttributeLocation, 4, gl.FLOAT, false, 8 * 4, 2 * 4);
            gl.vertexAttribPointer(texCoordAttributeLocation, 2, gl.FLOAT, false, 8 * 4, 6 * 4);
            // set the resolution
            gl.uniform2f(resolutionUniformLocation, gl.canvas.width, gl.canvas.height);
            // enable alpha blend
            gl.enable(gl.BLEND);
            gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);
        }
        end() {
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
            var vertexBuffer = [
                // x, y, r, g, b, a, u, v
                x1, y1, color[0], color[1], color[2], color[3], u1, v1,
                x2, y1, color[0], color[1], color[2], color[3], u2, v1,
                x1, y2, color[0], color[1], color[2], color[3], u1, v2,
                x1, y2, color[0], color[1], color[2], color[3], u1, v2,
                x2, y1, color[0], color[1], color[2], color[3], u2, v1,
                x2, y2, color[0], color[1], color[2], color[3], u2, v2,
            ];
            // draw
            this.setTexture(texture);
            gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(vertexBuffer), gl.STREAM_DRAW);
            gl.drawArrays(gl.TRIANGLES, 0, 6);
        }
        toDisplayPos(x, y) {
            var cr = canvas.getBoundingClientRect();
            return [x - cr.left - window.scrollX, y - cr.top - window.scrollY];
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
    class Input {
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
            body.addEventListener('touchleave', pointerLeave);
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
                }
                if (evt['mouse']) {
                    evt['x'] = e.clientX + window.pageXOffset;
                    evt['y'] = e.clientY + window.pageYOffset;
                }
                evt['maskedEvent'] = e;
                tgt.dispatchEvent(evt);
                return evt;
            }
        }
        _dispatch(ev) {
            for (var key in this.listeners[ev.type]) {
                if (this.listeners[ev.type].hasOwnProperty(key)) {
                    this.listeners[ev.type][key].call(this.listeners[ev.type][key], ev, ev.type, key);
                    if (ev.defaultPrevented) {
                        return;
                    }
                }
            }
            ev.preventDefault();
        }
        on(event, listener) {
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
        off(event, id) {
            if (this.listeners[event] != null && this.listeners[event][id] != null) {
                delete this.listeners[event][id];
                return true;
            }
            else {
                return false;
            }
        }
    }
    var input = null;
    function run() {
        return new Promise((resolve, reject) => {
            var container = document.createElement('div');
            container.style.width = '100vw';
            container.style.height = '100vh';
            container.style.display = 'flex';
            container.style.justifyContent = 'center';
            container.style.alignItems = 'center';
            if (!container) {
                throw new Error("your browser is not support div.");
            }
            document.body.appendChild(container);
            canvas = document.createElement('canvas');
            if (!canvas) {
                throw new Error("your browser is not support canvas.");
            }
            canvas.width = 640;
            canvas.height = 480;
            container.appendChild(canvas);
            gl = canvas.getContext("webgl");
            if (!gl) {
                throw new Error("your browser is not support webgl.");
            }
            // create GLSL shaders, upload the GLSL source, compile the shaders
            var vertexShader = new VertexShader(vertexShaderSource);
            var fragmentShader = new FragmentShader(fragmentShaderSource);
            // Link the two shaders into a program
            var program = new ShaderProgram(vertexShader, fragmentShader);
            display = new Display(program);
            resolve();
        }).then(() => {
            input = new Input();
        });
    }
    Game.run = run;
    ;
    function getDisplay() {
        return display;
    }
    Game.getDisplay = getDisplay;
    function getTimer() {
        return timer;
    }
    Game.getTimer = getTimer;
    function getInput() {
        return input;
    }
    Game.getInput = getInput;
})(Game || (Game = {}));
window.onload = () => {
    Game.run()
        .then(() => Game.loadTextures({
        mapchip: './assets/mapchip.png',
        charactor: './assets/charactor.png'
    })).then(() => {
        var layerconfig = {
            0: { size: [12, 12], offset: [0, 0] },
            1: { size: [12, 24], offset: [0, -12] },
            2: { size: [12, 12], offset: [0, -24] },
        };
        var mapchip = {
            0: {
                1: [48, 0, 12, 12],
            },
            1: {
                0: [72, 84, 12, 24],
            },
            2: {
                0: [72, 72, 12, 12],
            },
        };
        var map = [];
        for (var y = 0; y < 30; y++) {
            map[y] = [];
            for (var x = 0; x < 30; x++) {
                map[y][x] = 0;
            }
        }
        for (var y = 10; y < 20; y++) {
            for (var x = 10; x < 20; x++) {
                map[y][x] = 1;
            }
        }
        var changed = true;
        var px = 0;
        var py = 0;
        var pdir = 'down';
        var psbasex = 752;
        var psbasey = 235;
        var psprite = {
            down: [[0, 0], [1, 0], [2, 0], [3, 0]].map(xy => [psbasex + 47 * xy[0], psbasey + 47 * xy[1]]),
            left: [[4, 0], [5, 0], [6, 0], [7, 0]].map(xy => [psbasex + 47 * xy[0], psbasey + 47 * xy[1]]),
            up: [[8, 0], [9, 0], [10, 0], [11, 0]].map(xy => [psbasex + 47 * xy[0], psbasey + 47 * xy[1]]),
            right: [[12, 0], [13, 0], [14, 0], [15, 0]].map(xy => [psbasex + 47 * xy[0], psbasey + 47 * xy[1]]),
        };
        var movestate = "idle";
        Game.getInput().on("pointerup", (ev, evtype, id) => {
            switch (movestate) {
                case "idle":
                    var p = Game.getDisplay().toDisplayPos(ev['x'], ev['y']);
                    px = p[0] / 24;
                    py = p[1] / 24;
                    changed = true;
                    break;
            }
        });
        Game.getTimer().on((delta, now, id) => {
            if (!changed) {
                return;
            }
            changed = false;
            Game.getDisplay().start();
            for (var l = 0; l < 3; l++) {
                var lw = layerconfig[l].size[0];
                var lh = layerconfig[l].size[1];
                var lox = layerconfig[l].offset[0];
                var loy = layerconfig[l].offset[1];
                for (var y = 0; y < 30; y++) {
                    for (var x = 0; x < 30; x++) {
                        var chipid = map[y][x];
                        if (mapchip[l][chipid]) {
                            Game.getDisplay().rect({ left: 0 + x * 12 + lox, top: 0 + y * 12 + loy, width: lw, height: lh, texture: 'mapchip', uv: mapchip[l][chipid] });
                        }
                    }
                }
                if (l == 1) {
                    Game.getDisplay().rect({ left: 0 + px * 24, top: 0 + py * 24, width: 47, height: 47, texture: 'charactor', uv: [psprite[pdir][0][0], psprite[pdir][0][1], 47, 47] });
                }
            }
            Game.getDisplay().end();
        });
        Game.getTimer().start();
    });
};
//# sourceMappingURL=app.js.map