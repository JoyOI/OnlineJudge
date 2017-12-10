component.data = function () {
    return {
        paging: {
            current: 1,
            count: 1,
            total: 0
        },
        statuses: statuses,
        result: [],
        languages: languages,
        selectedStatus: null,
        selectedProblem: null,
        selectedSubmittor: null,
        selectedLanguage: null,
        selectedContest: {},
        selectedTime: null,
        submittorSearchResult: [],
        problemSearchResult: [],
        view: null
    };
};

component.watch = {
    selectedStatus: function () {
        $('.status-filter').removeClass('active');
        var args = this.generateQuery();
        app.redirect('/judge', '/judge', {}, args);
    },
    selectedProblem: function () {
        var args = this.generateQuery();
        delete args['paging.current'];
        app.redirect('/judge', '/judge', {}, args);
    },
    selectedSubmittor: function () {
        var args = this.generateQuery();
        delete args['paging.current'];
        app.redirect('/judge', '/judge', {}, args);
    },
    selectedLanguage: function () {
        $('.language-filter').removeClass('active');
        var args = this.generateQuery();
        delete args['paging.current'];
        app.redirect('/judge', '/judge', {}, args);
    },
    selectedContest: function () {
        var args = this.generateQuery();
        delete args['paging.current'];
        app.redirect('/judge', '/judge', {}, args);
    },
    selectedTime: function () {
        var args = this.generateQuery();
        delete args['paging.current'];
        app.redirect('/judge', '/judge', {}, args);
    },
    'paging.current': function (newVal, oldVal) {
        var args = this.generateQuery();
        app.redirect('/judge', '/judge', {}, args);
    },
    view: function (newVal, oldVal) {
        var fields = getFields(newVal.__cacheInfo.params);
        if (fields.length == 0 || fields.length == 1 && fields[0] == 'page') {
            newVal.subscribe('judge');
        }
    },
    deep: true
};

component.created = function () {
    app.title = '评测记录';
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
    searchSubmittor: function () {
        var val = $('.textbox-search-submittor').val();
        var self = this;
        if (!val) {
            self.submittorSearchResult = [];
            this.selectedSubmittor = null;
        } else {
            qv.createView('/api/user/all', { username: val }).fetch(x => {
                self.submittorSearchResult = x.data.result.map(y => {
                    return {
                        id: y.id,
                        username: y.userName,
                        avatarUrl: y.avatarUrl,
                        roleClass: app.lookup.user[y.id] ? app.lookup.user[y.id].class : undefined
                    }
                }).slice(0, 5);

                app.lookupUsers({ usernames: self.submittorSearchResult.map(y => y.username) })
                    .then(() => {
                        for (var i = 0; i < self.submittorSearchResult.length; i++) {
                            self.submittorSearchResult[i].roleClass = app.lookup.user[self.submittorSearchResult[i].username].class;
                        }
                        this.$forceUpdate();
                    });
            });
        }
    },
    selectSubmittor: function (username) {
        this.selectedSubmittor = username;
        $('.submit-filter').removeClass('active');
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
        self.view = qv.createView('/api/judge/all', {
            problemid: self.selectedProblem ? self.selectedProblem.id : null,
            status: self.selectedStatus,
            userId: self.selectedSubmittor,
            contestId: self.selectedContest.id,
            language: self.selectedLanguage,
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
                ret.result = formatJudgeResult(y.result);
                ret.resultClass = ConvertJudgeResultToCss(ret.result);
                ret.iconClass = ConvertJudgeResultToIconCss(ret.result);
                ret.userName = app.lookup.user[y.userId] ? app.lookup.user[y.userId].name : y.userId.substr(0, 8);
                ret.userRole = app.lookup.user[y.userId] ? app.lookup.user[y.userId].role : undefined;
                ret.roleClass = app.lookup.user[y.userId] ? app.lookup.user[y.userId].class : undefined;
                ret.problemTitle = app.lookup.problem[ret.problemId];
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

                app.lookupUsers({ userIds: x.data.result.map(y => y.userId) })
                    .then(() => {
                        for (var i = 0; i < self.result.length; i ++) {
                            self.result[i].userName = app.lookup.user[self.result[i].userId].name;
                            self.result[i].userRole = app.lookup.user[self.result[i].userId].role;
                            self.result[i].roleClass = app.lookup.user[self.result[i].userId].class;
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
        if (this.selectedStatus !== null) {
            args['selectedStatus'] = this.selectedStatus;
        }
        if (this.selectedProblem) {
            if (this.selectedProblem.id)
                args['selectedProblem.id'] = this.selectedProblem.id;
            if (this.selectedProblem.title)
                args['selectedProblem.title'] = this.selectedProblem.title;
        }
        if (this.selectedSubmittor) {
            args['selectedSubmittor'] = this.selectedSubmittor;
        }
        if (this.selectedLanguage) {
            args['selectedLanguage'] = this.selectedLanguage;
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