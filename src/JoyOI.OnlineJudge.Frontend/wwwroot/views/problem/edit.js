app.title = '编辑题目';
app.links = [{ text: '题目列表', to: '/problem' }, { text: '未知题目', to: '/problem' }];

component.data = function () {
    return {
        id: router.history.current.params.id,
        title: null,
        body: null,
        sampleData: [],
        source: null,
        timeLimitationPerCaseInMs: null,
        memoryLimitationPerCaseInByte: null,
        isSpecialJudge: null,
        claims: [],
        active: 'basic',
        testCases: [],
        testCaseType: testCaseType,
        zipSelectedTestCaseType: 'Sample',
        inputSelectedTestCaseType: 'Sample',
        uploadMode: null,
        validator: {
            code: null,
            language: null,
            error: null,
            blob: null
        },
        standard: {
            code: null,
            language: null,
            error: null,
            blob: null
        },
        range: {
            code: null,
            language: null,
            error: null,
            blob: null
        },
        tags: [],
        selected: [],
        languages: languages
    }
};

component.created = function () {
    var self = this;

    qv.createView('/api/problem/' + router.history.current.params.id).fetch(x => {
            self.title = x.data.title;
            app.links[1].text = x.data.title;
            app.links[1].to = { name: '/problem/:id', path: '/problem/' + router.history.current.params.id, params: { id: router.history.current.params.id } };
            self.timeLimitationPerCaseInMs = x.data.timeLimitationPerCaseInMs;
            self.memoryLimitationPerCaseInByte = x.data.memoryLimitationPerCaseInByte;
            self.body = x.data.body;
            self.validator.code = x.data.validatorCode;
            self.validator.language = x.data.validatorLanguage || app.preferences.language;
            self.validator.error = x.data.validatorError;
            self.validator.blob = x.data.validatorBlobId;
            self.standard.code = x.data.standardCode;
            self.standard.language = x.data.standardLanguage || app.preferences.language;
            self.standard.error = x.data.standardError;
            self.standard.blob = x.data.standardBlobId;
            self.range.code = x.data.RangeCode;
            self.range.langauge = x.data.rangeLanguage || app.preferences.language;
            self.range.error = x.data.rangeError;
            self.range.blob = x.data.rangeBlobId;
            self.selected = x.data.tags ? x.data.tags.split(',').map(x => x.trim()) : [];
            try {
                $('.markdown-textbox')[0].smde.codemirror.setValue(x.data.body);

                if ($('.spjEditor').length) {
                    var editor = $('.spjEditor')[0].editor;
                    editor.session.setMode('ace/mode/' + syntaxHighlighter[x.data.validatorLanguage]);
                }
                if ($('.stdEditor').length) {
                    var editor = $('.stdEditor')[0].editor;
                    editor.session.setMode('ace/mode/' + syntaxHighlighter[x.data.standardLanguage]);
                }
                if ($('.rangeEditor').length) {
                    var editor = $('.rangeEditor')[0].editor;
                    editor.session.setMode('ace/mode/' + syntaxHighlighter[x.data.rangeLanguage]);
                }
            } catch(ex) { }
        });

    qv.createView('/api/configuration/problemtags').fetch(x => {
        self.tags = JSON.parse(x.data.value);
    });

    console.log('fetching test cases');
    qv.createView('/api/problem/' + router.history.current.params.id + '/testcase/all').fetch(x => {
        self.testCases = x.data;
    });
};

component.methods = {
    saveBasic: function () {
        this.body = $('.markdown-textbox')[0].smde.codemirror.getValue();
        app.notification('pending', '正在保存题目');
        qv.patch('/api/problem/' + this.id, {
            title: this.title,
            timeLimitationPerCaseInMs: this.timeLimitationPerCaseInMs,
            memoryLimitationPerCaseInByte: this.memoryLimitationPerCaseInByte,
            body: this.body
        })
            .then((x) => {
                app.notification('succeeded', '题目编辑成功', x.msg);
            })
            .catch(err => {
                app.notification('error', '题目编辑失败', err.responseJSON.msg);
            });
    },
    saveTags: function () {
        app.notification('pending', '正在保存题目');
        qv.patch('/api/problem/' + this.id, {
            tags: this.selected.toString()
        })
            .then(x => {
                app.notification('succeeded', '题目编辑成功', x.msg);
            })
            .catch(err => {
                app.notification('error', '题目编辑失败', err.responseJSON.msg);
            });
    },
    saveSpj: function () {
        app.notification('pending', '正在保存题目');
        this.validator.code = $('.spjEditor')[0].editor.session.getValue();
        qv.patch('/api/problem/' + this.id, {
            validatorCode: this.validator.code,
            validatorLanguage: this.validator.language
        })
            .then(x => {
                app.notification('succeeded', '题目编辑成功', x.msg);
            })
            .catch(err => {
                app.notification('error', '题目编辑失败', err.responseJSON.msg);
            });
    },
    saveStd: function () {
        app.notification('pending', '正在保存题目');
        this.validator.code = $('.stdEditor')[0].editor.session.getValue();
        qv.patch('/api/problem/' + this.id, {
            standardCode: this.validator.code,
            standardLanguage: this.validator.language
        })
            .then(x => {
                app.notification('succeeded', '题目编辑成功', x.msg);
            })
            .catch(err => {
                app.notification('error', '题目编辑失败', err.responseJSON.msg);
            });
    },
    saveRange: function () {
        app.notification('pending', '正在保存题目');
        this.validator.code = $('.rangeEditor')[0].editor.session.getValue();
        qv.patch('/api/problem/' + this.id, {
            rangeCode: this.validator.code,
            rangeLanguage: this.validator.language
        })
            .then(x => {
                app.notification('succeeded', '题目编辑成功', x.msg);
            })
            .catch(err => {
                app.notification('error', '题目编辑失败', err.responseJSON.msg);
            });
    },
    triggerTag: function (tag) {
        if (this.selected.some(x => x == tag)) {
            var subs = this.selected.filter(x => x.indexOf(tag) >= 0);
            for (var i = 0; i < subs.length; i++) {
                this.selected.remove(subs[i]);
            }
        }
        else {
            this.selected.push(tag);
            if (tag.indexOf(':') >= 0 && tag.lastIndexOf(':') >= 0 && tag.indexOf(':') != tag.lastIndexOf(':')) {
                var parent = tag.substr(0, tag.lastIndexOf(':'));
                if (!this.selected.some(x => x == parent)) {
                    this.selected.push(parent);
                }
            }
        }
    },
    selectZipFile: function () {
        var self = this;
        $('#fileUpload')
            .unbind()
            .change(function (e) {
                var file = $('#fileUpload')[0].files[0];
                var reader = new FileReader();
                reader.onload = function (e) {
                    qv.put('/api/problem/' + self.id + '/testcase/zip', {
                        zip: e.target.result,
                        type: self.zipSelectedTestCaseType
                    });
                    // TODO:
                };  
                reader.readAsDataURL(file);
            });
        $('#fileUpload').click();
    },
    uploadInputTestCase: function () {
        var self = this;
        qv.put('/api/problem/' + self.id + '/testcase', {
            input: $('#txtInput').val(),
            output: $('#txtOutput').val(),
            type: self.inputSelectedTestCaseType
        });
    }
};

component.watch = {
    deep: true,
    'validator.language': function (val) {
        if ($('.spjEditor').length && val) {
            var editor = $('.spjEditor')[0].editor;
            editor.session.setMode('ace/mode/' + syntaxHighlighter[val]);
        }
    }
};