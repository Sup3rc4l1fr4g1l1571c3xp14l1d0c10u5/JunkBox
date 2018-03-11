function getDirectory(path: string): string {
    return path.substring(0, path.lastIndexOf("/"));
}

function normalizePath(path: string): string {
    return path.split("/").reduce((s, x) => {
        if (x === "..") {
            if (s.length > 1) {
                s.pop();
            } else {
                throw new Error("bad path");
            }
        } else if (x === ".") {
            if (s.length === 0) {
                s.push(x);
            }
        } else {
            s.push(x);
        }
        return s;
    }, new Array<string>()).join("/");
}
