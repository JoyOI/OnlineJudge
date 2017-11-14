app.title = '评测记录';
app.links = [];

component.data = function () {
    return {
        paging: {
            current: 1,
            count: 1
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
        this.loadStatuses();
    },
    selectedProblem: function () {
        this.loadStatuses();
    },
    selectedSubmittor: function () {
        this.loadStatuses();
    },
    selectedLanguage: function () {
        this.loadStatuses();
    },
    selectedContest: function () {
        this.loadStatuses();
    },
    selectedTime: function () {
        this.loadStatuses();
    },
    'paging.current': function () {
        this.loadStatuses();
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
    this.loadStatuses();
};

component.methods = {
    filterStatus: function (status) {
        $('[data-value="' + this.selectedStatus + '"]').removeClass('active');
        if (this.selectedStatus !== null && this.selectedStatus === status) {
            this.selectedStatus = null;
        } else {
            this.selectedStatus = status;
            $('[data-value="' + this.selectedStatus + '"]').addClass('active');
        }
        setTimeout(function () {
            $('.filter-outer').removeClass('active');
        }, 300);
    },
    filterLanguage: function (language) {
        $('[data-value="' + this.selectedLanguage + '"]').removeClass('active');
        if (this.selectedLanguage && this.selectedLanguage == language) {
            this.selectedLanguage = null;
        } else {
            this.selectedLanguage = language;
            $('[data-value="' + this.selectedLanguage + '"]').addClass('active');
        }
        setTimeout(function () {
            $('.filter-outer').removeClass('active');
        }, 300);
    },
    filterTime: function (start, end) {
        if (!start && !end) {
            this.selectedTime = null;
        } else {
            this.selectedTime = { start: start, end: end };
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

                var cachedUsers = Object.getOwnPropertyNames(app.lookup.user);
                var uncachedUsers = self.submittorSearchResult.map(y => y.username).filter(y => !cachedUsers.some(z => z == y));
                if (uncachedUsers.length) {
                    qv.get('/api/user/role', { usernames: self.submittorSearchResult.map(y => y.username).toString() }).then(y => {
                        for (var z in y.data) {
                            app.lookup.user[z] = {
                                id: z.id,
                                avatar: y.data[z].avatarUrl,
                                name: y.data[z].username,
                                role: y.data[z].role,
                                class: ConvertUserRoleToCss(y.data[z].role)
                            };

                            app.lookup[y.data[z].username] = app.lookup.user[z];

                            var impactedResults = self.submittorSearchResult.filter(r => r.username == z);
                            for (var i in impactedResults) {
                                impactedResults[i].roleClass = app.lookup.user[z].class;
                            }
                        }
                    });
                }
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
            self.paging.current = x.data.current;
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
                var cachedProblems = Object.getOwnPropertyNames(app.lookup.problem);
                var uncachedProblems = x.data.result.map(y => y.problemId).filter(y => !cachedProblems.some(z => z == y));
                if (uncachedProblems.length) {
                    qv.get('/api/problem/title', { problemids: uncachedProblems.toString() })
                        .then(y => {
                            for (var z in y.data) {
                                app.lookup.problem[z] = y.data[z].title;
                                var impactedResults = self.result.filter(a => a.problemId == z);
                                for (var i in impactedResults) {
                                    impactedResults[i].problemTitle = app.lookup.problem[z];
                                }
                            }
                            self.$forceUpdate();
                        });
                }

                var cachedUsers = Object.getOwnPropertyNames(app.lookup.user);
                var uncachedUsers = x.data.result.map(y => y.userId).filter(y => !cachedUsers.some(z => z == y));
                if (uncachedUsers.length) {
                    qv.get('/api/user/role', { userids: uncachedUsers.toString() })
                        .then(y => {
                            for (var z in y.data) {
                                app.lookup.user[z] = {
                                    id: z.id,
                                    avatar: y.data[z].avatarUrl,
                                    name: y.data[z].username,
                                    role: y.data[z].role,
                                    class: ConvertUserRoleToCss(y.data[z].role)
                                };

                                app.lookup[y.data[z].id] = app.lookup.user[z];

                                var impactedResults = self.result.filter(a => a.userId == z);
                                for (var i in impactedResults) {
                                    impactedResults[i].userName = app.lookup.user[z].name;
                                    impactedResults[i].userRole = app.lookup.user[z].role;
                                    impactedResults[i].roleClass = app.lookup.user[z].class;
                                }
                            }
                            self.$forceUpdate();
                        });
                }
            }

            self.view.subscribe('judge');
        });
    }
};