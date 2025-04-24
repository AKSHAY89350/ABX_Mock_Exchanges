//using System.Net.Sockets;
//using System.Text;
//using System.Text.Json;

//public class AbxPacket
//{
//    public string Symbol { get; set; }
//    public string Side { get; set; }
//    public int Quantity { get; set; }
//    public int Price { get; set; }
//    public int Sequence { get; set; }
//}
//class Program
//{
//    const string Host = "localhost";
//    const int Port = 3000;
//    const int PacketSize = 17;

//    static void Main()
//    {
//        var packets = RequestAllPackets();
//        //var receivedSequences = packets.Select(p => p.Sequence).OrderBy(s => s).ToList();
//        //var missing = FindMissingSequences(receivedSequences);

//        //Console.WriteLine($"Received {packets.Count} packets. Missing: {string.Join(",", missing)}");

//        //foreach (int seq in missing)
//        //{
//        //    var packet = RequestResendPacket(seq);
//        //    if (packet != null)
//        //        packets.Add(packet);
//        //}

//        var sortedPackets = packets.OrderBy(p => p.Sequence).ToList();
//        var json = JsonSerializer.Serialize(sortedPackets, new JsonSerializerOptions { WriteIndented = true });
//        File.WriteAllText("output.json", json);

//        Console.WriteLine("Output written to output.json");
//    }
//    static List<AbxPacket> RequestAllPackets()
//    {
//        var packets = new List<AbxPacket>();

//        using var client = new TcpClient(Host, Port);
//        using var stream = client.GetStream();

//        // Send callType = 1 (Stream All Packets)
//        stream.WriteByte(1);

//        var buffer = new byte[PacketSize];
//        int bytesRead;
//        while ((bytesRead = stream.Read(buffer, 0, PacketSize)) == PacketSize)
//        {
//            var packet = ParsePacket(buffer);
//            packets.Add(packet);

//            Console.WriteLine($"Symbol: {packet.Symbol}, Side: {packet.Side}, Quantity: {packet.Quantity}, Price: {packet.Price}, Sequence: {packet.Sequence}");
//        }


//        return packets;
//    }

//    static AbxPacket RequestResendPacket(int sequence)
//    {
//        using var client = new TcpClient(Host, Port);
//        using var stream = client.GetStream();

//        // Send callType = 2, resendSeq = sequence
//        stream.WriteByte(2);
//        stream.WriteByte((byte)sequence); // Only supports sequences < 256

//        var buffer = new byte[PacketSize];
//        int bytesRead = stream.Read(buffer, 0, PacketSize);

//        if (bytesRead == PacketSize)
//            return ParsePacket(buffer);

//        return null;
//    }

//    static AbxPacket ParsePacket(byte[] buffer)
//    {
//        string symbol = Encoding.ASCII.GetString(buffer[..4]);
//        string side = Encoding.ASCII.GetString(buffer[4..5]);
//        int quantity = BitConverter.ToInt32(buffer[5..9].Reverse().ToArray(), 0);
//        int price = BitConverter.ToInt32(buffer[9..13].Reverse().ToArray(), 0);
//        int sequence = BitConverter.ToInt32(buffer[13..17].Reverse().ToArray(), 0);

//        return new AbxPacket
//        {
//            Symbol = symbol,
//            Side = side,
//            Quantity = quantity,
//            Price = price,
//            Sequence = sequence
//        };
//    }

//    static List<int> FindMissingSequences(List<int> sortedSequences)
//    {
//        var missing = new List<int>();
//        for (int i = 0; i < sortedSequences.Count - 1; i++)
//        {
//            int current = sortedSequences[i];
//            int next = sortedSequences[i + 1];
//            for (int seq = current + 1; seq < next; seq++)
//            {
//                missing.Add(seq);
//            }
//        }
//        return missing;
//    }
//}

using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using System.Xml;
using Formatting = Newtonsoft.Json.Formatting;

