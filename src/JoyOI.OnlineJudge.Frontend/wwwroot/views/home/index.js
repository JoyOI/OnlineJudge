app.title = '首页';
app.links = [];

component.data = function () {
    return {
        contests: [
            { title: 'Joy OI Test Round #1', description: 'blablabla' },
            { title: 'Test', description: 'blablabla' },
        ],
        threads: []
    }
}

component.created = function () {
    var self = this;
    qv.createView('/api/forum/summary', null, 60000)
        .fetch(x =>
        {
            self.threads = x.data;
        })
        .catch(err => {
            app.notification('error', '获取论坛帖子失败', err.responseJSON.msg);
        });
}