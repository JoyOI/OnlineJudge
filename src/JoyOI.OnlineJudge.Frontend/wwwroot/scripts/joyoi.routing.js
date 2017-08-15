LazyRouting.SetRoute({
    '/home': null,
    '/problem/all': null,
    '/problem/:id': { id: '[a-zA-Z0-9-_]{4,128}' },
    '/judge/all': null,
    '/foo/:id': { id: '[0-9a-zA-Z-]{3,16}' },
    '/bar': null,
    '/404': null
});

LazyRouting.SetMirror({
    '/': '/home',
    '/problem': '/problem/all',
    '/judge': '/judge/all'
});