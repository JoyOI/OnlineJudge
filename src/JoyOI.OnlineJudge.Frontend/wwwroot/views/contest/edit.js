app.title = '编辑比赛';
app.links = [{ text: '比赛列表', to: '/contest' }, { text: '未知比赛', to: '/contest' }];

component.data = function () {
    return {
        id: router.history.current.params.id,
        current: 'basic',
        title: null,
        description: null,
        duration: null,
        attendPermission: null,
        attendeeCount: 0,
        begin: null,
        end: null,
        duration: null,
        type: null,
        claims: []
    };
};

component.methods = {
    loadContest: function () {
        qv.createView('/api/contest/' + this.id)
            .fetch(x => {
                this.title = x.data.title;
                this.description = x.data.description;
                this.duration = x.data.duration;
                this.attendeeCount = x.data.CachedAttendeeCount;
                this.begin = x.data.begin;
                this.end = x.data.end;
                this.type = x.data.type;
                this.attendPermission = x.data.attendPermission;
                app.links[1].text = x.data.title;
                app.links[1].to = { name: '/contest/:id', path: '/contest/' + this.id, params: { id: this.id } };
                try {
                    $('.markdown-textbox')[0].smde.codemirror.setValue(x.data.description);
                } catch (ex) { }
            })
            .catch(err => {
                app.notification('error', '获取比赛信息失败', err.responseJSON.msg);
            });
        qv.createView('/api/contest/' + this.id + '/claim/all')
            .fetch(x => {
                this.claims = x.data;
            });
    }
};

component.created = function () {
    this.loadContest();
};