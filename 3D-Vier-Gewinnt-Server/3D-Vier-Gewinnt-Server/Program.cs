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
        const bool USE_COMMAND_COUNTER = true;
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
            int position = x * 4 + y;
            Console.WriteLine($"Position: {position}");

            // EIN Klick = EIN Befehl = ein kompletter Spielzug (Stein nehmen UND
            // platzieren). Das ganze Muster wird EINMAL gemeinsam gesetzt:
            //   - Entnahme        → DI[111]      (welcher Stein, je nach Spieler)
            //   - Ablageposition  → DI[107..110] (4-Bit-Position)
            //   - Befehlszähler   → DI[101..103] (zuletzt = Trigger für den Roboter)
            // Erst die Datenleitungen setzen, den Befehlszähler als Letztes, damit
            // die Daten beim Trigger schon stabil anliegen.
            SetEntnahme(player);
            SetPosition(position);

            commandCounter = (commandCounter + 1) % 8;
            SendCommandCounter();
            Console.WriteLine($"[Befehl] gesendet: Zähler={commandCounter}, Position={position}, Player={player}");

            // EINMAL warten, bis der Roboter denselben Befehlszähler zurückschickt.
            // Solange blockiert das hier → ein neuer Spielzug ist erst nach der
            // Bestätigung möglich (HandleClient liest die nächste Zeile erst danach).
            if (WaitForRobot())
            {
                // Spielzug fertig → alle Daten-Pins löschen, der Zähler bleibt stehen
                // (er wechselt erst beim nächsten Befehl wieder).
                ClearEntnahme();
                ClearPosition();
            }
        }

        // Setzt den Entnahme-Pin je nach Spieler und lässt ihn AN.
        // Player 1 (Grün):  EntnahmePos1+2 AUS              → kein Pin nötig
        // Player 2 (Blöck): EntnahmePos1 AN (B/2)           → DI[111]
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

        // Setzt die Board-Position als 4-Bit-Binärwert (Ablage, siehe RobotConfig).
        // Bit 0 (1) → A/6 → DI[107]
        // Bit 1 (2) → A/7 → DI[108]
        // Bit 2 (4) → B/0 → DI[109]
        // Bit 3 (8) → B/1 → DI[110]
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
        static bool WaitForRobot()
        {
            int polls = 0;
            while (true)
            {
                int robotCounter = ReadFeedback();
                if (robotCounter == commandCounter)
                {
                    Console.WriteLine("Roboter hat bestätigt!");
                    return true;
                }
                if (polls % 60 == 0)
                    Console.WriteLine($"  ... Feedback={robotCounter}, erwartet={commandCounter}");
                polls++;
                Thread.Sleep(50);
            }
        }

        // Liest den Rückgabe-Befehlszähler vom Roboter. Welche Eingangslinie für
        // welches Bit steht, kommt aus RobotConfig.FeedbackPins (frei einstellbar
        // zum Durchprobieren). Port C bleibt standardmäßig Input (Datenblatt), wird
        // also nicht als Output konfiguriert. Nur im Vollbetrieb genutzt.
        static int ReadFeedback()
        {
            int value = 0;

            for (int i = 0; i < RobotConfig.FeedbackPins.Length; i++)
            {
                var (group, pin) = RobotConfig.FeedbackPins[i];

                if (usbInterface.DigitalInLine[group, pin])
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
