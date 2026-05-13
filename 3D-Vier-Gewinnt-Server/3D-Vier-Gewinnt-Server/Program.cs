using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace _3D_Vier_Gewinnt_Server
{
    public class Program
    {
        public const int cGroupA = 1; // Befehlszähler
        public const int cGroupB = 2; // Ablage & Entnahme
        public const int cGroupC = 3; // Eingänge vom Fanuc

        static LIBADX.LIBADX usbInterface;

        static void Main(string[] args)
        {
            usbInterface = new LIBADX.LIBADX();

            string deviceName = "USB-PIO";

            if (!usbInterface.Open(deviceName))
            {
                Console.WriteLine("USB-PIO konnte nicht geöffnet werden!");
                return;
            }

            Console.WriteLine("USB-PIO verbunden!");

            usbInterface.DigitalOutLine[cGroupB, 6] = true;

            //Richtung setzen
            usbInterface.DigitalDirection[cGroupA] = 0x0000; // Gruppe A
            usbInterface.DigitalDirection[cGroupB] = 0x0000; // Gruppe B

            //Server starten
            StartServer();

        }
        static void StartServer()
        {
            TcpListener server = new TcpListener(IPAddress.Any, 5000);
            server.Start();

            Console.WriteLine("Server läuft auf Port 5000...");

            while (true)
            {
                Console.WriteLine("Warte auf Verbindung...");
                TcpClient client = server.AcceptTcpClient();

                Console.WriteLine("Client verbunden!");

                Thread clientThread = new Thread(() => HandleClient(client));
                clientThread.Start();
            }
        }
        static void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            while (true)
            {
                int bytesRead;

                try
                {
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;
                }
                catch
                {
                    break;
                }

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine("Empfangen: " + message);

                ProcessMessage(message);
            }

            client.Close();
            Console.WriteLine("Client getrennt");
            ResetAll();
        }
        static void ProcessMessage(string message)
        {
            try
            {
                string[] parts = message.Split(',');

                int x = int.Parse(parts[0]);
                int y = int.Parse(parts[1]);
                int player = int.Parse(parts[2]);

                Console.WriteLine($"X:{x} Y:{y} Player:{player}");

                ExecuteMove(x, y, player);
            }
            catch
            {
                Console.WriteLine("Fehler beim Parsen!");
            }
        }
        static void ExecuteMove(int x, int y, int player)
        {
            ResetAll();

            // Position berechnen (0-15)
            int position = y * 4 + x;

            Console.WriteLine($"Position: {position}");

            // 1. Stein holen
            TakePiece(player);

            Thread.Sleep(500);

            // 2. Position binär auf Gruppe B senden
            SetBinary(cGroupB, position);

            Thread.Sleep(300);

            // 3. Trigger/Befehlszähler
            Trigger();
        }
        static void Trigger()
        {
            usbInterface.DigitalOutLine[cGroupA, 2] = true;

            Thread.Sleep(200);

            usbInterface.DigitalOutLine[cGroupA, 2] = false;
        }
        static void TakePiece(int player)
        {
            if (player == 1)
            {
                usbInterface.DigitalOutLine[cGroupB, 4] = true; // EntnahmePos1
            }
            else
            {
                usbInterface.DigitalOutLine[cGroupB, 5] = true; // EntnahmePos2
            }

            Thread.Sleep(300);

            usbInterface.DigitalOutLine[cGroupB, 4] = false;
            usbInterface.DigitalOutLine[cGroupB, 5] = false;
        }
        static void SetBinary(int group, int value)
        {
            for (int i = 0; i < 4; i++)
            {
                bool bit = (value & (1 << i)) != 0;
                usbInterface.DigitalOutLine[group, i] = bit;
            }
        }
        static void ResetAll()
        {
            for (int i = 0; i < 8; i++)
            {
                usbInterface.DigitalOutLine[cGroupA, i] = false;
                usbInterface.DigitalOutLine[cGroupB, i] = false;
            }
        }
    }
}
