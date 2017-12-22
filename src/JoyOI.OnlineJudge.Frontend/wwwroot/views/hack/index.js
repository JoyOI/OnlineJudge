component.data = function () {
    return {
        control: {
            judgeStatuses: statuses,
            hackStatuses: hackStatuses,
            languages: languages,
            highlighters: syntaxHighlighter
        },
        id: null,
        hint: null,
        data: null,
        time: null,
        problemTitle: null,
        problemId: null,
        userId: null,
        username: null,
        userAvatarUrl: null,
        userRoleClass: null,
        view: null,
        hackeeClass: null,
        hackeeResult: null,
        hackerClass: null,
        hackerResult: null,
        timeUsedInMs: null,
        memoryUsedInByte: null,
        judgeStatusId: null
    };
};

component.created = function () {
    app.title = 'Hack结果'
    app.links = [{ text: 'Hack', to: '/hack' }];

    this.id = router.history.current.params.id;
    var self = this;
    self.view = qv.createView('/api/hack/' + this.id);
    self.view.fetch(x => {
        self.data = x.data.hackDataBody;
        self.hint = x.data.hint;
        self.time = x.data.time;
        self.problemId = x.data.problemId;
        self.userId = x.data.userId.substr(0, 8);
        self.timeUsedInMs = x.data.timeUsedInMs;
        self.memoryUsedInByte = x.data.memoryUsedInByte;
        self.hackeeResult = formatJudgeResult(x.data.hackeeResult);
        self.hackerResult = formatJudgeResult(x.data.result);
        self.judgeStatusId = x.data.judgeStatusId;

        qv.createView('/api/user/role', { userids: x.data.userId })
            .fetch(y =>
            {
                self.username = y.data[x.data.userId].username;
                self.userRoleClass = ConvertUserRoleToCss(y.data[x.data.userId].role);
                self.userAvatarUrl = y.data[x.data.userId].avatarUrl;
            });
    })
        .catch(err => {
            if (err.responseJSON.code == 404) {
                app.redirect('/404', '/404');
            } else {
                app.notification('error', '获取评测信息失败', err.responseJSON.msg);
            }
        });
    this.view.subscribe('hack', this.id);
};