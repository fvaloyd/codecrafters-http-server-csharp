using System.Net;
using System.Net.Sockets;
using System.Text;

Console.WriteLine("Starting server...");
TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();

const string ECHO_PATH = "/echo/";
const string FILE_PATH = "/files";
const string USER_AGENT_PATH = "/user-agent";
byte[] OkSl = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\n");
byte[] CreatedSl = Encoding.ASCII.GetBytes("HTTP/1.1 201 Created\r\n");
byte[] NotFoundSl = Encoding.ASCII.GetBytes("HTTP/1.1 404 Not Found\r\n");

while (true)
{
    ThreadPool.QueueUserWorkItem(hr);
}

void hr(object? o)
{
    var s = server.AcceptSocket();
    Console.WriteLine("Connection accepted.");

    var rb = new byte[1024];
    s.Receive(rb);

    var cLen = Array.FindIndex(rb, 0, rb.Length, b => b == 00);
    rb = rb[..cLen];
    (byte[] StartLine, byte[] Headers, byte[] Content) = LookupParts(rb);
    string[] sl = ParseSl(StartLine);
    Dictionary<string, string> h = ParseHeaders(Headers);

    if (sl[1] == "/")
    {
        s.Send(OkSl.Concat(new byte[] { 13, 10, 13, 10 }).ToArray());
        s.Close();
    }
    else if (sl[1].StartsWith(ECHO_PATH))
    {
        handleEcho(s, sl[1]);
    }
    else if (sl[1].StartsWith(USER_AGENT_PATH))
    {
        handleUserAgent(s, h);
    }
    else if (sl[1].StartsWith(FILE_PATH))
    {
        handleFile(s, sl[0], sl[1], Content);
    }
    else
    {
        s.Send(NotFoundSl.Concat(new byte[] { 13, 10, 13, 10 }).ToArray());
        s.Close();
    }
}

(byte[] StartLine, byte[] Headers, byte[] Content) LookupParts(byte[] request)
{
    const byte CARRIAGE_RETURN = 13;
    const byte LINE_FEED = 10;
    const int ESCAPE_LEN = 4;
    byte[] sl = new byte[0], h = new byte[0], c = new byte[0];

    for (int i = 0; i < request.Length; i++)
    {
        if (request[i] == CARRIAGE_RETURN && request[i + 1] == LINE_FEED)
        {
            sl = request[..i];
            break;
        }
    }

    for (int i = sl.Length; i < request.Length; i++)
    {
        if (request[i] == CARRIAGE_RETURN && request[i + 1] == LINE_FEED && request[i + 2] == CARRIAGE_RETURN && request[i + 3] == LINE_FEED)
        {
            h = request[sl.Length..i];
            break;
        }
    }

    int totalReaded = sl.Length + h.Length + ESCAPE_LEN;
    c = request[totalReaded..];
    return (sl, h, c);
}

string[] ParseSl(byte[] slb)
{
    string sl = Encoding.ASCII.GetString(slb);
    return sl.Split(' ');
}

Dictionary<string, string> ParseHeaders(byte[] hb)
{
    Dictionary<string, string> headers = new();
    string h = Encoding.ASCII.GetString(hb);
    var lines = h.Split("\r\n");
    foreach (var line in lines)
    {
        if (string.IsNullOrEmpty(line)) continue;
        var header = line.Split(':');
        headers.TryAdd(header[0].Trim(), header[1].Trim());
    }
    return headers;
}

void handleEcho(Socket s, string path)
{
    var content = path.Replace(ECHO_PATH, "");
    var cb = Encoding.ASCII.GetBytes(content);
    var h = Encoding.ASCII.GetBytes($"Content-Type: text/plain\r\nContent-Length: {cb.Length}\r\n\r\n");
    byte[] response = OkSl.Concat(h).Concat(cb).ToArray();
    s.Send(response);
    s.Close();
    Console.WriteLine("Sending />\r\n" + Encoding.ASCII.GetString(response));
}

void handleUserAgent(Socket s, Dictionary<string, string> h)
{
    var content = h["User-Agent"];
    var cb = Encoding.ASCII.GetBytes(content);
    var hb = Encoding.ASCII.GetBytes($"Content-Type: text/plain\r\nContent-Length: {cb.Length}\r\n\r\n");
    byte[] response = OkSl.Concat(hb).Concat(cb).ToArray();
    s.Send(response);
    s.Close();
    Console.WriteLine("Sending />\r\n" + Encoding.ASCII.GetString(response));
}

void handleFile(Socket s, string httpMethod, string path, byte[] content)
{
    if (httpMethod == "GET")
    {
        handleFileGET(s, path);
    }
    else if (httpMethod == "POST")
    {
        handleFilePOST(s, path, content);
    }
}

void handleFileGET(Socket s, string path)
{
    var directoryName = args[Array.FindIndex(args, 0, args.Length, a => a == "--directory") + 1];
    var fileName = path.Replace(FILE_PATH, "");
    var fileFullPath = directoryName + "/" + fileName;

    if (File.Exists(fileFullPath))
    {
        byte[] content = File.ReadAllBytes(fileFullPath);
        byte[] headers = Encoding.ASCII.GetBytes($"Content-Type: application/octet-stream\r\nContent-Length: {content.Length}\r\n\r\n");
        byte[] response = OkSl.Concat(headers).Concat(content).Concat(new byte[] { 13, 10, 13, 10 }).ToArray();
        s.Send(response);
        s.Close();
        Console.WriteLine("Sending />\r\n" + Encoding.ASCII.GetString(response));
    }
    else
    {
        s.Send(NotFoundSl.Concat(new byte[] { 13, 10, 13, 10 }).ToArray());
        s.Close();
    }
}

void handleFilePOST(Socket s, string path, byte[] content)
{
    var directoryName = args[Array.FindIndex(args, 0, args.Length, a => a == "--directory") + 1];
    var fileName = path.Replace(FILE_PATH, "");
    var fileFullPath = directoryName + "/" + fileName;

    File.WriteAllBytes(fileFullPath, content);
    var hb = Encoding.ASCII.GetBytes($"Content-Type: text/plain\r\nContent-Length: {content.Length}\r\n\r\n");
    byte[] response = CreatedSl.Concat(hb).Concat(content).Concat(new byte[] { 13, 10, 13, 10 }).ToArray();
    s.Send(response);
    s.Close();
}
