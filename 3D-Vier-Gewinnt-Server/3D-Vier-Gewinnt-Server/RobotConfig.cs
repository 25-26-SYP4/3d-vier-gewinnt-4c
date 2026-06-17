namespace _3D_Vier_Gewinnt_Server
{
    /// <summary>
    /// Zentrale Konfiguration für alle USB-PIO Pins.
    ///
    /// USB-PIO = bmcm, 25-pol. D-Sub. WICHTIG: logische Linie (Port/Bit) ≠ Stecker-Pin!
    /// Gruppen im Code: GroupA=Port A, GroupB=Port B, GroupC=Port C; Pin = Bit-Nr (0-7).
    ///
    /// ACHTUNG: Zwischen D-Sub und Fanuc liegt ein um +2 verschobenes Kabel (am
    /// Gerät gemessen 16.06.26: B/n → DI[109+n]). Die folgende Belegung ist bereits
    /// so gewählt, dass am Roboter die richtigen DI[] ankommen (per Whiteboard-Muster
    /// bestätigt für Ablage/Entnahme). Logische Linie → was der Roboter empfängt:
    ///
    ///  Signal            Port/Bit   → Roboter
    ///  Befehlszähler 1   A/0          DI[101]
    ///  Befehlszähler 2   A/1          DI[102]
    ///  Befehlszähler 4   A/2          DI[103]
    ///  Ablage 1          A/6          DI[107]
    ///  Ablage 2          A/7          DI[108]
    ///  Ablage 4          B/0          DI[109]
    ///  Ablage 8          B/1          DI[110]
    ///  EntnahmePos1      B/2          DI[111]
    ///  EntnahmePos2      B/3          DI[112]
    ///  Versorgung (HIGH) B/6          (D-Sub 8, dauerhaft HIGH)
    ///  Masse             DGND         (D-Sub 13)
    ///  Feedback          C/0,1,2      Rück-Befehlszähler (Eingänge)
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

        // --- Befehlszähler (3 Bit) auf Port A ---  (nur Vollbetrieb)
        // Ziel laut Lehrer-Whiteboard: B1→DI[101], B2→DI[102], B4→DI[103].
        // A/0,A/1,A/2 treffen das laut bestätigtem +2-Kabelmuster → StartPin = 0.
        // (Die Alternative A/5-7 läge auf DI[106-108] und würde mit der Ablage
        // A/6,A/7 kollidieren.) Diese Pins wurden noch nicht direkt am Roboter
        // gemessen → im Vollbetrieb am Teach-Pendant gegenprüfen.
        public const int CommandCounterGroup = GroupA;
        public const int CommandCounterStartPin = 0;
        public const int CommandCounterBits = 3;

        // --- Ablage / Boardposition (4 Bit) ---
        // Am Gerät gemessen (16.06.26): Port B kommt am Fanuc um +2 verschoben an
        // (B/n → DI[109+n] statt DI[107+n]). Da B/0 schon die unterste Linie ist,
        // sind Fanuc 7/8 (DI[107]/[108]) aus Port B NICHT erreichbar – die werden
        // über den um +2 verschobenen Kabelstrang von den Linien getrieben, die die
        // Tabelle für Fanuc 5/6 vorsieht (D-Sub 4 = A/6, D-Sub 17 = A/7).
        // Anker ist der physische D-Sub-Pin: Soll-Fanuc − 2 = der D-Sub-Pin, der
        // dieses Fanuc-DI tatsächlich treibt. So landet Bit0..3 auf DI[107..110].
        // ANNAHME: der +2-Versatz gilt auch für die Port-A-Pins (D-Sub 4/17) – am
        // Gerät nur für Port B direkt gemessen, daher mit einem Zug verifizieren.
        public static readonly (int group, int pin)[] AblagePins = new (int, int)[]
        {
            (GroupA, 6),  // Ablage1 (1) → D-Sub 4  → +2 → Fanuc 7  → DI[107]
            (GroupA, 7),  // Ablage2 (2) → D-Sub 17 → +2 → Fanuc 8  → DI[108]
            (GroupB, 0),  // Ablage4 (4) → D-Sub 5  → +2 → Fanuc 9  → DI[109]
            (GroupB, 1),  // Ablage8 (8) → D-Sub 18 → +2 → Fanuc 10 → DI[110]
        };

        // --- Entnahme (2 Bit) auf Port B ---
        // Kodierung: Grün/Player1 = beide AUS, Blöck/Player2 = EntnahmePos1 AN.
        // +2-Kabelversatz kompensiert (am Gerät gemessen, B/n → DI[109+n]):
        // Pos1 auf B/2 → Fanuc 11 → DI[111], Pos2 auf B/3 → Fanuc 12 → DI[112].
        public const int EntnahmeGroup = GroupB;
        public const int EntnahmePos1Pin = 2;  // B/2 → D-Sub 6  → +2 → Fanuc 11 → DI[111]
        public const int EntnahmePos2Pin = 3;  // B/3 → D-Sub 19 → +2 → Fanuc 12 → DI[112]

        // --- Versorgung Schalter: dauerhaft HIGH ---
        public const int VersorgungGroup = GroupB;
        public const int VersorgungPin = 6;    // D-Sub 8

        // --- Roboter-Feedback (Rück-Befehlszähler, EINGÄNGE) ---
        // Port C bleibt nach dem Einschalten Input (Datenblatt) und wird nie als
        // Output gesetzt → getrennt von den Output-Linien, kein Konflikt.
        //
        // Belegung (laut Lehrer-Whiteboard bestätigt, 17.06.26): der Roboter gibt
        // den Rück-Befehlszähler auf Port C, Linien C/0..C/2 zurück; diese
        // entsprechen DI[102..104]. LSB zuerst:
        //   FeedbackPins[0] = Bit 0 (Wert 1) = C/0 → DI[102]
        //   FeedbackPins[1] = Bit 1 (Wert 2) = C/1 → DI[103]
        //   FeedbackPins[2] = Bit 2 (Wert 4) = C/2 → DI[104]
        // Mit einem Zug am Gerät gegenprüfen (LogPortCRaw zeigt, welche C/x kippen).
        public const int FeedbackGroup = GroupC;   // nur für die Log-Ausgabe
        public static readonly (int group, int pin)[] FeedbackPins = new (int, int)[]
        {
            (GroupC, 0),  // Bit 0 (Wert 1) → DI[102]
            (GroupC, 1),  // Bit 1 (Wert 2) → DI[103]
            (GroupC, 2),  // Bit 2 (Wert 4) → DI[104]
        };
    }
}
