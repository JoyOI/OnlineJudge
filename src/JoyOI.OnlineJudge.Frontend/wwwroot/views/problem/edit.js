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
        validator: {
            code: null,
            language: null,
            error: null,
            blob: null
        },
        tags: [],
        selected: []
    }
};

component.created = function () {
    var self = this;

    qv.createView('/api/problem/' + router.history.current.params.id)
        .fetch(x => {
            self.title = x.data.title;
            app.links[1].text = x.data.title;
            app.links[1].to = { name: '/problem/:id', path: '/problem/' + router.history.current.params.id, params: { id: router.history.current.params.id } };
            self.timeLimitationPerCaseInMs = x.data.timeLimitationPerCaseInMs;
            self.memoryLimitationPerCaseInByte = x.data.memoryLimitationPerCaseInByte;
            self.body = x.data.body;
            self.validator.code = x.data.validatorCode;
            self.validator.language = x.data.validatorLanguage;
            self.validator.error = x.data.validatorError;
            self.validator.blob = x.data.validatorBlobId;
            self.selected = x.data.tags.split(',').map(x => x.trim());
            $('.markdown-textbox')[0].smde.codemirror.setValue(x.data.body);
        });

    qv.createView('/api/configuration/problemtags').fetch(x => {
        self.tags = JSON.parse(x.data.value);
    });
};

component.methods = {
    saveBasic: function () {
        this.body = $('.markdown-textbox')[0].smde.codemirror.getValue();
        qv.patch('/api/problem/' + this.id, {
            title: this.title,
            timeLimitationPerCaseInMs: this.timeLimitationPerCaseInMs,
            memoryLimitationPerCaseInByte: this.memoryLimitationPerCaseInByte,
            body: this.body
        })
            .then((x) => {
                popResult(x.msg);
            });
    },
    saveTags: function () {
        qv.patch('/api/problem/' + this.id, {
            tags: this.selected.toString()
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
};