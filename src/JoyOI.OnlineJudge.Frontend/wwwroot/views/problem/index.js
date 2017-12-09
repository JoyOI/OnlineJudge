var __ace_style;

component.data = function () {
    return {
        id: router.history.current.params.id,
        title: null,
        body: null,
        template: null,
        sampleData: [],
        source: null,
        time: null,
        memory: null,
        display: 'problem',
        isVisible: null,
        isSpecialJudge: null,
        claims: [],
        control: {
            editorActiveTag: 'code',
            isInEditMode: false,
            isInSubmitMode: false,
            languages: languages,
            statuses: statuses
        },
        form: {
            code: '',
            language: app.preferences.language,
            data: []
        },
        result: {
            id: null,
            hint: null,
            substatuses: [],
            view: null
        },
        resolution: {
            resolutionView: null,
            data: [],
            paging: {
                current: 1,
                count: 1,
                total: 0
            }
        }
    };
};

component.computed = {
    judgeResult: function () {
        if (this.result.substatuses.length) {
            var mapping = this.control.statuses.map(y => y.display);
            var indexes = this.result.substatuses.map(x => mapping.indexOf(x.status));
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
    hasPermissionToEdit: function () {
        return this.$root.user.profile.role == 'Root'
            || this.$root.user.profile.root === 'Master'
            || this.claims.some(x => x.toLowerCase() === this.$root.user.profile.id.toLowerCase());
    }
};

component.created = function () {
    app.title = '题目';
    app.links = [{ text: '题目列表', to: '/problem' }];

    var self = this;
    qv.createView('/api/problem/' + router.history.current.params.id + '/testCase/all', { type: 'Sample' }).fetch(x => {
        self.sampleData = x;
    });
    var problemView = qv.createView('/api/problem/' + router.history.current.params.id);
    problemView
        .fetch(x => {
            app.title = x.data.title;
            self.title = x.data.title;
            self.time = x.data.timeLimitationPerCaseInMs;
            self.memory = x.data.memoryLimitationPerCaseInByte;
            self.body = x.data.body;
            self.template = x.data.codeTemplate;
            self.source = x.data.source;
            self.isVisible = x.data.isVisible;
            if (self.template) {
                self.control.languages = Object.getOwnPropertyNames(x.data.codeTemplate).filter(x => x !== '__ob__');
            }
            problemView.subscribe('problem', x.data.id);
        });
    var sampleView = qv.createView('/api/problem/' + router.history.current.params.id + '/testcase/all', { type: 'Sample', showContent: true })
    sampleView.fetch(x => {
        self.sampleData = x.data.map(x => { return { input: x.input, output: x.output } });
        sampleView.subscribe('problem-sample-data', self.id);
    });
    qv.get('/api/problem/' + router.history.current.params.id + '/claim/all')
        .then((x) => {
            self.claims = x.data.map(x => x.userId);
        });
    this.isSpecialJudge = false;
    this.form.language = app.preferences.language;
};

component.watch = {
    deep: true,
    title: function () {
        app.title = this.title;
    },
    display: function (val) {
        if (val === 'resolution' && !this.resolutionView) {
            this.loadResolutions();
        }
    },
    'resolution.paging.current': function () {
        this.loadResolutions();
    },
    'form.language': function (val) {
        var self = this;
        if (val) {
            $('#code-editor')[0].editor.session.setMode('ace/mode/' + syntaxHighlighter[val]);
            if (self.template) {
                $('#code-editor')[0].editor.setValue(self.template[val]);
                $('#code-editor')[0].editor.selection.moveCursorToPosition({ row: 0, column: 0 });
            }
        }
    },
    'result.view': function (newVal, oldVal) {
        if (oldVal)
            oldVal.unsubscribe();
    }
};

component.methods = {
    goToEditMode: function () {
        if (!this.form.code && this.template) {
            if (this.template[this.form.language]) {
                this.form.code = this.template[this.form.language];
            } else {
                this.form.language = Object.getOwnPropertyNames(this.template)[0];
                this.form.code = this.template[Object.getOwnPropertyNames(this.template)[0]];
            }
        }
        $('#code-editor')[0].editor.setValue(this.form.code);
        this.control.isInSubmitMode = false;
        this.control.isInEditMode = true;
        app.fullScreen = true;
        __ace_style = $('#code-editor').attr('class').replace('active', '').trim();
    },
    goToSubmitMode: function () {
        if (!this.form.code && this.template) {
            if (this.template[this.form.language]) {
                this.form.code = this.template[this.form.language];
            } else {
                this.form.language = Object.getOwnPropertyNames(this.template)[0];
                this.form.code = this.template[Object.getOwnPropertyNames(this.template)[0]];
            }
        }
        this.control.isInSubmitMode = true;
        $('#code-editor')[0].editor.setValue(this.form.code);
        setTimeout(function () { $('#code-editor')[0].editor.resize(); }, 250);
        __ace_style = $('#code-editor').attr('class').replace('active', '').trim();
    },
    backToViewMode: function () {
        this.form.code = $('#code-editor')[0].editor.getValue();
        this.control.isInEditMode = false;
        this.control.isInSubmitMode = false;
        app.fullScreen = false;
        $('.problem-body').attr('style', '');
    },
    addData: function () {
        this.form.data.push({ input: '', output: '' });
    },
    removeData: function (index) {
        if (index < this.form.data.length)
            this.form.data.splice(index, 1);
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
    toggleStatusHint: function (index) {
        var tr = $('.judge-panel-table tr');
        if (2 * index + 1 < tr.length) {
            $(tr[2 * index + 1]).toggle();
        }
    },
    submitToJudge: function () {
        var self = this;
        this.form.code = $('#code-editor')[0].editor.getValue();
        app.notification('pending', '正在提交评测...');

        if (app.user.profile.preferredLanguage !== self.form.language) {
            qv.patch('/api/user/' + app.user.profile.username, { preferredLanguage: self.form.language });
            app.preferences.language = self.form.language;
        }

        qv.put('/api/judge', {
            problemId: self.id,
            isSelfTest: self.form.data.length > 0,
            code: $('#code-editor')[0].editor.getValue(),
            language: self.form.language,
            data: self.form.data.length > 0 ? self.form.data : null
        })
        .then(x =>
        {
            app.notification('succeeded', '评测请求已被接受');
            self.result.view = qv.createView('/api/judge/' + x.data);
            self.result.view.fetch(y =>
            {
                self.result.hint = y.data.hint;
                self.result.substatuses = y.data.subStatuses.map(z => {
                    return { hint: z.hint, status: formatJudgeResult(z.result), time: z.timeUsedInMs, memory: z.memoryUsedInByte };
                });
                app.control.userInfoView.refresh();
            })
                .catch(err => {
                    app.notification('error', '获取评测详细信息失败', err.responseJSON.msg);
                });

            self.result.view.subscribe('judge', x.data);

            self.changeEditorMode('judge');
        })
            .catch(err => {
                app.notification('error', '提交评测失败', err.responseJSON.msg);
            });
    },
    loadResolutions: function () {
        var self = this;
        self.resolutionView = qv.createView('/api/problem/' + self.id + '/resolution', { page: self.resolution.paging.current })
            .fetch(x => {
                self.resolution.paging.count = x.data.count;
                self.resolution.paging.current = x.data.current;
                self.resolution.paging.total = x.data.total;
                self.resolution.data = x.data.result;
                $(window).scrollTop(0);
            });
    }
};