component.data = function () {
    return {
        id: null,
        title: null,
        begin: null,
        duration: null,
        attendPermission: 0,
        type: 0,
        passwordOrTeamId: null
    };
};

component.methods = {
};

component.created = function () {
    app.title = '创建比赛';
    app.links = [{ text: '比赛列表', to: '/contest' }];
};