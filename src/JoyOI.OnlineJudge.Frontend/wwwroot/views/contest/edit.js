app.title = '编辑比赛';
app.links = [{ text: '比赛列表', to: '/contest' }, { text: '未知比赛', to: '/contest' }];

component.data = function () {
    return {
        id: router.history.current.params.id,
        current: 'basic',
        title: null,
        description: null,
        duration: null,
        attendPermission: null,
        attendeeCount: 0,
        begin: null,
        end: null,
        duration: null,
        isHighlighted: null,
        type: null,
        disableVirtual: null,
        claims: [],
        view: null,
        problemView: null,
        selectedProblem: null,
        problems: [],
        problemSearchResult: [],
    };
};

component.methods = {
    loadContest: function () {
        if (!this.view) {
            this.view = qv.createView('/api/contest/' + this.id);
            this.view.fetch(x => {
                this.title = x.data.title;
                this.description = x.data.description;
                this.duration = x.data.duration;
                this.attendeeCount = x.data.CachedAttendeeCount;
                this.begin = x.data.begin;
                this.end = x.data.end;
                this.type = x.data.type;
                this.attendPermission = x.data.attendPermission;
                this.isHighlighted = x.data.isHighlighted;
                this.disableVirtual = x.data.disableVirtual;
                app.links[1].text = x.data.title;
                app.links[1].to = { name: '/contest/:id', path: '/contest/' + this.id, params: { id: this.id } };
                try {
                    $('.markdown-textbox')[0].smde.codemirror.setValue(x.data.description);
                } catch (ex) { }
            })
                .catch(err => {
                    app.notification('error', '获取比赛信息失败', err.responseJSON.msg);
                });
        } else {
            this.view.refresh();
        }

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
                });
        } else {
            this.problemView.refresh();
        }
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
    saveBasic: function () {
        this.description = $('.markdown-textbox')[0].smde.codemirror.getValue();
        app.notification('pending', '正在保存比赛');
        qv.patch('/api/contest/' + this.id, {
            title: this.title,
            description: this.description,
            duration: this.duration,
            attendPermission: this.attendPermission,
            begin: this.begin,
            isHighlighted: this.isHighlighted,
            disableVirtual: this.disableVirtual
        })
            .then((x) => {
                app.notification('succeeded', '比赛编辑成功', x.msg);
                this.view.refresh();
            })
            .catch(err => {
                app.notification('error', '比赛编辑失败', err.responseJSON.msg);
            });
    },
    addProblem: function (id) {
        app.notification('pending', '正在添加比赛题目');
        qv.put('/api/contest/' + this.id + '/problem/' + id, {
            point: $('#txtPoint').val(),
            number: $('#txtNumber').val()
        })
            .then((x) => {
                app.notification('succeeded', '比赛题目添加成功', x.msg);
                $('#txtPoint').val(100);
                $('#txtNumber').val('')
                this.selectedProblem = null;
                this.problemView.refresh();
            })
            .catch(err => {
                app.notification('error', '比赛题目添加失败', err.responseJSON.msg);
            });
    },
    removeProblem: function (id) {
        app.notification('pending', '正在删除比赛题目');
        qv.delete('/api/contest/' + this.id + '/problem/' + id)
            .then((x) => {
                app.notification('succeeded', '比赛题目删除成功', x.msg);
                this.problemView.refresh();
            })
            .catch(err => {
                app.notification('error', '比赛题目删除失败', err.responseJSON.msg);
            });
    }
};

component.created = function () {
    this.loadContest();
    this.loadContestProblem();
};