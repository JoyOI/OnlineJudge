component.data = function () {
    return {
        memberView: null,
        result: [],
        status: 'Approved',
        paging: {
            current: 1,
            count: 1,
            total: 0
        }
    };
};

component.created = function () {
    app.title = '成员管理';
    if (!app.isGroup || !app.groupSession || !app.groupSession.isMaster) {
        app.redirect('/', '/');
    }

    var self = this;

    this.memberView = qv.createView('/api/group/' + app.group.id + '/member/all',
        {
            status: this.status
        });

    this.memberView
        .fetch(x => {
            self.paging.count = x.data.count;
            self.paging.total = x.data.total;
            self.result = x.data.result.map(y => {
                var z = clone(y);
                z.username = app.lookup.user[y.userId] ? app.lookup.user[y.userId].name : undefined;
                z.roleClass = app.lookup.user[y.userId] ? app.lookup.user[y.userId].class : undefined;
                return z;
            });

            if (self.result.length) {
                app.lookupUsers({ userIds: x.data.result.map(y => y.userId) })
                    .then(() => {
                        for (var i = 0; i < self.result.length; i++) {
                            self.result[i].username = app.lookup.user[self.result[i].userId].name;
                            self.result[i].roleClass = app.lookup.user[self.result[i].userId].class;
                        }
                        this.$forceUpdate();
                    });
            }
        });
};

component.methods = {
    remove: function (username) {
        app.notification('pending', '正在移除团队成员');
        qv.delete('/api/group/' + app.group.id + '/member/' + username)
            .then(x => {
                app.notification('succeeded', '移除团队成员成功', x.msg);
                if (this.memberView) {
                    this.memberView.refresh();
                }
            });
    }
};