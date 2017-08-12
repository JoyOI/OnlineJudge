LazyRouting.SetRoute({
    "/home": null,
    "/foo/:id": { id: "[0-9a-zA-Z-]{3,16}" },
    "/bar": null,
    "/404": null
});

LazyRouting.SetMirror({
    "/": "/home"
});