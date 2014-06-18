using Eneter.Messaging.EndPoints.TypedMessages;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using USBHIDDRIVER;

namespace client_v2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        IDuplexTypedMessageSender<MyResponse, MyRequest> mySender;
        public MainWindow()
        {
            InitializeComponent();
            IDuplexTypedMessagesFactory aTypedMessagesFactory = new DuplexTypedMessagesFactory();
            mySender = aTypedMessagesFactory.CreateDuplexTypedMessageSender<MyResponse, MyRequest>();
            mySender.ResponseReceived += OnResponseReceived;

            // Create messaging based on TCP.
            IMessagingSystemFactory aMessagingSystemFactory = new TcpMessagingSystemFactory();
            IDuplexOutputChannel anOutputChannel = aMessagingSystemFactory.CreateDuplexOutputChannel("tcp://192.168.2.9:8060/");

            // Attach output channel and be able to send messages and receive response messages.
            mySender.AttachDuplexOutputChannel(anOutputChannel);
            MyRequest test = new MyRequest { side = "L", strength = 10 };
            mySender.SendRequestMessage(test);
            MyRequest reset = new MyRequest { side = "L", strength = 0 };
            mySender.SendRequestMessage(reset);
            try
            {
                USBInterface usb = new USBInterface("vid_044f", "pid_b108");
                usb.Connect();
                usb.enableUsbBufferEvent(new System.EventHandler(myEventCacher));
                usb.startRead();

                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void myEventCacher(object sender, EventArgs e)
        {
            int y = 50;
            int speedRight = 50;
            int x = 0;
            Console.Out.WriteLine("Event caught");
            if (USBHIDDRIVER.USBInterface.usbBuffer.Count > 0)
            {
                byte[] currentRecord = null;
                int counter = 0;
                while ((byte[])USBHIDDRIVER.USBInterface.usbBuffer[counter] == null)
                {
                    lock (USBHIDDRIVER.USBInterface.usbBuffer.SyncRoot)
                    {
                        USBHIDDRIVER.USBInterface.usbBuffer.RemoveAt(0);
                    }
                }
                currentRecord = (byte[])USBHIDDRIVER.USBInterface.usbBuffer[0];
                lock (USBHIDDRIVER.USBInterface.usbBuffer.SyncRoot)
                {
                    USBHIDDRIVER.USBInterface.usbBuffer.RemoveAt(0);
                }
                string hex = BitConverter.ToString(currentRecord);
                Console.WriteLine(hex);
                char[] splitby = { '-' };
                string[] hexxes = hex.Split(splitby);
                for (int i = 0; i < 23; i++)
                {
                    switch (i)
                    {
                        case 4:
                            int decValue3 = Convert.ToInt32(hexxes[5], 16) * 255 + Convert.ToInt32(hexxes[4], 16);
                            int percentage4 = (int)((((100f * (decValue3)) / 1020)) - 50) * 2;
                            //Console.WriteLine(decValue3);
                            //Console.WriteLine("x: {0}", percentage4);
                            x = percentage4;
                            break;

                        case 6:
                            int decValue = Convert.ToInt32(hexxes[7], 16) * 255 + Convert.ToInt32(hexxes[6], 16);
                            int percentage = (int)(0.5f + ((100f * (1020 - decValue)) / 1020));
                            speedRight = percentage;
                            break;
                        case 8:
                            int decValue2 = Convert.ToInt32(hexxes[8], 16);
                            int percentage2 = (int)(((100f * (255 - decValue2)) / 255) - 50) * 2;
                            //Console.WriteLine(decValue2);
                            //Console.WriteLine("y: {0}", percentage2);
                            y = percentage2;
                            break;
                    }
                }
                x = 0 - x;
                double V = (100f - Math.Abs(x)) * (y / 100f) + y;
                double W = (100f - Math.Abs(y)) * (x / 100f) + x;
             
                double R = (V + W) / 2;
                double L = (V - W) / 2;
          
                R = R / 100f * 15;
                L = L / 100f * 15;
                int strength1 = (int)L;
                int strength = (int)R;
                MyRequest request = new MyRequest { side = "L", strength = strength };
                mySender.SendRequestMessage(request);
                MyRequest request2 = new MyRequest { side = "R", strength = strength1 };
                mySender.SendRequestMessage(request2);
            }
        }

        private void OnResponseReceived(object sender, TypedResponseReceivedEventArgs<MyResponse> e)
        {
            Console.WriteLine(e.ResponseMessage);
        }
    }

    public class MyRequest
    {
        public string side { get; set; }
        public int strength { get; set; }
        public bool kill { get; set; }
    }

    //Response Message Type
    public class MyResponse
    {
        public int Length { get; set; }
    }

}
