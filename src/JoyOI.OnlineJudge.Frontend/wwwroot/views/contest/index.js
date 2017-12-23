component.data = function () {
    return {
        id: router.history.current.params.id,
        title: null,
        body: null,
        attendeeCount: 0,
        begin: null,
        end: null,
        type: null,
        claims: [],
        problems: [],
        sessionView: null,
        session: {
            begin: null,
            end: null,
            isRegistered: false,
            isBegan: true,
            isEnded: true,
            isStandingsAvailable: false,
            allowLock: false
        },
        disableVirtual: false
    };
};

component.computed = {
    status: function () {
        if (this.begin && new Date() < new Date(this.begin))
            return 'Pending';
        else if (this.end && new Date() <= new Date(this.end))
            return 'Live';
        else if (this.end && new Date() > new Date(this.end))
            return 'Done';
        else
            return 'Pending';
    },
    beginTimestamp: function () {
        return new Date(this.begin).getTime();
    },
    endTimestamp: function () {
        return new Date(this.end).getTime();
    },
    countDownTimestamp: function () {
        if (this.status === 'Pending')
            return this.beginTimestamp;
        else
            return this.endTimestamp;
    },
    hasPermissionToEdit: function () {
        return app.user.isSignedIn && (app.user.profile.role === 'Root' || app.user.profile.role === 'Master' || this.claims.some(x => x === app.user.profile.username));
    }
};

component.methods = {
    loadContest: function () {
        qv.createView('/api/contest/' + this.id)
            .fetch(x => {
                this.title = x.data.title;
                this.body = x.data.description;
                this.attendeeCount = x.data.CachedAttendeeCount;
                this.begin = x.data.begin;
                this.end = x.data.end;
                this.type = x.data.type;
                this.disableVirtual = x.data.disableVirtual;
                app.title = this.title;
            })
            .catch(err => {
                if (err.code == 404) {
                    app.redirect('/404', '/404');
                } else {
                    app.notification('error', '获取题目信息失败', err.responseJSON.msg);
                }
            });
        qv.createView('/api/contest/' + this.id + '/claim/all')
            .fetch(x => {
                this.claims = x.data;
            });
    },
    loadContestProblem: function () {
        var self = this;
        if (!this.problemView) {
            this.problemView = qv.createView('/api/contest/' + this.id + '/problem/all', {}, 60000);
            this.problemView
                .fetch(x => {
                    this.problems = x.data;

                    app.lookupProblems(x.data.map(y => y.problemId))
                        .then(() => {
                            for (var i = 0; i < this.problems.length; i++) {
                                this.problems[i].problemTitle = app.lookup.problem[x.data[i].problemId];
                            }
                            this.$forceUpdate();
                        });
                });
        } else {
            this.problemView.refresh();
        }
    },
    getContestSession: function () {
        var self = this;
        self.sessionView = qv.createView('/api/contest/' + self.id + '/session', {}, 60000);
        self.sessionView.fetch(x => {
            self.session = x.data;
            self.loadContestProblem();
        });
    },
    lockProblem: function (problemId) {
        if (confirm("锁定题目后您将无法再次提交该题，您确定要锁定该题吗？")) {
            app.notification('pending', '正在锁定题目');
            qv.put('/api/contest/' + this.id + '/problem/' + problemId + '/lock', {})
                .then(x => {
                    self.sessionView.refresh();
                    app.notification('succeeded', '题目锁定成功', x.msg);
                })
                .catch(err => {
                    app.notification('error', '题目锁定失败', err.responseJSON.msg);
                });
        }
    }
};

component.created = function () {
    app.title = '比赛';
    app.links = [{ text: '比赛列表', to: '/contest' }];

    this.loadContest();
    this.getContestSession();
};