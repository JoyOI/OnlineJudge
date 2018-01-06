function check() {
    "use strict";
    if (typeof Symbol == "undefined") return false;
    try {
        eval("class Foo {}");
        eval("var bar = (x) => x+1");
        eval("var bar2 = async (x) => 1;")
    } catch (e) {
        return false;
    }
    return true;
}

if (!check()) {
    window.location = '/badbrowser';
} 