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
        string message = $"{x},{y},{player}";
        byte[] data = Encoding.UTF8.GetBytes(message);

        stream.Write(data, 0, data.Length);
    }
    public string Receive()
    {
        byte[] buffer = new byte[1024];

        int bytesRead = stream.Read(buffer, 0, buffer.Length);

        return Encoding.UTF8.GetString(buffer, 0, bytesRead);
    }
}
