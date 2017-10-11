app.title = '用户资料';
app.links = [];

component.data = function () {
    return {
        id: null,
        role: null,
        roleClass: null,
        username: null,
        topics: [],
        passedProblems: []
    };
};

component.created = function () {
    qv.createView('/api/user/' + router.history.current.params.username)
        .fetch(x => {
                id = x.data.id,
                username = x.data.userName,
                passedProblems: x.data.passedProblems
        });
};