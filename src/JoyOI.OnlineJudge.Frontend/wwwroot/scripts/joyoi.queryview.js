var qv = {
    __cache: {},
    __cacheDictionary: {},
    __cacheExpire: {},
    __cacheFilters: {},
    __cacheSubscribe: {},
    __host: null,
    _toUrlString: function(params, ignorePage) {
        var keys = Object.keys(params).sort();
        if (!keys.length)
            return '';
        var ret = '?';
        for (var i = 0; i < keys.length; i ++) {
            if (ignorePage && keys[i] === 'page')
                continue;
            ret += keys[i] + '=' + encodeURI(params[keys[i]]) + '&';
        }
        return ret.substr(0, ret.length - 1);
    },
    _urlStringToObject(str) {
        str = str.substr(str.indexOf('?') + 1);
        var splitedKeyValuePairs = str.split('&');
        var ret = {};
        for (var i = 0; i < splitedKeyValuePairs.length; i ++) {
            var splitedKeyValuePair = splitedKeyValuePairs[i].split('=');
            ret[splitedKeyValuePair[0]] = decodeURI(splitedKeyValuePair[1]);
        }
        return ret;
    },
    _generateCacheKey: function (endpoint, params, isPaged) {
        var par = clone(params);
        if (isPaged && par.page) delete par.page;
        return endpoint + this._toUrlString(par);
    },
    _isPagedResult: function (result) {
        if (result.data == undefined || result.data.current === undefined || result.data.size === undefined || result.data.total === undefined || result.data.count === undefined)
            return false;
        else
            return true;
    },
    request: function (endpoint, method, params) {
        if (app && method !== 'GET') {
            app.control.apiLock = true;
        }
        var self = this;
        return new Promise(function (resolve, reject) {
            $.ajax({
                url: self.__host + endpoint,
                type: method,
                dataType: 'json',
                contentType: 'application/json',
                data: method == 'GET' ? params : JSON.stringify(params),
                beforeSend: function(request) {
                    request.setRequestHeader('joyoi_cookie', document.cookie);
                    request.setRequestHeader('joyoi_domain', window.location.host.toString());
                },
                success: function (ret) {
                    if (app) {
                        app.control.apiLock = false;
                    }
                    resolve(ret);
                },
                error: function (err) {
                    if (app) {
                        app.control.apiLock = false;
                    }
                    reject(err);
                }
            });
        });
    },
    get: function (endpoint, params) {
        return this.request(endpoint, 'GET', params);
    },
    patch: function (endpoint, params) {
        return this.request(endpoint, 'POST', params);
    },
    put: function (endpoint, params) {
        return this.request(endpoint, 'PUT', params);
    },
    delete: function (endpoint, params) {
        return this.request(endpoint, 'DELETE', params);
    },
    removeCache: function (endpoint, params) {
        var fields = getFields(params);
        var key = this._generateCacheKey(endpoint, params, fields.some(x => x === 'page'));
        if (this.__cache[key])
        {
            delete this.__cache[key];
        }
    },
    cache: function (endpoint, params, result, expire) {
        var key;
        if (!this.__cacheDictionary[endpoint])
            this.__cacheDictionary[endpoint] = [];
        var isPagedResult = this._isPagedResult(result);
        if (!isPagedResult) {
            key = this._generateCacheKey(endpoint, params);
            this.__cache[key] = result;
        } else {
            key = this._generateCacheKey(endpoint, params, true);
            if (!this.__cache[key]) this.__cache[key] = { isPaged: true };
            this.__cache[key][result.data.current] = result;
        }
        if (!this.__cacheDictionary[endpoint].some(x => x == key))
            this.__cacheDictionary[endpoint].push(key);

        if (expire)
            this.__cacheExpire[key] = new Date().getTime() + expire;
    },
    update: function (method, endpoint, item) {
        var filterFunc;
        if (this.__cacheFilters[endpoint])
        {
            filterFunc = this.__cacheFilters[endpoint];
        }

        for (var i = 0; i < this.__cacheDictionary[endpoint].length; i ++) {
            var cacheKey = this.__cacheDictionary[endpoint][i];
            if (filterFunc === undefined) {
                filterFunc = function (data) {
                    var id = data.item.Id;
                    var items = this.__cache[data.key].filter(x => x.Id == id);
                    if (items.length)
                        return this.__cache[data.key].indexOf(items[0]);
                    else return -1;
                };
            }
            var isPaged = this.__cache[cacheKey].isPaged !== undefined;
            var pos = filterFunc({ 
                isPaged: isPaged, 
                method: method, 
                endpoint: endpoint, 
                item: item, 
                key: cacheKey, 
                params: this._urlStringToObject(cacheKey) 
            });

            var reactToChanges = false;
            switch (method) 
            {
                case 'PUT':
                    if (!isPaged) {
                        if (pos >= 0) {
                            this.__cache[cacheKey].splice(pos, 0, item);
                            reactToChanges = true;
                        }
                    } else {
                        if (pos[0] > 0) {
                            this.__cache[cacheKey][pos[0]].splice(pos[1], 0, item);
                            reactToChanges = true;
                        }
                    }
                    break;
                case 'PATCH': 
                    if (!isPaged) {
                        pos = pos < 0 ? this.__cache[cacheKey].length : pos;
                        this.__cache[cacheKey].splice(pos, 1);
                        this.__cache[cacheKey].splice(pos, 0, item);
                            reactToChanges = true;
                    } else {
                        if (pos[0] >= 0) {
                            pos[1] = pos[1] < 0 ? this.__cache[cacheKey].length : pos[1];
                            this.__cache[cacheKey][pos[0]].splice(pos[1], 1);
                            this.__cache[cacheKey][pos[0]].splice(pos[1], 0, item);
                            reactToChanges = true;
                        }
                    }
                    break;
                case 'DELETE':
                    if (!isPaged) {
                        if (pos >= 0)
                            this.__cache[cacheKey].splice(pos, 1);
                            reactToChanges = true;
                    } else {
                        if (pos[0] >= 0 && pos[1] >= 0)
                            this.__cache[cacheKey][pos[0]].splice(pos[1], 1);
                            reactToChanges = true;
                    }
                    break;
                default: break;
            }

            if (reactToChanges) {
                var functions = this.__cacheSubscribe[cacheKey];
                if (functions && functions.length) {
                    for (var i = 0; i < functions.length; i ++) {
                        try {
                            functions[i](isPaged ? this.__cache[cacheKey][pos[0]] : this.__cache[cacheKey]);
                        } catch (ex) {
                            functions[i] = null;
                        }
                    }

                    this.__cacheSubscribe[cacheKey] = this.__cacheSubscribe[cacheKey].filter(x => x);
                }
            }
        }
    },
    addFilter: function (endpoint, func) {
        if (this.__cacheFilters === undefined)
            this.__cacheFilters = {};
        this.__cacheFilters[endpoint] = func;
    },
    subscribe: function (key, func) {
        let current = LazyRouting.GetCurrentComponent();
        let uid = current._uid;
        if (current) {
            func = function () {
                let com = LazyRouting.GetCurrentComponent();
                if (com && com._uid != uid) {
                    return;
                } else {
                    func();
                }
            }
        }
        if (this.__cacheSubscribe[key] === undefined)
            this.__cacheSubscribe[key] = [];
        this.__cacheSubscribe[key].push(func);
    },
    createView: function (endpoint, params, interval) {
        if (!params) params = {};
        var self = this;
        var ret = {
            bindings: [],
            _subscribe: null,
            _fetchFunc: null,
            __cacheInfo: {
                endpoint: endpoint,
                params: params
            },
            removeCache: function () {
                self.removeCache(this.__cacheInfo.endpoint, this.__cacheInfo.params);
            },
            fetch: function (func) {
                this._fetchFunc = func;
                var page = params.page;
                var key = self._generateCacheKey(endpoint, params, true);
                if (!self.__cache[key] || (page !== undefined && !self.__cache[key][page || 1])) {
                    return self.get(endpoint, params)
                        .then((result) => {
                            self.cache(endpoint, params, result, interval);
                            try {
                                if (func) {
                                    func(result);
                                }
                            } catch (err) { console.error(err); }
                        });
                } else {
                    if (self.__cache[key].isPaged) {
                        func(self.__cache[key][page || 1]);
                        return Promise.resolve(self.__cache[key][page || 1]);
                    } else {
                        func(self.__cache[key]);
                        return Promise.resolve(self.__cache[key]);
                    }
                }
            },
            subscribe: function (type, id, func) {
                this._subscribe = { view: this, id: id, type: type, func: func }
                app.signalr.onlinejudge.listeners.push(this._subscribe);
            },
            unsubscribe: function () {
                app.signalr.onlinejudge.listeners.removeByValue(this._subscribe);
            },
            refresh: function () {
                this.removeCache();
                this.fetch(this._fetchFunc);
            }
        };
        
        if (interval) {
            setInterval(function () {
                ret.refresh();
            }, interval);
        }

        return ret;
    },
    reset: function () {
        this.__cache = {};
        this.__cacheDictionary = { };
        this.__cacheExpire = { };
        this.__cacheFilters = { };
        this.__cacheSubscribe = { };
    }
};