"use strict";

interface CanvasRenderingContext2D {
    mozImageSmoothingEnabled: boolean;
    imageSmoothingEnabled: boolean;
    webkitImageSmoothingEnabled: boolean;
    ellipse: (x: number, y: number, radiusX: number, radiusY: number, rotation: number, startAngle: number, endAngle: number, anticlockwise?: boolean) => void;
}

namespace Game {
    export interface VideoConfig {
        id: string;
        offscreenWidth: number;
        offscreenHeight: number;
        scaleX: number;
        scaleY: number;
    }
    export class Video implements VideoConfig {
        private canvasElement: HTMLCanvasElement;
        private canvasRenderingContext2D: CanvasRenderingContext2D;
        private images: Map<string, HTMLImageElement>;
        public id: string;
        public offscreenWidth: number;
        public offscreenHeight: number;
        public scaleX: number;
        public scaleY: number;

        constructor(config: VideoConfig) {
            this.canvasElement = (document.getElementById(config.id) as HTMLCanvasElement);
            if (!this.canvasElement) {
                throw new Error("your browser is not support canvas.");
            }
            this.id = config.id;
            this.offscreenWidth = config.offscreenWidth;
            this.offscreenHeight = config.offscreenHeight;
            this.scaleX = config.scaleX;
            this.scaleY = config.scaleY;

            this.canvasElement.width = this.offscreenWidth * this.scaleX;
            this.canvasElement.height = this.offscreenHeight * this.scaleY;

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
        public get canvas(): HTMLCanvasElement { return this.canvasRenderingContext2D.canvas; }

        public get fillStyle(): string | CanvasGradient | CanvasPattern { return this.canvasRenderingContext2D.fillStyle; }

        public set fillStyle(value: string | CanvasGradient | CanvasPattern) { this.canvasRenderingContext2D.fillStyle = value; }

        public get font(): string { return this.canvasRenderingContext2D.font; }

        public set font(value: string) { this.canvasRenderingContext2D.font = value; }

        public get globalAlpha(): number { return this.canvasRenderingContext2D.globalAlpha; }

        public set globalAlpha(value: number) { this.canvasRenderingContext2D.globalAlpha = value; }

        public get globalCompositeOperation(): string { return this.canvasRenderingContext2D.globalCompositeOperation; }

        public set globalCompositeOperation(value: string) { this.canvasRenderingContext2D.globalCompositeOperation = value; }

        public get lineCap(): string { return this.canvasRenderingContext2D.lineCap; }

        public set lineCap(value: string) { this.canvasRenderingContext2D.lineCap = value; }

        public get lineDashOffset(): number { return this.canvasRenderingContext2D.lineDashOffset; }

        public set lineDashOffset(value: number) { this.canvasRenderingContext2D.lineDashOffset = value; }

        public get lineJoin(): string { return this.canvasRenderingContext2D.lineJoin; }

        public set lineJoin(value: string) { this.canvasRenderingContext2D.lineJoin = value; }

        public get lineWidth(): number { return this.canvasRenderingContext2D.lineWidth; }

        public set lineWidth(value: number) { this.canvasRenderingContext2D.lineWidth = value; }

        public get miterLimit(): number { return this.canvasRenderingContext2D.miterLimit; }

        public set miterLimit(value: number) { this.canvasRenderingContext2D.miterLimit = value; }

        // get msFillRule(): string { return this.context.msFillRule; }
        // set msFillRule(value: string) { this.context.msFillRule = value; }

        public get shadowBlur(): number { return this.canvasRenderingContext2D.shadowBlur; }

        public set shadowBlur(value: number) { this.canvasRenderingContext2D.shadowBlur = value; }

        public get shadowColor(): string { return this.canvasRenderingContext2D.shadowColor; }

        public set shadowColor(value: string) { this.canvasRenderingContext2D.shadowColor = value; }

        public get shadowOffsetX(): number { return this.canvasRenderingContext2D.shadowOffsetX; }

        public set shadowOffsetX(value: number) { this.canvasRenderingContext2D.shadowOffsetX = value; }

        public get shadowOffsetY(): number { return this.canvasRenderingContext2D.shadowOffsetY; }

        public set shadowOffsetY(value: number) { this.canvasRenderingContext2D.shadowOffsetY = value; }

        public get strokeStyle(): string | CanvasGradient | CanvasPattern { return this.canvasRenderingContext2D.strokeStyle; }

        public set strokeStyle(value: string | CanvasGradient | CanvasPattern) { this.canvasRenderingContext2D.strokeStyle = value; }

        public get textAlign(): string { return this.canvasRenderingContext2D.textAlign; }

        public set textAlign(value: string) { this.canvasRenderingContext2D.textAlign = value; }

        public get textBaseline(): string { return this.canvasRenderingContext2D.textBaseline; }

        public set textBaseline(value: string) { this.canvasRenderingContext2D.textBaseline = value; }

        public get imageSmoothingEnabled(): boolean {
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

        public set imageSmoothingEnabled(value: boolean) {
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

        public arc: (
            x: number,
            y: number,
            radius: number,
            startAngle: number,
            endAngle: number,
            anticlockwise?: boolean
        ) => void;
        public arcTo: (x1: number, y1: number, x2: number, y2: number, radius: number) => void;
        public beginPath: () => void;
        public bezierCurveTo: (cp1x: number, cp1y: number, cp2x: number, cp2y: number, x: number, y: number) => void;
        public clearRect: (x: number, y: number, w: number, h: number) => void;
        public clip: (fillRule?: string) => void;
        public closePath: () => void;
        public createImageData: (imageDataOrSw: number | ImageData, sh?: number) => ImageData;
        public createLinearGradient: (x0: number, y0: number, x1: number, y1: number) => CanvasGradient;
        public createPattern: (
            image: HTMLImageElement | HTMLCanvasElement | HTMLVideoElement,
            repetition: string
        ) => CanvasPattern;
        public createRadialGradient: (
            x0: number,
            y0: number,
            r0: number,
            x1: number,
            y1: number,
            r1: number
        ) => CanvasGradient;
        public drawImage: (
            image: HTMLImageElement | HTMLCanvasElement | HTMLVideoElement,
            offsetX: number,
            offsetY: number,
            width?: number,
            height?: number,
            canvasOffsetX?: number,
            canvasOffsetY?: number,
            canvasImageWidth?: number,
            canvasImageHeight?: number) => void;
        public fill: (fillRule?: string) => void;
        public fillRect: (x: number, y: number, w: number, h: number) => void;
        public fillText: (text: string, x: number, y: number, maxWidth?: number) => void;
        public getImageData: (sx: number, sy: number, sw: number, sh: number) => ImageData;
        public getLineDash: () => number[];
        public isPointInPath: (x: number, y: number, fillRule?: string) => boolean;
        public lineTo: (x: number, y: number) => void;
        public measureText: (text: string) => TextMetrics;
        public moveTo: (x: number, y: number) => void;
        public putImageData: (
            imagedata: ImageData,
            dx: number,
            dy: number,
            dirtyX?: number,
            dirtyY?: number,
            dirtyWidth?: number,
            dirtyHeight?: number
        ) => void;
        public quadraticCurveTo: (cpx: number, cpy: number, x: number, y: number) => void;
        public rect: (x: number, y: number, w: number, h: number) => void;
        public restore: () => void;
        public rotate: (angle: number) => void;
        public save: () => void;
        public scale: (x: number, y: number) => void;
        public setLineDash: (segments: number[]) => void;
        public setTransform: (m11: number, m12: number, m21: number, m22: number, dx: number, dy: number) => void;
        public stroke: () => void;
        public strokeRect: (x: number, y: number, w: number, h: number) => void;
        public strokeText: (text: string, x: number, y: number, maxWidth?: number) => void;
        public transform: (m11: number, m12: number, m21: number, m22: number, dx: number, dy: number) => void;
        public translate: (x: number, y: number) => void;

        public ellipse: (
            x: number,
            y: number,
            radiusX: number,
            radiusY: number,
            rotation: number,
            startAngle: number,
            endAngle: number,
            anticlockwise?: boolean) => void;

        public drawTile(
            image: HTMLImageElement | HTMLCanvasElement | HTMLVideoElement,
            offsetX: number,
            offsetY: number,
            sprite: number[][],
            spritesize: number[],
            tile: Array2D): void {
            for (let y = 0; y < tile.height; y++) {
                for (let x = 0; x < tile.width; x++) {
                    const chip = tile.value(x, y);
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

        public get width(): number {
            return this.canvasRenderingContext2D.canvas.width;
        }

        public get height(): number {
            return this.canvasRenderingContext2D.canvas.height;
        }

        public loadImage(asserts: { [id: string]: string }, startCallback: (id: string) => void = () => { }, endCallback: (id: string) => void = () => { }): Promise<boolean> {
            return Promise.all(
                Object.keys(asserts).map((x) => new Promise<void>((resolve, reject) => {
                    startCallback(x);
                    const img = new Image();
                    img.onload = () => {
                        this.images.set(x, img);
                        endCallback(x);
                        resolve();
                    };
                    img.onerror = () => {
                        const msg = `ファイル ${asserts[x]}のロードに失敗。`;
                        console.error(msg);
                        reject(msg);
                    };
                    img.src = asserts[x];
                }))
            ).then(() => {
                return true;
            });
        }

        public texture(id: string) {
            return this.images.get(id);
        }

        //

        public begin() {
            Game.getScreen().save();
            Game.getScreen().clearRect(0, 0, this.width, this.height);
            Game.getScreen().scale(this.scaleX, this.scaleY);
            Game.getScreen().save();

        }
        public end() {
            Game.getScreen().restore();
            Game.getScreen().restore();
        }

        public pagePointToScreenPoint(x: number, y: number): number[] {
            const cr = this.canvasRenderingContext2D.canvas.getBoundingClientRect();
            const sx = (x - (cr.left + window.pageXOffset));
            const sy = (y - (cr.top + window.pageYOffset));
            return [sx / this.scaleX, sy / this.scaleY];
        }

        public pagePointContainScreen(x: number, y: number): boolean {
            const pos = this.pagePointToScreenPoint(x, y);
            return 0 <= pos[0] && pos[0] < this.offscreenWidth && 0 <= pos[1] && pos[1] < this.offscreenHeight;
        }
    }
}
