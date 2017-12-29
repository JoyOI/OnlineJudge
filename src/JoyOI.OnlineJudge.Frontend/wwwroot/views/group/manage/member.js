component.data = function () {
    return {
        name: app.group.name,
        description: app.group.description,
        joinMethod: app.group.joinMethod,
        domain: app.group.domain
    };
};

component.created = function () {
    app.title = '成员管理';
};

component.methods = {
    save: function () {
        this.description = $('.markdown-textbox')[0].smde.codemirror.getValue();
        app.notification('pending', '正在编辑团队信息');
        qv.patch('/api/group/' + app.group.id, {
            name: this.name,
            joinMethod: this.joinMethod,
            description: this.description,
            domain: this.domain
        })
            .then(x => {
                app.notification('succeeded', '团队信息编辑成功', x.msg);
                app.group.name = this.name;
                app.group.description = this.description;
                app.group.joinMethod = this.joinMethod;
                app.group.domain = this.domain;
            })
            .catch(err => {
                app.notification('error', '团队信息编辑失败', err.responseJSON.msg);
            });
    }
};