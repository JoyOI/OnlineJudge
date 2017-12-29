component.data = function () {
    return {
        paging: {
            current: 1,
            count: 1,
            total: 0
        },
        contestTypes: contestTypes,
        result: [],
        request: {
            title: null,
            type: null
        }
    }
}

component.watch = {
    'paging.current': function () {
        var args = this.generateQuery();
        app.redirect('/contest', '/contest', {}, args);
    },
    'request.title': function () {
        var args = this.generateQuery();
        delete args['paging.current'];
        app.redirect('/contest', '/contest', {}, args);
    },
    'request.type': function () {
        var args = this.generateQuery();
        delete args['paging.current'];
        app.redirect('/contest', '/contest', {}, args);
    },
    deep: true
};

component.created = function () {
    app.title = '比赛';
    app.links = [];
    this.loadContests();
}

component.methods = {
    filterContests: function () {
        this.request.title = $('#txtSearchContestTitle').val();
        this.request.type = $('#lstSearchContestType').val();
    },
    loadContests: function () {
        var self = this;
        qv.createView('/api/contest/all', {
            title: self.request.title,
            type: self.request.type,
            page: self.paging.current
        }, 60000)
            .fetch(x => {
                self.paging.count = x.data.count;
                self.paging.current = x.data.current;
                self.paging.total = x.data.total;

                var results = clone(x.data.result);
                results = results.map(y => {
                    var begin = new Date(y.begin);
                    var end = new Date(begin.getTime() + parseTimeSpan(y.duration))
                    if (y.status == 0) {
                        y.status = 'Ready';
                        y.statusClass = 'contest-ready';
                    } else if (y.status == 1) {
                        y.status = 'Live';
                        y.statusClass = 'contest-live';
                    } else {
                        y.status = 'Done';
                        y.statusClass = 'contest-done';
                    }
                    return y;
                });
                self.result = results;
            })
            .catch(err => {
                app.notification('error', '获取比赛失败', err.responseJSON.msg);
            });
    },
    generateQuery: function () {
        var ret = {};
        if (this.paging.current > 1) {
            ret['paging.current'] = this.paging.current;
        }
        if (this.request.title) {
            ret['request.title'] = this.request.title;
        }
        if (this.request.type) {
            ret['request.type'] = this.request.type;
        }
        return ret;
    }
};