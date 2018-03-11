"use strict";

function ajax(uri: string, type: XMLHttpRequestResponseType): Promise<XMLHttpRequest> {
    return new Promise((resolve, reject) => {
        const xhr: XMLHttpRequest = new XMLHttpRequest();
        xhr.responseType = type;
        xhr.open("GET", uri, true);
        xhr.onerror = (ev) => {
            reject(ev);
        };
        xhr.onload = () => {
            resolve(xhr);
        };

        xhr.send();
    });
}
