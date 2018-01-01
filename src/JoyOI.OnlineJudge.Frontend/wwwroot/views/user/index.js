component.data = function () {
    return {
        id: null,
        role: null,
        username: null,
        avatarUrl: null,
        registeryTime: null,
        activeTime: null,
        topics: [],
        passedProblems: [],
        uploadedProblems: [],
        posts: [],
        postCount: 0,
        motto: null,
        tab: 'passed',
        groups: {
            current: 1,
            count: 1,
            total: 0,
            result: []
        }
    };
};

component.watch = {
    deep: true,
    'groups.current': function () {
        app.redirect('/group', '/group', {}, { 'groups.current': this.paging.current });
    }
};

component.created = function () {
    app.title = '用户资料';
    app.links = [];

    var self = this;
    qv.createView('/api/user/' + router.history.current.params.username, 600 * 1000)
        .fetch(x => {
            self.id = x.data.id;
            self.username = x.data.username;
            self.avatarUrl = x.data.avatarUrl;
            self.registeryTime = x.data.registeryTime;
            self.activeTime = x.data.activeTime;
            self.motto = x.data.motto;
            self.preferredLanguage = x.data.preferredLanguage;
            self.passedProblems = x.data.passedProblems.map(y => {
                return {
                    id: y,
                    title: null
                }
            });

            if (x.data.passedProblems.length) {
                qv.createView('/api/problem/title', { problemids: x.data.passedProblems.toString() })
                    .fetch(y => {
                        for (var i = 0; i < self.passedProblems.length; i++) {
                            self.passedProblems[i].title = y.data[self.passedProblems[i].id].title;
                        }
                    });
            }

            qv.createView('/api/user/role', { userids: x.data.id })
                .fetch(y => {
                    self.role = y.data[x.data.id].role;
                });
        })
        .catch(err => {
            if (err.responseJSON.code == 404) {
                app.redirect('/404', '/404');
            } else {
                app.notification('error', '获取用户信息失败', err.responseJSON.msg);
            }
        });;
    qv.createView('/api/user' + router.history.current.params.username + '/uploadedproblem', 600 * 1000)
        .fetch(x => {
            self.uploadedProblems = x.data.result;
        });
    qv.createView('/api/user/' + router.history.current.params.username + '/blog/posts', 1800 * 1000)
        .fetch(x => {
            self.posts = x.data.result;
            self.postCount = x.data.count;
        });

    qv.createView('/api/user/' + router.history.current.params.username + '/group', 3600 * 1000)
        .fetch(x => {
            self.groups.count = x.data.count;
            self.groups.current = x.data.current;
            self.groups.total = x.data.total;
            self.groups.result = x.data.result;
        });
};

component.watch = {
    tab: function (val) {
        if (val === 'passed') {
            app.redirect('/user/:username', '/user/' + this.id, { id: this.id });
        } else {
            app.redirect('/user/:username', '/user/' + this.id, { id: this.id }, { tab: val });
        }
    }
};