component.data = function () {
    return {
        paging: {
            current: 1,
            count: 1,
            total: 0
        },
        hackStatuses: hackStatuses,
        judgeStatuses: statuses,
        result: [],
        selectedHackStatus: null,
        selectedJudgeStatus: null,
        selectedProblem: null,
        selectedHacker: null,
        selectedHackee: null,
        selectedContest: {},
        selectedTime: null,
        hackerSearchResult: [],
        hackeeSearchResult: [],
        problemSearchResult: [],
        view: null
    };
};

component.watch = {
    selectedHackStatus: function () {
        $('.status-filter').removeClass('active');
        var args = this.generateQuery();
        if (this.view) {
            this.view.unsubscribe();
        }
        app.redirect('/hack', '/hack', {}, args);
    },
    selectedJudgeStatus: function () {
        $('.status-filter').removeClass('active');
        var args = this.generateQuery();
        if (this.view) {
            this.view.unsubscribe();
        }
        app.redirect('/hack', '/hack', {}, args);
    },
    selectedProblem: function () {
        var args = this.generateQuery();
        delete args['paging.current'];
        if (this.view) {
            this.view.unsubscribe();
        }
        app.redirect('/hack', '/hack', {}, args);
    },
    selectedHacker: function () {
        var args = this.generateQuery();
        delete args['paging.current'];
        if (this.view) {
            this.view.unsubscribe();
        }
        app.redirect('/hack', '/hack', {}, args);
    },
    selectedHackee: function () {
        var args = this.generateQuery();
        delete args['paging.current'];
        if (this.view) {
            this.view.unsubscribe();
        }
        app.redirect('/hack', '/hack', {}, args);
    },
    selectedContest: function () {
        var args = this.generateQuery();
        delete args['paging.current'];
        if (this.view) {
            this.view.unsubscribe();
        }
        app.redirect('/hack', '/hack', {}, args);
    },
    selectedTime: function () {
        var args = this.generateQuery();
        delete args['paging.current'];
        if (this.view) {
            this.view.unsubscribe();
        }
        app.redirect('/hack', '/hack', {}, args);
    },
    'paging.current': function (newVal, oldVal) {
        var args = this.generateQuery();
        if (this.view) {
            this.view.unsubscribe();
        }
        app.redirect('/hack', '/hack', {}, args);
    },
    view: function (newVal, oldVal) {
        var fields = getFields(newVal.__cacheInfo.params);
        if (fields.length == 0 || fields.length == 1 && fields[0] == 'page') {
            newVal.subscribe('judge');
        }
        if (oldVal) {
            oldVal.unsubscribe();
        }
    },
    deep: true
};

component.created = function () {
    app.title = 'Hack记录';
    app.links = [];

    $('.datetime').datetimepicker();
    this.loadStatuses();
};

