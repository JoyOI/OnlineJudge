app.title = '题解';
app.links = [{ text: '题目列表', to: '/problem' }, { text: '未知题目', to: '/problem' }];

component.data = function () {
    return {
        id: router.history.current.params.id,
        title: null,
        paging: {
            current: 1,
            count: 1,
            total: 0
        },
        request: {
            tag: null,
            title: null,
            page: 1
        },
        result: [],
        blogUrl: app.user.isSignedIn ? app.hosts.blog.replace('{USERNAME}', app.user.profile.username) : null
    };
};

component.created = function () {
    var self = this;

    qv.createView('/api/problem/' + this.id)
        .fetch(x => {
            self.title = x.data.title;
            app.links[1].text = self.title;
            app.links[1].to = {
                name: '/problem/:id',
                path: '/problem/' + self.id,
                params: { id: self.id }
            };
        })
        .catch(err => {
            app.notification('error', '获取题目信息失败', err.responseJSON.msg);
        });

    if (app.user.isSignedIn) {
        qv.createView('/api/user/' + app.user.profile.username + '/blog')
        .fetch(x => {
            self.blogUrl = x.data;
        })
        .catch(err => {
        });
    }

    qv.createView('/api/problem/' + this.id + '/resolution', this.request)
        .fetch(x => {
            self.result = x.data.result;
            this.paging.count = x.data.count;
            this.paging.current = x.data.current;
            this.paging.total = x.data.total;

            qv.createView('/api/user/role', { usernames: self.result.map(y => y.username).toString()}).fetch(y => {
                for (var i = 0; i < self.submittorSearchResult.length; i++) {
                    self.result[i].roleClass = ConvertUserRoleToCss(y.data[self.submittorSearchResult[i].username].role);
                }
            })
            .catch(err => {
                app.notification('error', '获取用户角色失败', err.responseJSON.msg);
            });
        })
        .catch(err => {
            app.notification('error', '获取题解失败', err.responseJSON.msg);
        });
};