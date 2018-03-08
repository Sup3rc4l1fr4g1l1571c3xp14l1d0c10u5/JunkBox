"use strict";

namespace Dispatcher {
    type EventHandler = (...args: any[]) => void;

    export class SingleDispatcher {
        private /*@readonly@*/ listeners: Array<EventHandler>;

        constructor() {
            this.listeners = [];
        }

        public clear(): SingleDispatcher {
            this.listeners.length = 0;
            return this;
        }

        public on(listener: (...args: any[]) => void): SingleDispatcher {
            this.listeners.push(listener);
            return this;
        }

        public off(listener: EventHandler): SingleDispatcher {
            const index = this.listeners.indexOf(listener);
            if (index !== -1) {
                this.listeners.splice(index, 1);
            }
            return this;
        }

        public fire(...args: any[]): SingleDispatcher {
            const temp = this.listeners.slice();
            temp.forEach((dispatcher) => dispatcher.apply(this, args));
            return this;
        }

        public one(listener: EventHandler): SingleDispatcher {
            var func = (...args: any[]) => {
                var result = listener.apply(this, args);
                this.off(func);
                return result;
            };

            this.on(func);

            return this;
        }

    }

    export class EventDispatcher {

        private /*@readonly@*/ listeners: Map<string, SingleDispatcher>;

        constructor() {
            this.listeners = new Map<string, SingleDispatcher>();
        }

        public on(eventName: string, listener: (...args: any[]) => void): EventDispatcher {
            if (!this.listeners.has(eventName)) {
                this.listeners.set(eventName, new SingleDispatcher());
            }

            this.listeners.get(eventName).on(listener);
            return this;
        }

        public off(eventName: string, listener: EventHandler): EventDispatcher {
            this.listeners.get(eventName).off(listener);
            return this;
        }

        public fire(eventName: string, ...args: any[]): EventDispatcher {
            if (this.listeners.has(eventName)) {
                const dispatcher = this.listeners.get(eventName);
                dispatcher.fire.apply(dispatcher, args);
            }
            return this;
        }

        public one(eventName: string, listener: EventHandler): EventDispatcher {
            if (!this.listeners.has(eventName)) {
                this.listeners.set(eventName, new SingleDispatcher());
            }
            this.listeners.get(eventName).one(listener);

            return this;
        }

        public hasEventListener(eventName: string): boolean {
            return this.listeners.has(eventName);
        }

        public clearEventListener(eventName: string): EventDispatcher {
            if (this.listeners.has(eventName)) {
                this.listeners.get(eventName).clear();
            }
            return this;
        }
    }
}
