app.title = '题目列表';
app.links = [];

component.data = function () {
    return {
        tags: [],
        selected: [],
        paging: {
            current: 1,
            count: 0
        },
        request: {
            tag: null,
            title: null,
            page: 1
        },
        result: []
    };
}

component.watch = {
    selected: function (value) {
        console.log(value);
        value = value.filter(x => !value.some(y => x != y && y.indexOf(x) >= 0));
        this.request.tag = value.join(', ');
    },
    request: {
        handler: function (value) {
            console.log(value);
            var self = this;
            qv.createView('/api/problem/all', this.request).fetch(x => {
                self.paging.count = x.data.count;
                self.paging.current = x.data.current;
                self.result = x.data.result;
            });
        },
        deep: true
    }
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
    toPage: function (p) {
        this.paging.current = p;
        this.request.page = p;
    }
};

component.created = function () {
    qv.createView('/api/problem/all', this.request).fetch(x => {
        this.paging.count = x.data.count;
        this.paging.current = x.data.current;
        this.result = x.data.result;
    });

    qv.createView('/api/configuration/problemtags').fetch(x => {
        this.tags = JSON.parse(x.data.value);
    });
};
