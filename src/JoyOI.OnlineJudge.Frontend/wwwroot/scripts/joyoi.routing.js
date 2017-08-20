LazyRouting.SetRoute({
    '/home': null,
    '/problem/all': null,
    '/problem/:id': { id: '[a-zA-Z0-9-_]{4,128}' },
    '/contest/all': null,
    '/contest/:id': { id: '[a-zA-Z0-9-_]{4,128}' },
    '/judge/all': null,
    '/judge/:id': { id: '[a-zA-Z0-9]{8}-[a-zA-Z0-9]{4}-[a-zA-Z0-9]{4}-[a-zA-Z0-9]{4}-[a-zA-Z0-9]{12}' },
    '/404': null
});

LazyRouting.SetMirror({
    '/': '/home',
    '/problem': '/problem/all',
    '/judge': '/judge/all',
    '/contest': '/contest/all'
});