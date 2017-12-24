component.created = function () {
    app.title = '比赛排名';
    app.links = [{ text: '比赛列表', to: '/contest' }, { text: '未知比赛', to: '/contest' }];
    this.loadStandings();
};

component.data = function () {
    return {
        control: {
            hackStatuses: hackStatuses,
            editorActiveTag: 'data',
            isInHackMode: false
        },
        id: router.history.current.params.id,
        attendees: [],
        problems: [],
        columns: {},
        excludeVirtual: false,
        view: null,
        code: null,
        hackView: null,
        hackResult: null,
        statusId: null,
        form: { data: '' }
    };
};

component.computed = {
    sortedStandings: function () {
        return this.attendees.sort(function (a, b) {
            if (a.isInvisible == b.isInvisible) {
                if (a.point == b.point) {
                    if (a.point2 == b.point2) {
                        if (a.point3 == b.point3) {
                            if (a.point4 == b.point4) {
                                if (a.timeSpan == b.timeSpan) {
                                    if (a.timeSpan2 < b.timeSpan2) {
                                        return -1;
                                    } else {
                                        return 1;
                                    }
                                } else if (a.timeSpan < b.timeSpan) {
                                    return -1;
                                } else {
                                    return 1;
                                }
                            } else if (a.point4 < b.point4) {
                                return -1;
                            } else {
                                return 1;
                            }
                        }
                        else if (a.point3 < b.point3) {
                            return -1;
                        } else {
                            return 1;
                        }
                    } else if (a.point2 > b.point2) {
                        return -1;
                    } else {
                        return 1;
                    }
                } else if (a.point > b.point) {
                    return -1;
                } else {
                    return 1;
                }
            } else if (a.isInvisible) {
                return 1;
            }
            else {
                return -1;
            }
        });
    }
};

component.watch = {
    excludeVirtual: function (val) {
        if (val) {
            this.view.unsubscribe();
            app.redirect('/contest/:id/standings', '/contest/' + this.id + '/standings', { id: this.id }, { 'excludeVirtual': this.excludeVirtual.toString() });
        } else {
            this.view.unsubscribe();
            app.redirect('/contest/:id/standings', '/contest/' + this.id + '/standings', { id: this.id });
        }
    }
};

