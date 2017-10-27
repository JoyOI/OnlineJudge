app.title = '比赛';
app.links = [];

component.data = function () {
    return {
        contests: [
            { title: 'Joy OI Test Round #1', description: 'blablabla' },
            { title: 'Test', description: 'blablabla' },
        ]
    }
}

component.created = function () {
    var self = this;
    qv.createView('/api/contest/all', null, 60000)
        .fetch(x => {
            self.contests = x.data;
        })
        .catch(err => {
            app.notification('error', '获取比赛失败', err.responseJSON.msg);
        });
}