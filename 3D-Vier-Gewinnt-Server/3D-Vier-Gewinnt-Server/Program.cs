using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace _3D_Vier_Gewinnt_Server
{
    public class Program
    {
        // Befehlszähler läuft 0-7 (3 Bit → DI[101, 102, 103])
        static int commandCounter = 0;

        static LIBADX.LIBADX usbInterface;

        // ── Modus wählen ──────────────────────────────────────────────────────
        // true  → MIT Befehlszähler + Handshake (Produktivbetrieb)
        // false → OHNE Befehlszähler, nur Pins setzen + Sleep (zum Testen)
        const bool USE_COMMAND_COUNTER = false;
        // ──────────────────────────────────────────────────────────────────────

        static void Main(string[] args)
        {
            usbInterface = new LIBADX.LIBADX();

            if (!usbInterface.Open("USB-PIO"))
            {
                Console.WriteLine("USB-PIO konnte nicht geöffnet werden!");
                return;
            }

            Console.WriteLine("USB-PIO verbunden!");

            // Alle genutzten Gruppen als Output konfigurieren
            usbInterface.DigitalDirection[RobotConfig.GroupA] = 0x0000;
            usbInterface.DigitalDirection[RobotConfig.GroupB] = 0x0000;

            if (USE_COMMAND_COUNTER)
            {
                Console.WriteLine("Modus: MIT Befehlszähler");
                StartServer();
            }
            else
            {
                Console.WriteLine("Modus: OHNE Befehlszähler (Test)");
                ProgramSimple.Run(usbInterface);
            }
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

                byte[] response = Encoding.UTF8.GetBytes("DONE");
                stream.Write(response, 0, response.Length);
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
            int position = y * 4 + x;
            Console.WriteLine($"Position: {position}");

            // ===== Schritt 1: Entnahme =====
            // Entnahme-Pin setzen (bleibt AN solange der Roboter arbeitet)
            SetEntnahme(player);

            // Befehlszähler erhöhen und senden → Roboter startet Befehl
            commandCounter = (commandCounter + 1) % 8;
            SendCommandCounter();
            Console.WriteLine($"[Entnahme] Befehlszähler gesendet: {commandCounter}");

            // Warten bis Roboter denselben Zählerwert zurückschickt
            WaitForRobot();

            // Entnahme-Pin löschen (Stein wurde geholt)
            ClearEntnahme();

            // ===== Schritt 2: Ablage =====
            // Position-Pins setzen (bleiben AN solange der Roboter arbeitet)
            SetPosition(position);

            // Befehlszähler erhöhen und senden → Roboter fährt zur Position
            commandCounter = (commandCounter + 1) % 8;
            SendCommandCounter();
            Console.WriteLine($"[Ablage] Befehlszähler gesendet: {commandCounter}");

            // Warten bis Roboter bestätigt
            WaitForRobot();

            // Position-Pins löschen (Stein wurde platziert)
            ClearPosition();
        }

        // Setzt den Entnahme-Pin je nach Spieler und lässt ihn AN.
        // Player 1 (Grün):  DI[111]=OFF, DI[112]=OFF → kein Pin nötig
        // Player 2 (Blöck): DI[111]=ON               → Group B Pin 2
        static void SetEntnahme(int player)
        {
            if (player == 2)
            {
                usbInterface.DigitalOutLine[RobotConfig.EntnahmeGroup, RobotConfig.EntnahmeBlockPin] = true;
                Console.WriteLine("Entnahme: Blöck (DI[111]=ON)");
            }
            else
            {
                Console.WriteLine("Entnahme: Grün (DI[111]=OFF, DI[112]=OFF)");
            }
        }

        static void ClearEntnahme()
        {
            usbInterface.DigitalOutLine[RobotConfig.EntnahmeGroup, RobotConfig.EntnahmeBlockPin] = false;
        }

        // Setzt die Board-Position als 4-Bit-Binärwert.
        // Bit 0 (A^1) → Group A Pin 6 → DI[107]
        // Bit 1 (A^2) → Group A Pin 7 → DI[108]
        // Bit 2 (A^4) → Group B Pin 0 → DI[109]
        // Bit 3 (A^8) → Group B Pin 1 → DI[110]
        static void SetPosition(int position)
        {
            for (int i = 0; i < RobotConfig.AblagePins.Length; i++)
            {
                bool bit = (position & (1 << i)) != 0;
                var (group, pin) = RobotConfig.AblagePins[i];
                usbInterface.DigitalOutLine[group, pin] = bit;
            }

            Console.WriteLine($"Position gesetzt: {position} (binär: {Convert.ToString(position, 2).PadLeft(4, '0')})");
        }

        static void ClearPosition()
        {
            foreach (var (group, pin) in RobotConfig.AblagePins)
            {
                usbInterface.DigitalOutLine[group, pin] = false;
            }
        }

        // Sendet den aktuellen Befehlszähler als 3-Bit-Wert (0-7).
        // Bit 0 → DI[101] (B^1)
        // Bit 1 → DI[102] (B^2)
        // Bit 2 → DI[103] (B^4)
        static void SendCommandCounter()
        {
            for (int i = 0; i < RobotConfig.CommandCounterBits; i++)
            {
                bool bit = (commandCounter & (1 << i)) != 0;
                usbInterface.DigitalOutLine[RobotConfig.CommandCounterGroup, RobotConfig.CommandCounterStartPin + i] = bit;
            }
        }

        // Blockiert bis der Roboter denselben Befehlszählerwert zurückschickt.
        static void WaitForRobot()
        {
            Console.WriteLine($"Warte auf Roboter-Bestätigung (Zähler={commandCounter})...");

            while (true)
            {
                int robotCounter = ReadFeedback();

                if (robotCounter == commandCounter)
                {
                    Console.WriteLine("Roboter hat bestätigt!");
                    break;
                }

                Thread.Sleep(50);
            }
        }

        // Liest den Rückgabe-Befehlszähler vom Roboter (Group C).
        static int ReadFeedback()
        {
            int value = 0;

            for (int i = 0; i < RobotConfig.FeedbackBits; i++)
            {
                bool bit = usbInterface.DigitalInLine[RobotConfig.FeedbackGroup, RobotConfig.FeedbackStartPin + i];

                if (bit)
                    value |= (1 << i);
            }

            return value;
        }

        static void ResetAll()
        {
            for (int i = 0; i < 8; i++)
            {
                usbInterface.DigitalOutLine[RobotConfig.GroupA, i] = false;
                usbInterface.DigitalOutLine[RobotConfig.GroupB, i] = false;
            }

            commandCounter = 0;
        }
    }
}
