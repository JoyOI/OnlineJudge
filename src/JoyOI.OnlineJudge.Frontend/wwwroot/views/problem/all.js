app.title = '题目列表';
app.links = [];

component.data = function () {
    return {
        tags: [],
        selected: [],
        paging: {
            current: 1,
            count: 1,
            total: 0
        },
        request: {
            tag: null,
            title: null
        },
        view: null,
        result: []
    };
}

component.watch = {
    selected: function (value) {
        value = value.filter(x => !value.some(y => x != y && y.indexOf(x) >= 0));
        this.request.tag = value.join(', ');
    },
    'request.tag': function (value) {
        this.paging.current = 1;
        this.loadProblems();
    },
    'request.title': function (value) {
        this.paging.current = 1;
        this.loadProblems();
    },
    'paging.current': function (value) {
        this.loadProblems();
    },
    deep: true
};

component.methods = {
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
    setSearchTitle: function () {
        this.request.title = $('#txtSearchProblemTitle').val();
    },
    loadProblems: function () {
        var self = this;

        if (this.view) {
            this.view.unsubscribe();
        }

        this.view = qv.createView('/api/problem/all', { tag: self.request.tag, title: self.request.title, page: self.paging.current })
        this.view.fetch(x => {
            self.paging.count = x.data.count;
            self.paging.current = x.data.current;
            self.paging.total = x.data.total;
            self.result = x.data.result;
            self.view.subscribe('problem-list');
            $(window).scrollTop(0);
        });
    }
};

component.created = function () {
    this.view = qv.createView('/api/problem/all', this.request);
    this.view.fetch(x => {
        this.paging.count = x.data.count;
        this.paging.current = x.data.current;
        this.paging.total = x.data.total;
        this.result = x.data.result;
        this.view.subscribe('problem-list');
    });

    qv.createView('/api/configuration/problemtags').fetch(x => {
        this.tags = JSON.parse(x.data.value);
    });
};
