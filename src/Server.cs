using System.Net;
using System.Net.Sockets;
using System.Text;

Console.WriteLine("Starting server...");

TcpListener server = new TcpListener(IPAddress.Any, 4221);

server.Start();

while (true)
{
    ThreadPool.QueueUserWorkItem(HandleRequest);
}

void HandleRequest(object? o)
{
    var socket = server.AcceptSocket();
    Console.WriteLine("Connection accepted.");

    var responseBytes = new byte[1024];
    int bytesReceived = socket.Receive(responseBytes);

    string request = Encoding.ASCII.GetString(responseBytes);
    Request rq = Request.CreateFromStrRequest(request);
    RequestStartLine sl = rq.StartLine;


    var echoPath = "/echo/";
    var userAgentPath = "/user-agent";
    var filePath = "/files/";

    if (sl.Path == "/")
    {
        socket.Send(Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\n\r\n"));
    }
    else if (sl.Path.StartsWith(filePath))
    {
        var directoryName = args[Array.FindIndex(args, 0, args.Length, a => a == "--directory") + 1];
        var fileName = sl.Path.Replace(filePath, "");
        var fileFullPath = directoryName + "/" + fileName;

        if (File.Exists(fileFullPath))
        {
            using var sr = File.OpenText(fileFullPath);
            StringBuilder content = new();
            string currLine;
            while ((currLine = sr.ReadLine()!) != null)
            {
                content.Append(currLine);
            }
            var response = Response.Ok(content.ToString());
            response.AddHeader("Content-Type", "application/octet-stream");
            socket.Send(response.ToByte());
            Console.WriteLine(response.Format());
        }
        else
        {
            socket.Send(Encoding.ASCII.GetBytes("HTTP/1.1 404 Not Found\r\n\r\n"));
            socket.Close();
            Console.WriteLine("Not found");
        }
    }
    else if (sl.Path.StartsWith(userAgentPath))
    {
        var content = rq.Headers["User-Agent"];
        var response = Response.Ok(content);
        socket.Send(response.ToByte());
        Console.WriteLine(response.Format());
    }
    else if (sl.Path.StartsWith(echoPath))
    {
        var content = sl.Path.Replace(echoPath, "");
        var response = Response.Ok(content);
        socket.Send(response.ToByte());
        Console.WriteLine(response.Format());
    }
    else
    {
        socket.Send(Encoding.ASCII.GetBytes("HTTP/1.1 404 Not Found\r\n\r\n"));
    }
}

record Response
{
    public Dictionary<string, string> Headers { get; } = new();
    public ResponseStartLine StartLine { get; private set; }
    public string Content { get; } = string.Empty;
    private Response(ResponseStartLine startLine, Dictionary<string, string> headers, string content)
        => (StartLine, Headers, Content) = (startLine, headers, content);

    public static Response Ok(string content)
    {
        var headers = new Dictionary<string, string>()
        {
            {"Content-Type", "text/plain"},
            {"Content-Length", content.Length.ToString()}
        };
        return new(ResponseStartLine.Ok(), headers, content);
    }

    public void AddHeader(string type, string value)
    {
        var found = Headers.TryGetValue(type, out var v);
        if (found)
        {
            Headers[type] = value;
        }
        else
        {
            Headers.Add(type, value);
        }
    }

    public string FormatHeaders()
    {
        var headersFormated = new StringBuilder();
        foreach (var kvp in Headers)
        {
            headersFormated.Append($"{kvp.Key.Trim()}: {kvp.Value.Trim()}\r\n");
        }
        return headersFormated.ToString();
    }

    public string Format()
    {
        var response = new StringBuilder();
        response.Append(StartLine.ToString());
        response.Append(FormatHeaders() + "\r\n");
        response.Append(Content + "\r\n\r\n");
        return response.ToString();
    }

    public byte[] ToByte()
        => Encoding.ASCII.GetBytes(Format());
}

record struct ResponseStartLine(string HttpVersion, string StatusCode, string StatusText)
{
    public static ResponseStartLine Ok()
        => new("HTTP/1.1", "200", "OK");
    public static ResponseStartLine NotFound(Request request)
        => new("HTTP/1.1", "404", "Not Found");

    public override string ToString()
        => $"{HttpVersion} {StatusCode} {StatusText}\r\n";
}

record Request
{
    public RequestStartLine StartLine { get; private set; }
    public Dictionary<string, string> Headers { get; } = new();

    private Request(RequestStartLine sl, Dictionary<string, string> headers)
        => (StartLine, Headers) = (sl, headers);

    public static Request CreateFromStrRequest(string request)
    {
        var sl = RequestStartLine.ParseFromStrRequest(request);
        var headers = ParseHeadersFromStrRequest(request);
        return new(sl, headers);
    }

    static Dictionary<string, string> ParseHeadersFromStrRequest(string request)
    {
        Dictionary<string, string> headers = new();
        var lines = request.Split("\r\n")[1..];
        foreach (var line in lines)
        {
            if (string.IsNullOrEmpty(line)) break;
            var header = line.Split(':');
            headers.TryAdd(header[0].Trim(), header[1].Trim());
        }
        return headers;
    }

    public string FormatHeaders()
    {
        var headersFormated = new StringBuilder();
        foreach (var kvp in Headers)
        {
            headersFormated.AppendLine($"{kvp.Key}: {kvp.Value}\r\n");
        }
        return headersFormated.ToString();
    }

    public string Format()
    {
        var request = new StringBuilder();
        request.Append(StartLine.ToString());
        request.Append(FormatHeaders());
        return request.ToString();
    }

    public byte[] ToByte()
        => Encoding.ASCII.GetBytes(Format());
}

record struct RequestStartLine(string HttpMethod, string Path, string HttpVersion)
{
    public static RequestStartLine ParseFromStrRequest(string request)
    {
        var startLine = request
                .Split("\r\n")
                .First()
                .Split(' ');
        return new(startLine[0], startLine[1], startLine[2]);
    }

    public override string ToString()
        => $"{HttpMethod} {Path} {HttpVersion}\r\n";

    public byte[] ToByte()
        => Encoding.ASCII.GetBytes(ToString());
}
