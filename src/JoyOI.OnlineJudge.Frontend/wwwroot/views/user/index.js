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
        tab: 'passed'
    };
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
        });
    qv.createView('/api/user' + router.history.current.params.username + '/uploadedproblem', 600 * 1000)
        .fetch(x => {
            self.uploadedProblems = x.data.result;
        });
    qv.createView('/api/user/' + router.history.current.params.username + '/blog/posts', 1800 * 1000)
        .fetch(x => {
            self.posts = x.data.result;
            self.postCount = x.data.count;
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