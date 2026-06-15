using System;
using System.Collections.Generic;

namespace _3D_Vier_Gewinnt_Server
{
    // Kapselt die digitalen Ausgänge der USB-PIO Karte (bmcm LibadX).
    //
    // WARUM diese Klasse:
    // Das einzelne Schalten von Pins über usb.DigitalOutLine[group, pin] war
    // unzuverlässig – beim Setzen mehrerer Bits einer Gruppe blieb oft nur 1 Pin
    // an (z. B. Position 15 = 1111 → nur 1 LED). Ursache: das Setzen einer Linie
    // macht intern ein Read-Modify-Write, das auf der belasteten USB-PIO-Karte die
    // bereits gesetzten Bits wieder verliert.
    //
    // LÖSUNG:
    // Wir halten pro Gruppe ein Schatten-Byte im Speicher und schreiben bei jeder
    // Änderung das KOMPLETTE Byte über usb.DigitalOut[group]. Dadurch
    //  - gehen vorher gesetzte Bits derselben Gruppe nie verloren,
    //  - werden mehrere Bits einer Gruppe in EINEM Hardware-Schreibvorgang atomar
    //    gesetzt.
    public class PioOutput
    {
        private readonly LIBADX.LIBADX usb;
        private readonly Dictionary<int, int> shadow = new Dictionary<int, int>();

        // Konfiguriert die angegebenen Gruppen als Output und setzt sie auf 0.
        public PioOutput(LIBADX.LIBADX usb, params int[] outputGroups)
        {
            this.usb = usb;

            foreach (int group in outputGroups)
            {
                // Richtungs-Polarität ist hardware-abhängig → Wert kommt aus
                // RobotConfig.OutputDirectionMask (siehe Kommentar dort).
                usb.DigitalDirection[group] = RobotConfig.OutputDirectionMask;
                shadow[group] = 0;
                usb.DigitalOut[group] = 0;
                Console.WriteLine($"Gruppe {group}: Direction gesetzt auf 0x{RobotConfig.OutputDirectionMask:X4}, " +
                                  $"gelesen 0x{usb.DigitalDirection[group]:X4}");
                LogIfError($"Init Gruppe {group}");
            }
        }

        // Setzt eine einzelne Linie und schreibt die ganze Gruppe atomar.
        public void SetLine(int group, int pin, bool on)
        {
            int value = shadow[group];
            if (on) value |= (1 << pin);
            else value &= ~(1 << pin);
            Write(group, value);
        }

        // Setzt mehrere Linien und schreibt jede betroffene Gruppe nur EINMAL,
        // d. h. alle Bits einer Gruppe wechseln im selben Hardware-Schreibvorgang.
        public void SetLines(IEnumerable<(int group, int pin, bool on)> lines)
        {
            var pending = new Dictionary<int, int>();

            foreach (var (group, pin, on) in lines)
            {
                if (!pending.TryGetValue(group, out int value))
                    value = shadow[group];

                if (on) value |= (1 << pin);
                else value &= ~(1 << pin);

                pending[group] = value;
            }

            foreach (var kv in pending)
                Write(kv.Key, kv.Value);
        }

        // Schaltet alle bekannten Output-Gruppen auf 0.
        public void ClearAll()
        {
            foreach (int group in new List<int>(shadow.Keys))
                Write(group, 0);
        }

        private void Write(int group, int value)
        {
            shadow[group] = value;
            usb.DigitalOut[group] = value;
            LogIfError($"DigitalOut[{group}]=0x{value:X2}");
        }

        private void LogIfError(string context)
        {
            int err = usb.LastError();
            if (err != 0)
                Console.WriteLine($"[LIBADX-Fehler] {context}: {err} ({usb.LastErrorString()})");
        }
    }
}
