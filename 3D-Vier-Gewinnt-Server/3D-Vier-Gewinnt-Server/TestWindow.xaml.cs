using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace _3D_Vier_Gewinnt_Server
{
    public partial class TestWindow : Window
    {
        private readonly PioOutput pio;
        private readonly LIBADX.LIBADX usb;
        private readonly DispatcherTimer feedbackTimer;

        public TestWindow(PioOutput pio, LIBADX.LIBADX usb)
        {
            InitializeComponent();
            this.pio = pio;
            this.usb = usb;

            feedbackTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            feedbackTimer.Tick += (s, e) => UpdateFeedback();
            feedbackTimer.Start();
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            int bz  = Bit(CbBz0, 0) | Bit(CbBz1, 1) | Bit(CbBz2, 2);
            bool ent = CbEnt.IsChecked == true;
            int abl = Bit(CbAbl0, 0) | Bit(CbAbl1, 1) | Bit(CbAbl2, 2) | Bit(CbAbl3, 3);

            var lines = new List<(int, int, bool)>();

            // Befehlszähler (3 Bit)
            for (int i = 0; i < RobotConfig.CommandCounterBits; i++)
                lines.Add((RobotConfig.CommandCounterGroup,
                            RobotConfig.CommandCounterStartPin + i,
                            (bz & (1 << i)) != 0));

            // Entnahme
            lines.Add((RobotConfig.EntnahmeGroup, RobotConfig.EntnahmePos1Pin, ent));

            // Ablage (4 Bit)
            for (int i = 0; i < RobotConfig.AblagePins.Length; i++)
            {
                var (group, pin) = RobotConfig.AblagePins[i];
                lines.Add((group, pin, (abl & (1 << i)) != 0));
            }

            pio.SetLines(lines);

            TxStatus.Text = $"BZ={bz} ({Convert.ToString(bz, 2).PadLeft(3, '0')}b)   " +
                            $"Entnahme={ent}   " +
                            $"Ablage={abl} ({Convert.ToString(abl, 2).PadLeft(4, '0')}b)";
        }

        private void Abl_Changed(object sender, RoutedEventArgs e)
        {
            int abl = Bit(CbAbl0, 0) | Bit(CbAbl1, 1) | Bit(CbAbl2, 2) | Bit(CbAbl3, 3);
            TxAblageWert.Text = $"Position: {abl}  ({Convert.ToString(abl, 2).PadLeft(4, '0')}b)";
        }

        private void UpdateFeedback()
        {
            int fb = 0;
            for (int i = 0; i < RobotConfig.FeedbackPins.Length; i++)
            {
                var (group, pin) = RobotConfig.FeedbackPins[i];
                if (usb.DigitalInLine[group, pin])
                    fb |= (1 << i);
            }
            TxFeedback.Text = $"Wert = {fb}  ({Convert.ToString(fb, 2).PadLeft(3, '0')}b)" +
                              $"   Bit0={fb & 1}  Bit1={(fb >> 1) & 1}  Bit2={(fb >> 2) & 1}";
        }

        private static int Bit(CheckBox cb, int position)
            => cb.IsChecked == true ? (1 << position) : 0;
    }
}
