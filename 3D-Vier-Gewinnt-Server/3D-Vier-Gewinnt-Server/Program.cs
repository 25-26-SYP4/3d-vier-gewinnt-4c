using System;

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

            // 1. Verbindung öffnen (Hier den Namen deines USB-PIO eintragen, z.B. "USB-PIO_1")
            // In einer Konsolen-App musst du den Namen kennen oder vorher abfragen
            string deviceName = "USB-PIO";

            if (usbInterface.Open(deviceName))
            {
                // Port-Richtung setzen (0 = Output)
                usbInterface.DigitalDirection[1] = 0x0000;

                Console.WriteLine("Testlauf: LED an Pin 16 sollte jetzt blinken...");

                while (true)
                {
                    // Schalte Leitung 0 in Gruppe 1 (Pin 16) an
                    usbInterface.DigitalOutLine[1,0] = true;
                    System.Threading.Thread.Sleep(500);

                    // Und wieder aus
                    usbInterface.DigitalOutLine[1, 0] = false;
                    System.Threading.Thread.Sleep(500);
                }
            } 
            else
            {
                Console.WriteLine("Fehler: Interface konnte nicht geöffnet werden.");
            }

            Console.WriteLine("Programm beendet. Taste drücken...");
            Console.ReadKey();
        }

        static void RunSimulation()
        {
            bool exit = false;
            while (!exit)
            {
                Console.Clear();
                Console.WriteLine("--- Roboter Simulation (Steckbrett) ---");
                Console.WriteLine("1: Befehlszähler 1 setzen (A/0)");
                Console.WriteLine("2: Ablage 1 aktivieren (B/0)");
                Console.WriteLine("3: Ablage 2 aktivieren (B/1)");
                Console.WriteLine("4: Entnahme Pos 1 (B/4)");
                Console.WriteLine("0: Alles AUS & Beenden");

                var key = Console.ReadKey().Key;

                switch (key)
                {
                    case ConsoleKey.D1:
                        SetSignal(cGroupA, 0, "Befehlszähler 1");
                        break;
                    case ConsoleKey.D2:
                        SetSignal(cGroupB, 0, "Ablage 1");
                        break;
                    case ConsoleKey.D3:
                        SetSignal(cGroupB, 1, "Ablage 2");
                        break;
                    case ConsoleKey.D4:
                        SetSignal(cGroupB, 4, "Entnahme Pos 1");
                        break;
                    case ConsoleKey.D0:
                        ResetAll();
                        exit = true;
                        break;
                }
            }
        }

        // Hilfsmethode zum Toggeln einer LED
        static void SetSignal(int group, int line, string label)
        {
            // Aktuellen Status lesen und umkehren
            bool currentState = usbInterface.DigitalOutLine[group, line];
            usbInterface.DigitalOutLine[group, line] = !currentState;

            Console.WriteLine($"\n{label} ist nun: " + (!currentState ? "AN" : "AUS"));
            System.Threading.Thread.Sleep(500); // Kleiner Delay für die Anzeige
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