component.methods = {
    loadStandings: function () {
        var self = this;
        this.view = qv.createView('/api/contest/' + this.id + '/standings/all', { includingVirtual: this.excludeVirtual ? false : true }, 60000);
        this.view.fetch(x => {
            this.problems = x.data.problems;
            this.attendees = x.data.attendees;
            this.columns = x.data.columnDefinations;
            app.links[1].text = x.data.title;
            app.links[1].to = { name: '/contest/:id', path: '/contest/' + this.id, params: { id: this.id } };

            app.lookupUsers({ userIds: x.data.attendees.map(y => y.userId) })
                .then(() => {
                    for (var i = 0; i < self.attendees.length; i++) {
                        self.attendees[i].roleClass = app.lookup.user[self.attendees[i].userId].class;
                        self.attendees[i].avatarUrl = app.lookup.user[self.attendees[i].userId].avatar;
                        self.attendees[i].username = app.lookup.user[self.attendees[i].userId].name;
                    }
                    this.$forceUpdate();
                });
        });
    },
    backToViewMode: function () {
        this.form.data = $('#code-editor')[0].editor.getValue();
        this.control.isInHackMode = false;
        app.fullScreen = false;
        $('.problem-body').attr('style', '');
    },
    goToEditMode: function (id) {
        this.statusId = id;
        app.notification('pending', '正在准备Hack编辑器');
        qv.get('/api/judge/' + id, {})
            .then(x => {
                if (x.data.code) {
                    this.code = x.data.code;
                    $('.hack-data').html('<pre><code></code></pre>');
                    $('.hack-data pre code').html(this.code);
                    $('.hack-data pre code').each(function (i, block) {
                        hljs.highlightBlock(block);
                    });

                    $('#code-editor')[0].editor.setValue(this.form.data);

                    setTimeout(function () { $('#code-editor')[0].editor.resize(); }, 250);

                    this.control.isInHackMode = true;
                    app.fullScreen = true;
                    __ace_style = $('#code-editor').attr('class').replace('active', '').trim();
                } else {
                    app.notification('error', '你还不能Hack这条记录');
                }
            })
            .catch(err => {
                app.notification('error', '你还不能Hack这条记录', err.toString());
            });
    },
    changeEditorMode: function (mode) {
        if (mode != 'code') {
            __ace_style = $('#code-editor').attr('class').replace('active', '').trim();
            $('#code-editor').attr('class', __ace_style);
        }
        this.control.editorActiveTag = mode;
        if (mode == 'code') {
            $('#code-editor').attr('class', __ace_style + ' active');
        }
    },
    selectHackFile: function () {
        var self = this;
        $('#fileUpload')
            .unbind()
            .change(function (e) {
                var file = $('#fileUpload')[0].files[0];
                var reader = new FileReader();
                reader.onload = function (e) {
                    app.notification('pending', '正在提交Hack...');
                    qv.put('/api/hack', {
                        judgeStatusId: self.statusId,
                        data: e.target.result,
                        contestId: self.id,
                        IsBase64: true
                    })
                        .then(x => {
                            app.notification('succeeded', 'Hack请求已被处理...', x.msg);
                            if (self.hackView) {
                                this.hackView.unsubscribe();
                            }
                            self.control.editorActiveTag = 'result';
                            self.hackView = qv.createView('/api/hack/' + x.data);
                            self.hackView.fetch(y => {
                                self.hackResult = y.data;
                                self.hackResult.result = formatJudgeResult(y.data.result);
                                self.hackResult.hackeeResult = formatJudgeResult(y.data.hackeeResult);
                            });
                            self.hackView.subscribe('hack', x.data);
                        })
                        .catch(err => {
                            app.notification('error', 'Hack提交失败', err.responseJSON.msg);
                        });
                };
                reader.readAsDataURL(file);
            });
        $('#fileUpload').click();
    },
    sendToHack: function () {
        app.notification('pending', '正在提交Hack...');
        qv.put('/api/hack', {
            judgeStatusId: this.statusId,
            data: $('#code-editor')[0].editor.getValue(),
            contestId: this.id
        })
            .then(x => {
                app.notification('succeeded', 'Hack请求已被处理...', x.msg);
                if (this.hackView) {
                    this.hackView.unsubscribe();
                }
                this.control.editorActiveTag = 'result';
                this.hackView = qv.createView('/api/hack/' + x.data);
                this.hackView.fetch(y => {
                    this.hackResult = y.data;
                    this.hackResult.result = formatJudgeResult(y.data.result);
                    this.hackResult.hackeeResult = formatJudgeResult(y.data.hackeeResult);
                });
                this.hackView.subscribe('hack', x.data);
            })
            .catch(err => {
                app.notification('error', 'Hack提交失败', err.responseJSON.msg);
            });
    },
    updateStandings: async function (user, user2) {
        var needSort = false;
        if (user) {
            var result = await this.getSingleStandings(user);
            if (result) {
                needSort = true;
                if (this.attendees.some(x => x.userId == user)) {
                    var existed = this.attendees.filter(x => x.userId == user)[0];
                    for (var x in result) {
                        existed[x] = result[x];
                    }
                } else {
                    this.attendees.push(result);
                }
            }
        }
        if (user2) {
            var result = await this.getSingleStandings(user2);
            if (result) {
                needSort = true;
                if (this.attendees.some(x => x.userId == user2)) {
                    var existed = this.attendees.filter(x => x.userId == user2)[0];
                    for (var x in result) {
                        existed[x] = result[x];
                    }
                } else {
                    this.attendees.push(result);
                }
            }
        }
    },
    getSingleStandings: async function (user) {
        var attendee = (await qv.get('/api/contest/' + this.id + '/standings/' + user)).data;
        if (excludeVirtual && attendee.isVirtual) return null;
        await app.lookupUsers({ userIds: x.data.attendees.map(y => y.userId) });
        attendee.roleClass = app.lookup.user[attendee.userId].class;
        attendee.avatarUrl = app.lookup.user[attendee.userId].avatar;
        attendee.username = app.lookup.user[attendee.userId].name;
    }
};