component.created = function () {
    app.title = '团队列表';
    app.links = [];

    this.view = qv.createView('/api/group/all', {
        page: this.paging.current,
        name: this.request.name
    });

    this.view.fetch(x => {
        this.paging.count = x.data.count;
        this.paging.current = x.data.current;
        this.paging.total = x.data.total;
        this.result = x.data.result;
    });
};

component.watch = {
    deep: true,
    'paging.current': function () {
        app.redirect('/group', '/group', {}, { 'paging.current': this.paging.current || 1, 'request.name': this.request.name || '' });
    },
    'request.name': function () {
        app.redirect('/group', '/group', {}, { 'request.name': this.request.name || '' });
    }
};

component.methods = {
    filterGroup: function () {
        this.request.name = $('#txtSearchGroupTitle').val();
    }
};

component.data = function () {
    return {
        result: [],
        paging: {
            current: 1,
            count: 1,
            total: 0
        },
        request: {
            name: null
        },
        view: null
    };
};