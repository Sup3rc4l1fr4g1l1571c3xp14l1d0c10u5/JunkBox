"use strict";

namespace Data.Charactor {

    interface CharactorConfig {
        id: string;
        name: string;
        status: {
            hp: number;
            mp: number;
            atk: number;
            def: number;
        };
        sprite: SpriteAnimation.ISpriteSheet;
    };

    const charactorTable: CharactorConfig[] = [
        {
            id: "_u01",
            name: "ウ1",
            status: {
                hp: 100,
                mp: 100,
                atk: 0,
                def: 0
            },
            sprite: {
                source: {
                    0: "./assets/charactor/_u01/walk.png"
                },
                sprite: {
                    0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                },
                animation: {
                    idle: [
                        { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                    ],
                    move_down: [
                        { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_left: [
                        { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_up: [
                        { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_right: [
                        { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                }
            }

        },
        {
            id: "_u02",
            name: "ウ2",
            status: {
                hp: 100,
                mp: 100,
                atk: 0,
                def: 0
            },
            sprite: {
                source: {
                    0: "./assets/charactor/_u02/walk.png"
                },
                sprite: {
                    0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                },
                animation: {
                    idle: [
                        { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                    ],
                    move_down: [
                        { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_left: [
                        { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_up: [
                        { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_right: [
                        { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                }
            }

        },
        {
            id: "_u03",
            name: "ウ3",
            status: {
                hp: 100,
                mp: 100,
                atk: 0,
                def: 0
            },
            sprite: {
                source: {
                    0: "./assets/charactor/_u03/walk.png"
                },
                sprite: {
                    0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                },
                animation: {
                    idle: [
                        { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                    ],
                    move_down: [
                        { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_left: [
                        { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_up: [
                        { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_right: [
                        { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                }
            }

        },
        {
            id: "_u04",
            name: "ウ4",
            status: {
                hp: 100,
                mp: 100,
                atk: 0,
                def: 0
            },
            sprite: {
                source: {
                    0: "./assets/charactor/_u04/walk.png"
                },
                sprite: {
                    0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                },
                animation: {
                    idle: [
                        { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                    ],
                    move_down: [
                        { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_left: [
                        { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_up: [
                        { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_right: [
                        { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                }
            }

        },
        {
            id: "_u05",
            name: "ウ5",
            status: {
                hp: 100,
                mp: 100,
                atk: 0,
                def: 0
            },
            sprite: {
                source: {
                    0: "./assets/charactor/_u05/walk.png"
                },
                sprite: {
                    0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                },
                animation: {
                    idle: [
                        { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                    ],
                    move_down: [
                        { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_left: [
                        { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_up: [
                        { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_right: [
                        { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                }
            }

        },
        {
            id: "_u06",
            name: "ウ6",
            status: {
                hp: 100,
                mp: 100,
                atk: 0,
                def: 0
            },
            sprite: {
                source: {
                    0: "./assets/charactor/_u06/walk.png"
                },
                sprite: {
                    0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                },
                animation: {
                    idle: [
                        { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                    ],
                    move_down: [
                        { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_left: [
                        { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_up: [
                        { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_right: [
                        { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                }
            }

        },
        {
            id: "_u07",
            name: "ウ7",
            status: {
                hp: 100,
                mp: 100,
                atk: 0,
                def: 0
            },
            sprite: {
                source: {
                    0: "./assets/charactor/_u07/walk.png"
                },
                sprite: {
                    0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                },
                animation: {
                    idle: [
                        { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                    ],
                    move_down: [
                        { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_left: [
                        { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_up: [
                        { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_right: [
                        { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                }
            }

        },
        {
            id: "_u08",
            name: "ウ8",
            status: {
                hp: 100,
                mp: 100,
                atk: 0,
                def: 0
            },
            sprite: {
                source: {
                    0: "./assets/charactor/_u08/walk.png"
                },
                sprite: {
                    0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                },
                animation: {
                    idle: [
                        { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                    ],
                    move_down: [
                        { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_left: [
                        { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_up: [
                        { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_right: [
                        { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                }
            }

        },
        {
            id: "_u09",
            name: "ウ9",
            status: {
                hp: 100,
                mp: 100,
                atk: 0,
                def: 0
            },
            sprite: {
                source: {
                    0: "./assets/charactor/_u09/walk.png"
                },
                sprite: {
                    0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                },
                animation: {
                    idle: [
                        { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                    ],
                    move_down: [
                        { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_left: [
                        { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_up: [
                        { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_right: [
                        { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                }
            }

        },
        {
            id: "_u10",
            name: "ウ10",
            status: {
                hp: 100,
                mp: 100,
                atk: 0,
                def: 0
            },
            sprite: {
                source: {
                    0: "./assets/charactor/_u10/walk.png"
                },
                sprite: {
                    0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                },
                animation: {
                    idle: [
                        { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                    ],
                    move_down: [
                        { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_left: [
                        { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_up: [
                        { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_right: [
                        { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                }
            }

        },
        {
            id: "_u11",
            name: "ウ11",
            status: {
                hp: 100,
                mp: 100,
                atk: 0,
                def: 0
            },
            sprite: {
                source: {
                    0: "./assets/charactor/_u11/walk.png"
                },
                sprite: {
                    0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                },
                animation: {
                    idle: [
                        { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                    ],
                    move_down: [
                        { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_left: [
                        { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_up: [
                        { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_right: [
                        { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                }
            }

        },
        {
            id: "_u12",
            name: "ウ12",
            status: {
                hp: 100,
                mp: 100,
                atk: 0,
                def: 0
            },
            sprite: {
                source: {
                    0: "./assets/charactor/_u12/walk.png"
                },
                sprite: {
                    0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                },
                animation: {
                    idle: [
                        { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                    ],
                    move_down: [
                        { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_left: [
                        { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_up: [
                        { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_right: [
                        { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                }
            }

        },
        {
            id: "_u13",
            name: "ウ13",
            status: {
                hp: 100,
                mp: 100,
                atk: 0,
                def: 0
            },
            sprite: {
                source: {
                    0: "./assets/charactor/_u13/walk.png"
                },
                sprite: {
                    0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                },
                animation: {
                    idle: [
                        { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                    ],
                    move_down: [
                        { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_left: [
                        { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_up: [
                        { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_right: [
                        { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                }
            }

        },
        {
            id: "_u14",
            name: "ウ14",
            status: {
                hp: 100,
                mp: 100,
                atk: 0,
                def: 0
            },
            sprite: {
                source: {
                    0: "./assets/charactor/_u14/walk.png"
                },
                sprite: {
                    0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                },
                animation: {
                    idle: [
                        { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                    ],
                    move_down: [
                        { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_left: [
                        { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_up: [
                        { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_right: [
                        { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                }
            }

        },
        {
            id: "_u15",
            name: "ウ15",
            status: {
                hp: 100,
                mp: 100,
                atk: 0,
                def: 0
            },
            sprite: {
                source: {
                    0: "./assets/charactor/_u15/walk.png"
                },
                sprite: {
                    0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                },
                animation: {
                    idle: [
                        { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                    ],
                    move_down: [
                        { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_left: [
                        { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_up: [
                        { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_right: [
                        { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                }
            }

        },
        {
            id: "_u16",
            name: "ウ16",
            status: {
                hp: 100,
                mp: 100,
                atk: 0,
                def: 0
            },
            sprite: {
                source: {
                    0: "./assets/charactor/_u16/walk.png"
                },
                sprite: {
                    0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                },
                animation: {
                    idle: [
                        { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                    ],
                    move_down: [
                        { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_left: [
                        { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_up: [
                        { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_right: [
                        { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                }
            }

        },
        {
            id: "_u17",
            name: "ウ17",
            status: {
                hp: 100,
                mp: 100,
                atk: 0,
                def: 0
            },
            sprite: {
                source: {
                    0: "./assets/charactor/_u17/walk.png"
                },
                sprite: {
                    0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                },
                animation: {
                    idle: [
                        { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                    ],
                    move_down: [
                        { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_left: [
                        { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_up: [
                        { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_right: [
                        { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                }
            }

        },
        {
            id: "_u18",
            name: "ウ18",
            status: {
                hp: 100,
                mp: 100,
                atk: 0,
                def: 0
            },
            sprite: {
                source: {
                    0: "./assets/charactor/_u18/walk.png"
                },
                sprite: {
                    0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                },
                animation: {
                    idle: [
                        { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                    ],
                    move_down: [
                        { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_left: [
                        { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_up: [
                        { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_right: [
                        { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                }
            }

        },
        {
            id: "_u19",
            name: "ウ19",
            status: {
                hp: 100,
                mp: 100,
                atk: 0,
                def: 0
            },
            sprite: {
                source: {
                    0: "./assets/charactor/_u19/walk.png"
                },
                sprite: {
                    0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                },
                animation: {
                    idle: [
                        { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                    ],
                    move_down: [
                        { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_left: [
                        { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_up: [
                        { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_right: [
                        { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                }
            }

        },
        {
            id: "_u20",
            name: "ウ20",
            status: {
                hp: 100,
                mp: 100,
                atk: 0,
                def: 0
            },
            sprite: {
                source: {
                    0: "./assets/charactor/_u20/walk.png"
                },
                sprite: {
                    0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                },
                animation: {
                    idle: [
                        { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                    ],
                    move_down: [
                        { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_left: [
                        { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_up: [
                        { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_right: [
                        { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                }
            }

        },
        {
            id: "_u21",
            name: "ウ21",
            status: {
                hp: 100,
                mp: 100,
                atk: 0,
                def: 0
            },
            sprite: {
                source: {
                    0: "./assets/charactor/_u21/walk.png"
                },
                sprite: {
                    0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                },
                animation: {
                    idle: [
                        { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                    ],
                    move_down: [
                        { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_left: [
                        { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_up: [
                        { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_right: [
                        { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                }
            }

        },
        {
            id: "_u22",
            name: "ウ22",
            status: {
                hp: 100,
                mp: 100,
                atk: 0,
                def: 0
            },
            sprite: {
                source: {
                    0: "./assets/charactor/_u22/walk.png"
                },
                sprite: {
                    0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                },
                animation: {
                    idle: [
                        { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                    ],
                    move_down: [
                        { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_left: [
                        { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_up: [
                        { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_right: [
                        { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                }
            }

        },
        {
            id: "_u23",
            name: "ウ23",
            status: {
                hp: 100,
                mp: 100,
                atk: 0,
                def: 0
            },
            sprite: {
                source: {
                    0: "./assets/charactor/_u23/walk.png"
                },
                sprite: {
                    0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                },
                animation: {
                    idle: [
                        { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                    ],
                    move_down: [
                        { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_left: [
                        { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_up: [
                        { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_right: [
                        { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                }
            }

        },
        {
            id: "_u24",
            name: "ウ24",
            status: {
                hp: 100,
                mp: 100,
                atk: 0,
                def: 0
            },
            sprite: {
                source: {
                    0: "./assets/charactor/_u24/walk.png"
                },
                sprite: {
                    0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                },
                animation: {
                    idle: [
                        { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                    ],
                    move_down: [
                        { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_left: [
                        { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_up: [
                        { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_right: [
                        { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                }
            }

        },
        {
            id: "_u25",
            name: "ウ25",
            status: {
                hp: 100,
                mp: 100,
                atk: 0,
                def: 0
            },
            sprite: {
                source: {
                    0: "./assets/charactor/_u25/walk.png"
                },
                sprite: {
                    0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                },
                animation: {
                    idle: [
                        { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                    ],
                    move_down: [
                        { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_left: [
                        { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_up: [
                        { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_right: [
                        { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                }
            }

        },
        {
            id: "_u26",
            name: "ウ26",
            status: {
                hp: 100,
                mp: 100,
                atk: 0,
                def: 0
            },
            sprite: {
                source: {
                    0: "./assets/charactor/_u26/walk.png"
                },
                sprite: {
                    0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                },
                animation: {
                    idle: [
                        { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                    ],
                    move_down: [
                        { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_left: [
                        { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_up: [
                        { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_right: [
                        { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                }
            }

        },
        {
            id: "_u27",
            name: "ウ27",
            status: {
                hp: 100,
                mp: 100,
                atk: 0,
                def: 0
            },
            sprite: {
                source: {
                    0: "./assets/charactor/_u27/walk.png"
                },
                sprite: {
                    0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                },
                animation: {
                    idle: [
                        { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                    ],
                    move_down: [
                        { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_left: [
                        { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_up: [
                        { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_right: [
                        { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                }
            }

        },
        {
            id: "_u28",
            name: "ウ28",
            status: {
                hp: 100,
                mp: 100,
                atk: 0,
                def: 0
            },
            sprite: {
                source: {
                    0: "./assets/charactor/_u28/walk.png"
                },
                sprite: {
                    0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                },
                animation: {
                    idle: [
                        { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                    ],
                    move_down: [
                        { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_left: [
                        { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_up: [
                        { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_right: [
                        { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                }
            }

        },
        {
            id: "_u29",
            name: "ウ29",
            status: {
                hp: 100,
                mp: 100,
                atk: 0,
                def: 0
            },
            sprite: {
                source: {
                    0: "./assets/charactor/_u29/walk.png"
                },
                sprite: {
                    0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                },
                animation: {
                    idle: [
                        { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                    ],
                    move_down: [
                        { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_left: [
                        { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_up: [
                        { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_right: [
                        { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                }
            }

        },
        {
            id: "_u30",
            name: "ウ30",
            status: {
                hp: 100,
                mp: 100,
                atk: 0,
                def: 0
            },
            sprite: {
                source: {
                    0: "./assets/charactor/_u30/walk.png"
                },
                sprite: {
                    0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                    15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                },
                animation: {
                    idle: [
                        { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                    ],
                    move_down: [
                        { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_left: [
                        { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_up: [
                        { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_right: [
                        { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                }
            }

        },

    ];

    export interface CharactorData {
        id: string;
        name: string;
        status: {
            hp: number;
            mp: number;
            atk: number;
            def: number;
        };
        sprite: SpriteAnimation.SpriteSheet;
    }

    const charactorData: Map<string, CharactorData> = new Map<string, CharactorData>();

    async function configToData(config: CharactorConfig, loadStartCallback: () => void, loadEndCallback: () => void): Promise<CharactorData> {
        const id = config.id;
        const name = config.name;
        const status = config.status;
        const sprite = await SpriteAnimation.SpriteSheet.Create(config.sprite, loadStartCallback, loadEndCallback);
        return { id: id, name: name, status: status, sprite: sprite };
    }

    export async function SetupCharactorData(loadStartCallback: () => void, loadEndCallback: () => void): Promise<void> {
        const datas = await Promise.all(charactorTable.map(x => configToData(x, loadStartCallback, loadEndCallback)));
        datas.forEach(x => charactorData.set(x.id, x));
    }

    export function getPlayerIds(): string[] {
        return Array.from(charactorData.keys());
    }

    export function getPlayerConfig(id: string): CharactorData {
        return charactorData.get(id);
    }

}
