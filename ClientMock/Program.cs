// See https://aka.ms/new-console-template for more information

using System.Net;
using System.Net.Sockets;
using ClientMock;
using Shared;

Console.WriteLine("Hello, World!");

using TcpClient client = new TcpClient();
client.Connect(new IPEndPoint(IPAddress.Loopback, 2597));
using var reader = new BinaryReader(client.GetStream());
using var writer = new BinaryWriter(client.GetStream());

Packets.readPacket(reader, "Protocol");
writer.Write(Packets.LoginPacket());
Packets.readPacket(reader, "LoginResponse");
Packets.readPacket(reader, "ClientList");
Packets.readPacket(reader, "ClientConnected");
Packets.readPacket(reader, "SetClientPos");
writer.Write(Packets.RadarHandlingPacket());
Packets.readPacket(reader, "RadarChecksum");
writer.Write(Packets.RegionListPacket());
Packets.readPacket(reader, "RegionList");
writer.Write(Packets.RequestBlockPacket());
Packets.readPacket(reader, "BlocksPacket");







client.Dispose();