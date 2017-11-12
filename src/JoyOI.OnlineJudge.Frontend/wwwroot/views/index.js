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
                passed: []
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
        control: {
            apiLock: false,
            currentNotification: null,
            notifications: [],
            notificationLock: false,
            userInfoView: null
        }
    },
    created: function () {
        /* Initialize host addresses */
        this.hosts.blog = 'http://{USERNAME}.blog.joyoi.net';
        this.hosts.forum = 'http://forum.joyoi.net';
        this.hosts.api = 'http://localhost:5000';
        this.hosts.uc = 'http://uc.joyoi.cn';
        qv.__host = this.hosts.api;

        this.signalr.onlinejudge.connection = new signalR.HubConnection(this.hosts.api + '/signalr/onlinejudge');
        this.signalr.onlinejudge.connection.on('ItemUpdated', (type, id) => {
            var listners = this.signalr.onlinejudge.listeners.filter(x => x.type === type && (x.id === id || !x.id)).map(x => {
                x.view.removeCache();
                return x.view.fetch(x.view._fetchFunc);
            });
        });
        this.signalr.onlinejudge.connection.start()

        if (document.cookie.indexOf("AspNetCore") >= 0) {
            var self = this;
            self.user.isSignedIn = true;
            self.control.userInfoView = qv.createView('/api/user/session/info');
            self.control.userInfoView.fetch((x) => {
                self.user.profile.username = x.data.username;
                self.user.profile.role = x.data.role;
                self.user.profile.id = x.data.id;
                self.user.tried = x.data.tried;
                self.user.passed = x.data.passed;
            });
        }
        else {
            this.user.isSignedIn = false;
        }
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
                    document.cookie = result.data.cookie;
                    self.user.isSignedIn = true;
                    return qv.get('/api/user/session/info');
                })
                .then(x => {
                    self.user.profile.username = x.data.username;
                    self.user.profile.role = x.data.role;
                    self.user.profile.id = x.data.id;
                    self.preferences.language = x.data.preferredLanguage;
                    self.notification("succeeded", "登录成功");
                    self.toggleLoginBox();
                })
                .catch(err => {
                    self.notification("error", "登录失败", err.responseJSON.msg);
                });
        },
        logout: function () {
            document.cookie += '; Expires=' + new Date(0).toUTCString();
            this.user.isSignedIn = false;
            app.notification('succeeded', '注销成功', '您已经注销了Joy OI的登录状态');
        },
        marked: function (str) {
            return filterXSS(marked(str || ""))
        },
        xss: function (str) {
            return filterXSS(str);
        },
        notification: function (level, title, detail) {
            this.control.notifications.push({ level: level, title: title, detail: detail });
            if (this.control.currentNotification && this.control.currentNotification.level === 'pending') {
                this.control.notificationLock = false;
            }
            this._showNotification();
        },
        redirect: function (name, path, params) {
            if (name && !path)
                path = name;
            LazyRouting.RedirectTo(name, path, params);
        },
        toLocalTime: function (timeStr) {
            return moment(new Date(timeStr + 'Z')).format('YYYY-MM-DD HH:mm:ss');
        },
        _showNotification: function () {
            var self = this;
            if (!this.control.notificationLock && this.control.notifications.length) {
                this.control.notificationLock = true;
                var notification = this.control.notifications[0];
                this.control.notifications = this.control.notifications.slice(1);
                this.control.currentNotification = notification;
                setTimeout(function () {
                    self.control.currentNotification = null;
                    setTimeout(function () {
                        self.control.notificationLock = false;
                        if (self.control.notifications.length) {
                            self._showNotification();
                        }
                    }, 250);
                }, 4000);
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
