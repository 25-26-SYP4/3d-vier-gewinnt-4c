using UnityEngine;
public class USBPIOController : MonoBehaviour
{
    private LIBADX.LIBADX usb;

    void Start()
    {
        usb = new LIBADX.LIBADX();
    }
}
