component.data = function () {
    return {
        username: null,
        password: null
    };
}

component.methods = {
    login: function () {
        var self = app;
        app.notification('pending', '正在登录...');
        qv.put('/api/user/session', { username: this.username, password: this.password })
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

                if (this.isGroup && this.groupSessionView) {
                    this.groupSessionView.refresh();
                }

                qv.reset();

                app.redirect('/', '/');
            })
            .catch(err => {
                self.notification("error", "登录失败", err.responseJSON.msg);
            });
    }
};

component.created = function () {
    app.title = '登录';
    app.links = [];
};