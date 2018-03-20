"use strict";

namespace Game {
    let video: Video = null;
    let sceneManager: Scene.SceneManager = null;
    let inputDispacher: Input.InputManager = null;
    let timer: Timer.AnimationTimer = null;
    let soundManager: Sound.SoundManager = null;

    export function create(config: {
        title: string;
        video: VideoConfig;
    }) {
        return new Promise<void>((resolve, reject) => {
            try {
                ConsoleView.install();
                document.title = config.title;
                video = new Video(config.video);
                video.imageSmoothingEnabled = false;
                sceneManager = new Scene.SceneManager();
                timer = new Timer.AnimationTimer();
                inputDispacher = new Input.InputManager();
                soundManager = new Sound.SoundManager();

                resolve();
            } catch (e) {
                reject(e);
            }
        });
    }

    export function getScreen(): Video {
        return video;
    }

    export function getTimer(): Timer.AnimationTimer {
        return timer;
    }

    export function getSceneManager(): Scene.SceneManager {
        return sceneManager;
    }

    export function getInput(): Input.InputManager {
        return inputDispacher;
    }

    export function getSound(): Sound.SoundManager {
        return soundManager;
    }

}
