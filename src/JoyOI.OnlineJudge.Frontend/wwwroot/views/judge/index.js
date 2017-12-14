component.data = function () {
    return {
        control: {
            statuses: statuses,
            languages: languages,
            highlighters: syntaxHighlighter
        },
        id: null,
        language: null,
        hint: null,
        code: null,
        substatuses: [],
        time: null,
        problem: { id: null, title: null },
        user: { id: null, username: null, roleClass: null, avatarUrl: null },
        view: null
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
            self.user.id = x.data.userId.substr(0, 8);
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
    }
};
