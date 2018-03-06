"use strict";

module Game {
    export class Video {
        private canvasElement: HTMLCanvasElement;
        private canvasRenderingContext2D: CanvasRenderingContext2D;
        private images: Map<string, HTMLImageElement>;

        constructor(id : string ) {
            this.canvasElement = (document.getElementById(id) as HTMLCanvasElement);
            if (!this.canvasElement) {
                throw new Error("your browser is not support canvas.");
            }

            this.canvasRenderingContext2D = this.canvasElement.getContext("2d");
            if (!this.canvasRenderingContext2D) {
                throw new Error("your browser is not support CanvasRenderingContext2D.");
            }

            this.images = new Map<string, HTMLImageElement>();

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
        get canvas(): HTMLCanvasElement { return this.canvasRenderingContext2D.canvas; }

        get fillStyle(): string | CanvasGradient | CanvasPattern { return this.canvasRenderingContext2D.fillStyle; }

        set fillStyle(value: string | CanvasGradient | CanvasPattern) { this.canvasRenderingContext2D.fillStyle = value; }

        get font(): string { return this.canvasRenderingContext2D.font; }

        set font(value: string) { this.canvasRenderingContext2D.font = value; }

        get globalAlpha(): number { return this.canvasRenderingContext2D.globalAlpha; }

        set globalAlpha(value: number) { this.canvasRenderingContext2D.globalAlpha = value; }

        get globalCompositeOperation(): string { return this.canvasRenderingContext2D.globalCompositeOperation; }

        set globalCompositeOperation(value: string) { this.canvasRenderingContext2D.globalCompositeOperation = value; }

        get lineCap(): string { return this.canvasRenderingContext2D.lineCap; }

        set lineCap(value: string) { this.canvasRenderingContext2D.lineCap = value; }

        get lineDashOffset(): number { return this.canvasRenderingContext2D.lineDashOffset; }

        set lineDashOffset(value: number) { this.canvasRenderingContext2D.lineDashOffset = value; }

        get lineJoin(): string { return this.canvasRenderingContext2D.lineJoin; }

        set lineJoin(value: string) { this.canvasRenderingContext2D.lineJoin = value; }

        get lineWidth(): number { return this.canvasRenderingContext2D.lineWidth; }

        set lineWidth(value: number) { this.canvasRenderingContext2D.lineWidth = value; }

        get miterLimit(): number { return this.canvasRenderingContext2D.miterLimit; }

        set miterLimit(value: number) { this.canvasRenderingContext2D.miterLimit = value; }

        //get msFillRule(): string { return this.context.msFillRule; }
        //set msFillRule(value: string) { this.context.msFillRule = value; }

        get shadowBlur(): number { return this.canvasRenderingContext2D.shadowBlur; }

        set shadowBlur(value: number) { this.canvasRenderingContext2D.shadowBlur = value; }

        get shadowColor(): string { return this.canvasRenderingContext2D.shadowColor; }

        set shadowColor(value: string) { this.canvasRenderingContext2D.shadowColor = value; }

        get shadowOffsetX(): number { return this.canvasRenderingContext2D.shadowOffsetX; }

        set shadowOffsetX(value: number) { this.canvasRenderingContext2D.shadowOffsetX = value; }

        get shadowOffsetY(): number { return this.canvasRenderingContext2D.shadowOffsetY; }

        set shadowOffsetY(value: number) { this.canvasRenderingContext2D.shadowOffsetY = value; }

        get strokeStyle(): string | CanvasGradient | CanvasPattern { return this.canvasRenderingContext2D.strokeStyle; }

        set strokeStyle(value: string | CanvasGradient | CanvasPattern) { this.canvasRenderingContext2D.strokeStyle = value; }

        get textAlign(): string { return this.canvasRenderingContext2D.textAlign; }

        set textAlign(value: string) { this.canvasRenderingContext2D.textAlign = value; }

        get textBaseline(): string { return this.canvasRenderingContext2D.textBaseline; }

        set textBaseline(value: string) { this.canvasRenderingContext2D.textBaseline = value; }

        get imageSmoothingEnabled(): boolean {
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

        set imageSmoothingEnabled(value: boolean) {
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

        arc: (x: number,
            y: number,
            radius: number,
            startAngle: number,
            endAngle: number,
            anticlockwise?: boolean) => void;
        arcTo: (x1: number, y1: number, x2: number, y2: number, radius: number) => void;
        beginPath: () => void;
        bezierCurveTo: (cp1x: number, cp1y: number, cp2x: number, cp2y: number, x: number, y: number) => void;
        clearRect: (x: number, y: number, w: number, h: number) => void;
        clip: (fillRule?: string) => void;
        closePath: () => void;
        createImageData: (imageDataOrSw: number | ImageData, sh?: number) => ImageData;
        createLinearGradient: (x0: number, y0: number, x1: number, y1: number) => CanvasGradient;
        createPattern: (image: HTMLImageElement | HTMLCanvasElement | HTMLVideoElement, repetition: string) =>
            CanvasPattern;
        createRadialGradient: (x0: number, y0: number, r0: number, x1: number, y1: number, r1: number) =>
            CanvasGradient;
        drawImage: (image: HTMLImageElement | HTMLCanvasElement | HTMLVideoElement,
            offsetX: number,
            offsetY: number,
            width?: number,
            height?: number,
            canvasOffsetX?: number,
            canvasOffsetY?: number,
            canvasImageWidth?: number,
            canvasImageHeight?: number) => void;
        fill: (fillRule?: string) => void;
        fillRect: (x: number, y: number, w: number, h: number) => void;
        fillText: (text: string, x: number, y: number, maxWidth?: number) => void;
        getImageData: (sx: number, sy: number, sw: number, sh: number) => ImageData;
        getLineDash: () => number[];
        isPointInPath: (x: number, y: number, fillRule?: string) => boolean;
        lineTo: (x: number, y: number) => void;
        measureText: (text: string) => TextMetrics;
        moveTo: (x: number, y: number) => void;
        putImageData: (imagedata: ImageData,
            dx: number,
            dy: number,
            dirtyX?: number,
            dirtyY?: number,
            dirtyWidth?: number,
            dirtyHeight?: number) => void;
        quadraticCurveTo: (cpx: number, cpy: number, x: number, y: number) => void;
        rect: (x: number, y: number, w: number, h: number) => void;
        restore: () => void;
        rotate: (angle: number) => void;
        save: () => void;
        scale: (x: number, y: number) => void;
        setLineDash: (segments: number[]) => void;
        setTransform: (m11: number, m12: number, m21: number, m22: number, dx: number, dy: number) => void;
        stroke: () => void;
        strokeRect: (x: number, y: number, w: number, h: number) => void;
        strokeText: (text: string, x: number, y: number, maxWidth?: number) => void;
        transform: (m11: number, m12: number, m21: number, m22: number, dx: number, dy: number) => void;
        translate: (x: number, y: number) => void;
        // 
        ellipse: (x: number,
            y: number,
            radiusX: number,
            radiusY: number,
            rotation: number,
            startAngle: number,
            endAngle: number,
            anticlockwise?: boolean) => void;

        //
        drawTile(image: HTMLImageElement | HTMLCanvasElement | HTMLVideoElement,
            offsetX: number,
            offsetY: number,
            sprite: number[][],
            spritesize: number[],
            tile: Matrix): void {
            for (var y = 0; y < tile.height; y++) {
                for (var x = 0; x < tile.width; x++) {
                    var chip = tile.value(x, y);
                    this.drawImage(
                        image,
                        sprite[chip][0] * spritesize[0],
                        sprite[chip][1] * spritesize[1],
                        spritesize[0],
                        spritesize[1],
                        offsetX + x * spritesize[0],
                        offsetY + y * spritesize[1],
                        spritesize[0],
                        spritesize[1]
                    );
                }
            }
        }

        //
        get width(): number {
            return this.canvasRenderingContext2D.canvas.width;
        }

        get height(): number {
            return this.canvasRenderingContext2D.canvas.height;
        }


        loadImage(asserts: { [id: string]: string }): Promise<boolean> {
            return Promise.all(
                Object.keys(asserts).map((x) => new Promise<void>((resolve, reject) => {
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
                }))
            ).then(() => {
                return true;
            });
        }

        texture(id: string) {
            return this.images.get(id);
        }

        pagePointToScreenPoint(x: number, y: number): number[] {
            const cr = this.canvasRenderingContext2D.canvas.getBoundingClientRect();
            const sx = (x - (cr.left + window.pageXOffset));
            const sy = (y - (cr.top + window.pageYOffset));
            return [sx, sy];
        }

        pagePointContainScreen(x: number, y: number): boolean {
            const pos = this.pagePointToScreenPoint(x, y);
            return 0 <= pos[0] && pos[0] < this.width && 0 <= pos[1] && pos[1] < this.height;
        }
    }
}
