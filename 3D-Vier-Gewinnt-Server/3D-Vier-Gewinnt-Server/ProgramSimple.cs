using System;
using System.Collections.Generic;
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

        private static PioOutput pio;

        public static void Run(PioOutput pioOutput)
        {
            pio = pioOutput;

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
            // Liest zeilenweise (newline-getrennt) und ruft pro Zug ExecuteMove auf;
            // schickt danach automatisch "DONE\n" zurück. Siehe MessageProtocol.
            MessageProtocol.ServeMoves(client, "[Simple]", ExecuteMove);

            Console.WriteLine("[Simple] Client getrennt");
            ResetAll();
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

        // Player 1 (Grün):  EntnahmePos1+2 AUS                → kein Pin nötig
        // Player 2 (Blöck): EntnahmePos1 AN (B/4, D-Sub 7)    → Fanuc 11
        static void SetEntnahme(int player)
        {
            if (player == 2)
            {
                pio.SetLine(RobotConfig.EntnahmeGroup, RobotConfig.EntnahmePos1Pin, true);
                Console.WriteLine("[Simple] Entnahme: Blöck (EntnahmePos1 AN)");
            }
            else
            {
                Console.WriteLine("[Simple] Entnahme: Grün (EntnahmePos1+2 AUS)");
            }
        }

        static void ClearEntnahme()
        {
            pio.SetLine(RobotConfig.EntnahmeGroup, RobotConfig.EntnahmePos1Pin, false);
        }

        static void SetPosition(int position)
        {
            var lines = new List<(int, int, bool)>();
            for (int i = 0; i < RobotConfig.AblagePins.Length; i++)
            {
                bool bit = (position & (1 << i)) != 0;
                var (group, pin) = RobotConfig.AblagePins[i];
                lines.Add((group, pin, bit));
            }
            // Alle Ablage-Bits in einem Rutsch setzen (pro Gruppe ein Hardware-Write).
            pio.SetLines(lines);

            Console.WriteLine($"[Simple] Position gesetzt: {position} (binär: {Convert.ToString(position, 2).PadLeft(4, '0')})");
        }

        static void ClearPosition()
        {
            var lines = new List<(int, int, bool)>();
            foreach (var (group, pin) in RobotConfig.AblagePins)
                lines.Add((group, pin, false));
            pio.SetLines(lines);
        }

        static void ResetAll()
        {
            pio.ClearAll();
            // Versorgung Schalter muss immer HIGH bleiben.
            pio.SetLine(RobotConfig.VersorgungGroup, RobotConfig.VersorgungPin, true);
        }
    }
}
