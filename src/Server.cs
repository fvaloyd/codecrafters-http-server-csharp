using System.Net;
using System.Net.Sockets;

Console.WriteLine("Logs from your program will appear here!");

TcpListener server = new TcpListener(IPAddress.Any, 4221);

server.Start();

server.AcceptSocket();
