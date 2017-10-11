app.title = '用户资料';
app.links = [];

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
        motto: null,
        tab: 'passed'
    };
};

component.created = function () {
    var self = this;
    qv.createView('/api/user/' + router.history.current.params.username)
        .fetch(x => {
            self.id = x.data.id;
            self.username = x.data.username;
            self.avatarUrl = x.data.avatarUrl;
            self.registeryTime = x.data.registeryTime;
            self.activeTime = x.data.activeTime;
            self.motto = x.data.motto;
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
                .fetch(y =>
                {
                    self.role = y.data[x.data.id].role;
                });
        });
};