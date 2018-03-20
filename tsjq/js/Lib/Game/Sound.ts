"use strict";

namespace Game {
    export namespace Sound {
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
            private /*@readonly@*/ buffer: AudioBuffer = null;
            private /*@readonly@*/ sound: SoundManager = null;

            constructor(sound: SoundManager, buffer: AudioBuffer) {
                this.buffer = buffer;
                this.sound = sound;
                this.reset();
            }

            public reset(): void {
                this.stop();
                this.bufferSource = this.sound.createBufferSource(this.buffer);
                this.bufferSource.onended = () => this.isEnded = true;
            }

            public loopplay(): void {
                if (this.isEnded) {
                    this.bufferSource.loop = true;
                    this.bufferSource.start(0);
                    this.isEnded = false;
                }
            }

            public play(): void {
                if (this.isEnded) {
                    this.bufferSource.loop = false;
                    this.bufferSource.start(0);
                    this.isEnded = false;
                }
            }

            public stop(): void {
                if (!this.isEnded) {
                    this.bufferSource.stop(0);
                    this.bufferSource.disconnect();
                    this.isEnded = true;
                }
            }
        }

        export class SoundManager {
            private audioContext: AudioContext;

            private channels: Map<string, ManagedSoundChannel>;
            private bufferSourceIdCount: number = 0;
            private playingBufferSources: Map<number, { id: string; buffer: AudioBufferSourceNode }>;

            constructor() {
                if ((window as any).AudioContext) {
                    console.log("Use AudioContext.");
                    this.audioContext = new (window as any).AudioContext();
                } else if ((window as any).webkitAudioContext) {
                    console.log("Use webkitAudioContext.");
                    this.audioContext = new (window as any).webkitAudioContext();
                } else {
                    console.error("Neither AudioContext nor webkitAudioContext is supported by your browser.");
                    throw new Error("Neither AudioContext nor webkitAudioContext is supported by your browser.");
                }
                this.channels = new Map<string, ManagedSoundChannel>();
                this.bufferSourceIdCount = 0;
                this.playingBufferSources = new Map<number, { id: string; buffer: AudioBufferSourceNode }>();
                this.reset();

                const touchEventHooker = () => {
                    // A small hack to unlock AudioContext on mobile safari.
                    const buffer = this.audioContext.createBuffer(1, (this.audioContext.sampleRate / 100), this.audioContext.sampleRate);
                    const channel = buffer.getChannelData(0);
                    channel.fill(0);
                    const src = this.audioContext.createBufferSource();
                    src.buffer = buffer;
                    src.connect(this.audioContext.destination);
                    src.start(this.audioContext.currentTime);
                    document.body.removeEventListener('touchstart', touchEventHooker);
                };
                document.body.addEventListener('touchstart', touchEventHooker);

            }

            public createBufferSource(buffer: AudioBuffer): AudioBufferSourceNode {
                const bufferSource = this.audioContext.createBufferSource();
                bufferSource.buffer = buffer;
                bufferSource.connect(this.audioContext.destination);
                return bufferSource;
            }

            public loadSound(file: string): Promise<AudioBuffer> {
                return ajax(file, "arraybuffer").then(xhr => {
                    return new Promise<AudioBuffer>((resolve, reject) => {
                        this.audioContext.decodeAudioData(
                            xhr.response,
                            (audioBufferNode) => {
                                resolve(audioBufferNode);
                            },
                            () => {
                                reject(new Error(`cannot decodeAudioData : ${file} `));
                            }
                        );
                    });
                });
                //
                // decodeAudioData dose not return 'Promise Object 'on mobile safari :-(
                // Therefore, these codes will not work ...
                //
                // const xhr: XMLHttpRequest = await ajax(file, "arraybuffer");
                // var audioBufferNode = await this.audioContext.decodeAudioData(xhr.response);
                // return audioBufferNode;
            }

            public async loadSoundToChannel(file: string, channelId: string): Promise<void> {
                const audioBufferNode: AudioBuffer = await this.loadSound(file);
                const channel = new ManagedSoundChannel();
                channel.audioBufferNode = audioBufferNode;
                this.channels.set(channelId, channel);
                return;
            }

            public loadSoundsToChannel(
                config: { [channelId: string]: string },
                startCallback: (id: string) => void = () => { },
                endCallback: (id: string) => void = () => { }
            ): Promise<void> {
                return Promise.all(
                    Object.keys(config).map((channelId) => {
                        startCallback(channelId);
                        const ret = this.loadSoundToChannel(config[channelId], channelId).then(() => endCallback(channelId));
                        return ret;
                    })
                ).then(() => { });
            }

            public createUnmanagedSoundChannel(file: string): Promise<UnmanagedSoundChannel> {
                return this.loadSound(file)
                    .then((audioBufferNode: AudioBuffer) => new UnmanagedSoundChannel(this, audioBufferNode));
            }

            public reqPlayChannel(channelId: string, loop: boolean = false): void {
                const channel = this.channels.get(channelId);
                if (channel) {
                    channel.playRequest = true;
                    channel.loopPlay = loop;
                }
            }

            public reqStopChannel(channelId: string): void {
                const channel = this.channels.get(channelId);
                if (channel) {
                    channel.stopRequest = true;
                }
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
                        const src = this.audioContext.createBufferSource();
                        if (src == null) {
                            throw new Error("createBufferSourceに失敗。");
                        }
                        const bufferid = this.bufferSourceIdCount++;
                        this.playingBufferSources.set(bufferid, { id: i, buffer: src });
                        src.buffer = c.audioBufferNode;
                        src.loop = c.loopPlay;
                        src.connect(this.audioContext.destination);
                        src.onended = () => {
                            src.stop(0);
                            src.disconnect();
                            this.playingBufferSources.set(bufferid, null);
                            this.playingBufferSources.delete(bufferid);
                            src.onended = null; // If you forget this null assignment, the AudioBufferSourceNode object will not be destroyed and a memory leak will occur. :-(
                        };
                        src.start(0);
                    }
                });
            }

            public stop(): void {
                const oldPlayingBufferSources: Map<number, { id: string; buffer: AudioBufferSourceNode }> = this.playingBufferSources;
                this.playingBufferSources = new Map<number, { id: string; buffer: AudioBufferSourceNode }>();
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
                this.channels.clear();
                this.playingBufferSources.clear();
            }
        }
    }
}
