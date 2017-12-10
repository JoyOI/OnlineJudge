component.data = function () {
    return {
        id: router.history.current.params.id,
        virtualDisabled: false,
        needPassword: false,
        view: null,
        contestEnd: false,
        contestBegin: false
    };
};

component.methods = {
    getRegisterStatus: function () {
        this.view = qv.createView('/api/contest/' + this.id + '/register');

        this.view.fetch(x => {
            if (x.isRegistered) {
                app.redirect('/contest/:id', '/contest/' + this.id, { id: this.id });
            }
        });
    },
    register: function (isVirtual) {
        qv.put('/api/contest/' + this.id + '/register', {
            isVirtual: isVirtual,
            password: this.needPassword ? $('#txtPassword').val() : null
        })
            .then((x) => {
                app.notification('succeeded', '报名参赛成功', x.msg);
                this.view.refresh();
                qv.createView('/api/contest/' + this.id + '/session').refresh();
                app.redirect('/contest/:id', '/contest/' + this.id, { id: this.id });
            })
            .catch(err => {
                app.notification('error', '报名参赛失败', err.responseJSON.msg);
            });
    }
};

component.created = function () {
    app.title = '报名参赛';
    app.links = [{ text: '比赛列表', to: '/contest' }, { text: '未知比赛', to: '/contest' }];

    qv.createView('/api/contest/' + this.id)
        .fetch(x => {
            this.virtualDisabled = x.data.disableVirtual;
            this.needPassword = x.data.attendPermission === 1;
            this.contestBegin = new Date(app.ensureUTCTimeString(x.data.begin)) <= new Date();
            this.contestEnd = new Date(app.ensureUTCTimeString(x.data.end)) <= new Date();
            app.links[1].text = x.data.title;
            app.links[1].to = { name: '/contest/:id', path: '/contest/' + this.id, params: { id: this.id } };
        });
    this.getRegisterStatus();
};