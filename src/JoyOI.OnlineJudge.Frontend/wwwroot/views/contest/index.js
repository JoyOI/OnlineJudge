app.title = '比赛';
app.links = [{ text: '比赛列表', to: '/contest' }];

component.data = function () {
    return {
        id: router.history.current.params.id,
        title: null,
        body: null,
        attendeeCount: 0,
        begin: null,
        end: null,
        hasPermissionToEdit: false
    };
};

component.computed = {
    status: function () {
        if (this.begin && new Date() < new Date(this.begin))
            return 'Pending';
        else if (this.end && new Date() <= new Date(this.end))
            return 'Live';
        else if (this.end && new Date() > new Date(this.end))
            return 'Done';
        else
            return 'Pending';
    },
    beginTimestamp: function () {
        return new Date(this.begin).getTime();
    },
    endTimestamp: function () {
        return new Date(this.end).getTime();
    },
    countDownTimestamp: function () {
        if (this.status === 'Pending')
            return this.beginTimestamp;
        else
            return this.endTimestamp;
    }
};

component.methods = {
    loadContest: function () {
        qv.createView('/api/contest/' + this.id)
            .fetch(x => {
                this.title = x.data.title;
                this.body = x.data.description;
                this.attendeeCount = x.data.CachedAttendeeCount;
                this.begin = x.data.begin;
                this.end = x.data.end;
                app.title = this.title;
            })
            .catch(err => {
                app.notification('error', '获取比赛信息失败', err.responseJSON.msg);
            });
    }
};

component.created = function () {
    this.loadContest();
};