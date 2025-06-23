using System.Net;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseWebSockets();

app.MapGet("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var handler = new WebSocketRequestHandler();
        await handler.HandleAsync(webSocket);
    }
    else
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        await context.Response.WriteAsync("Expected web socket");
    }
});

app.Run();
