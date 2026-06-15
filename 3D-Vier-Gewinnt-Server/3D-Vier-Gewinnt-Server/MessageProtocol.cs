using System;
using System.Net.Sockets;
using System.Text;

namespace _3D_Vier_Gewinnt_Server
{
    // Gemeinsames TCP-Protokoll zwischen Unity und Server.
    //
    // WICHTIG: TCP ist ein BYTE-STROM, keine Nachrichten-Schlange. Ohne Trennzeichen
    // können mehrere schnell gesendete Züge in EINEM Read() zusammengeklebt ankommen
    // (z. B. "1,2,1" + "0,3,2" → "1,2,10,3,2"), was beim Split(',') in viel zu viele
    // Parameter zerfällt. Deshalb wird jede Nachricht mit '\n' abgeschlossen und hier
    // sauber wieder in einzelne Zeilen zerlegt.
    //
    // Format Unity → Server:  "x,y,player\n"   (player = 1 oder 2)
    // Format Server → Unity:  "DONE\n"         (nach abgeschlossenem Roboter-Zug)
    public static class MessageProtocol
    {
        public const char Delimiter = '\n';
        public const string DoneResponse = "DONE\n";

        // Liest Zeile für Zeile vom Client, parst jeden Zug robust und ruft onMove auf.
        // Nach jedem erfolgreich verarbeiteten Zug wird "DONE\n" zurückgeschickt.
        // Kehrt zurück, wenn der Client die Verbindung schließt.
        public static void ServeMoves(TcpClient client, string tag, Action<int, int, int> onMove)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            string leftover = "";

            while (true)
            {
                int bytesRead;
                try
                {
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break; // Verbindung geschlossen
                }
                catch
                {
                    break;
                }

                leftover += Encoding.UTF8.GetString(buffer, 0, bytesRead);

                // Alle vollständigen Zeilen (bis '\n') verarbeiten, Rest aufheben.
                int nl;
                while ((nl = leftover.IndexOf(Delimiter)) >= 0)
                {
                    string line = leftover.Substring(0, nl);
                    leftover = leftover.Substring(nl + 1);

                    if (TryParseMove(line, out int x, out int y, out int player))
                    {
                        Console.WriteLine($"{tag} Empfangen: X={x} Y={y} Player={player}");
                        onMove(x, y, player);

                        byte[] response = Encoding.UTF8.GetBytes(DoneResponse);
                        stream.Write(response, 0, response.Length);
                    }
                    else if (!string.IsNullOrWhiteSpace(line))
                    {
                        // Defekte/unerwartete Nachricht NICHT an die Roboter-Pins geben.
                        Console.WriteLine($"{tag} Ungültige Nachricht ignoriert: '{line.Trim()}'");
                    }
                }
            }

            client.Close();
        }

        // Parst genau "x,y,player". Liefert false bei falschem Format.
        public static bool TryParseMove(string line, out int x, out int y, out int player)
        {
            x = y = player = 0;
            if (string.IsNullOrWhiteSpace(line)) return false;

            string[] parts = line.Trim().Split(',');
            if (parts.Length != 3) return false;

            return int.TryParse(parts[0], out x)
                && int.TryParse(parts[1], out y)
                && int.TryParse(parts[2], out player);
        }
    }
}
