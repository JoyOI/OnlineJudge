component.data = function () {
    return {
        groupId: null,
        name: null
    };
}

component.methods = {
    create: function () {
        app.notification('pending', '正在创建团队');
        qv.put('/api/group/' + this.groupId, {
            name: this.name
        })
            .then(x => {
                app.notification('succeeded', '团队创建成功', x.msg);
                window.location = app.hosts.group.replace('{GROUPID}', this.groupId);
            })
            .catch(err => {
                app.notification('error', '团队创建失败', err.responseJSON.msg);
            });
    }
};

component.created = function () {
    app.title = '创建团队';
    app.links = [{ text: '团队列表', to: '/group' }];
};