using System;
using System.Collections.Generic;
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

        // Kapselt die digitalen Ausgänge (schreibt immer ganze Gruppen-Bytes).
        static PioOutput pio;

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

            // Diagnose-Modus:  3D-Vier-Gewinnt-Server.exe diag
            // Probiert Richtung/Schreibart/Gruppe durch, um das LED-Problem
            // eindeutig einzugrenzen (siehe PioDiagnostic.cs).
            if (args.Length > 0 && args[0].Equals("diag", StringComparison.OrdinalIgnoreCase))
            {
                PioDiagnostic.Run(usbInterface);
                return;
            }

            // Group A und B als Output konfigurieren. Ab jetzt laufen ALLE
            // Pin-Ausgaben über pio, das pro Gruppe ein Schatten-Byte hält und
            // immer das komplette Byte über DigitalOut[group] schreibt
            // (siehe PioOutput.cs – behebt das "nur 1 Pin leuchtet"-Problem).
            pio = new PioOutput(usbInterface, RobotConfig.GroupA, RobotConfig.GroupB);

            // Versorgung Schalter dauerhaft HIGH. Bleibt durch das Schatten-Byte
            // erhalten, auch wenn Ablage/Entnahme-Pins wechseln.
            pio.SetLine(RobotConfig.VersorgungGroup, RobotConfig.VersorgungPin, true);

            if (USE_COMMAND_COUNTER)
            {
                Console.WriteLine("Modus: MIT Befehlszähler");
                StartServer();
            }
            else
            {
                Console.WriteLine("Modus: OHNE Befehlszähler (Test)");
                ProgramSimple.Run(pio);
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
            // Liest zeilenweise (newline-getrennt) und ruft pro Zug ExecuteMove auf;
            // schickt danach automatisch "DONE\n" zurück. Siehe MessageProtocol.
            MessageProtocol.ServeMoves(client, "", ExecuteMove);

            Console.WriteLine("Client getrennt");
            ResetAll();
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
        // Player 1 (Grün):  EntnahmePos1+2 AUS                → kein Pin nötig
        // Player 2 (Blöck): EntnahmePos1 AN (B/4, D-Sub 7)    → Fanuc 11
        static void SetEntnahme(int player)
        {
            if (player == 2)
            {
                pio.SetLine(RobotConfig.EntnahmeGroup, RobotConfig.EntnahmePos1Pin, true);
                Console.WriteLine("Entnahme: Schwarz (EntnahmePos1 AN)");
            }
            else
            {
                Console.WriteLine("Entnahme: Weiß (EntnahmePos1+2 AUS)");
            }
        }

        static void ClearEntnahme()
        {
            pio.SetLine(RobotConfig.EntnahmeGroup, RobotConfig.EntnahmePos1Pin, false);
        }

        // Setzt die Board-Position als 4-Bit-Binärwert (Ablage, Port B).
        // Bit 0 (1) → B/0 → D-Sub 5  → Fanuc 7
        // Bit 1 (2) → B/1 → D-Sub 18 → Fanuc 8
        // Bit 2 (4) → B/2 → D-Sub 6  → Fanuc 9
        // Bit 3 (8) → B/3 → D-Sub 19 → Fanuc 10
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

            Console.WriteLine($"Position gesetzt: {position} (binär: {Convert.ToString(position, 2).PadLeft(4, '0')})");
        }

        static void ClearPosition()
        {
            var lines = new List<(int, int, bool)>();
            foreach (var (group, pin) in RobotConfig.AblagePins)
                lines.Add((group, pin, false));
            pio.SetLines(lines);
        }

        // Sendet den aktuellen Befehlszähler als 3-Bit-Wert (0-7).
        // Bit 0 → DI[101] (B^1)
        // Bit 1 → DI[102] (B^2)
        // Bit 2 → DI[103] (B^4)
        static void SendCommandCounter()
        {
            var lines = new List<(int, int, bool)>();
            for (int i = 0; i < RobotConfig.CommandCounterBits; i++)
            {
                bool bit = (commandCounter & (1 << i)) != 0;
                lines.Add((RobotConfig.CommandCounterGroup, RobotConfig.CommandCounterStartPin + i, bit));
            }
            pio.SetLines(lines);

            // Diagnose: zeigt genau, welcher Wert auf welchen Linien rausgeht.
            int last = RobotConfig.CommandCounterStartPin + RobotConfig.CommandCounterBits - 1;
            Console.WriteLine($"  → Befehlszähler {commandCounter} (binär {Convert.ToString(commandCounter, 2).PadLeft(RobotConfig.CommandCounterBits, '0')}) " +
                              $"auf Port {RobotConfig.CommandCounterGroup}, Linien {RobotConfig.CommandCounterStartPin}..{last}");
        }

        // Blockiert bis der Roboter denselben Befehlszählerwert zurückschickt.
        static void WaitForRobot()
        {
            Console.WriteLine($"Warte auf Roboter-Bestätigung (erwarte Zähler={commandCounter}, lese Port {RobotConfig.FeedbackGroup})...");

            int polls = 0;
            while (true)
            {
                int robotCounter = ReadFeedback();

                if (robotCounter == commandCounter)
                {
                    Console.WriteLine("Roboter hat bestätigt!");
                    break;
                }

                // Alle ~1 s anzeigen, was tatsächlich von Port C gelesen wird –
                // so siehst du, ob/welches Feedback ankommt (Hänger = kein Feedback).
                if (polls % 20 == 0)
                    Console.WriteLine($"  ... Feedback aktuell={robotCounter}, erwartet={commandCounter}");

                polls++;
                Thread.Sleep(50);
            }
        }

        // Liest den Rückgabe-Befehlszähler vom Roboter (Port C, Eingänge C/0..C/2).
        // Port C bleibt nach dem Einschalten standardmäßig Input (Datenblatt), wird
        // also nicht als Output konfiguriert. Nur im Vollbetrieb genutzt.
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
            pio.ClearAll();
            // Versorgung danach wieder einschalten – die muss immer HIGH bleiben.
            pio.SetLine(RobotConfig.VersorgungGroup, RobotConfig.VersorgungPin, true);
            commandCounter = 0;
        }
    }
}
