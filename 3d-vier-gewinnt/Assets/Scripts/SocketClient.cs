using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class SocketClient : MonoBehaviour
{
    TcpClient client;
    NetworkStream stream;

    public void Connect()
    {
        client = new TcpClient("127.0.0.1", 5000);
        stream = client.GetStream();
    }

    public void Send(int x, int y, int player)
    {
        // Jede Nachricht mit '\n' abschließen, damit der Server mehrere schnell
        // gesendete Züge sauber trennen kann (TCP ist ein Byte-Strom!).
        string message = $"{x},{y},{player}\n";
        byte[] data = Encoding.UTF8.GetBytes(message);

        stream.Write(data, 0, data.Length);
    }

    // Liest genau EINE Zeile (bis '\n'). So werden keine zwei Server-Antworten
    // zusammengezogen und keine halbe Antwort zurückgegeben.
    public string Receive()
    {
        StringBuilder sb = new StringBuilder();
        byte[] one = new byte[1];

        while (true)
        {
            int n = stream.Read(one, 0, 1);
            if (n == 0) break; // Verbindung geschlossen

            char c = (char)one[0];
            if (c == '\n') break;
            if (c != '\r') sb.Append(c);
        }

        return sb.ToString();
    }
}
