using System.Net;
using System.Net.Sockets;

Console.WriteLine("Starting server...");

TcpListener server = new TcpListener(IPAddress.Any, 4221);

server.Start();

var socket = server.AcceptSocket();
Console.WriteLine("Connection accepted.");

string response = "HTTP/1.1 200 OK\r\n\r\n";
byte[] sendBytes = System.Text.Encoding.ASCII.GetBytes(response);
socket.Send(sendBytes);

Console.WriteLine("Message sent /> : " + response);
