var LazyRouting = {};
LazyRouting._routeMap = {};
LazyRouting.__routeMap = {};
LazyRouting._controlJs = {};
LazyRouting.__mirror = [];

var router = new VueRouter({
    mode: 'history'
});

router.beforeEach(function (to, from, next) {
    if (to.name && !LazyRouting.__routeMap[to.name] && !LazyRouting.__mirror.some(x => x.src == to.path))
        LazyRouting._loadComponentAsync(to.name, LazyRouting.__mirror.filter(x => x.src == router.history.current.fullPath && x.dest == to.name));

    if (to.name && LazyRouting._controlJs[to.name]) {
        try {
            var component = { };
            eval(LazyRouting._controlJs[to.name]);
            component.data = LazyRouting._parseQueryString(component.data);
        } catch (ex) {
            console.error(ex);
        }
    }
    else if (to.name && LazyRouting.__mirror.some(x => x.src == to.name) && LazyRouting._controlJs[LazyRouting.__mirror.filter(x => x.src == to.name)[0].dest])
    {
        try {
            var component = { };
            eval(LazyRouting._controlJs[LazyRouting.__mirror.filter(x => x.src == to.name)[0].dest]);
            component.data = LazyRouting._parseQueryString(component.data);
        } catch (ex) {
            console.error(ex);
        }
    }
    next();
})

var app;

LazyRouting.SetRoute = function (routemap) {
    LazyRouting._routeMap = routemap;
}

LazyRouting.SetMirror = function (map) {
    LazyRouting.__mirror = [];
    for (var x in map)
    {
        LazyRouting.__mirror.push({ src: x, dest: map[x] });
    }
};

LazyRouting._documentReadyPromise = new Promise(function (resolve, reject) {
    $(document).ready(function () {
        LazyRouting._getHtmlAsync("/views/index.js", true).then(function (js) {
            try {
                eval(js);
            } catch (ex) {
                console.error(ex);
            }
            return Promise.resolve();
        }, function () { return Promise.resolve(); })
            .then(function () {
                for (var x in LazyRouting._routeMap) {
                    var regexStr = x;
                    var is404 = true;

                    if (LazyRouting._routeMap[x]) {
                        for (var y in LazyRouting._routeMap[x]) {
                            regexStr = regexStr.replace(new RegExp(':' + y, "g"), LazyRouting._routeMap[x][y]);
                        }
                    }
                    var regex = new RegExp("^" + regexStr + "$", "g");
                    var fullPath = LazyRouting._hideQueryString(router.history.current.fullPath);
                    if (regex.test(fullPath) || LazyRouting.__mirror.some(y => y.src == fullPath && y.dest == x)) {
                        var mapping = [];
                        LazyRouting._loadComponentAsync(x, LazyRouting.__mirror.filter(y => y.src == fullPath && y.dest == x));
                        is404 = false;
                        break;
                    }
                }

                if (is404) {
                    router.push('/404');
                }

                app.$mount('#app');
                resolve();
            });
    });
});

LazyRouting._getHtmlAsync = async function (url) {
    /* For Web Hosting */
    var result = await fetch(url);
    var text = await result.text();
    if (text.indexOf('<head>') >= 0)
        throw "Invalid content";
    return text;
}

LazyRouting._hideQueryString = function (path) {
    if (path.indexOf('?') >= 0)
        return path.substr(0, path.indexOf('?'));
    else
        return path;
}

LazyRouting._parseQueryString = function (dataFunc) {
    var data = dataFunc();
    var queryString = window.location.toString();
    if (queryString.indexOf('?') >= 0) {
        queryString = queryString.substr(queryString.indexOf('?') + 1);
        var params = queryString.split('&');
        for (var i = 0; i < params.length; i++) {
            var splitedKeyValuePair = params[i].split('=');
            var key = splitedKeyValuePair[0];
            var value = decodeURIComponent(splitedKeyValuePair[1]);
            try {
                LazyRouting._liftCreate(data, key);
                if (!isNaN(parseInt(value)) || !isNaN(parseFloat(value))) {
                    eval('data.' + key + '=' + value + ';');
                } else {
                    eval('data.' + key + '=value;');
                }
            } catch (ex) { console.error(ex) }
        }
        dataFunc = function () { return data; };
    }
    return dataFunc;
}

