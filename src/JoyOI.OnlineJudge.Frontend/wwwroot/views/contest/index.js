app.title = '比赛';
app.links = [{ text: '比赛列表', to: '/contest' }];

component.data = function () {
    return {
        id: router.history.current.params.id,
        title: null,
        body: null
    };
};