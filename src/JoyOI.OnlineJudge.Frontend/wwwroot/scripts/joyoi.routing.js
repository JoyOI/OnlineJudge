LazyRouting.SetRoute({
    "/home": null,
    "/foo/:id": { id: "[0-9a-zA-Z-]{3,16}" },
    "/bar": null
});
LazyRouting.PushAsync("/404", "/views/404.html");