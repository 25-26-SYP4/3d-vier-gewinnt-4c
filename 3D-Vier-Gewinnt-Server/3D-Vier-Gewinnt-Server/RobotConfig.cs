namespace _3D_Vier_Gewinnt_Server
{
    /// <summary>
    /// Zentrale Konfiguration für alle USB-PIO Pins.
    /// Belegung laut Lehrer-Tabelle "PinBelegungIOs4Gewinnt" (03.03.26).
    ///
    /// USB-PIO = bmcm, 25-pol. D-Sub. WICHTIG: logische Linie (Port/Bit) ≠ Stecker-Pin!
    /// Gruppen im Code: GroupA=Port A, GroupB=Port B, GroupC=Port C; Pin = Bit-Nr (0-7).
    ///
    ///  Signal            Port/Bit   D-Sub-Pin   Fanuc
    ///  Befehlszähler 1   A/0        16  (*)      4
    ///  Befehlszähler 2   A/1         4  (*)      5
    ///  Befehlszähler 4   A/2        17  (*)      6
    ///  Ablage 1          B/0         5           7
    ///  Ablage 2          B/1        18           8
    ///  Ablage 4          B/2         6           9
    ///  Ablage 8          B/3        19          10
    ///  EntnahmePos1      B/4         7          11
    ///  EntnahmePos2      B/5        20          12
    ///  Versorgung (HIGH) B/6         8           -
    ///  Masse             DGND       13          18
    ///  Feedback          C/0,1,2    9,22,10      -   (Eingänge)
    ///
    /// (*) KONFLIKT: Die Tabelle nennt für den Befehlszähler die Labels A/0,A/1,A/2,
    ///     aber die D-Sub-Pins 16/4/17. Laut bmcm-Datenblatt sind 16/4/17 = A/5,A/6,A/7
    ///     (A/0,A/1,A/2 lägen auf Pin 1,14,2). Beim Lehrer klären, welche BITS gesetzt
    ///     werden sollen. Betrifft nur den Vollbetrieb (USE_COMMAND_COUNTER = true).
    /// </summary>
    public static class RobotConfig
    {
        // USB-PIO Gruppen (= Ports A/B/C)
        public const int GroupA = 1;
        public const int GroupB = 2;
        public const int GroupC = 3;

        // --- Richtung der Output-Gruppen (DigitalDirection) ---
        // Polarität ist hardware-abhängig → am Gerät verifizieren (Server mit "diag").
        //   0x0000  oder  0x00FF
        public const int OutputDirectionMask = 0x0000;

        // --- Befehlszähler (3 Bit) auf Port A ---  (nur Vollbetrieb, siehe (*) oben)
        public const int CommandCounterGroup = GroupA;
        public const int CommandCounterStartPin = 0;        // oder 5
        public const int CommandCounterBits = 3;

        // --- Ablage / Boardposition (4 Bit) auf Port B ---
        public static readonly (int group, int pin)[] AblagePins = new (int, int)[]
        {
            (GroupB, 0),  // Ablage1 (1) → D-Sub 5  → Fanuc 7
            (GroupB, 1),  // Ablage2 (2) → D-Sub 18 → Fanuc 8
            (GroupB, 2),  // Ablage4 (4) → D-Sub 6  → Fanuc 9
            (GroupB, 3),  // Ablage8 (8) → D-Sub 19 → Fanuc 10
        };

        // --- Entnahme (2 Bit) auf Port B ---
        // Annahme (wie urspr. Lehrer-Notizen): Grün/Player1 = beide AUS,
        // Blöck/Player2 = EntnahmePos1 AN. Genaue Kodierung ggf. beim Lehrer prüfen.
        public const int EntnahmeGroup = GroupB;
        public const int EntnahmePos1Pin = 4;  // D-Sub 7  → Fanuc 11
        public const int EntnahmePos2Pin = 5;  // D-Sub 20 → Fanuc 12

        // --- Versorgung Schalter: dauerhaft HIGH ---
        public const int VersorgungGroup = GroupB;
        public const int VersorgungPin = 6;    // D-Sub 8

        // --- Roboter-Feedback (3 Bit, EINGÄNGE) auf Port C ---
        // Eigene Gruppe (Port C), getrennt von den Output-Linien → kein Konflikt mehr.
        public const int FeedbackGroup = GroupC;
        public const int FeedbackStartPin = 0;
        public const int FeedbackBits = 3;
    }
}
