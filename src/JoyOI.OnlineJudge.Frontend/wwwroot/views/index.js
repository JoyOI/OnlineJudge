app = new Vue({
    router: router,
    data: {
        links: [],
        title: '',
        hosts: {
            vue: null,
            api: null,
            uc: null,
            forum: null,
            blog: null
        },
        showUrlCover: router.history.current.fullPath != '/' && router.history.current.fullPath != '/home',
        fullScreen: false,
        layout: {
            loginBoxOpened: false
        },
        user: {
            isSignedIn: false,
            profile: {
                id: null,
                username: null,
                avatarUrl: null,
                role: null,
                tried: [],
                passed: [],
                chat: null
            }
        },
        preferences: {
            language: 'C++'
        },
        signalr: {
            onlinejudge: {
                connection: null,
                listeners: []
            }
        },
        lookup: {
            user: {},
            problem: {}
        },
        control: {
            apiLock: false,
            currentNotification: null,
            notifications: [],
            notificationLock: false,
            userInfoView: null,
            chatIframeUrl: null,
            chatInterval: null
        }
    },
    created: function () {
        var self = this;

        /* Initialize host addresses */
        this.hosts.blog = 'http://{USERNAME}.blog.joyoi.cn';
        this.hosts.forum = 'http://forum.joyoi.cn';
        this.hosts.api = 'http://localhost:5000';
        this.hosts.uc = 'http://uc.joyoi.cn';
        qv.__host = this.hosts.api;

        this.signalr.onlinejudge.connection = new signalR.HubConnection(this.hosts.api + '/signalr/onlinejudge');
        this.signalr.onlinejudge.connection.on('ItemUpdated', (type, id, user, hint, hint2) => {
            if (this.user.profile && type === 'hack' && user === this.user.profile.id && hint === 'Succeeded') {
                app.lookupProblems([hint2])
                    .then(() => {
                        app.notification('important', 'Hack 通知', `您在比赛中提交的${app.lookup.problem[hint2]}已被其他选手Hack！`, '我知道了');
                    });
            }

            var listners = this.signalr.onlinejudge.listeners.filter(x => x.type === type && (x.id === id || !x.id)).map(x => {
                x.view.removeCache();
                return x.view.fetch(x.view._fetchFunc);
            });
        });
        this.signalr.onlinejudge.connection.on('StandingsUpdated', (id, user, user2) => {
            var current = LazyRouting.GetCurrentComponent();
            if (current && current.updateStandings && current.id && current.id == id) {
                current.updateStandings(user, user2);
            }
        });
        this.signalr.onlinejudge.connection.start()

        if (document.cookie.indexOf("AspNetCore") >= 0) {
            self.user.isSignedIn = true;
            self.control.userInfoView = qv.createView('/api/user/session/info');
            self.control.userInfoView.fetch((x) => {
                if (x.data.isSignedIn) {
                    self.user.profile.username = x.data.username;
                    self.user.profile.role = x.data.role;
                    self.user.profile.id = x.data.id;
                    self.user.tried = x.data.tried;
                    self.user.passed = x.data.passed;
                    self.user.chat = x.data.chat;
                    self.preferences.language = x.data.preferredLanguage || 'C++';
                    self.control.chatIframeUrl = x.data.chat;
                } else {
                    self.user.isSignedIn = false;
                }
            });
        }
        else {
            this.user.isSignedIn = false;
        }

        qv.createView('/api/user/message', null, 30000)
            .fetch(x => {
                if (x.data) {
                    self.control.chatInterval = setInterval(function () {
                        if ($('#back-to-top').hasClass('msg-alert'))
                            $('#back-to-top').removeClass('msg-alert');
                        else
                            $('#back-to-top').addClass('msg-alert');
                    }, 1000);
                } else {
                    if (self.control.chatInterval) {
                        clearInterval(self.control.chatInterval);
                        $('#back-to-top').removeClass('msg-alert');
                    }
                }
            });
    },
    watch: {
        title: function (val) {
            $('title').text(val + ' - JoyOI Online Judge');
        },
        fullScreen: function (val) {
            if (val) {
                $('body').addClass('full-screen');
            } else {
                $('body').removeClass('full-screen');
            }
        },
        'host.api': function (val) {
            if (qv)
                qv.__host = val;
        },
        deep: true
    },
    methods: {
        toggleLoginBox: function () {
            if (this.layout.loginBoxOpened) {
                this.layout.loginBoxOpened = false;
            } else {
                this.layout.loginBoxOpened = true;
            }
        },
        loginFocusIn: function (selector) {
            $(selector).parent('label').addClass('login-focus-label');
        },
        loginFocusOut: function (selector) {
            $(selector).parent('label').removeClass('login-focus-label');
        },
        login: function () {
            var self = this;
            app.notification('pending', '正在登录...');
            qv.put('/api/user/session', { username: $('#username').val(), password: $('#password').val() })
                .then(function (result) {
                    document.cookie.split(";").forEach(function (c) { document.cookie = c.replace(/^ +/, "").replace(/=.*/, "=;expires=" + new Date().toUTCString() + ";path=/"); });
                    if (document.cookie) {
                        qv.delete('/api/user/session');
                    }
                    document.cookie = result.data.cookie;
                    self.user.isSignedIn = true;
                    return qv.get('/api/user/session/info');
                })
                .then(x => {
                    self.user.profile.username = x.data.username;
                    self.user.profile.role = x.data.role;
                    self.user.profile.id = x.data.id;
                    self.user.tried = x.data.tried;
                    self.user.passed = x.data.passed;
                    self.user.groups = x.data.groups;
                    self.user.chat = x.data.chat;
                    self.preferences.language = x.data.preferredLanguage;
                    self.control.chatIframeUrl = x.data.chat;
                    self.notification("succeeded", "登录成功");
                    self.toggleLoginBox();

                    qv.reset();

                    var current = LazyRouting.GetCurrentComponent();
                    if (current && current.$options.created.length) {
                        current.$options.created[0].call(current);
                    }
                })
                .catch(err => {
                    self.notification("error", "登录失败", err.responseJSON.msg);
                });
        },
        logout: function () {
            document.cookie.split(";").forEach(function (c) { document.cookie = c.replace(/^ +/, "").replace(/=.*/, "=;expires=" + new Date().toUTCString() + ";path=/"); });
            if (document.cookie) {
                qv.delete('/api/user/session');
            }
            this.user.isSignedIn = false;
            app.notification('succeeded', '注销成功', '您已经注销了Joy OI的登录状态');
            qv.reset();

            var current = LazyRouting.GetCurrentComponent();
            if (current && current.$options.created.length) {
                current.$options.created[0].call(current);
            }
        },
        marked: function (str) {
            var dom = $(filterXSS(marked(str || "")));
            var ret = [];
            for (var i = 0; i < dom.length; i++) {
                if ($(dom[i]).is('pre')) {
                    hljs.highlightBlock(dom[i]);
                } else {
                    var blocks = $(dom[i]).find('pre code');
                    for (var j = 0; j < blocks.length; j++) {
                        hljs.highlightBlock(blocks[j]);
                    }
                }
                ret.push(dom[i].outerHTML);
            }
            return ret.join('\r\n');
        },
        xss: function (str) {
            return filterXSS(str);
        },
        notification: function (level, title, detail, button) {
            var item = { level: level, title: title, detail: detail };
            if (level === 'important') {
                item.button = button;
            }
            this.control.notifications.push(item);
            if (this.control.currentNotification && this.control.currentNotification.level === 'pending') {
                this.control.notificationLock = false;
            }
            this._showNotification(level === 'important' ? true : false);
        },
        redirect: function (name, path, params, query) {
            if (name && !path)
                path = name;
            LazyRouting.RedirectTo(name, path, params, query);
        },
        ensureUTCTimeString: function (timeStr) {
            if (!timeStr)
                return null;
            if (timeStr[timeStr.length - 1] !== 'Z')
                return timeStr + 'Z';
            else
                return timeStr;
        },
        toLocalTime: function (timeStr) {
            return moment(new Date(this.ensureUTCTimeString(timeStr))).format('YYYY-MM-DD HH:mm:ss');
        },
        toggleChatBox: function () {
            if ($('.chat-iframe').hasClass('active')) {
                $('.chat-iframe').removeClass('active');
                $('#back-to-top').css('margin-right', 0);
            } else {
                $('.chat-iframe').addClass('active');
                $('#back-to-top').css('margin-right', 350);
            }
        },
        resolveUrl: function (to) {
            if (typeof to === 'string')
                return to;
            if (to.name && !to.path)
                return to.name;
            if (!to.query)
                return to.path;
            var baseUrl = to.path + (to.path.indexOf('?') >= 0 ? '&' : '?');
            var args = [];
            for (var x in to.query) {
                args.push(x + '=' + encodeURIComponent(to.query[x]));
            }
            return baseUrl += args.join('&');
        },
        clickNotification: function () {
            this._releaseNotification();
        },
        _showNotification: function (manualRelease) {
            var self = this;
            if (!this.control.notificationLock && this.control.notifications.length) {
                this.control.notificationLock = true;
                var notification = this.control.notifications[0];
                this.control.notifications = this.control.notifications.slice(1);
                this.control.currentNotification = notification;
                if (!manualRelease) {
                    setTimeout(function () {
                        self._releaseNotification();
                    }, 5000);
                }
            }
        },
        _releaseNotification: function () {
            var self = this;
            self.control.currentNotification = null;
            setTimeout(function () {
                self.control.notificationLock = false;
                if (self.control.notifications.length) {
                    self._showNotification();
                }
            }, 250);
        },
        lookupUsers: function (query) {
            var cachedUsers = Object.getOwnPropertyNames(app.lookup.user);
            var uncachedUsers = (query.userIds ? query.userIds : query.usernames).filter(x => !cachedUsers.some(y => y == x));
            if (uncachedUsers.length) {
                return qv.get('/api/user/role', query.userIds ? { userIds: query.userIds.toString() } : { usernames: query.usernames.toString() })
                    .then(result => {
                        for (var i in result.data) {
                            var x = result.data[i];
                            app.lookup.user[x.id] = {
                                id: x.id,
                                avatar: x.avatarUrl,
                                name: x.username,
                                role: x.role,
                                class: ConvertUserRoleToCss(x.role)
                            };
                            app.lookup.user[x.username] = app.lookup.user[x.id];
                        }
                        return Promise.resolve();
                    })
                    .catch(err => {
                        console.error(err);
                    });
            } else {
                return Promise.resolve();
            }
        },
        lookupProblems: function (problemIds) {
            var cachedProblems = Object.getOwnPropertyNames(app.lookup.problem);
            var uncachedProblems = problemIds.filter(x => !cachedProblems.some(y => y == x));
            if (uncachedProblems.length) {
                return qv.get('/api/problem/title', { problemids: uncachedProblems.toString() })
                    .then(result => {
                        for (var i in result.data) {
                            var x = result.data[i];
                            app.lookup.problem[i] = x.title;
                        }
                        return Promise.resolve();
                    })
                    .catch(err => {
                        console.error(err);
                    });
            } else {
                return Promise.resolve();
            }
        }
    }
});

router.beforeEach((to, from, next) => {
    $('.xdsoft_datetimepicker').remove();
    app.fullScreen = false;
    next();
});

router.afterEach(() => {
    app.showUrlCover = router.history.current.fullPath != '/' && router.history.current.fullPath != '/home';
});
