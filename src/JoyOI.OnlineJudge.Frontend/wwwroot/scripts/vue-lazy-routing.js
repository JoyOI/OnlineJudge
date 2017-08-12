var LazyRouting = {};
LazyRouting._routeMap = {};
LazyRouting.__routeMap = {};
LazyRouting._controlJs = {};
LazyRouting.__mirror = [];

var router = new VueRouter({
    mode: 'history'
});

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

LazyRouting._documentReadyPromise = new Promise(function (resolve) {
    $(document).ready(function () {
        LazyRouting._getHtmlAsync("/views/index.control.html", true).then(function (js) {
            js = js.replace('<route-script>', '').replace('</route-script>', '');
            try {
                js = $('<div/>').html(js).text();
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
                    if (regex.test(router.history.current.fullPath) || LazyRouting.__mirror.some(y => y.src == router.history.current.fullPath && y.dest == x)) {
                        var mapping = [];
                        LazyRouting._loadComponentAsync(LazyRouting._convertToViewNameBase(x), x, LazyRouting.__mirror.filter(y => y.src == router.history.current.fullPath && y.dest == x));
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

LazyRouting._getHtmlAsync =function(url, unsafe) {
    return new Promise(async function (resolve, reject) {
        /* For Web Hosting */
        //$.get(url, {}, function (result) {
        //    if (result.indexOf('<head>') < 0)
        //    {
        //        resolve(result);
        //    }
        //    else
        //    {
        //        reject(url + "Invalid");
        //    }
        //});

        /* For Mobile */
        var id = "vue-" + parseInt(Math.random() * 1000000);
        var frm = document.createElement('iframe');
        frm.setAttribute('id', id);
        frm.setAttribute('style', 'display:none');
        frm.src = url;
        frm.onload = function () {
            try {
                if ($(frm).contents().find('head')[0].innerHTML) {
                    throw url + " invalid";
                }
                var html = $(frm).contents().find('body').html();
                frm.parentNode.removeChild(frm);
                resolve(html);
            } catch (ex) {
                frm.parentNode.removeChild(frm);
                reject(ex);
            }
        };
        if (!unsafe)
        {
            await LazyRouting._documentReadyPromise;
        }
        document.body.appendChild(frm);
    });
}

LazyRouting._loadComponentAsync = function (path, rule, map) {
    if (LazyRouting.__mirror.some(x => x.src == path))
        return Promise.reject();

    return LazyRouting._getHtmlAsync("/views" + path + ".html")
        .then(async (result) => {
            try {
                var js = await LazyRouting._getHtmlAsync("/views" + path + ".control.html");
                js = $('<div/>').html(js).text();
                LazyRouting._controlJs[path] = js.replace('<route-script>', '').replace('</route-script>', '');
            }
            catch (ex) {
                LazyRouting._controlJs[path] = '';
            }
            return Promise.resolve(result);
        }, () => {
            return LazyRouting._getHtmlAsync("/views" + path + "/index.html");
        })
        .then(async (result) => {
            try {
                var js = await LazyRouting._getHtmlAsync("/views" + path + "/index.control.html");
                js = $('<div/>').html(js).text();
                LazyRouting._controlJs[path] = js.replace('<route-script>', '').replace('</route-script>', '');
            }
            catch (ex) {
                LazyRouting._controlJs[path] = '';
            }

            var component = { template: result };
            if (LazyRouting._controlJs[path]) {
                eval(LazyRouting._controlJs[path]);
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
        e.preventDefault();
        var name = typeof (e.target.__vue__.$options.propsData.to) === "string" ? e.target.__vue__.$options.propsData.to : e.target.__vue__.$options.propsData.to.name;
        var path = typeof (e.target.__vue__.$options.propsData.to) === "string" ? e.target.__vue__.$options.propsData.to : e.target.__vue__.$options.propsData.to.path;

        await LazyRouting._loadComponentAsync(LazyRouting._convertToViewNameBase(name), name);
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