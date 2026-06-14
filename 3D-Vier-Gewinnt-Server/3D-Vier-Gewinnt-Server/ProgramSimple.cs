using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace _3D_Vier_Gewinnt_Server
{
    // Version OHNE Befehlszähler – nur zum Testen ob die richtigen Pins gesetzt werden.
    // Pins bleiben je WAIT_MS Millisekunden AN, dann wird DONE zurückgeschickt.
    public static class ProgramSimple
    {
        private const int WAIT_MS = 3000;

        private static LIBADX.LIBADX usbInterface;

        public static void Run(LIBADX.LIBADX usb)
        {
            usbInterface = usb;

            TcpListener server = new TcpListener(IPAddress.Any, 5000);
            server.Start();

            Console.WriteLine("[Simple] Server läuft auf Port 5000...");

            while (true)
            {
                Console.WriteLine("[Simple] Warte auf Verbindung...");
                TcpClient client = server.AcceptTcpClient();
                Console.WriteLine("[Simple] Client verbunden!");

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
                catch { break; }

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine("[Simple] Empfangen: " + message);

                ProcessMessage(message);

                byte[] response = Encoding.UTF8.GetBytes("DONE");
                stream.Write(response, 0, response.Length);
            }

            client.Close();
            Console.WriteLine("[Simple] Client getrennt");
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

                Console.WriteLine($"[Simple] X:{x} Y:{y} Player:{player}");
                ExecuteMove(x, y, player);
            }
            catch
            {
                Console.WriteLine("[Simple] Fehler beim Parsen!");
            }
        }

        static void ExecuteMove(int x, int y, int player)
        {
            int position = y * 4 + x;
            Console.WriteLine($"[Simple] Position: {position}");

            // ===== Schritt 1: Entnahme =====
            SetEntnahme(player);
            Console.WriteLine($"[Simple] Entnahme AN – warte {WAIT_MS}ms...");
            Thread.Sleep(WAIT_MS);
            ClearEntnahme();
            Console.WriteLine("[Simple] Entnahme AUS");

            // ===== Schritt 2: Ablage =====
            SetPosition(position);
            Console.WriteLine($"[Simple] Position AN – warte {WAIT_MS}ms...");
            Thread.Sleep(WAIT_MS);
            ClearPosition();
            Console.WriteLine("[Simple] Position AUS");
        }

        // Player 1 (Grün):  DI[111]=OFF, DI[112]=OFF → kein Pin nötig
        // Player 2 (Blöck): DI[111]=ON               → Group B Pin 2
        static void SetEntnahme(int player)
        {
            if (player == 2)
            {
                usbInterface.DigitalOutLine[RobotConfig.EntnahmeGroup, RobotConfig.EntnahmeBlockPin] = true;
                Console.WriteLine("[Simple] Entnahme: Blöck (DI[111]=ON)");
            }
            else
            {
                Console.WriteLine("[Simple] Entnahme: Grün (DI[111]=OFF, DI[112]=OFF)");
            }
        }

        static void ClearEntnahme()
        {
            usbInterface.DigitalOutLine[RobotConfig.EntnahmeGroup, RobotConfig.EntnahmeBlockPin] = false;
        }

        static void SetPosition(int position)
        {
            for (int i = 0; i < RobotConfig.AblagePins.Length; i++)
            {
                bool bit = (position & (1 << i)) != 0;
                var (group, pin) = RobotConfig.AblagePins[i];
                usbInterface.DigitalOutLine[group, pin] = bit;
            }

            Console.WriteLine($"[Simple] Position gesetzt: {position} (binär: {Convert.ToString(position, 2).PadLeft(4, '0')})");
        }

        static void ClearPosition()
        {
            foreach (var (group, pin) in RobotConfig.AblagePins)
                usbInterface.DigitalOutLine[group, pin] = false;
        }

        static void ResetAll()
        {
            for (int i = 0; i < 8; i++)
            {
                usbInterface.DigitalOutLine[RobotConfig.GroupA, i] = false;
                usbInterface.DigitalOutLine[RobotConfig.GroupB, i] = false;
            }
        }
    }
}
