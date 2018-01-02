component.data = function () {
    return {
        id: null,
        title: null,
        begin: null,
        duration: '3:00:00',
        attendPermission: app.isGroup ? 2 : 0,
        type: 0,
        passwordOrTeamId: null
    };
};

component.methods = {
    createContest: function () {
        app.notification('pending', '正在创建比赛');
        qv.put('/api/contest/' + this.id, {
            title: this.title,
            begin: (new Date($('#txtBegin').val())).toGMTString(),
            duration: this.duration,
            attendPermission: this.attendPermission,
            type: this.type,
            passwordOrTeamId: this.passwordOrTeamId
        })
            .then((x) => {
                app.notification('succeeded', '比赛创建成功', x.msg);
                var contestView = qv.createView('/api/contest/all', {
                    title: null,
                    type: null,
                    page: 1
                });
                contestView.refresh();
                app.redirect('/contest/:id/edit', '/contest/' + this.id + '/edit', { id: this.id });
            })
            .catch(err => {
                console.error(err);
                app.notification('error', '比赛创建失败', err.responseJSON.msg);
            });
    }
};

component.created = function () {
    app.title = '创建比赛';
    app.links = [{ text: '比赛列表', to: '/contest' }];
};