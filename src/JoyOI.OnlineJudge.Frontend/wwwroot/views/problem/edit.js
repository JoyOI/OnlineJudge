component.data = function () {
    return {
        id: router.history.current.params.id,
        title: null,
        body: null,
        difficulty: 0,
        sampleData: [],
        source: null,
        isVisiable: false,
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
        languages: languages,
        testCaseView: null,
        problemView: null
    }
};

component.created = function () {
    app.title = '编辑题目';
    app.links = [{ text: '题目列表', to: '/problem' }, { text: '未知题目', to: '/problem' }];

    var self = this;
    self.problemView = qv.createView('/api/problem/' + router.history.current.params.id);
    self.problemView.fetch(x => {
            self.title = x.data.title;
            app.links[1].text = x.data.title;
            app.links[1].to = { name: '/problem/:id', path: '/problem/' + router.history.current.params.id, params: { id: router.history.current.params.id } };
            self.timeLimitationPerCaseInMs = x.data.timeLimitationPerCaseInMs;
            self.memoryLimitationPerCaseInByte = x.data.memoryLimitationPerCaseInByte;
            self.body = x.data.body;
            self.difficulty = x.data.difficulty;
            self.source = x.data.source;
            self.isVisible = x.data.isVisible;
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
                if ($('.markdown-textbox').length && $('.markdown-textbox')[0].smde) {
                    $('.markdown-textbox')[0].smde.codemirror.setValue(x.data.body);
                }

                if ($('.spjEditor').length && $('.spjEditor')[0].editor) {
                    var editor = $('.spjEditor')[0].editor;
                    editor.setValue(this.validator.code);
                    editor.session.setMode('ace/mode/' + syntaxHighlighter[x.data.validatorLanguage]);
                }

                if ($('.stdEditor').length && $('.stdEditor')[0].editor) {
                    var editor = $('.stdEditor')[0].editor;
                    editor.setvalue(self.standard.code);
                    editor.session.setMode('ace/mode/' + syntaxHighlighter[x.data.standardLanguage]);
                }

                if ($('.rangeEditor').length && $('.rangeEditor')[0].editor) {
                    var editor = $('.rangeEditor')[0].editor;
                    editor.setvalue(self.range.code);
                    editor.session.setMode('ace/mode/' + syntaxHighlighter[x.data.rangeLanguage]);
                }

            } catch (ex) { console.error(ex); }
        });

    qv.createView('/api/configuration/problemtags').fetch(x => {
        self.tags = JSON.parse(x.data.value);
    });

    this.testCaseView = qv.createView('/api/problem/' + router.history.current.params.id + '/testcase/all')
    this.testCaseView.fetch(x => {
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
            difficulty: this.difficulty,
            body: this.body,
            isVisible: this.isVisible
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
        var self = this;
        app.notification('pending', '正在保存题目');
        this.validator.code = $('.spjEditor')[0].editor.session.getValue();
        qv.patch('/api/problem/' + this.id, {
            validatorCode: this.validator.code,
            validatorLanguage: this.validator.language
        })
            .then(x => {
                app.notification('succeeded', '题目编辑成功', x.msg);
                self.problemView.refresh();
            })
            .catch(err => {
                app.notification('error', '题目编辑失败', err.responseJSON.msg);
                self.problemView.refresh();
            });
    },
    saveStd: function () {
        var self = this;
        app.notification('pending', '正在保存题目');
        this.validator.code = $('.stdEditor')[0].editor.session.getValue();
        qv.patch('/api/problem/' + this.id, {
            standardCode: this.validator.code,
            standardLanguage: this.validator.language
        })
            .then(x => {
                app.notification('succeeded', '题目编辑成功', x.msg);
                self.problemView.refresh();
            })
            .catch(err => {
                app.notification('error', '题目编辑失败', err.responseJSON.msg);
                self.problemView.refresh();
            });
    },
    saveRange: function () {
        var self = this;
        app.notification('pending', '正在保存题目');
        this.validator.code = $('.rangeEditor')[0].editor.session.getValue();
        qv.patch('/api/problem/' + this.id, {
            rangeCode: this.validator.code,
            rangeLanguage: this.validator.language
        })
            .then(x => {
                app.notification('succeeded', '题目编辑成功', x.msg);
                self.problemView.refresh();
            })
            .catch(err => {
                app.notification('error', '题目编辑失败', err.responseJSON.msg);
                self.problemView.refresh();
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
                    app.notification('pending', '正在上传...', '您提交的测试数据正在上传至服务器，请勿关闭窗口！');
                    qv.put('/api/problem/' + self.id + '/testcase/zip', {
                        zip: e.target.result,
                        type: self.zipSelectedTestCaseType
                    })
                        .then(x => {
                            app.notification('succeeded', '上传成功', x.msg);
                            self.testCaseView.refresh();
                        })
                        .catch(err => {
                            app.notification('error', '上传失败', err.responseJSON.msg);
                        });
                };  
                reader.readAsDataURL(file);
            });
        $('#fileUpload').click();
    },
    uploadInputTestCase: function () {
        var self = this;
        app.notification('pending', '正在上传...', '您提交的测试数据正在上传至服务器，请勿关闭窗口！');
        qv.put('/api/problem/' + self.id + '/testcase', {
            input: $('#txtInput').val(),
            output: $('#txtOutput').val(),
            type: self.inputSelectedTestCaseType
        })
            .then(x => {
                app.notification('succeeded', '上传成功', x.msg);
                self.testCaseView.refresh();
                $('#txtInput').val('');
                $('#txtOutput').val('');
            })
            .catch(err => {
                app.notification('error', '上传失败', err.responseJSON.msg);
            });
    },
    removeTestCase: function (id) {
        var self = this;
        if (confirm("删除测试用例后将无法恢复，确定要这样做吗？")) {
            app.notification('pending', '正在删除测试用例...');
            qv.delete('/api/problem/' + self.id + '/testcase/' + id)
                .then(x => {
                    app.notification('succeeded', '删除成功', x.msg);
                    self.testCaseView.refresh();
                    $('#txtInput').val('');
                    $('#txtOutput').val('');
                })
                .catch(err => {
                    app.notification('error', '删除失败', err.responseJSON.msg);
                });
        }
    }
};

component.watch = {
    deep: true,
    'validator.language': function (val) {
        if ($('.spjEditor').length && val) {
            var editor = $('.spjEditor')[0].editor;
            editor.session.setMode('ace/mode/' + syntaxHighlighter[val]);
        }
    },
    active: function (val) {
        if (val === 'basic') {
            app.redirect('/problem/:id/edit', '/problem/' + this.id + '/edit', { id: this.id });
        } else {
            app.redirect('/problem/:id/edit', '/problem/' + this.id + '/edit', { id: this.id }, { active: val });
        }
    }
};