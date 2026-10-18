using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Titan.ChiefOfStaff.Eleads;

public static class CdpTargetReader
{
    public static async Task<PageSnapshot?> TryReadAsync(CdpPageTarget target)
    {
        if (string.IsNullOrWhiteSpace(target.WebSocketDebuggerUrl)
            || !CdpEndpoint.IsTrusted(target.WebSocketDebuggerUrl))
        {
            return null;
        }

        try
        {
            return await ReadAsync(target);
        }
        catch
        {
            return null;
        }
    }

    private static async Task<PageSnapshot> ReadAsync(CdpPageTarget target)
    {
        using var socket = new ClientWebSocket();
        using var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        await socket.ConnectAsync(new Uri(target.WebSocketDebuggerUrl!), cancel.Token);
        try
        {
            var result = await SendAsync(socket, cancel.Token, "Runtime.evaluate", new
            {
                expression = FrameTextExpression,
                returnByValue = true
            });
            return ParseEvaluate(target, result);
        }
        finally
        {
            if (socket.State == WebSocketState.Open)
            {
                try
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
                }
                catch
                {
                    // The page stays open. Dropping this debug socket is enough.
                }
            }
        }
    }

    private static async Task<JsonElement> SendAsync(ClientWebSocket socket, CancellationToken cancel, string method, object parameters)
    {
        var messageId = 1;
        var payload = JsonSerializer.Serialize(new
        {
            id = messageId,
            method,
            @params = parameters
        });
        var bytes = Encoding.UTF8.GetBytes(payload);
        await socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancel);
        while (true)
        {
            var text = await ReceiveTextAsync(socket, cancel);
            using var doc = JsonDocument.Parse(text);
            if (!doc.RootElement.TryGetProperty("id", out var id) || id.GetInt32() != messageId)
            {
                continue;
            }

            if (doc.RootElement.TryGetProperty("error", out _))
            {
                throw new InvalidOperationException("CDP target read failed.");
            }

            if (!doc.RootElement.TryGetProperty("result", out var result))
            {
                return default;
            }

            return result.Clone();
        }
    }

    private static async Task<string> ReceiveTextAsync(ClientWebSocket socket, CancellationToken cancel)
    {
        var buffer = new byte[8192];
        using var stream = new MemoryStream();
        while (true)
        {
            var received = await socket.ReceiveAsync(buffer, cancel);
            if (received.MessageType == WebSocketMessageType.Close)
            {
                throw new InvalidOperationException("CDP socket closed.");
            }

            stream.Write(buffer, 0, received.Count);
            if (stream.Length > 4_000_000)
            {
                throw new InvalidOperationException("CDP target text is too large to read.");
            }

            if (received.EndOfMessage)
            {
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
    }

    private static PageSnapshot ParseEvaluate(CdpPageTarget target, JsonElement result)
    {
        var fallbackUrl = target.Url ?? "";
        if (result.ValueKind != JsonValueKind.Object
            || !result.TryGetProperty("result", out var remote)
            || !remote.TryGetProperty("value", out var value)
            || value.ValueKind != JsonValueKind.Array)
        {
            return new PageSnapshot(fallbackUrl, [new FrameSnapshot(fallbackUrl, "")]);
        }

        var frames = new List<FrameSnapshot>();
        foreach (var row in value.EnumerateArray())
        {
            var url = row.TryGetProperty("url", out var urlEl) ? urlEl.GetString() ?? "" : "";
            var text = row.TryGetProperty("text", out var textEl) ? textEl.GetString() ?? "" : "";
            if (string.IsNullOrWhiteSpace(url))
            {
                url = fallbackUrl;
            }

            frames.Add(new FrameSnapshot(url, text));
        }

        if (frames.Count == 0)
        {
            frames.Add(new FrameSnapshot(fallbackUrl, ""));
        }

        var pageUrl = string.IsNullOrWhiteSpace(frames[0].Url) ? fallbackUrl : frames[0].Url;
        return new PageSnapshot(pageUrl, frames);
    }

    private const string FrameTextExpression = """
        (() => {
          const frames = [];
          const seen = [];
          const visit = (frameWindow, depth) => {
            if (!frameWindow || depth > 8 || frames.length > 30) return;
            try {
              if (seen.indexOf(frameWindow) !== -1) return;
              seen.push(frameWindow);
            } catch (e) {}
            let url = '';
            let text = '';
            try { url = String(frameWindow.location && frameWindow.location.href || ''); } catch (e) {}
            try {
              const body = frameWindow.document && frameWindow.document.body;
              text = body ? String(body.innerText || '') : '';
            } catch (e) {}
            frames.push({ url, text });
            let count = 0;
            try { count = frameWindow.frames ? frameWindow.frames.length : 0; } catch (e) { count = 0; }
            for (let i = 0; i < count; i++) {
              try { visit(frameWindow.frames[i], depth + 1); } catch (e) {}
            }
          };
          visit(window, 0);
          return frames;
        })()
        """;
}
