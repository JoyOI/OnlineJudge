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
        session: {
            begin: null,
            end: null,
            isRegistered: false,
            isBegan: true,
            isEnded: true,
            isStandingsAvailable: false
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
                app.notification('error', '获取比赛信息失败', err.responseJSON.msg);
            });
        qv.createView('/api/contest/' + this.id + '/claim/all')
            .fetch(x => {
                this.claims = x.data;
            });
    },
    loadContestProblem: function () {
        var self = this;
        if (!this.problemView) {
            this.problemView = qv.createView('/api/contest/' + this.id + '/problem/all');
            this.problemView
                .fetch(x => {
                    this.problems = x.data;
                    if (x.data.length) {
                        var cachedProblems = Object.getOwnPropertyNames(app.lookup.problem);
                        var uncachedProblems = x.data.map(y => y.problemId).filter(y => !cachedProblems.some(z => z == y));
                        if (uncachedProblems.length) {
                            qv.get('/api/problem/title', { problemids: uncachedProblems.toString() })
                                .then(y => {
                                    for (var z in y.data) {
                                        app.lookup.problem[z] = y.data[z].title;
                                        var impactedResults = self.problems.filter(a => a.problemId == z);
                                        for (var i in impactedResults) {
                                            impactedResults[i].problemTitle = app.lookup.problem[z];
                                        }
                                    }
                                    self.$forceUpdate();
                                });
                        }
                    }
                });
        } else {
            this.problemView.refresh();
        }
    },
    getContestSession: function () {
        qv.createView('/api/contest/' + this.id + '/session', 60000)
            .fetch(x => {
                this.session = x.data;
            });
    }
};

component.created = function () {
    app.title = '比赛';
    app.links = [{ text: '比赛列表', to: '/contest' }];

    this.loadContest();
    this.loadContestProblem();
    if (app.user.isSignedIn) {
        this.getContestSession();
    }
};