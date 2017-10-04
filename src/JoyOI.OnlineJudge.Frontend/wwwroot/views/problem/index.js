var __ace_style;

app.title = '题目';
app.links = [{ text: '题目列表', to: '/problem' }];

component.data = function () {
    return {
        id: router.history.current.params.id,
        title: null,
        body: null,
        sampleData: [],
        source: null,
        timelimit: null,
        memorylimit: null,
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
            language: null,
            data: []
        },
        result: {
            id: null,
            hint: null,
            substatuses: []
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
    var self = this;
    qv.createView('/api/problem/' + router.history.current.params.id)
        .fetch(x => {
            app.title = x.data.title;
            self.title = x.data.title;
            self.timelimit = x.data.timeLimitationPerCaseInMs;
            self.memory = x.data.memoryLimitationPerCaseInByte;
            self.body = x.data.body;
        });
    qv.createView('/api/problem/' + router.history.current.params.id + '/testcase/all', { type: 'Sample' })
        .fetch(x => {
            self.sampleData = x.data.map(x => { return { input: x.input, output: x.output } });
        });
    qv.get('/api/problem/' + router.history.current.params.id + '/claim/all')
        .then((x) => {
            self.claims = x.data.map(x => x.userId);
        });
    this.isSpecialJudge = false;
    this.source = '本地';
    this.form.language = app.preferences.language;
};

component.watch = {
    deep: true,
    title: function () {
        app.title = this.title;
    },
    'form.language': function (val) {
        if (val) {
            $('#code-editor')[0].editor.session.setMode('ace/mode/' + syntaxHighlighter[val]);
        }
    }
};

component.methods = {
    goToEditMode: function () {
        $('#code-editor')[0].editor.setValue(this.form.code);
        this.control.isInSubmitMode = false;
        this.control.isInEditMode = true;
        app.fullScreen = true;
        __ace_style = $('#code-editor').attr('class').replace('active', '').trim();
    },
    goToSubmitMode: function () {
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
        if (mode == 'judge') {
            this.result.substatuses = [];
            var self = this;
            setTimeout(function () {
                self.result.hint = 'JoyOI.ManagementService.Core.ActorExecuteException: JoyOI.ManagementService.Core.ActorExecuteException: Unhandled Exception: System.NotSupportedException: Your source code does not support to compile. at JoyOI.ManagementService.Playground.TyvjCompileActor.Main(String[] args) /actor/run-actor.sh: line 10: 15 Aborted (core dumped) dotnet /actor/bin/Debug/netcoreapp2.0/actor.dll at JoyOI.ManagementService.Services.Impl.StateMachineInstanceStore.';
                self.result.substatuses = [{ status: 'Pending', time: 0, memory: 0, hint: '' }, { status: 'Pending', time: 0, memory: 0, hint: '' }];
                setTimeout(function () {
                    self.result.substatuses[0].status = 'Accepted';
                    self.result.substatuses[1].status = 'Wrong Answer';
                    self.result.substatuses[1].hint = '选手输出2，答案期望3';
                }, 500);
            }, 100);
        }
    },
    toggleStatusHint: function (index) {
        var tr = $('.judge-panel-table tr');
        if (2 * index + 1 < tr.length) {
            $(tr[2 * index + 1]).toggle();
        }
    }
};