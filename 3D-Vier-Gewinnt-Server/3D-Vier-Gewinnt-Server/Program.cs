using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3D_Vier_Gewinnt_Server
{
    public class Program
    {
        public static LIBADX.LIBADX usb = new LIBADX.LIBADX();
        static void Main(string[] args)
        {
            if (usb.Open("USB-PIO"))
            {
                Console.WriteLine("USB-Interface opened succesfully");
            }
        }
    }
}
