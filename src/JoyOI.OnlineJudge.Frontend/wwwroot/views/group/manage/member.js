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


component.watch = {
    'paging.current': function (value, old) {
        app.redirect('/group/manage/member', '/group/manage/member', {}, { status: this.status, 'paging.current': this.paging.current });
    },
    deep: true
};

component.created = function () {
    app.title = '成员管理';
    app.links = [];
    if (!app.isGroup || !app.groupSession || !app.groupSession.isMaster) {
        app.redirect('/', '/');
    }

    var self = this;

    this.memberView = qv.createView('/api/group/' + app.group.id + '/member/all',
        {
            status: this.status,
            page: this.paging.current
        });

    this.memberView
        .fetch(x => {
            self.paging.count = x.data.count;
            self.paging.total = x.data.total;
            self.result = x.data.result.map(y => {
                var z = clone(y);
                z.username = app.lookup.user[y.userId] ? app.lookup.user[y.userId].name : undefined;
                z.roleClass = app.lookup.user[y.userId] ? app.lookup.user[y.userId].class : undefined;
                z.avatarUrl = app.lookup.user[y.userId] ? app.lookup.user[y.userId].avatar : undefined;
                return z;
            });

            if (self.result.length) {
                app.lookupUsers({ userIds: x.data.result.map(y => y.userId) })
                    .then(() => {
                        for (var i = 0; i < self.result.length; i++) {
                            self.result[i].username = app.lookup.user[self.result[i].userId].name;
                            self.result[i].roleClass = app.lookup.user[self.result[i].userId].class;
                            self.result[i].avatarUrl = app.lookup.user[self.result[i].userId].avatar;
                        }
                        this.$forceUpdate();
                    });
            }
        });
};

component.methods = {
    remove: function (username) {
        if (confirm("您确定要移除这名团队成员吗？")) {
            app.notification('pending', '正在移除团队成员');
            qv.delete('/api/group/' + app.group.id + '/member/' + username)
                .then(x => {
                    app.notification('succeeded', '移除团队成员成功', x.msg);
                    if (this.memberView) {
                        this.memberView.refresh();
                    }
                })
                .catch(err => {
                    app.notification('error', '移除团队成员失败', err.responseJSON.msg);
                });
        }
    },
    promote: function (username) {
        if (confirm("您确定要提升这名成员成为团队管理员吗？")) {
            app.notification('pending', '正在提升管理员');
            qv.put('/api/group/' + app.group.id + '/claim/' + username)
                .then(x => {
                    app.notification('succeeded', '提升管理员成功', x.msg);
                    if (this.memberView) {
                        this.memberView.refresh();
                    }
                })
                .catch(err => {
                    app.notification('error', '提升管理员失败', err.responseJSON.msg);
                });
        }
    },
    demote: function (username) {
        if (confirm("您确定要撤销这名成员的管理员身份吗？")) {
            app.notification('pending', '正在撤销管理员');
            qv.delete('/api/group/' + app.group.id + '/claim/' + username)
                .then(x => {
                    app.notification('succeeded', '撤销管理员成功', x.msg);
                    if (this.memberView) {
                        this.memberView.refresh();
                    }
                })
                .catch(err => {
                    app.notification('error', '撤销管理员失败', err.responseJSON.msg);
                });
        }
    },
    approve: function (username) {
        app.notification('pending', '正在批准加入团队申请');
        qv.patch('/api/group/' + app.group.id + '/member/' + username, { status: 'Approved' })
            .then(x => {
                app.notification('succeeded', '批准加入团队申请成功', x.msg);
                if (this.memberView) {
                    this.memberView.refresh();
                }
            })
            .catch(err => {
                app.notification('error', '撤销管理员失败', err.responseJSON.msg);
            });
    }
};