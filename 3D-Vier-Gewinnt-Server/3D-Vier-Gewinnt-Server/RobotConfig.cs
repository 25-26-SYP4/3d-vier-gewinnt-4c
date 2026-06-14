namespace _3D_Vier_Gewinnt_Server
{
    /// <summary>
    /// Zentrale Konfiguration für alle USB-PIO Pins.
    /// Zum Anpassen reicht es, hier die Werte zu ändern.
    ///
    /// USB-PIO Gruppen → Fanuc DI-Pins:
    ///   Group A (=1): Pins 0-7 → DI[101] - DI[108]
    ///   Group B (=2): Pins 0-7 → DI[109] - DI[116]
    ///   Group C (=3): Pins 0-7 → DI[117] - DI[124]
    /// </summary>
    public static class RobotConfig
    {
        // USB-PIO Gruppen
        public const int GroupA = 1;
        public const int GroupB = 2;
        public const int GroupC = 3;

        // --- Befehlszähler ---
        // Bits 0-2 auf Group A → DI[101]=B^1, DI[102]=B^2, DI[103]=B^4
        public const int CommandCounterGroup = GroupA;
        public const int CommandCounterStartPin = 0;
        public const int CommandCounterBits = 3;

        // --- Entnahme (Stein holen) ---
        // Grün  (Player 1): DI[111]=OFF, DI[112]=OFF  → kein Bit setzen
        // Blöck (Player 2): DI[111]=ON,  DI[112]=OFF  → Group B Pin 2 setzen
        public const int EntnahmeGroup = GroupB;
        public const int EntnahmeGreenPin = -1;  // beide aus → kein Pin
        public const int EntnahmeBlockPin = 2;   // DI[111] = Group B Pin 2

        // --- Ablage (Boardposition) ---
        // Bit 0 (A^1) → Group A Pin 6 → DI[107]
        // Bit 1 (A^2) → Group A Pin 7 → DI[108]
        // Bit 2 (A^4) → Group B Pin 0 → DI[109]
        // Bit 3 (A^8) → Group B Pin 1 → DI[110]
        public static readonly (int group, int pin)[] AblagePins = new (int, int)[]
        {
            (GroupA, 6),  // Bit 0 → DI[107]
            (GroupA, 7),  // Bit 1 → DI[108]
            (GroupB, 0),  // Bit 2 → DI[109]
            (GroupB, 1),  // Bit 3 → DI[110]
        };

        // --- Roboter-Feedback (Befehlszähler-Echo) ---
        // Der Roboter schickt den bestätigten Zählerwert auf denselben Pins zurück
        // wie wir ihn senden → DI[101]=B^1, DI[102]=B^2, DI[103]=B^4
        public const int FeedbackGroup = CommandCounterGroup;    // Group A
        public const int FeedbackStartPin = CommandCounterStartPin; // Pin 0
        public const int FeedbackBits = CommandCounterBits;         // 3 Bit (0-7)
    }
}
