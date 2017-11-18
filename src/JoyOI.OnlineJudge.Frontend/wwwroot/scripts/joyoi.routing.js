LazyRouting.SetRoute({
    '/home': null,
    '/problem/all': null,
    '/problem/new': null,
    '/problem/:id/edit': { id: '[a-zA-Z0-9-_]{4,128}' },
    '/problem/:id/resolution': { id: '[a-zA-Z0-9-_]{4,128}' },
    '/problem/:id': { id: '[a-zA-Z0-9-_]{4,128}' },
    '/contest/all': null,
    '/contest/:id': { id: '[a-zA-Z0-9-_]{4,128}' },
    '/judge/all': null,
    '/judge/:id': { id: '[a-zA-Z0-9]{8}-[a-zA-Z0-9]{4}-[a-zA-Z0-9]{4}-[a-zA-Z0-9]{4}-[a-zA-Z0-9]{12}' },
    '/user/:username': { username: '[\u3040-\u309F\u30A0-\u30FF\u4e00-\u9fa5A-Za-z0-9_-]{4,128}' },
    '/404': null
});

LazyRouting.SetMirror({
    '/': '/home',
    '/problem': '/problem/all',
    '/judge': '/judge/all',
    '/contest': '/contest/all'
});