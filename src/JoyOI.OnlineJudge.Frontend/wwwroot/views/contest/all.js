component.data = function () {
    return {
        paging: {
            current: 1,
            count: 1
        },
        contestTypes: contestTypes,
        result: [],
        request: {
            title: null,
            type: null
        }
    }
}

component.created = function () {
    app.title = '比赛';
    app.links = [];
    this.loadContests();
}

component.methods = {
    filterContests: function () {
        this.request.title = $('#txtSearchContestTitle').val();
        this.request.type = $('#lstSearchContestType').val();
        this.loadContests();
    },
    loadContests: function () {
        var self = this;
        qv.createView('/api/contest/all', self.request, 60000)
            .fetch(x => {
                self.paging.count = x.data.count;
                self.paging.current = x.data.current;
                self.paging.total = x.data.total;

                var results = clone(x.data.result);
                results = results.map(y => {
                    var begin = new Date(y.begin);
                    var end = new Date(begin.getTime() + parseTimeSpan(y.duration))
                    if (new Date() < begin) {
                        y.status = 'Ready';
                        y.statusClass = 'contest-ready';
                    } else if (new Date() < end) {
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
    }
};