component.methods = {
    filterTime: function (begin, end) {
        if (!begin && !end) {
            this.selectedTime = null;
        } else {
            this.selectedTime = { begin: begin, end: end };
        }
    },
    searchHacker: function () {
        var val = $('.textbox-search-hacker').val();
        var self = this;
        if (!val) {
            self.hackerSearchResult = [];
            this.selectedHacker = null;
        } else {
            qv.createView('/api/user/all', { username: val }).fetch(x => {
                self.hackerSearchResult = x.data.result.map(y => {
                    return {
                        id: y.id,
                        username: y.userName,
                        avatarUrl: y.avatarUrl,
                        roleClass: app.lookup.user[y.id] ? app.lookup.user[y.id].class : undefined
                    }
                }).slice(0, 5);

                app.lookupUsers({ usernames: self.hackerSearchResult.map(y => y.username) })
                    .then(() => {
                        for (var i = 0; i < self.hackerSearchResult.length; i++) {
                            self.hackerSearchResult[i].roleClass = app.lookup.user[self.hackerSearchResult[i].username].class;
                        }
                        this.$forceUpdate();
                    });
            });
        }
    },
    searchHackee: function () {
        var val = $('.textbox-search-hackee').val();
        var self = this;
        if (!val) {
            self.hackeeSearchResult = [];
            this.selectedHackee = null;
        } else {
            qv.createView('/api/user/all', { username: val }).fetch(x => {
                self.hackeeSearchResult = x.data.result.map(y => {
                    return {
                        id: y.id,
                        username: y.userName,
                        avatarUrl: y.avatarUrl,
                        roleClass: app.lookup.user[y.id] ? app.lookup.user[y.id].class : undefined
                    }
                }).slice(0, 5);

                app.lookupUsers({ usernames: self.hackeeSearchResult.map(y => y.username) })
                    .then(() => {
                        for (var i = 0; i < self.hackeeSearchResult.length; i++) {
                            self.hackeeSearchResult[i].roleClass = app.lookup.user[self.hackeeSearchResult[i].username].class;
                        }
                        this.$forceUpdate();
                    });
            });
        }
    },
    selectHacker: function (username) {
        this.selectedHacker = username;
        $('.filter-outer').removeClass('active');
    },
    selectHackee: function (username) {
        this.selectedHackee = username;
        $('.filter-outer').removeClass('active');
    },
    searchProblem: function () {
        var val = $('.textbox-search-problem').val();
        var self = this;
        if (!val) {
            self.problemSearchResult = [];
            self.selectedProblem = null;
        } else {
            qv.createView('/api/problem/all', { title: val }).fetch(x => {
                self.problemSearchResult = x.data.result.map(y => {
                    return {
                        id: y.id,
                        title: y.title
                    };
                }).slice(0, 5);
            });
        }
    },
    selectProblem: function (id, title) {
        this.selectedProblem = { id: id, title: title };
        $('.problem-filter').removeClass('active');
    },
    selectTimeRange: function () {
        this.selectedTime = {
            begin: $('.time-range-begin').val(),
            end: $('.time-range-end').val()
        };
        $('.time-filter').removeClass('active');
    },
    clearTimeRange: function () {
        this.selectedTime = null;
        $('.time-range-begin').val('')
        $('.time-range-end').val('')
        $('.time-filter').removeClass('active');
    },
    loadStatuses: function () {
        var self = this;
        if (self.view) {
            self.view.unsubscribe();
        }
        self.view = qv.createView('/api/hack/all', {
            problemid: self.selectedProblem ? self.selectedProblem.id : null,
            hackStatus: self.selectedHackStatus,
            judgeStatus: self.selectedJudgeStatus,
            hacker: self.selectedHacker,
            hackee: self.selectedHackee,
            contestId: self.selectedContest.id,
            begin: self.selectedTime && self.selectedTime.begin ? new Date(self.selectedTime.begin).toISOString() : null,
            end: self.selectedTime && self.selectedTime.end ? new Date(self.selectedTime.end).toISOString() : null,
            page: self.paging.current
        });
        self.view.fetch(x => {
            if (this.selectedContest.id) {
                app.links = [
                    {
                        text: '比赛列表',
                        to: '/contest'
                    },
                    {
                        text: this.selectedContest.title,
                        to: { name: '/contest/:id', path: '/contest/' + this.selectedContest.id, params: { id: this.selectedContest.id } }
                    }
                ];
            }

            self.paging.count = x.data.count;
            self.result = x.data.result.map(y =>
            {
                var ret = clone(y);
                ret.hackResult = formatJudgeResult(y.hackResult);
                ret.judgeResult = formatJudgeResult(y.judgeResult);
                ret.hackClass = ConvertJudgeResultToCss(ret.hackResult);
                ret.judgeClass = ConvertJudgeResultToCss(ret.judgeResult, true);
                ret.hackIcon = ConvertJudgeResultToIconCss(ret.hackResult);
                ret.judgeIcon = ConvertJudgeResultToIconCss(ret.judgeResult);
                ret.hacker = app.lookup.user[y.hackerId] ? app.lookup.user[y.hackerId].name : y.hackerId.substr(0, 8);
                ret.hackee = app.lookup.user[y.hackeeId] ? app.lookup.user[y.hackeeId].name : y.hackeeId.substr(0, 8);
                ret.hackerRole = app.lookup.user[y.hackerId] ? app.lookup.user[y.hackerId].class : undefined;
                ret.hackeeRole = app.lookup.user[y.hackeeId] ? app.lookup.user[y.hackeeId].class : undefined;
                return ret;
            });

            if (self.result.length) {
                app.lookupProblems(x.data.result.map(y => y.problemId))
                    .then(() => {
                        for (var i in self.result) {
                            self.result[i].problemTitle = app.lookup.problem[self.result[i].problemId];
                        }
                        self.$forceUpdate();
                    });

                app.lookupUsers({ userIds: x.data.result.map(y => y.hackerId) })
                    .then(() => {
                        for (var i = 0; i < self.result.length; i ++) {
                            self.result[i].hacker = app.lookup.user[self.result[i].hackerId].name;
                            self.result[i].hackerRole = app.lookup.user[self.result[i].hackerId].class;
                        }
                        this.$forceUpdate();
                    });

                app.lookupUsers({ userIds: x.data.result.map(y => y.hackeeId) })
                    .then(() => {
                        for (var i = 0; i < self.result.length; i++) {
                            self.result[i].hackee = app.lookup.user[self.result[i].hackeeId].name;
                            self.result[i].hackeeRole = app.lookup.user[self.result[i].hackeeId].class;
                        }
                        this.$forceUpdate();
                    });
            }

            self.view.subscribe('judge');
        });
    },
    generateQuery: function () {
        var args = {};
        if (this.paging.current && this.paging.current !== 1) {
            args['paging.current'] = this.paging.current;
        }
        if (this.selectedHackStatus !== null) {
            args['selectedHackStatus'] = this.selectedHackStatus;
        }
        if (this.selectedJudgeStatus !== null) {
            args['selectedJudgeStatus'] = this.selectedJudgeStatus;
        }
        if (this.selectedProblem) {
            if (this.selectedProblem.id)
                args['selectedProblem.id'] = this.selectedProblem.id;
            if (this.selectedProblem.title)
                args['selectedProblem.title'] = this.selectedProblem.title;
        }
        if (this.selectedHacker) {
            args['selectedHacker'] = this.selectedHacker;
        }
        if (this.selectedHackee) {
            args['selectedHackee'] = this.selectedHackee;
        }
        if (this.selectedContest) {
            if (this.selectedContest.title)
                args['selectedContest.title'] = this.selectedContest.title;
            if (this.selectedContest.id)
                args['selectedContest.id'] = this.selectedContest.id;
        }
        if (this.selectedTime) {
            if (this.selectedTime.begin)
                args['selectedTime.begin'] = this.selectedTime.begin;
            if (this.selectedTime.end)
                args['selectedTime.end'] = this.selectedTime.end;
        }
        return args;
    }
};