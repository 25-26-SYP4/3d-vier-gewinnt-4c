using System;

namespace _3D_Vier_Gewinnt_Server
{
    // Interaktives Diagnose-Programm zum Eingrenzen des LED-Problems.
    // Start:  3D-Vier-Gewinnt-Server.exe diag
    //
    // Es probiert systematisch durch:
    //   1) welcher DigitalDirection-Wert die Linien auf OUTPUT stellt,
    //   2) ob das Schreiben des ganzen Bytes (DigitalOut) ODER einzelner
    //      Linien (DigitalOutLine) funktioniert,
    //   3) welche physische Gruppe (1/2/3) tatsächlich an den LEDs hängt.
    //
    // Du beobachtest die LEDs und drückst nach jedem Schritt Enter.
    public static class PioDiagnostic
    {
        public static void Run(LIBADX.LIBADX usb)
        {
            Console.WriteLine("\n================= USB-PIO DIAGNOSE =================");
            Console.WriteLine("Beobachte die LEDs und drücke nach jedem Schritt Enter.");
            Console.WriteLine("Notiere bei welchem Schritt ALLE 8 LEDs einer Gruppe leuchten.\n");

            // Beide gängigen Richtungs-Polaritäten testen.
            int[] directionValues = { 0x0000, 0x00FF };

            // Alle drei physischen Gruppen testen (1/2/3).
            int[] groups = { RobotConfig.GroupA, RobotConfig.GroupB, RobotConfig.GroupC };

            foreach (int dir in directionValues)
            {
                Console.WriteLine($"\n########## DIRECTION = 0x{dir:X4} ##########");

                foreach (int group in groups)
                {
                    // Richtung setzen + zurücklesen + Fehler prüfen.
                    usb.DigitalDirection[group] = dir;
                    int readBack = usb.DigitalDirection[group];
                    Console.WriteLine($"\n--- Gruppe {group}: Direction gesetzt 0x{dir:X4}, gelesen 0x{readBack:X4} ---");
                    LogIfError(usb, $"SetDirection Gruppe {group}");

                    // Methode A: ganzes Byte auf 0xFF.
                    usb.DigitalOut[group] = 0xFF;
                    LogIfError(usb, $"DigitalOut[{group}]=0xFF");
                    Pause($"Gruppe {group} | dir 0x{dir:X4} | DigitalOut=0xFF  → leuchten alle 8 LEDs? Enter...");
                    usb.DigitalOut[group] = 0x00;

                    // Methode B: jede Linie einzeln einschalten.
                    for (int pin = 0; pin < 8; pin++)
                        usb.DigitalOutLine[group, pin] = true;
                    LogIfError(usb, $"DigitalOutLine[{group},0..7]=true");
                    Pause($"Gruppe {group} | dir 0x{dir:X4} | DigitalOutLine 0..7  → leuchten alle 8 LEDs? Enter...");
                    for (int pin = 0; pin < 8; pin++)
                        usb.DigitalOutLine[group, pin] = false;
                }
            }

            Console.WriteLine("\n================= DIAGNOSE ENDE =================");
            Console.WriteLine("Welche Kombination (Direction / Methode / Gruppe) hat funktioniert?");
            Console.WriteLine("→ Direction-Wert in RobotConfig.OutputDirectionMask eintragen.");
            Console.WriteLine("Drücke Enter zum Beenden...");
            Console.ReadLine();
        }

        private static void Pause(string message)
        {
            Console.WriteLine(message);
            Console.ReadLine();
        }

        private static void LogIfError(LIBADX.LIBADX usb, string context)
        {
            int err = usb.LastError();
            if (err != 0)
                Console.WriteLine($"   [LIBADX-Fehler] {context}: {err} ({usb.LastErrorString()})");
        }
    }
}
