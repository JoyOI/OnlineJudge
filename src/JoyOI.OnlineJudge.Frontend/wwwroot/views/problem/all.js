component.data = function () {
    return {
        tags: [],
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

component.computed = {
    selected: function () {
        if (this.request.tag) {
            return this.request.tag.split(',');
        } else {
            return [];
        }
    }
};

component.watch = {
    'request.tag': function (value, old) {
        var args = this.generateQuery();
        delete args['paging.current'];
        app.redirect('/problem', '/problem', {}, args);
    },
    'request.title': function (value, old) {
        var args = this.generateQuery();
        delete args['paging.current'];
        app.redirect('/problem', '/problem', {}, args);
    },
    'paging.current': function (value, old) {
        var args = this.generateQuery();
        app.redirect('/problem', '/problem', {}, args);
    },
    deep: true
};

component.methods = {
    triggerTag: function (tag) {
        var tags = this.selected;
        if (this.selected.some(x => x == tag)) {
            var subs = this.selected.filter(x => x.indexOf(tag) >= 0);
            for (var i = 0; i < subs.length; i++) {
                this.request.tag = tags.remove(subs[i]).join(',');
            }
        }
        else {
            var tags = this.selected;
            tags.push(tag);
            if (tag.indexOf(':') >= 0 && tag.lastIndexOf(':') >= 0 && tag.indexOf(':') != tag.lastIndexOf(':')) {
                var parent = tag.substr(0, tag.lastIndexOf(':'));
                if (!this.selected.some(x => x == parent)) {
                    tags.push(parent);
                }
            }
            this.request.tag = tags.join(',');;
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
            self.paging.total = x.data.total;
            self.result = x.data.result;
            self.view.subscribe('problem-list');
            $(window).scrollTop(0);
        });
    },
    generateQuery: function () {
        var args = {};
        if (this.paging.current && this.paging.current !== 1) {
            args['paging.current'] = this.paging.current;
        }
        if (this.request.tag) {
            args['request.tag'] = this.request.tag;
        }
        if (this.request.title) {
            args['request.title'] = this.request.title;
        }
        return args;
    },
    addToGroup: function (id) {
        var self = this;
        app.notification('pending', '正在添加题目');
        qv.put('/api/group/cur/problem/' + id)
            .then(x => {
                app.notification('succeeded', '题目添加成功', x.msg);
                self.view.refresh();
            })
            .catch(err => {
                app.notification('error', '题目添加失败', err.responseJSON.msg);
            });
    },
    removeFromGroup: function (id) {
        var self = this;
        app.notification('pending', '正在删除题目');
        qv.delete('/api/group/cur/problem/' + id)
            .then(x => {
                app.notification('succeeded', '题目删除成功', x.msg);
                self.view.refresh();
            })
            .catch(err => {
                app.notification('error', '题目删除失败', err.responseJSON.msg);
            });
    }
};

component.created = function () {
    app.title = '题目列表';
    app.links = [];

    var self = this;
    this.view = qv.createView('/api/problem/all', { tag: self.request.tag, title: self.request.title, page: self.paging.current });
    this.view.fetch(x => {
        this.paging.count = x.data.count;
        this.paging.total = x.data.total;
        this.result = x.data.result;
        this.view.subscribe('problem-list');
    });

    qv.createView('/api/configuration/problemtags').fetch(x => {
        this.tags = JSON.parse(x.data.value);
    });
};
