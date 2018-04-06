"use strict";
// ReSharper restore InconsistentNaming
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
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, srcImageOrWidth);
        }
        else {
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, srcImageOrWidth, height, 0, gl.RGBA, gl.UNSIGNED_BYTE, null);
        }
        // �t���[���o�b�t�@�𐶐����A�e�N�X�`���Ɗ֘A�Â�
        this.framebuffer = gl.createFramebuffer();
        gl.bindFramebuffer(gl.FRAMEBUFFER, this.framebuffer);
        gl.framebufferTexture2D(gl.FRAMEBUFFER, gl.COLOR_ATTACHMENT0, gl.TEXTURE_2D, this.texture, 0);
    }
}
class PixelBuffer {
    constructor(gl, width, height) {
        this.gl = gl;
        this.width = width;
        this.height = height;
        this.pbo = gl.createBuffer();
        gl.bindBuffer(gl.PIXEL_PACK_BUFFER, this.pbo);
        gl.bufferData(gl.PIXEL_PACK_BUFFER, this.width * this.height * 4, gl.STREAM_READ);
        gl.bindBuffer(gl.PIXEL_PACK_BUFFER, null);
    }
}
class ImageProcessing {
    // �t�B���^�J�[�l���̏d�ݍ��v�i�d�ݍ��v��0�ȉ��̏ꍇ��1�Ƃ���j
    static computeKernelWeight(kernel) {
        const weight = kernel.reduce((prev, curr) => prev + curr);
        return weight <= 0 ? 1 : weight;
    }
    constructor(gl, image) {
        this.gl = gl;
        this.width = image.width;
        this.height = image.height;
        // ���_�o�b�t�@�i���W�j��쐬
        this.positionBuffer = gl.createBuffer();
        gl.bindBuffer(gl.ARRAY_BUFFER, this.positionBuffer);
        {
            const x1 = 0;
            const x2 = image.width;
            const y1 = 0;
            const y2 = image.height;
            gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([
                x1, y1,
                x2, y1,
                x1, y2,
                x1, y2,
                x2, y1,
                x2, y2
            ]), gl.STATIC_DRAW);
        }
        // ���_�o�b�t�@�i�e�N�X�`�����W�j��쐬
        this.texcoordBuffer = gl.createBuffer();
        gl.bindBuffer(gl.ARRAY_BUFFER, this.texcoordBuffer);
        gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([
            0.0, 0.0,
            1.0, 0.0,
            0.0, 1.0,
            0.0, 1.0,
            1.0, 0.0,
            1.0, 1.0
        ]), gl.STATIC_DRAW);
        // �t�B���^�̓��͗p�Əo�͗p�ƂȂ�t���[���o�b�t�@�𐶐�
        this.offscreenTargets = [
            new OffscreenTarget(gl, image),
            new OffscreenTarget(gl, image.width, image.height) // 1�Ԃ͏��񎞂ɏo�͑��ɂȂ�̂ŋ�̃e�N�X�`���𐶐�
        ];
        // �t���b�v��ԕϐ��������
        this.flipCnt = 0;
        //// ���Z���ʎ擾�p�̃t���[���o�b�t�@�ƃ����_�[�o�b�t�@�A�s�N�Z���o�b�t�@�𐶐�
        //// ��̃����_�[�o�b�t�@�𐶐����A�t�H�[�}�b�g��ݒ�
        //this.renderbuffer = gl.createRenderbuffer();
        //gl.bindRenderbuffer(gl.RENDERBUFFER, this.renderbuffer);
        //gl.renderbufferStorage(gl.RENDERBUFFER, gl.RGBA8, this.width, this.height);
        //// �t���[���o�b�t�@�𐶐����A�����_�[�o�b�t�@��J���[�o�b�t�@�Ƃ��ăt���[���o�b�t�@�ɃA�^�b�`
        //this.fbo = gl.createFramebuffer();
        //gl.bindFramebuffer(gl.FRAMEBUFFER, this.fbo);
        //gl.framebufferRenderbuffer(gl.FRAMEBUFFER, gl.COLOR_ATTACHMENT0, gl.RENDERBUFFER, this.renderbuffer);
        //gl.bindFramebuffer(gl.FRAMEBUFFER, null);
        // �s�N�Z���o�b�t�@�𐶐�
        this.pixelBuffer = new PixelBuffer(gl, this.width, this.height);
    }
    applyKernel(kernel, program) {
        const gl = this.gl;
        // arrtibute�ϐ��̈ʒu��擾
        const positionLocation = gl.getAttribLocation(program.program, "a_position");
        const texcoordLocation = gl.getAttribLocation(program.program, "a_texCoord");
        // uniform�ϐ��̈ʒu��擾
        const resolutionLocation = gl.getUniformLocation(program.program, "u_resolution");
        const textureSizeLocation = gl.getUniformLocation(program.program, "u_textureSize");
        const kernelLocation = gl.getUniformLocation(program.program, "u_kernel[0]");
        const kernelWeightLocation = gl.getUniformLocation(program.program, "u_kernelWeight");
        // �V�F�[�_��ݒ�
        gl.useProgram(program.program);
        // �V�F�[�_�̒��_���WAttribute��L����
        gl.enableVertexAttribArray(positionLocation);
        // ���_�o�b�t�@�i���W�j��ݒ�
        gl.bindBuffer(gl.ARRAY_BUFFER, this.positionBuffer);
        // ���_���WAttribute�̈ʒu����ݒ�
        gl.vertexAttribPointer(positionLocation, 2, // 2 components per iteration
        gl.FLOAT, // the data is 32bit floats
        false, // don't normalize the data
        0, // 0 = move forward size * sizeof(type) each iteration to get the next position
        0);
        // �V�F�[�_�̃e�N�X�`�����WAttribute��L����
        gl.enableVertexAttribArray(texcoordLocation);
        // ���_�o�b�t�@�i�e�N�X�`�����W�j��ݒ�
        gl.bindBuffer(gl.ARRAY_BUFFER, this.texcoordBuffer);
        // �e�N�X�`�����WAttribute�̈ʒu����ݒ�
        gl.vertexAttribPointer(texcoordLocation, 2, // 2 components per iteration
        gl.FLOAT, // the data is 32bit floats
        false, // don't normalize the data
        0, // 0 = move forward size * sizeof(type) each iteration to get the next position
        0 // start at the beginning of the buffer
        );
        // �e�N�X�`���T�C�Y�����V�F�[�_��Uniform�ϐ��ɐݒ�
        gl.uniform2f(textureSizeLocation, this.width, this.height);
        // ���͌��Ƃ��郌���_�����O�^�[�Q�b�g�̃e�N�X�`����I��
        gl.bindTexture(gl.TEXTURE_2D, this.offscreenTargets[(this.flipCnt) % 2].texture);
        // �o�͐�Ƃ��郌���_�����O�^�[�Q�b�g�̃t���[���o�b�t�@��I����A�����_�����O�𑜓x�ƃr���[�|�[�g��ݒ�
        gl.bindFramebuffer(gl.FRAMEBUFFER, this.offscreenTargets[(this.flipCnt + 1) % 2].framebuffer);
        gl.uniform2f(resolutionLocation, this.width, this.height);
        gl.viewport(0, 0, this.width, this.height);
        // �J�[�l���V�F�[�_��K�p�����I�t�X�N���[�������_�����O����s
        gl.uniform1fv(kernelLocation, kernel);
        gl.uniform1f(kernelWeightLocation, ImageProcessing.computeKernelWeight(kernel));
        gl.drawArrays(gl.TRIANGLES, 0, 6);
        // �X�e�b�v�J�E���g��X�V
        this.flipCnt++;
        // �t�B���^�K�p����
    }
    applyShader(program) {
        const gl = this.gl;
        // arrtibute�ϐ��̈ʒu��擾
        const positionLocation = gl.getAttribLocation(program.program, "a_position");
        const texcoordLocation = gl.getAttribLocation(program.program, "a_texCoord");
        // uniform�ϐ��̈ʒu��擾
        const resolutionLocation = gl.getUniformLocation(program.program, "u_resolution");
        const textureSizeLocation = gl.getUniformLocation(program.program, "u_textureSize");
        // �����_�����O���s
        gl.useProgram(program.program);
        // �V�F�[�_�̒��_���WAttribute��L����
        gl.enableVertexAttribArray(positionLocation);
        // ���_�o�b�t�@�i���W�j��ݒ�
        gl.bindBuffer(gl.ARRAY_BUFFER, this.positionBuffer);
        // ���_���WAttribute�̈ʒu����ݒ�
        gl.vertexAttribPointer(positionLocation, 2, // 2 components per iteration
        gl.FLOAT, // the data is 32bit floats
        false, // don't normalize the data
        0, // 0 = move forward size * sizeof(type) each iteration to get the next position
        0);
        // �V�F�[�_�̃e�N�X�`�����WAttribute��L����
        gl.enableVertexAttribArray(texcoordLocation);
        // ���_�o�b�t�@�i�e�N�X�`�����W�j��ݒ�
        gl.bindBuffer(gl.ARRAY_BUFFER, this.texcoordBuffer);
        // �e�N�X�`�����WAttribute�̈ʒu����ݒ�
        gl.vertexAttribPointer(texcoordLocation, 2, // 2 components per iteration
        gl.FLOAT, // the data is 32bit floats
        false, // don't normalize the data
        0, // 0 = move forward size * sizeof(type) each iteration to get the next position
        0 // start at the beginning of the buffer
        );
        // �e�N�X�`���T�C�Y�����V�F�[�_��Uniform�ϐ��ɐݒ�
        gl.uniform2f(textureSizeLocation, this.width, this.height);
        // ���͌��Ƃ��郌���_�����O�^�[�Q�b�g�̃e�N�X�`����I��
        gl.bindTexture(gl.TEXTURE_2D, this.offscreenTargets[(this.flipCnt) % 2].texture);
        // �o�͐�Ƃ��郌���_�����O�^�[�Q�b�g�̃t���[���o�b�t�@��I����A�����_�����O�𑜓x�ƃr���[�|�[�g��ݒ�
        gl.bindFramebuffer(gl.FRAMEBUFFER, this.offscreenTargets[(this.flipCnt + 1) % 2].framebuffer);
        gl.uniform2f(resolutionLocation, this.width, this.height);
        gl.viewport(0, 0, this.width, this.height);
        // �I�t�X�N���[�������_�����O����s
        gl.drawArrays(gl.TRIANGLES, 0, 6);
        // �X�e�b�v�J�E���g��X�V
        this.flipCnt++;
        // �K�p����
    }
    download() {
        const gl = this.gl;
        if (gl.checkFramebufferStatus(gl.FRAMEBUFFER) !== gl.FRAMEBUFFER_COMPLETE) {
            console.log("framebuffer with RGB8 color buffer is incomplete");
            return null;
        }
        else {
            const data = new Uint8Array(this.width * this.height * 4);
            // �t���[���o�b�t�@��s�N�Z���o�b�t�@�Ƀ��[�h
            gl.bindBuffer(gl.PIXEL_PACK_BUFFER, this.pixelBuffer.pbo);
            gl.readPixels(0, 0, this.width, this.height, gl.RGBA, gl.UNSIGNED_BYTE, 0);
            // �s�N�Z���o�b�t�@����CPU���̔z��Ƀ��[�h
            gl.fenceSync(gl.SYNC_GPU_COMMANDS_COMPLETE, 0);
            gl.getBufferSubData(gl.PIXEL_PACK_BUFFER, 0, data);
            gl.bindBuffer(gl.PIXEL_PACK_BUFFER, null);
            return data;
        }
    }
}
// �t�B���^�J�[�l��
const kernels = {
    normal: [
        0, 0, 0,
        0, 1, 0,
        0, 0, 0
    ],
    gaussianBlur: [
        0.045, 0.122, 0.045,
        0.122, 0.332, 0.122,
        0.045, 0.122, 0.045
    ],
};
function main() {
    const image = new Image();
    image.src = "leaves.jpg";
    image.onload = () => { render(image); };
}
function render(image) {
    const canvas1 = document.getElementById("can1");
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
        const s0 = Date.now();
        const ip = new ImageProcessing(gl, image);
        console.log("initialize time: ", Date.now() - s0);
        //// �t�B���^��K�p
        const s1 = Date.now();
        ip.applyKernel(kernels["gaussianBlur"], program);
        console.log("gaussianBlur time: ", Date.now() - s1);
        const s2 = Date.now();
        ip.applyShader(program2);
        console.log("swap r and b time: ", Date.now() - s2);
        // �����_�����O���ʂ�ImageData�Ƃ��Ď擾
        const s3 = Date.now();
        const data = ip.download();
        console.log("capture rendering data: ", Date.now() - s3);
        // CPU���̔z�񂩂�canvas�ɓ]��
        const s4 = Date.now();
        const imageData = ctx.createImageData(image.width, image.height);
        imageData.data.set(data);
        console.log("copy to context: ", Date.now() - s4);
        ctx.putImageData(imageData, 0, 0);
        return;
    }
}
window.requestAnimationFrame(main);
//# sourceMappingURL=glcanvas.js.map