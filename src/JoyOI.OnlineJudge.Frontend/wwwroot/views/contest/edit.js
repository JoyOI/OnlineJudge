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
        referees: [],
        view: null,
        problemView: null,
        selectedProblem: null,
        problems: [],
        problemSearchResult: [],
        allLanguages: languages,
        languages: [],
        claimView: null
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
                this.begin = app.toLocalTime(x.data.begin);
                this.end = app.toLocalTime(x.data.end);
                this.type = x.data.type;
                this.attendPermission = x.data.attendPermission;
                this.isHighlighted = x.data.isHighlighted;
                this.disableVirtual = x.data.disableVirtual;
                this.languages = x.data.bannedLanguagesArray || [];
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

        this.claimView = qv.createView('/api/contest/' + this.id + '/claim/all')
        this.claimView
            .fetch(x => {
                var self = this;
                self.claims = x.data;
                self.referees = x.data.map(y => {
                    return { userId: y.userId };
                });
                app.lookupUsers({ userIds: self.referees.map(y => y.userId) })
                    .then(() => {
                        for (var i = 0; i < self.referees.length; i++) {
                            self.referees[i].username = app.lookup.user[self.referees[i].userId].name;
                            self.referees[i].roleClass = app.lookup.user[self.referees[i].userId].class;
                            self.referees[i].avatarUrl = app.lookup.user[self.referees[i].userId].avatar;
                        }
                        self.$forceUpdate();
                    });
            });
    },
    loadContestProblem: function () {
        var self = this;
        if (!this.problemView) {
            this.problemView = qv.createView('/api/contest/' + this.id + '/problem/all');
            this.problemView
                .fetch(x => {
                    self.problems = x.data;
                    if (x.data.length) {
                        app.lookupProblems(x.data.map(y => y.problemId))
                            .then(() => {
                                for (var i = 0; i < self.problems.length; i++) {
                                    self.problems[i].problemTitle = app.lookup.problem[self.problems[i].problemId];
                                }
                                self.$forceUpdate();
                            });
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
        var request = {
            title: this.title,
            description: this.description,
            duration: this.duration,
            attendPermission: this.attendPermission,
            begin: $('#txtBegin').length ? new Date($('#txtBegin').val()).toGMTString() : null,
            isHighlighted: this.isHighlighted,
            disableVirtual: this.disableVirtual
        };

        if (!request.begin) {
            delete request.begin;
        }
        qv.patch('/api/contest/' + this.id, request)
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
    },
    saveBanLang: function () {
        var lang = [];
        var doms = $('.language-ban-selector');
        for (var i = 0; i < doms.length; i++) {
            if ($(doms[i]).val() === 'deny') {
                lang.push($(doms[i]).attr('data-value'));
            }
        }

        app.notification('pending', '正在设置比赛语言');
        qv.patch('/api/contest/' + this.id, { bannedLanguages: lang.toString() })
            .then((x) => {
                app.notification('succeeded', '比赛语言设置成功', x.msg);
            })
            .catch(err => {
                app.notification('error', '比赛语言设置失败', err.responseJSON.msg);
            });
    },
    addReferee: function () {
        app.notification('pending', '正在添加比赛裁判');
        qv.put('/api/contest/' + this.id + '/claim/' + $('#txtUsername').val())
            .then((x) => {
                app.notification('succeeded', '比赛裁判添加成功', x.msg);
                $('#txtUsername').val('');
                this.claimView.refresh();
            })
            .catch(err => {
                app.notification('error', '裁判添加失败', err.responseJSON.msg);
            });
    },
    removeReferee: function (username) {
        app.notification('pending', '正在删除比赛裁判');
        qv.delete('/api/contest/' + this.id + '/claim/' + username)
            .then((x) => {
                app.notification('succeeded', '比赛裁判删除成功', x.msg);
                this.claimView.refresh();
            })
            .catch(err => {
                app.notification('error', '裁判删除失败', err.responseJSON.msg);
            });
    }
};

component.created = function () {
    app.title = '编辑比赛';
    app.links = [{ text: '比赛列表', to: '/contest' }, { text: '未知比赛', to: '/contest' }];

    this.loadContest();
    this.loadContestProblem();
};