public class Packet
{
    public string Symbol { get; set; }
    public char BuySellIndicator { get; set; }
    public int Quantity { get; set; }
    public int Price { get; set; }
    public int PacketSequence { get; set; }
}

class ABXClient
{
    const int PacketSize = 17;
    const string Host = "127.0.0.1";
    const int Port = 3000;

    public static void Main()
    {
        Console.WriteLine("Connected to ABX Mock Exchange...");
        Console.WriteLine("Streaming all packets (Call Type 1)...");

        Dictionary<int, Packet> allPackets = StreamAllPackets();

        Console.WriteLine("Do you want to fetch missing packets? (y/n): ");
        var input = Console.ReadLine()?.Trim().ToLower();

        if (input == "y")
        {
            var missing = FindMissingSequences(allPackets);
            foreach (var seq in missing)
            {
                var packet = RequestMissingPacket(seq);
                if (packet != null)
                {
                    allPackets[packet.PacketSequence] = packet;
                }
            }
        }

        //var allPackets = StreamAllPackets();

        //var missing = FindMissingSequences(allPackets);
        //foreach (var seq in missing)
        //{
        //    var packet = RequestMissingPacket(seq);
        //    if (packet != null)
        //        allPackets[packet.PacketSequence] = packet;
        //}

        var finalList = allPackets.Values.OrderBy(p => p.PacketSequence).ToList();
        foreach (var packet in finalList)
        {
            Console.WriteLine($"Seq: {packet.PacketSequence}, Symbol: {packet.Symbol}, " +
                              $"Side: {packet.BuySellIndicator}, Qty: {packet.Quantity}, Price: {packet.Price}");
        }
        File.WriteAllText("packets.json", JsonConvert.SerializeObject(finalList, Formatting.Indented));

        Console.WriteLine("JSON file created with all packets.");
    }

    static Dictionary<int, Packet> StreamAllPackets()
    {
        var packets = new Dictionary<int, Packet>();
        using var client = new TcpClient(Host, Port);
        using var stream = client.GetStream();

        stream.Write(new byte[] { 1, 0 }); // Stream all packets

        var buffer = new byte[PacketSize];
        int bytesRead;
        while ((bytesRead = stream.Read(buffer, 0, PacketSize)) == PacketSize)
        {
            var packet = ParsePacket(buffer);
            packets[packet.PacketSequence] = packet;
        }

        return packets;
    }

    static List<int> FindMissingSequences(Dictionary<int, Packet> packets)
    {
        int max = packets.Keys.Max();
        var missing = new List<int>();
        for (int i = 1; i < max; i++)
            if (!packets.ContainsKey(i))
                missing.Add(i);
        return missing;
    }

    static Packet RequestMissingPacket(int sequence)
    {
        if (sequence < 0 || sequence > 255)
        {
            Console.WriteLine($"Skipping invalid sequence number: {sequence}");
            return null;
        }
        using var client = new TcpClient(Host, Port);
        using var stream = client.GetStream();

        stream.Write(new byte[] { 2, (byte)sequence });

        var buffer = new byte[PacketSize];
        int bytesRead = stream.Read(buffer, 0, PacketSize);
        return bytesRead == PacketSize ? ParsePacket(buffer) : null;
    }

    static Packet ParsePacket(byte[] buffer)
    {
        var symbol = Encoding.ASCII.GetString(buffer, 0, 4);
        var indicator = (char)buffer[4];
        int quantity = BitConverter.ToInt32(buffer.Skip(5).Take(4).Reverse().ToArray(), 0);
        int price = BitConverter.ToInt32(buffer.Skip(9).Take(4).Reverse().ToArray(), 0);
        int sequence = BitConverter.ToInt32(buffer.Skip(13).Take(4).Reverse().ToArray(), 0);

        return new Packet
        {
            Symbol = symbol,
            BuySellIndicator = indicator,
            Quantity = quantity,
            Price = price,
            PacketSequence = sequence
        };
    }
}
