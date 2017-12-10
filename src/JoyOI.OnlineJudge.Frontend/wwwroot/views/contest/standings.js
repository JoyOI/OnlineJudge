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

component.watch = {
    excludeVirtual: function (val) {
        if (val) {
            app.redirect('/contest/:id/standings', '/contest/' + this.id + '/standings', { id: this.id }, { 'excludeVirtual': this.excludeVirtual.toString() });
        } else {
            app.redirect('/contest/:id/standings', '/contest/' + this.id + '/standings', { id: this.id });
        }
    }
};

component.methods = {
    loadStandings: function () {
        var self = this;
        this.view = qv.createView('/api/contest/' + this.id + '/standings/all', { includingVirtual: this.excludeVirtual ? false : true }, 60000);
        this.view.fetch(x => {
            this.problems = x.data.problems;
            this.attendees = x.data.attendees;
            this.columns = x.data.columnDefinations;
            app.links[1].text = x.data.title;
            app.links[1].to = { name: '/contest/:id', path: '/contest/' + this.id, params: { id: this.id } };

            app.lookupUsers({ userIds: x.data.attendees.map(y => y.userId) })
                .then(() => {
                    for (var i = 0; i < self.attendees.length; i++) {
                        self.attendees[i].roleClass = app.lookup.user[self.attendees[i].userId].class;
                        self.attendees[i].avatarUrl = app.lookup.user[self.attendees[i].userId].avatar;
                        self.attendees[i].username = app.lookup.user[self.attendees[i].userId].name;
                    }
                    this.$forceUpdate();
                });
        });
    }
};