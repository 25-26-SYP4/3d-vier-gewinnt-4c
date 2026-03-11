using System.Collections;
using UnityEngine;
public class USBPIOController : MonoBehaviour
{
    public const int cGroupA = 1;
    public const int cGroupB = 2;
    public const int cGroupC = 3;

    private LIBADX.LIBADX usb;

    void Start()
    {
        usb = new LIBADX.LIBADX();

        if (usb.Open("USB-Interface 1")) // ggf. anpassen
        {
            Debug.Log("USB Interface geöffnet");

            usb.DigitalDirection[cGroupA] = 0x0000; // Outputs
            usb.DigitalDirection[cGroupB] = 0x0000; // Outputs
            usb.DigitalDirection[cGroupC] = 0xffff; // Inputs
        }
        else
        {
            Debug.LogError("USB Interface konnte nicht geöffnet werden");
        }
    }

    public void SendColumn(int columnNumber)
    {
        StartCoroutine(SendPulse(columnNumber));
    }

    IEnumerator SendPulse(int columnNumber)
    {
        // Binär auf 3 Bits
        bool bit0 = (columnNumber & 1) != 0;
        bool bit1 = (columnNumber & 2) != 0;
        bool bit2 = (columnNumber & 4) != 0;

        usb.DigitalOutLine[cGroupA, 0] = bit0; // A/0
        usb.DigitalOutLine[cGroupA, 1] = bit1; // A/1
        usb.DigitalOutLine[cGroupA, 2] = bit2; // A/2

        yield return new WaitForSeconds(0.5f); // 500ms Impuls

        usb.DigitalOutLine[cGroupA, 0] = false;
        usb.DigitalOutLine[cGroupA, 1] = false;
        usb.DigitalOutLine[cGroupA, 2] = false;
    }
}
