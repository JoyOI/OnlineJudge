var __ace_style;
component.data = function () {
    return {
        control: {
            statuses: statuses,
            languages: languages,
            hackStatuses: hackStatuses,
            highlighters: syntaxHighlighter,
            editorActiveTag: 'data',
            isInHackMode: false
        },
        form: {
            data: ''
        },
        id: null,
        language: null,
        hint: null,
        contestId: null,
        code: null,
        isHackable: false,
        substatuses: [],
        time: null,
        problem: { id: null, title: null },
        user: { id: null, username: null, roleClass: null, avatarUrl: null },
        view: null,
        hackView: null,
        hackResult: null,
        isRejudgable: false
    };
};

component.created = function () {
    app.title = '评测结果'
    app.links = [{ text: '评测', to: '/judge' }];

    this.id = router.history.current.params.id;
    var self = this;
    self.view = qv.createView('/api/judge/' + this.id);
    self.view.fetch(x => {
            self.code = x.data.code;
            self.hint = x.data.hint;
            self.time = x.data.createdTime;
            self.problem.id = x.data.problemId;
            self.contestId = x.data.contestId;
            self.user.id = x.data.userId.substr(0, 8);
            self.isHackable = x.data.isHackable;
            self.isRejudgable = x.data.isRejudgable;
            self.substatuses = x.data.subStatuses.map(y =>
            {
                return { hint: y.hint, status: formatJudgeResult(y.result), time: y.timeUsedInMs, memory: y.memoryUsedInByte };
            });
            self.language = x.data.language;
            qv.createView('/api/user/role', { userids: x.data.userId })
                .fetch(y =>
                {
                    self.user.username = y.data[x.data.userId].username;
                    self.user.roleClass = ConvertUserRoleToCss(y.data[x.data.userId].role);
                    self.user.avatarUrl = y.data[x.data.userId].avatarUrl;
                });

            qv.createView('/api/problem/title', { problemids: self.problem.id })
                .fetch(y =>
                {
                    self.problem.title = y.data[self.problem.id].title;
                })
    })
        .catch(err => {
            if (err.responseJSON.code == 404) {
                app.redirect('/404', '/404');
            } else {
                app.notification('error', '获取评测信息失败', err.responseJSON.msg);
            }
        });
    self.view.subscribe('judge', self.id);
};

component.computed = {
    status: function () {
        if (this.substatuses.length) {
            var mapping = this.control.statuses.map(y => y.display);
            var indexes = this.substatuses.map(x => mapping.indexOf(x.status));
            var maxIndex = -1;
            for (var i = 0; i < indexes.length; i++) {
                maxIndex = Math.max(maxIndex, indexes[i]);
            }
            if (maxIndex >= 0 && maxIndex < this.control.statuses.length)
                return this.control.statuses[maxIndex].display;
            else
                return 'Unkown Result';
        } else {
            return 'Unknown Result';
        }
    },
    totalTime: function () {
        var time = 0;
        for (var i = 0; i < this.substatuses.length; i++) {
            time += this.substatuses[i].time;
        }
        return time;
    },
    peakMemory: function () {
        var mem = 0;
        for (var i = 0; i < this.substatuses.length; i++) {
            mem = Math.max(this.substatuses[i].memory, mem);
        }
        return mem;
    }
};

component.watch = {
    language: function (val) {
        var mode = this.control.highlighters[val];
        var dom = $('.code-box-outer .code-box');
        if (!dom.length || !dom[0].editor) return;
        dom[0].editor.session.setMode('ace/mode/' + mode);
    },
    code: function (val) {
        var dom = $('.code-box-outer .code-box');
        if (!dom.length || !dom[0].editor) return;
        dom[0].editor.setValue(val);
        dom[0].editor.selection.moveCursorToPosition({ row: 0, column: 0 });
    }
};

component.methods = {
    toggleStatusHint: function (index) {
        var tr = $('.judge-panel-table tr');
        if (2 * index + 1 < tr.length) {
            $(tr[2 * index + 1]).toggle();
        }
    },
    backToViewMode: function () {
        this.form.data = $('#code-editor')[0].editor.getValue();
        this.control.isInHackMode = false;
        app.fullScreen = false;
        $('.problem-body').attr('style', '');
    },
    goToEditMode: function () {
        $('.hack-data pre code').each(function (i, block) {
            hljs.highlightBlock(block);
        });
        $('#code-editor')[0].editor.setValue(this.form.data);

        setTimeout(function () { $('#code-editor')[0].editor.resize(); }, 250);

        this.control.isInHackMode = true;
        app.fullScreen = true;
        __ace_style = $('#code-editor').attr('class').replace('active', '').trim();
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
                        judgeStatusId: self.id,
                        data: e.target.result,
                        contestId: self.contestId,
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
            judgeStatusId: this.id,
            data: $('#code-editor')[0].editor.getValue(),
            contestId: this.contestId
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
    rejudge: function () {
        app.notification('pending', '正在提交重新评测请求...');
        qv.patch('/api/judge/' + this.id, { result: 'Pending' })
            .then(x => {
                app.notification('succeeded', '重新评测请求已被处理...', x.msg);
            })
            .catch(err => {
                app.notification('error', '重新评测请求提交失败', err.responseJSON.msg);
            });
    }
};
