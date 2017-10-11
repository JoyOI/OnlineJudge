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
        tab: 'passed'
    };
};

component.created = function () {
    var self = this;
    qv.createView('/api/user/' + router.history.current.params.username)
        .fetch(x => {
            self.id = x.data.id;
            self.username = x.data.userName;
            self.avatarUrl = x.data.avatarUrl;
            self.registeryTime = x.data.registeryTime;
            self.activeTime = x.data.activeTime;
            self.passedProblems = x.data.passedProblems;

            qv.createView('/api/user/role', { userids: x.data.id })
                .fetch(y =>
                {
                    self.role = y.data[x.data.id].role;
                });
        });
};