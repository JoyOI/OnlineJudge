component.data = function () {
    return {
        step: 0
    };
}

component.created = function () {
    app.title = '加入团队';
    if (app.groupSession && app.groupSession.isMember) {
        window.location = '/';
    }
};

component.methods = {
    submit: function () {
        app.notification('pending', '正在提交团队加入申请');
        qv.put('/api/group/' + app.group.id + '/member/' + app.user.profile.username, {
            message: $('#txtMessage').val()
        })
            .then(x => {
                app.notification('succeeded', '团队加入申请已经提交成功', x.msg);
                this.step = 1;
            })
            .catch(err => {
                app.notification('error', '团队加入申请已经提交失败', err.responseJSON.msg);
            });
    }
};