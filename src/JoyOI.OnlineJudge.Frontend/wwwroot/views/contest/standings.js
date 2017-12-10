component.created = function () {
    app.title = '比赛排名';
    app.links = [{ text: '比赛列表', to: '/contest' }, { text: '未知比赛', to: '/contest' }];
    this.loadStandings();
};

component.data = function () {
    return {
        id: router.history.current.params.id,
        attendees: [],
        problems: [],
        columns: {},
        excludeVirtual: false,
        view: null
    };
};

component.methods = {
    loadStandings: function () {
        var self = this;
        this.view = qv.createView('/api/contest/' + this.id + '/standings/all', { includingVirtual: !this.excludeVirtual }, 60000);
        this.view.fetch(x => {
            this.problems = x.data.problems;
            this.attendees = x.data.attendees;
            this.columns = x.data.columnDefinations;
            app.links[1].text = x.data.title;
            app.links[1].to = { name: '/contest/:id', path: '/contest/' + this.id, params: { id: this.id } };

            var cachedUsers = Object.getOwnPropertyNames(app.lookup.user);
            var uncachedUsers = self.attendees.map(y => y.userId).filter(y => !cachedUsers.some(z => z == y));
            if (uncachedUsers.length) {
                qv.get('/api/user/role', { userIds: self.attendees.map(y => y.userId).toString() }).then(y => {
                    for (var z in y.data) {
                        app.lookup.user[z] = {
                            id: z.id,
                            avatar: y.data[z].avatarUrl,
                            name: y.data[z].username,
                            role: y.data[z].role,
                            class: ConvertUserRoleToCss(y.data[z].role)
                        };

                        app.lookup[y.data[z].username] = app.lookup.user[z];

                        var impactedResults = self.attendees.filter(r => r.userId == z);
                        for (var i in impactedResults) {
                            impactedResults[i].roleClass = app.lookup.user[z].class;
                            impactedResults[i].avatarUrl = app.lookup.user[z].avatar;
                            impactedResults[i].username = app.lookup.user[z].name;
                        }

                        self.$forceUpdate();
                    }
                });
            }
        });
    }
};