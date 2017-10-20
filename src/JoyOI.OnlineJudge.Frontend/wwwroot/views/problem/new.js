component.methods = {
    newProblem: function () {
        var id = $('#txtProblemId').val();
        app.notification('pending', '正在创建题目...');
        qv.put('/api/problem/' + id, {
            title: app.user.profile.username + '创建的新题目'
        })
            .then(x => {
                app.notification('succeeded', '题目创建成功');
                app.redirect('/problem/:id/edit', '/problem/' + id + '/edit', { id: id });
            })
            .catch(err => {
                app.notification('error', '题目创建失败', err.responseJSON.msg);
            });
    }
};