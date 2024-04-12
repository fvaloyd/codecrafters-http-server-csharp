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
RequestStartLine sl = RequestStartLine.Create(request);

string okResponseStartLine = "HTTP/1.1 200 OK\r\n\r\n";
string notFoundResponseStartLine = "HTTP/1.1 404 NotFound\r\n\r\n";

if (sl.Path == "/")
{
    byte[] sendBytes = Encoding.ASCII.GetBytes(okResponseStartLine);
    socket.Send(sendBytes);
    Console.WriteLine(okResponseStartLine + "response sended");
}
else
{
    byte[] sendBytes = Encoding.ASCII.GetBytes(notFoundResponseStartLine);
    socket.Send(sendBytes);
    Console.WriteLine(notFoundResponseStartLine + "response sended");
}

record struct RequestStartLine(string HttpMethod, string Path, string HttpVersion)
{
    public static RequestStartLine Create(string request)
    {
        var startLine = request
                .Split("\r\n")
                .First()
                .Split(' ');
        return new(startLine[0], startLine[1], startLine[2]);
    }

    public override string ToString()
        => $"{HttpMethod} {Path} {HttpVersion}";

    public byte[] ToByte()
        => Encoding.ASCII.GetBytes(ToString());
}
