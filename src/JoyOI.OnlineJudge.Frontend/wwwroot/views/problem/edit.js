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
        claims: []
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
            $('.markdown-textbox')[0].smde.codemirror.setValue(x.data.body);
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
    }
};