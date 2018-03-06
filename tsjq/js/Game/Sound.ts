"use strict";

module Game {
    export module Sound {
        class ManagedSoundChannel {
            public audioBufferNode: AudioBuffer = null;
            public playRequest: boolean = false;
            public stopRequest: boolean = false;
            public loopPlay: boolean = false;

            public reset(): void {
                this.audioBufferNode = null;
                this.playRequest = false;
                this.stopRequest = false;
                this.loopPlay = false;
            }
        }

        class UnmanagedSoundChannel {
            private isEnded: boolean = true;
            private bufferSource: AudioBufferSourceNode = null;
            private readonly buffer: AudioBuffer = null;
            private readonly sound: SoundManager = null;

            constructor(sound: SoundManager, buffer: AudioBuffer) {
                this.buffer = buffer;
                this.sound = sound;
                this.reset();
            }

            reset(): void {
                this.stop();
                this.bufferSource = this.sound.createBufferSource(this.buffer);
                this.bufferSource.onended = () => this.isEnded = true;
            }

            loopplay(): void {
                if (this.isEnded) {
                    this.bufferSource.loop = true;
                    this.bufferSource.start(0);
                    this.isEnded = false;
                }
            }

            play(): void {
                if (this.isEnded) {
                    this.bufferSource.loop = false;
                    this.bufferSource.start(0);
                    this.isEnded = false;
                }
            }

            stop(): void {
                if (!this.isEnded) {
                    this.bufferSource.stop(0);
                    this.bufferSource.disconnect();
                    this.isEnded = true;
                }
            }

        }

        export class SoundManager {
            private static readonly soundChannelMax: number = 36 * 36;

            private audioContext: AudioContext;

            private channels: ManagedSoundChannel[] = new Array(SoundManager.soundChannelMax);
            private bufferSourceIdCount: number = 0;
            private playingBufferSources: Map<number, { id: number; buffer: AudioBufferSourceNode }>;

            constructor() {
                this.audioContext = new AudioContext();
                this.channels = new Array(SoundManager.soundChannelMax);
                this.bufferSourceIdCount = 0;
                this.playingBufferSources = new Map<number, { id: number; buffer: AudioBufferSourceNode }>();
                this.reset();
            }

            public createBufferSource(buffer: AudioBuffer): AudioBufferSourceNode {
                var bufferSource = this.audioContext.createBufferSource();
                bufferSource.buffer = buffer;
                bufferSource.connect(this.audioContext.destination);
                return bufferSource;
            }

            private loadSound(file: string): Promise<AudioBuffer> {
                return new Promise<XMLHttpRequest>((resolve, reject) => {
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
                    .then((xhr) => new Promise<AudioBuffer>((resolve, reject) => {
                            this.audioContext.decodeAudioData(
                                xhr.response,
                                (audioBufferNode: AudioBuffer): void => {
                                    resolve(audioBufferNode);
                                },
                                (): void => {
                                    var msg = `ファイル ${file}のdecodeAudioDataに失敗。`;
                                    reject(msg);
                                }
                            );
                        })
                    );
            }

            public loadSoundToChannel(file: string, channel: number): Promise<void> {
                return this.loadSound(file).then((audioBufferNode: AudioBuffer) => {
                    this.channels[channel].audioBufferNode = audioBufferNode;
                });
            }

            public loadSoundsToChannel(config: { [channel: number]: string }): Promise<void> {
                return Promise.all(
                    Object.keys(config)
                    .map((x) => ~~x)
                    .map((channel) => this.loadSound(config[channel]).then((audioBufferNode: AudioBuffer) => {
                        this.channels[channel].audioBufferNode = audioBufferNode;
                    }))
                ).then(() => {});
            }


            public createUnmanagedSoundChannel(file: string): Promise<UnmanagedSoundChannel> {
                return this.loadSound(file)
                    .then((audioBufferNode: AudioBuffer) => new UnmanagedSoundChannel(this, audioBufferNode));
            }

            public reqPlayChannel(channel: number, loop: boolean = false): void {
                this.channels[channel].playRequest = true;
                this.channels[channel].loopPlay = loop;
            }

            public reqStopChannel(channel: number): void {
                this.channels[channel].stopRequest = true;
            }

            public playChannel(): void {
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
                        src.onended = ((): void => {
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

            public stop(): void {
                const oldPlayingBufferSources: Map<number, { id: number; buffer: AudioBufferSourceNode }> =
                    this.playingBufferSources;
                this.playingBufferSources = new Map<number, { id: number; buffer: AudioBufferSourceNode }>();
                oldPlayingBufferSources.forEach((value, key) => {
                    const s: AudioBufferSourceNode = value.buffer;
                    if (s != null) {
                        s.stop(0);
                        s.disconnect();
                        oldPlayingBufferSources.set(key, null);
                        oldPlayingBufferSources.delete(key);
                    }
                });
            }

            public reset(): void {
                for (let i: number = 0; i < SoundManager.soundChannelMax; i++) {
                    this.channels[i] = this.channels[i] || (new ManagedSoundChannel());
                    this.channels[i].reset();
                    this.bufferSourceIdCount = 0;
                }
                this.playingBufferSources = new Map<number, { id: number; buffer: AudioBufferSourceNode }>();
            }
        }
    }
}
