component.data = function () {
    return {
        contestView: null,
        result: [],
        status: 'Approved',
        paging: {
            current: 1,
            count: 1,
            total: 0
        }
    };
};


component.watch = {
    'paging.current': function (value, old) {
        app.redirect('/group/manage/contest', '/group/manage/contest', {}, { 'paging.current': this.paging.current });
    },
    deep: true
};

component.created = function () {
    app.title = '比赛管理';
    app.links = [];
    if (!app.isGroup || !app.groupSession || !app.groupSession.isMaster) {
        app.redirect('/', '/');
    }

    var self = this;

    this.contestView = qv.createView('/api/contest/all', {
        page: self.paging.current,
        pageSize: 20
    });

    this.contestView
        .fetch(x => {
            self.paging.count = x.data.count;
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
        });
};

component.methods = {
    cancelLink: function (contestId) {
        var self = this;
        app.notification('pending', '正在取消外部比赛链接');
        qv.delete('/api/group/cur/contest/' + contestId)
            .then(x => {
                app.notification('succeeded', '外部比赛链接取消成功', x.msg);
                self.contestView.refresh();
            })
            .catch(err => {
                app.notification('error', '外部比赛链接取消失败', err.responseJSON.msg);
            });
    },
    addLink: function (contestId) {
        var self = this;
        app.notification('pending', '正在添加外部比赛链接');
        qv.put('/api/group/cur/contest/' + $('#txtContestId').val())
            .then(x => {
                app.notification('succeeded', '外部比赛链接添加成功', x.msg);
                self.contestView.refresh();
            })
            .catch(err => {
                app.notification('error', '外部比赛链接添加失败', err.responseJSON.msg);
            });
    }
};