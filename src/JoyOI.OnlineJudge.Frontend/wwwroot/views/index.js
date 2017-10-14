app = new Vue({
    router: router,
    data: {
        links: [],
        title: '',
        hosts: {
            vue: null,
            api: null,
            uc: null
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
                role: null
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
            notifications: []
        }
    },
    created: function () {
        this.hosts.api = 'http://localhost:5000';
        qv.__host = this.hosts.api;

        this.signalr.onlinejudge.connection = new signalR.HubConnection(this.hosts.api + '/signalr/onlinejudge');
        this.signalr.onlinejudge.connection.on('ItemUpdated', (type, id) =>
        {
            var listners = this.signalr.onlinejudge.listeners.filter(x => x.type === type && (x.id === id || !x.id)).map(x => {
                x.view.removeCache();
                return x.view.fetch(x.view._fetchFunc);
            });
        });
        this.signalr.onlinejudge.connection.start()

        if (document.cookie.indexOf("AspNetCore") >= 0) {
            var self = this;
            self.user.isSignedIn = true;
            qv.get('/api/user/session/info')
                .then((x) => {
                    self.user.profile.username = x.data.username;
                    self.user.profile.role = x.data.role;
                    self.user.profile.id = x.data.id;
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
                    self.toggleLoginBox();
                });
        },
        logout: function () {
            document.cookie += '; Expires=' + new Date(0).toUTCString();
            this.user.isSignedIn = false;
        },
        marked: function (str) {
            return filterXSS(marked(str || ""))
        },
        xss: function (str) {
            return filterXSS(str);
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
