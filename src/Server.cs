using System.Net;
using System.Net.Sockets;
using System.Text;

Console.WriteLine("Starting server...");

TcpListener server = new TcpListener(IPAddress.Any, 4221);

server.Start();

var socket = server.AcceptSocket();
Console.WriteLine("Connection accepted.");

var responseBytes = new byte[255];
int bytesReceived = socket.Receive(responseBytes);

string request = Encoding.ASCII.GetString(responseBytes);
RequestStartLine sl = RequestStartLine.ParseFromStrRequest(request);


if (sl.Path == "/" || sl.Path.StartsWith("/echo"))
{
    var randomString = sl.Path.Split('/').Last();
    var response = Response.Ok(randomString);
    socket.Send(response.ToByte());
    Console.WriteLine(response.Format());
}
else
{
    socket.Send(Encoding.ASCII.GetBytes("HTTP/1.1 404 Not Found\r\n\r\n"));
}

record Response
{
    public Dictionary<string, string> Headers { get; } = new();
    public ResponseStartLine StartLine { get; private set; }
    public string Body { get; } = string.Empty;
    private Response(ResponseStartLine startLine, Dictionary<string, string> headers, string body)
        => (StartLine, Headers, Body) = (startLine, headers, body);

    public static Response Ok(string body)
    {
        var headers = new Dictionary<string, string>()
        {
            {"Content-Type", "text/plain"},
            {"Content-Length", body.Length.ToString()}
        };
        return new(ResponseStartLine.Ok(), headers, body);
    }

    public string FormatHeaders()
    {
        var headersFormated = new StringBuilder();
        foreach (var kvp in Headers)
        {
            headersFormated.Append($"{kvp.Key}: {kvp.Value}\r\n");
        }
        return headersFormated.ToString();
    }

    public string Format()
    {
        var response = new StringBuilder();
        response.Append(StartLine.ToString());
        response.Append(FormatHeaders());
        response.Append("\r\n");
        response.Append(Body + "\r\n\r\n");
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
        var lines = request.Split("\r\n");
        foreach (var line in lines)
        {
            if (string.IsNullOrEmpty(line)) break;
            var header = line.Split(':');
            headers.TryAdd(header[0], header[1]);
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
