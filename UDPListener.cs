using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Net.NetworkInformation;


namespace UnifiedHoat
{
    public struct UdpState
    {
        public UdpClient u;
        public IPEndPoint e;
    }

    public class UDPListener
    {
        public OpenFileDialogForm mainForm;
        private CancellationTokenSource cts;
        private UdpClient udpServer;
        private IPAddress ip;
        private string host;
        private int Port;
        byte[] myIP;
        private IPEndPoint remoteEndPoint;
        public Dictionary<IPEndPoint, TaskCompletionSource<byte[]>> receivedIPs; // Added dictionary to store received IPs
        private TaskCompletionSource<bool> responseReceivedTcs;

        public UDPListener()
        {
            cts = new CancellationTokenSource();
            receivedIPs = new Dictionary<IPEndPoint, TaskCompletionSource<byte[]>>();
        }

        public void closeConnection()
        {
            cts?.Cancel();
        }

        public void writeLog(string txt)
        {
            if (mainForm.InvokeRequired)
            {
                mainForm.BeginInvoke(() =>
                {
                    mainForm.writeLog(txt);
                });
            }
            else
            {
                mainForm.writeLog(txt);
            }
        }

        public void sendData(byte[] data, IPEndPoint mc = null)
        {
            IPEndPoint all = new IPEndPoint(IPAddress.Parse("192.168.1.255"), Port);

            if (mc != null)
                udpServer.Send(data, data.Length, mc);
            else if (host == "any")
                udpServer.Send(data, data.Length, all);
            else
                udpServer.Send(data, data.Length, remoteEndPoint);
        }

        public void startReceiveOperation()
        {
            UdpState s = new UdpState();
            s.e = this.remoteEndPoint;
            s.u = this.udpServer;

            this.udpServer.BeginReceive(new AsyncCallback(ReceiveCallback), s);
        }

        public void ReceiveCallback(IAsyncResult ar)
        {
            UdpClient u = ((UdpState)(ar.AsyncState)).u;
            IPEndPoint e = ((UdpState)(ar.AsyncState)).e;

            byte[] receivedData = u.EndReceive(ar, ref e);

            if (e.Address.ToString() == (new IPAddress(myIP).ToString()))
            {
                startReceiveOperation();
                return;
            }
            e.Port = Port;

            if (receivedIPs.ContainsKey(e))
            {
                if (receivedIPs[e].Task.IsCompleted)
                    receivedIPs[e] = new TaskCompletionSource<byte[]>(); // Reset the task if it is already completed

                receivedIPs[e].TrySetResult(receivedData); // Store received IP in the dictionary
            }
            else
            {
                receivedIPs[e] = new TaskCompletionSource<byte[]>();
            }

            byte[] initializationResp = { 0x00, 0x0E, 0x00, 0x10 };

            if (receivedData.SequenceEqual(initializationResp))
            {
                writeLog("Device connected: " + e.Address.ToString());
            }

            if (receivedIPs.Count == 2)
                responseReceivedTcs?.TrySetResult(true);
            
            startReceiveOperation();

            //MessageBox.Show(e.Address.ToString());
            //string receivedMessage = BitConverter.ToString(receiveBytes);
            //MessageBox.Show("RECEIVED: " + receivedMessage);
        }

        public async Task<bool> WaitTillConnected()
        {
            responseReceivedTcs = new TaskCompletionSource<bool>();
            return await responseReceivedTcs.Task;
        }

        static byte[] getIpV4Address()
        {
            IPAddress[] addresses = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
            foreach (IPAddress address in addresses)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    return address.GetAddressBytes();
                }
            }
            return null;
        }

        async void initializeMicroController()
        {

            // A command was sent to the microcontroller to initialize the connection with the PC
            // The command is 0x00 0x3f followed by the IP address of the PC and the MAC address of the PC
            // This helps the microcontroller to identify the PC and establish a connection with it
            byte[] cmd = { 0x00, 0x3f };
            byte[] mac = { 0xE4, 0xA8, 0xDF, 0xE5, 0x24, 0x02 };
            byte[] req = cmd.Concat(myIP).Concat(mac).ToArray();
            sendData(req);

            // Turn the LED on to indicate that the microcontroller has connectected successfully
            byte[] data = { 0x00, 0x10 };
            sendData(data);

            Task<bool> responseTask = WaitTillConnected();
            Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
            Task completedTask = await Task.WhenAny(responseTask, timeoutTask);

            if (completedTask != responseTask)
            {
                writeLog("Timeout: Not all devices are connected.");
            }
            else
            {
                // If both devices are connected, send a message to the microcontroller to turn the LED off
                sendData(new byte[] { 0x00, 0x11 });
                writeLog("All devices are connected.");
            }

        }

        public void write_flash_kbyte(byte[] data, byte[] index, IPEndPoint mc = null)
        {
            // Send the initialization message to the microcontroller
            byte[] cmd = { 0x00, 0x55 };
            byte[] request = cmd.Concat(index).Concat(data).ToArray();
            if (mc != null)
                sendData(request, mc);
            else
                sendData(request);
        }

        public async Task startConnection(string host, int port, bool initialize = true)
        {

            this.host = host;
            this.Port = port;
            this.myIP = getIpV4Address();

            writeLog("You IP address is: " + new IPAddress(myIP).ToString());

            // Create a UDP client (server socket) on port 65500
            using (this.udpServer = new UdpClient(port))
            {
                //MessageBox.Show("UDP Server is listening on port " + port + ".");
                //udpServer.EnableBroadcast = true;
                try
                {
                    if (host == "any")
                    {
                        this.ip = IPAddress.Any;
                        udpServer.EnableBroadcast = true;
                    }
                    else
                    {
                        this.ip = IPAddress.Parse(host);
                    }

                    // Create an endpoint to receive data from any IP address and port
                    this.remoteEndPoint = new IPEndPoint(this.ip, port);

                    startReceiveOperation();

                    if (initialize)
                        initializeMicroController();

                    // This loop is rquired to keep the server running
                    while (!cts.IsCancellationRequested)
                    { }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Exception: {ex.Message}");
                }
            } // The UdpClient will be closed here
        }
    }
}