LazyRouting._liftCreate = function (obj, key) {
    var fields = key.split('.');
    var prefix = 'obj';
    for (var i = 0; i < fields.length; i++) {
        var needCreate = false;
        eval('if (!' + prefix + '.' + fields[i] + ') { needCreate = true; }');
        if (needCreate) {
            eval(prefix + '.' + fields[i] + '= {};');
        }
        prefix = prefix + '.' + fields[i];
    }
}

LazyRouting._loadComponentAsync = function (rule, map) {
    if (LazyRouting.__mirror.some(x => x.src == rule))
        return Promise.reject();

    var path = LazyRouting._convertToViewNameBase(rule);

    return LazyRouting._getHtmlAsync("/views" + path + ".html")
        .then(async (result) => {
            try {
                var js = await LazyRouting._getHtmlAsync("/views" + path + ".js");
                LazyRouting._controlJs[rule] = js;
            }
            catch (ex) {
            }
            return Promise.resolve(result);
        }, () => {
            return LazyRouting._getHtmlAsync("/views" + path + "/index.html");
        })
        .then(async (result) => {
            try {
                var js = await LazyRouting._getHtmlAsync("/views" + path + "/index.js");
                LazyRouting._controlJs[rule] = js;
            }
            catch (ex) {
            }

            var component = { template: result };
            if (LazyRouting._controlJs[rule]) {
                eval(LazyRouting._controlJs[rule]);
                component.data = LazyRouting._parseQueryString(component.data);
            }
            LazyRouting.__routeMap[rule] = { path: rule, name: rule, component: component };
            router.addRoutes([LazyRouting.__routeMap[rule]]);
            if (map && map.length > 0)
            {
                for (var i = 0; i < map.length; i++)
                {
                    try {
                        router.addRoutes([{ path: map[i].src, name: map[i].src, component: component }]);
                    } catch (ex) {
                    }
                }
            }
            return Promise.resolve(component);
        }, (err) => console.error(err));
}

LazyRouting._convertToViewNameBase = function(path) {
    path = path.replace(/(:[0-9a-zA-Z]{1,}\/|:[0-9a-zA-Z]{1,})/g, "");
    if (path[path.length - 1] == '/') {
        path = path.substr(0, path.length - 1);
    }
    return path;
}

$(window).click(async function (e) {
    if (e.target.__vue__ && !e.target.lazyload) {
        var name = typeof (e.target.__vue__.$options.propsData.to) === "string" ? e.target.__vue__.$options.propsData.to : e.target.__vue__.$options.propsData.to.name;
        var path = typeof (e.target.__vue__.$options.propsData.to) === "string" ? e.target.__vue__.$options.propsData.to : e.target.__vue__.$options.propsData.to.path;

        if (LazyRouting.__mirror.some(x => x.src == path))
        {
            var dest = LazyRouting.__mirror.filter(x => x.src == path)[0].dest;
            await LazyRouting._loadComponentAsync(dest, [{ src: path, dest: dest }]);
        }
        else
        {
            await LazyRouting._loadComponentAsync(name, LazyRouting.__mirror.filter(y => y.src == router.history.current.fullPath && y.dest == name));
        }
        e.target.lazyload = true;
        if (e.target.__vue__.$options.propsData.to.params)
        {
            var params = e.target.__vue__.$options.propsData.to.params;
            path = name;
            for (var x in params)
            {
                path = path.replace(new RegExp(":" + x, "g"), params[x]);
            }
        }
        router.push(path);
        return false;
    }
});

LazyRouting.GetCurrentComponent = function () {
    if (router.history.current.matched.length)
        return router.history.current.matched[0].instances.default;
    else
        return null;
}