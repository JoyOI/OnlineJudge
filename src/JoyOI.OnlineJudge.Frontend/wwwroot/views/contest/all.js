app.title = '比赛';
app.links = [];

component.data = function () {
    return {
        paging: {
            current: 1,
            count: 1
        },
        result: [ ]
    }
}

component.created = function () {
    var self = this;
    qv.createView('/api/contest/all', null, 60000)
        .fetch(x => {
            var results = clone(x.data.result);
            results = results.map(y =>
            {
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