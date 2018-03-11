"use strict";

namespace Game {
    export class ConsoleView {
        private static instance: ConsoleView;

        public static install(): boolean {
            if (!this.instance) {
                this.instance = new ConsoleView();
                return true;
            } else {
                return false;
            }
        }

        constructor() {
            const log = console.log.bind(console);
            const error = console.error.bind(console);
            const warn = console.warn.bind(console);
            const table = (console as any).table ? (console as any).table.bind(console) : null;
            const toString = (x: any) => (x instanceof Error) ? x.message : (typeof x === 'string' ? x : JSON.stringify(x));
            const outer = document.createElement('div');
            outer.id = 'console';
            const div = document.createElement('div');
            outer.appendChild(div);

            const printToDiv = (stackTraceObject: Error, ...args: any[]) => {
                const msg = Array.prototype.slice.call(args, 0)
                    .map(toString)
                    .join(' ');
                const text = div.textContent;
                const trace = stackTraceObject ? (stackTraceObject as any).stack.split(/\n/)[1] : "";
                div.textContent = text + trace + ": " + msg + '\n';
                while (div.clientHeight > document.body.clientHeight) {
                    const lines = div.textContent.split(/\n/);
                    lines.shift();
                    div.textContent = lines.join('\n');
                }
            };

            console.log = (...args: any[]) => {
                log.apply(null, args);
                const dupargs = Array.prototype.slice.call(args, 0);
                dupargs.unshift(new Error());
                printToDiv.apply(null, dupargs);
            };
            console.error = (...args: any[]) => {
                error.apply(null, args);
                const dupargs = Array.prototype.slice.call(args, 0);
                dupargs.unshift('ERROR:');
                dupargs.unshift(new Error());
                printToDiv.apply(null, dupargs);
            };
            console.warn = (...args: any[]) => {
                warn.apply(null, args);
                const dupargs = Array.prototype.slice.call(args, 0);
                dupargs.unshift('WARNING:');
                dupargs.unshift(new Error());
                printToDiv.apply(null, dupargs);
            };
            (console as any).table = (...args: any[]) => {
                if (typeof table === 'function') {
                    table.apply(null, args);
                }

                const objArr = args[0];
                const keys: string[] = (typeof objArr[0] !== 'undefined') ? Object.keys(objArr[0]) : [];
                const numCols = keys.length;
                const len = objArr.length;
                const $table = document.createElement('table');
                const $head = document.createElement('thead');
                let $tdata = document.createElement('td');
                $tdata.innerHTML = 'Index';
                $head.appendChild($tdata);

                for (let k = 0; k < numCols; k++) {
                    $tdata = document.createElement('td');
                    $tdata.innerHTML = keys[k];
                    $head.appendChild($tdata);
                }
                $table.appendChild($head);

                for (let i = 0; i < len; i++) {
                    const $line = document.createElement('tr');
                    $tdata = document.createElement('td');
                    $tdata.innerHTML = "" + i;
                    $line.appendChild($tdata);

                    for (let j = 0; j < numCols; j++) {
                        $tdata = document.createElement('td');
                        $tdata.innerHTML = objArr[i][keys[j]];
                        $line.appendChild($tdata);
                    }
                    $table.appendChild($line);
                }
                div.appendChild($table);
            };
            window.addEventListener('error', (err: ErrorEvent) => {
                printToDiv(null, 'EXCEPTION:', err.message + '\n  ' + err.filename, err.lineno + ':' + err.colno);
            });
            document.body.appendChild(outer);
        }
    }
}
