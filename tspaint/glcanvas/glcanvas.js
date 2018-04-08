"use strict";
var m3;
(function (m3) {
    function projection(width, height) {
        // Note: This matrix flips the Y axis so that 0 is at the top.
        return [
            2 / width, 0, 0,
            0, 2 / height, 0,
            -1, -1, 1
        ];
    }
    m3.projection = projection;
    function identity() {
        return [
            1, 0, 0,
            0, 1, 0,
            0, 0, 1
        ];
    }
    m3.identity = identity;
    function translation(tx, ty) {
        return [
            1, 0, 0,
            0, 1, 0,
            tx, ty, 1
        ];
    }
    m3.translation = translation;
    function rotation(angleInRadians) {
        const c = Math.cos(angleInRadians);
        const s = Math.sin(angleInRadians);
        return [
            c, -s, 0,
            s, c, 0,
            0, 0, 1
        ];
    }
    m3.rotation = rotation;
    function scaling(sx, sy) {
        return [
            sx, 0, 0,
            0, sy, 0,
            0, 0, 1
        ];
    }
    m3.scaling = scaling;
    function multiply(a, b) {
        const a00 = a[0 * 3 + 0];
        const a01 = a[0 * 3 + 1];
        const a02 = a[0 * 3 + 2];
        const a10 = a[1 * 3 + 0];
        const a11 = a[1 * 3 + 1];
        const a12 = a[1 * 3 + 2];
        const a20 = a[2 * 3 + 0];
        const a21 = a[2 * 3 + 1];
        const a22 = a[2 * 3 + 2];
        const b00 = b[0 * 3 + 0];
        const b01 = b[0 * 3 + 1];
        const b02 = b[0 * 3 + 2];
        const b10 = b[1 * 3 + 0];
        const b11 = b[1 * 3 + 1];
        const b12 = b[1 * 3 + 2];
        const b20 = b[2 * 3 + 0];
        const b21 = b[2 * 3 + 1];
        const b22 = b[2 * 3 + 2];
        return [
            b00 * a00 + b01 * a10 + b02 * a20,
            b00 * a01 + b01 * a11 + b02 * a21,
            b00 * a02 + b01 * a12 + b02 * a22,
            b10 * a00 + b11 * a10 + b12 * a20,
            b10 * a01 + b11 * a11 + b12 * a21,
            b10 * a02 + b11 * a12 + b12 * a22,
            b20 * a00 + b21 * a10 + b22 * a20,
            b20 * a01 + b21 * a11 + b22 * a21,
            b20 * a02 + b21 * a12 + b22 * a22
        ];
    }
    m3.multiply = multiply;
})(m3 || (m3 = {}));
class Shader {
    constructor(gl, shader, shaderType) {
        this.gl = gl;
        this.shader = shader;
        this.shaderType = shaderType;
    }
    static create(gl, shaderSource, shaderType) {
        // Create the shader object
        const shader = gl.createShader(shaderType);
        // Load the shader source
        gl.shaderSource(shader, shaderSource);
        // Compile the shader
        gl.compileShader(shader);
        // Check the compile status
        const compiled = gl.getShaderParameter(shader, gl.COMPILE_STATUS);
        if (!compiled) {
            // Something went wrong during compilation; get the error
            const lastError = gl.getShaderInfoLog(shader);
            console.error("*** Error compiling shader '" + shader + "':" + lastError);
            gl.deleteShader(shader);
            return null;
        }
        return new Shader(gl, shader, shaderType);
    }
    static loadShaderById(gl, id, optShaderType) {
        const shaderScript = document.getElementById(id);
        if (!shaderScript) {
            throw ("*** Error: unknown element `" + id + "`");
        }
        const shaderSource = shaderScript.text;
        let shaderType = optShaderType;
        if (!optShaderType) {
            if (shaderScript.type === "x-shader/x-vertex") {
                shaderType = gl.VERTEX_SHADER;
            }
            else if (shaderScript.type === "x-shader/x-fragment") {
                shaderType = gl.FRAGMENT_SHADER;
            }
            else if (shaderType !== gl.VERTEX_SHADER && shaderType !== gl.FRAGMENT_SHADER) {
                throw ("*** Error: unknown shader type");
            }
        }
        return Shader.create(gl, shaderSource, shaderType);
    }
}
class Program {
    constructor(gl, program) {
        this.gl = gl;
        this.program = program;
    }
    static create(gl, shaders, optAttribs, optLocations) {
        const program = gl.createProgram();
        shaders.forEach(shader => {
            gl.attachShader(program, shader);
        });
        if (optAttribs) {
            optAttribs.forEach((attrib, ndx) => {
                gl.bindAttribLocation(program, optLocations ? optLocations[ndx] : ndx, attrib);
            });
        }
        gl.linkProgram(program);
        // Check the link status
        const linked = gl.getProgramParameter(program, gl.LINK_STATUS);
        if (!linked) {
            // something went wrong with the link
            const lastError = gl.getProgramInfoLog(program);
            console.error("Error in program linking:" + lastError);
            gl.deleteProgram(program);
            return null;
        }
        return new Program(gl, program);
    }
    static loadShaderById(gl, ids, shaderTypes, optAttribs, optLocations) {
        const shaders = [];
        for (let ii = 0; ii < ids.length; ++ii) {
            shaders.push(Shader.loadShaderById(gl, ids[ii], shaderTypes[ii]));
        }
        return Program.create(gl, shaders.map((x) => x.shader), optAttribs, optLocations);
    }
}
class OffscreenTarget {
    static createAndBindTexture(gl) {
        const texture = gl.createTexture();
        gl.activeTexture(gl.TEXTURE0);
        gl.bindTexture(gl.TEXTURE_2D, texture);
        // Set up texture so we can render any size image and so we are
        // working with pixels.
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.NEAREST);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.NEAREST);
        return texture;
    }
    constructor(gl, srcImageOrWidth, height) {
        this.gl = gl;
        // �t���[���o�b�t�@�e�N�X�`���𐶐��B�s�N�Z���t�H�[�}�b�g��RGBA(8bit)�B�摜�T�C�Y�͎w�肳�ꂽ��̂�g�p�B
        this.texture = OffscreenTarget.createAndBindTexture(gl);
        if (srcImageOrWidth instanceof HTMLImageElement) {
            const img = srcImageOrWidth;
            this.width = img.width;
            this.height = img.height;
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, img);
        }
        else {
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, srcImageOrWidth, height, 0, gl.RGBA, gl.UNSIGNED_BYTE, null);
            this.width = srcImageOrWidth;
            this.height = height;
        }
        // �t���[���o�b�t�@�𐶐����A�e�N�X�`���Ɗ֘A�Â�
        this.framebuffer = gl.createFramebuffer();
        gl.bindFramebuffer(gl.FRAMEBUFFER, this.framebuffer);
        gl.framebufferTexture2D(gl.FRAMEBUFFER, gl.COLOR_ATTACHMENT0, gl.TEXTURE_2D, this.texture, 0);
    }
    clear() {
        const gl = this.gl;
        gl.bindFramebuffer(gl.FRAMEBUFFER, this.framebuffer);
        gl.viewport(0, 0, this.width, this.height);
        gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT | gl.STENCIL_BUFFER_BIT);
    }
}
class PixelBuffer {
    constructor(gl, width, height) {
        this.gl = gl;
        this.width = width;
        this.height = height;
        this.pbo = gl.createBuffer();
        this.data = new Uint8ClampedArray(this.width * this.height * 4);
        gl.bindBuffer(gl.PIXEL_PACK_BUFFER, this.pbo);
        gl.bufferData(gl.PIXEL_PACK_BUFFER, this.width * this.height * 4, gl.STREAM_READ);
        gl.bindBuffer(gl.PIXEL_PACK_BUFFER, null);
    }
    capture(src) {
        const gl = this.gl;
        gl.bindFramebuffer(gl.FRAMEBUFFER, src.framebuffer);
        // �t���[���o�b�t�@��s�N�Z���o�b�t�@�Ƀ��[�h
        gl.bindBuffer(gl.PIXEL_PACK_BUFFER, this.pbo);
        gl.readPixels(0, 0, this.width, this.height, gl.RGBA, gl.UNSIGNED_BYTE, 0);
        // �s�N�Z���o�b�t�@����CPU���̔z��Ƀ��[�h
        gl.fenceSync(gl.SYNC_GPU_COMMANDS_COMPLETE, 0);
        gl.getBufferSubData(gl.PIXEL_PACK_BUFFER, 0, this.data);
        gl.bindBuffer(gl.PIXEL_PACK_BUFFER, null);
        return this.data;
    }
}
class ImageProcessing {
    // �t�B���^�J�[�l���̏d�ݍ��v�i�d�ݍ��v��0�ȉ��̏ꍇ��1�Ƃ���j
    static computeKernelWeight(kernel) {
        const weight = kernel.reduce((prev, curr) => prev + curr);
        return weight <= 0 ? 1 : weight;
    }
    constructor(gl) {
        this.gl = gl;
        // ���_�o�b�t�@�i���W�j��쐬
        this.positionBuffer = gl.createBuffer();
        gl.bindBuffer(gl.ARRAY_BUFFER, this.positionBuffer);
        // ���_�o�b�t�@�i�e�N�X�`�����W�j��쐬
        this.texcoordBuffer = gl.createBuffer();
        gl.bindBuffer(gl.ARRAY_BUFFER, this.texcoordBuffer);
    }
    setVertexPosition(array) {
        const gl = this.gl;
        gl.bindBuffer(gl.ARRAY_BUFFER, this.positionBuffer);
        gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(array), gl.STATIC_DRAW);
    }
    setTexturePosition(array) {
        const gl = this.gl;
        gl.bindBuffer(gl.ARRAY_BUFFER, this.texcoordBuffer);
        gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(array), gl.STATIC_DRAW);
    }
    createBlankOffscreenTarget(width, height) {
        return new OffscreenTarget(this.gl, width, height);
    }
    createOffscreenTargetFromImage(image) {
        return new OffscreenTarget(this.gl, image);
    }
    static createRectangle(x1, y1, x2, y2, target) {
        if (target) {
            Array.prototype.push.call(target, x1, y1, x2, y1, x1, y2, x1, y2, x2, y1, x2, y2);
            return target;
        }
        else {
            return [
                x1, y1,
                x2, y1,
                x1, y2,
                x1, y2,
                x2, y1,
                x2, y2
            ];
        }
    }
    applyKernel(dst, src, { kernel = null, program = null }) {
        const gl = this.gl;
        // arrtibute�ϐ��̈ʒu��擾
        const positionLocation = gl.getAttribLocation(program.program, "a_position");
        const texcoordLocation = gl.getAttribLocation(program.program, "a_texCoord");
        // uniform�ϐ��̈ʒu��擾
        //const resolutionLocation = gl.getUniformLocation(program.program, "u_resolution");
        const matrixLocation = gl.getUniformLocation(program.program, "u_matrix");
        const textureSizeLocation = gl.getUniformLocation(program.program, "u_textureSize");
        const kernelLocation = gl.getUniformLocation(program.program, "u_kernel[0]");
        const kernelWeightLocation = gl.getUniformLocation(program.program, "u_kernelWeight");
        const texture0Lication = gl.getUniformLocation(program.program, 'texture0');
        // �V�F�[�_��ݒ�
        gl.useProgram(program.program);
        // �V�F�[�_�̒��_���WAttribute��L����
        gl.enableVertexAttribArray(positionLocation);
        // ���_�o�b�t�@�i���W�j��ݒ�
        this.setVertexPosition(ImageProcessing.createRectangle(0, 0, src.width, src.height));
        // ���_���WAttribute�̈ʒu����ݒ�
        gl.vertexAttribPointer(positionLocation, 2, // 2 components per iteration
        gl.FLOAT, // the data is 32bit floats
        false, // don't normalize the data
        0, // 0 = move forward size * sizeof(type) each iteration to get the next position
        0 // start at the beginning of the buffer
        );
        // �V�F�[�_�̃e�N�X�`�����WAttribute��L����
        gl.enableVertexAttribArray(texcoordLocation);
        // ���_�o�b�t�@�i�e�N�X�`�����W�j��ݒ�
        this.setTexturePosition(ImageProcessing.createRectangle(0, 0, 1, 1));
        // �e�N�X�`�����WAttribute�̈ʒu����ݒ�
        gl.vertexAttribPointer(texcoordLocation, 2, // 2 components per iteration
        gl.FLOAT, // the data is 32bit floats
        false, // don't normalize the data
        0, // 0 = move forward size * sizeof(type) each iteration to get the next position
        0 // start at the beginning of the buffer
        );
        // �ϊ��s���ݒ�
        const projectionMatrix = m3.projection(dst.width, dst.height);
        gl.uniformMatrix3fv(matrixLocation, false, projectionMatrix);
        // �t�B���^���Z�Ŏg�����̓e�N�X�`���T�C�Y��ݒ�
        gl.uniform2f(textureSizeLocation, src.width, src.height);
        // ���͌��Ƃ��郌���_�����O�^�[�Q�b�g�̃e�N�X�`����I��
        gl.activeTexture(gl.TEXTURE0);
        gl.bindTexture(gl.TEXTURE_2D, src.texture);
        gl.uniform1i(texture0Lication, 0);
        // �o�͐�Ƃ��郌���_�����O�^�[�Q�b�g�̃t���[���o�b�t�@��I����A�r���[�|�[�g��ݒ�
        gl.bindFramebuffer(gl.FRAMEBUFFER, dst.framebuffer);
        gl.viewport(0, 0, dst.width, dst.height);
        // �J�[�l���V�F�[�_��K�p�����I�t�X�N���[�������_�����O����s
        gl.uniform1fv(kernelLocation, kernel);
        gl.uniform1f(kernelWeightLocation, ImageProcessing.computeKernelWeight(kernel));
        gl.drawArrays(gl.TRIANGLES, 0, 6);
        // �t�B���^�K�p����
    }
    applyShader(dst, src, { program = null }) {
        const gl = this.gl;
        // arrtibute�ϐ��̈ʒu��擾
        const positionLocation = gl.getAttribLocation(program.program, "a_position");
        const texcoordLocation = gl.getAttribLocation(program.program, "a_texCoord");
        // uniform�ϐ��̈ʒu��擾
        const matrixLocation = gl.getUniformLocation(program.program, "u_matrix");
        const texture0Lication = gl.getUniformLocation(program.program, 'texture0');
        // �V�F�[�_��ݒ�
        gl.useProgram(program.program);
        // �V�F�[�_�̒��_���WAttribute��L����
        gl.enableVertexAttribArray(positionLocation);
        // ���_�o�b�t�@�i���W�j��ݒ�
        this.setVertexPosition(ImageProcessing.createRectangle(0, 0, src.width, src.height));
        // ���_���WAttribute�̈ʒu����ݒ�
        gl.vertexAttribPointer(positionLocation, 2, // 2 components per iteration
        gl.FLOAT, // the data is 32bit floats
        false, // don't normalize the data
        0, // 0 = move forward size * sizeof(type) each iteration to get the next position
        0 // start at the beginning of the buffer
        );
        // �V�F�[�_�̃e�N�X�`�����WAttribute��L����
        gl.enableVertexAttribArray(texcoordLocation);
        // ���_�o�b�t�@�i�e�N�X�`�����W�j��ݒ�
        this.setTexturePosition(ImageProcessing.createRectangle(0, 0, 1, 1));
        // �e�N�X�`�����WAttribute�̈ʒu����ݒ�
        gl.vertexAttribPointer(texcoordLocation, 2, // 2 components per iteration
        gl.FLOAT, // the data is 32bit floats
        false, // don't normalize the data
        0, // 0 = move forward size * sizeof(type) each iteration to get the next position
        0 // start at the beginning of the buffer
        );
        //// �e�N�X�`���T�C�Y�����V�F�[�_��Uniform�ϐ��ɐݒ�
        //gl.uniform2f(textureSizeLocation, this.width, this.height);
        // ���͌��Ƃ��郌���_�����O�^�[�Q�b�g�̃e�N�X�`����I��
        gl.activeTexture(gl.TEXTURE0);
        gl.bindTexture(gl.TEXTURE_2D, src.texture);
        gl.uniform1i(texture0Lication, 0);
        if (dst == null) {
            // �I�t�X�N���[�������_�����O�ɂ͂��Ȃ�
            gl.bindFramebuffer(gl.FRAMEBUFFER, null);
            if (gl.canvas.width !== gl.canvas.clientWidth || gl.canvas.height !== gl.canvas.clientHeight) {
                gl.canvas.width = gl.canvas.clientWidth;
                gl.canvas.height = gl.canvas.clientHeight;
            }
            //const projectionMatrix = m3.projection(gl.canvas.width, gl.canvas.height);
            const projectionMatrix = m3.multiply(m3.scaling(1, -1), m3.projection(gl.canvas.width, gl.canvas.height));
            gl.uniformMatrix3fv(matrixLocation, false, projectionMatrix);
            gl.viewport(0, 0, gl.canvas.width, gl.canvas.height);
        }
        else {
            // �o�͐�Ƃ��郌���_�����O�^�[�Q�b�g�̃t���[���o�b�t�@��I����A�����_�����O�𑜓x�ƃr���[�|�[�g��ݒ�
            gl.bindFramebuffer(gl.FRAMEBUFFER, dst.framebuffer);
            const projectionMatrix = m3.projection(dst.width, dst.height);
            gl.uniformMatrix3fv(matrixLocation, false, projectionMatrix);
            gl.viewport(0, 0, dst.width, dst.height);
        }
        // �I�t�X�N���[�������_�����O����s
        gl.drawArrays(gl.TRIANGLES, 0, 6);
        // �K�p����
    }
    drawLines(dst, { program = null, vertexes = null, size = null, color = null }) {
        const gl = this.gl;
        // arrtibute�ϐ��̈ʒu��擾
        const positionLocation = gl.getAttribLocation(program.program, "a_position");
        // uniform�ϐ��̈ʒu��擾
        const matrixLocation = gl.getUniformLocation(program.program, "u_matrix");
        const startLocation = gl.getUniformLocation(program.program, "u_start");
        const endLocation = gl.getUniformLocation(program.program, "u_end");
        const sizeLocation = gl.getUniformLocation(program.program, "u_size");
        const colorLocation = gl.getUniformLocation(program.program, "u_color");
        //const texture0Lication = gl.getUniformLocation(program.program, 'texture0');
        // �V�F�[�_��ݒ�
        gl.useProgram(program.program);
        // �V�F�[�_�̒��_���WAttribute��L����
        gl.enableVertexAttribArray(positionLocation);
        // ���͌��Ƃ��郌���_�����O�^�[�Q�b�g�͂Ȃ�
        gl.activeTexture(gl.TEXTURE0);
        gl.bindTexture(gl.TEXTURE_2D, null);
        //gl.uniform1i(texture0Lication, 0);
        // �o�͐�Ƃ��郌���_�����O�^�[�Q�b�g�̃t���[���o�b�t�@��I����A�����_�����O�𑜓x�ƃr���[�|�[�g��ݒ�
        gl.bindFramebuffer(gl.FRAMEBUFFER, dst.framebuffer);
        gl.viewport(0, 0, dst.width, dst.height);
        const projectionMatrix = m3.projection(dst.width, dst.height);
        gl.uniformMatrix3fv(matrixLocation, false, projectionMatrix);
        // �F��ݒ�
        gl.uniform4fv(colorLocation, color);
        // ���_�o�b�t�@�i���W�j����[�N�o�b�t�@�ɐ؂�ւ��邪�A���_���͌�Őݒ�
        gl.bindBuffer(gl.ARRAY_BUFFER, this.positionBuffer);
        // ���_���WAttribute�̈ʒu����ݒ�
        gl.vertexAttribPointer(positionLocation, 2, // 2 components per iteration
        gl.FLOAT, // the data is 32bit floats
        false, // don't normalize the data
        0, // 0 = move forward size * sizeof(type) each iteration to get the next position
        0 // start at the beginning of the buffer
        );
        const len = ~~(vertexes.length / 2) - 1;
        // �T�C�Y��ݒ�
        gl.uniform1f(sizeLocation, size / 2);
        // ��`�ݒ�
        for (let i = 0; i < len; i++) {
            const x1 = vertexes[i * 2 + 0] + 0.5;
            const y1 = vertexes[i * 2 + 1] + 0.5;
            const x2 = vertexes[i * 2 + 2] + 0.5;
            const y2 = vertexes[i * 2 + 3] + 0.5;
            const left = Math.min(x1, x2) - size * 2;
            const top = Math.min(y1, y2) - size * 2;
            const right = Math.max(x1, x2) + size * 2;
            const bottom = Math.max(y1, y2) + size * 2;
            gl.uniform2f(startLocation, x1, y1);
            gl.uniform2f(endLocation, x2, y2);
            this.setVertexPosition(ImageProcessing.createRectangle(left, top, right, bottom));
            // �J�[�l���V�F�[�_��K�p�����I�t�X�N���[�������_�����O����s
            gl.drawArrays(gl.TRIANGLES, 0, 6);
        }
    }
    composit(dst, front, back, program) {
        const gl = this.gl;
        // arrtibute�ϐ��̈ʒu��擾
        const positionLocation = gl.getAttribLocation(program.program, "a_position");
        const texcoordLocation = gl.getAttribLocation(program.program, "a_texCoord");
        // uniform�ϐ��̈ʒu��擾
        const matrixLocation = gl.getUniformLocation(program.program, "u_matrix");
        const texture0Lication = gl.getUniformLocation(program.program, 'u_front');
        const texture1Lication = gl.getUniformLocation(program.program, 'u_back');
        // �V�F�[�_��ݒ�
        gl.useProgram(program.program);
        // �V�F�[�_�̒��_���WAttribute��L����
        gl.enableVertexAttribArray(positionLocation);
        // ���_�o�b�t�@�i���W�j��ݒ�
        this.setVertexPosition(ImageProcessing.createRectangle(0, 0, front.width, front.height));
        // ���_���WAttribute�̈ʒu����ݒ�
        gl.vertexAttribPointer(positionLocation, 2, // 2 components per iteration
        gl.FLOAT, // the data is 32bit floats
        false, // don't normalize the data
        0, // 0 = move forward size * sizeof(type) each iteration to get the next position
        0 // start at the beginning of the buffer
        );
        // �V�F�[�_�̃e�N�X�`�����WAttribute��L����
        gl.enableVertexAttribArray(texcoordLocation);
        // ���_�o�b�t�@�i�e�N�X�`�����W�j��ݒ�
        this.setTexturePosition(ImageProcessing.createRectangle(0, 0, 1, 1));
        // �e�N�X�`�����WAttribute�̈ʒu����ݒ�
        gl.vertexAttribPointer(texcoordLocation, 2, // 2 components per iteration
        gl.FLOAT, // the data is 32bit floats
        false, // don't normalize the data
        0, // 0 = move forward size * sizeof(type) each iteration to get the next position
        0 // start at the beginning of the buffer
        );
        const projectionMatrix = m3.projection(dst.width, dst.height);
        gl.uniformMatrix3fv(matrixLocation, false, projectionMatrix);
        //// �e�N�X�`���T�C�Y�����V�F�[�_��Uniform�ϐ��ɐݒ�
        //gl.uniform2f(textureSizeLocation, this.width, this.height);
        // ���͌��Ƃ��郌���_�����O�^�[�Q�b�g�̃e�N�X�`����I��
        gl.activeTexture(gl.TEXTURE0);
        gl.bindTexture(gl.TEXTURE_2D, front.texture);
        gl.uniform1i(texture0Lication, 0);
        gl.activeTexture(gl.TEXTURE1);
        gl.bindTexture(gl.TEXTURE_2D, back.texture);
        gl.uniform1i(texture1Lication, 1);
        // �o�͐�Ƃ��郌���_�����O�^�[�Q�b�g�̃t���[���o�b�t�@��I����A�����_�����O�𑜓x�ƃr���[�|�[�g��ݒ�
        gl.bindFramebuffer(gl.FRAMEBUFFER, dst.framebuffer);
        gl.viewport(0, 0, dst.width, dst.height);
        // �I�t�X�N���[�������_�����O����s
        gl.drawArrays(gl.TRIANGLES, 0, 6);
        // �K�p����
    }
    createPixelBuffer(width, height) {
        return new PixelBuffer(this.gl, width, height);
    }
}
// �t�B���^�J�[�l��
const kernels = {
    normal: new Float32Array([
        0, 0, 0,
        0, 1, 0,
        0, 0, 0
    ]),
    gaussianBlur: new Float32Array([
        0.045, 0.122, 0.045,
        0.122, 0.332, 0.122,
        0.045, 0.122, 0.045
    ]),
};
function main() {
    const image = new Image();
    image.src = "leaves.jpg";
    image.onload = () => { render(image); };
}
function render(image) {
    const canvas1 = document.getElementById("can2");
    //const canvas1 = document.createElement("canvas") as HTMLCanvasElement;
    const gl = canvas1.getContext('webgl2');
    const canvas2 = document.getElementById("can2");
    const ctx = canvas2.getContext('2d');
    if (!gl) {
        console.error("context does not exist");
    }
    else {
        // �t�B���^�V�F�[�_��ǂݍ���
        const program = Program.loadShaderById(gl, ["2d-vertex-shader", "2d-fragment-shader"], [gl.VERTEX_SHADER, gl.FRAGMENT_SHADER]);
        const program2 = Program.loadShaderById(gl, ["2d-vertex-shader-2", "2d-fragment-shader-2"], [gl.VERTEX_SHADER, gl.FRAGMENT_SHADER]);
        const program3 = Program.loadShaderById(gl, ["2d-vertex-shader-3", "2d-fragment-shader-3"], [gl.VERTEX_SHADER, gl.FRAGMENT_SHADER]);
        const program4 = Program.loadShaderById(gl, ["2d-vertex-shader-4", "2d-fragment-shader-4"], [gl.VERTEX_SHADER, gl.FRAGMENT_SHADER]);
        const program5 = Program.loadShaderById(gl, ["2d-vertex-shader-5", "2d-fragment-shader-5"], [gl.VERTEX_SHADER, gl.FRAGMENT_SHADER]);
        const s0 = Date.now();
        const ip = new ImageProcessing(gl);
        console.log("initialize time: ", Date.now() - s0);
        // ��ƃG���A�Ȃǂ���
        const t1 = ip.createOffscreenTargetFromImage(image);
        const t2 = ip.createBlankOffscreenTarget(image.width, image.height);
        const t3 = ip.createBlankOffscreenTarget(image.width, image.height);
        const t4 = ip.createBlankOffscreenTarget(image.width, image.height);
        const ret = ip.createPixelBuffer(image.width, image.height);
        // �t�B���^��K�p
        const s1 = Date.now();
        ip.applyKernel(t2, t1, { kernel: kernels["gaussianBlur"], program: program });
        console.log("gaussianBlur time: ", Date.now() - s1);
        // �V�F�[�_��K�p
        const s2 = Date.now();
        ip.applyShader(t1, t2, { program: program2 });
        console.log("swap r and b time: ", Date.now() - s2);
        // �u���V��z�肵��������
        const s3 = Date.now();
        ip.drawLines(t3, { program: program3, vertexes: new Float32Array([100, 100, 150, 150]), size: 5, color: [0, 0, 1, 0.5] });
        console.log("drawline: ", Date.now() - s3);
        //// ���C���[����
        //const s4 = Date.now();
        //ip.composit(t4, t3, t2, program4);
        //console.log("composit layer: ", Date.now() - s4);
        // �����S�������i���C���[�����̓���`�j
        const s4 = Date.now();
        ip.composit(t4, t3, t2, program5);
        console.log("eraser: ", Date.now() - s4);
        /*
        // �����_�����O���ʂ�Uint8Array�Ƃ��Ď擾
        const s5 = Date.now();
        const data = ret.capture(t4);
        console.log("capture rendering data: ", Date.now() - s5);

        // Uint8Array����canvas�ɓ]��
        const s6 = Date.now();
        const imageData = ctx.createImageData(image.width, image.height);
        imageData.data.set(data);
        console.log("copy to context: ", Date.now() - s6);

        ctx.putImageData(imageData, 0, 0);

        //*/
        ip.applyShader(null, t4, { program: program2 });
        return;
    }
}
window.requestAnimationFrame(main);
//# sourceMappingURL=glcanvas.js.map