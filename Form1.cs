using System.Security;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using static System.Windows.Forms.DataFormats;
using System.Collections.Generic;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Xml.Serialization;
using System.Reflection;

namespace UnifiedHoat
{

    public partial class Form1 : Form
    {
        static Form1 instance;
        public OpenFileDialogForm mainForm;

        private Form1()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            instance = this;
            this.AcceptButton = button1;
        }

        public static Form1 getInstance()
        {
            if (instance is null || instance.IsDisposed)
            {
                instance = new Form1();
            }

            return instance;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (int.TryParse(textBox1.Text, out int port))
            {
                if (mainForm.port == -1)
                {
                    mainForm.handler = new UDPListener();
                    mainForm.handler.mainForm = this.mainForm;
                    Task.Run(() => mainForm.handler.startConnection("any", port));
                    Form1.getInstance().Close();
                } 
                else
                {
                    mainForm.handler.closeConnection();
                    mainForm.handler = new UDPListener();
                    mainForm.handler.mainForm = this.mainForm;

                    if (port == mainForm.port)
                        Task.Run(() => mainForm.handler.startConnection("any", port, false));
                    else
                        Task.Run(() => mainForm.handler.startConnection("any", port, true));


                    Form1.getInstance().Close();
                }

                mainForm.displayConnStatus("Server Status: Started at port: " + port);
            }
            else
            {
                MessageBox.Show("Enter a valid port number.");
            }
        }
    }

    public class LogForm : Form
    {
        static LogForm instance;
        private System.Windows.Forms.TextBox textBox1;
        public LogForm()
        {
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimumSize = new Size(560, 400);
            this.Text = "UnifiedHost for dummies - Log";

            // 
            // textBox1
            // 
            textBox1 = new System.Windows.Forms.TextBox
            {
                Size = new Size(MinimumSize.Width, MinimumSize.Height),
                Location = new Point(0, 0),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true
            };

            Controls.Add(textBox1);
        }

        public static LogForm getInstance()
        {
            if (instance is null || instance.IsDisposed)
            {
                instance = new LogForm();
            }

            return instance;
        }

        // Override OnFormClosing to hide the form instead of closing it
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Check if the form is being closed by the user (not programmatically)
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true; // Cancel the close event
                this.Hide(); // Hide the form instead
            }
            else
            {
                base.OnFormClosing(e); // Allow the form to close if not closed by the user
            }
        }

        public void write(string txt)
        {
            textBox1.AppendText(txt);
        }

    }

    public class OpenFileDialogForm : Form
    {
        private OpenFileDialog openFileDialog1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.Button nextButton;
        private System.Windows.Forms.Button previousButton;
        private System.Windows.Forms.Button writeFlash;
        private System.Windows.Forms.ToolStrip toolBar;
        public UDPListener handler;
        private Label connectionStatus;
        private string[] fileSections;
        private List<byte[]> dataSections = new List<byte[]>();
        private int currentSectionIndex;
        private int currentWroteSection;
        public byte[] lastResponse;
        public int port = -1;
        private LogForm logs;

        public OpenFileDialogForm()
        {

            //this.FormBorderStyle = FormBorderStyle.FixedSingle;
            //this.MaximizeBox = false;
            this.MinimumSize = new Size(560, 530);
            this.Text = "UnifiedHost for dummies";

            openFileDialog1 = new OpenFileDialog
            {
                // Set the filter to show only .hex files
                Filter = "Hex Files (*.hex)|*.hex",
                FilterIndex = 1,
                Title = "Select a Hex File"
            };

            // 
            // textBox1
            // 
            textBox1 = new System.Windows.Forms.TextBox
            {
                Size = new Size(500, 470),
                Location = new Point(15, 70),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };


            // 
            // comboBox1
            // 
            comboBox1 = new ComboBox();
            comboBox1.FormattingEnabled = true;
            comboBox1.Items.AddRange(new object[] { "1", "5", "10", "25", "50"});
            comboBox1.Location = new Point(360, 30);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(151, 28);
            comboBox1.TabIndex = 1;
            comboBox1.Text = "1";
            comboBox1.SelectedIndexChanged += new EventHandler(comboBox1_SelectedIndexChanged);

            // 
            // nextButton
            // 
            nextButton = new System.Windows.Forms.Button
            {
                Size = new Size(100, 30),
                Location = new Point(ClientSize.Width - 115, ClientSize.Height - 40),
                Text = "Next"
            };
            nextButton.Click += new EventHandler(nextButton_Click);

            // 
            // previousButton
            // 
            previousButton = new System.Windows.Forms.Button
            {
                Size = new Size(100, 30),
                Location = new Point(ClientSize.Width - 215, ClientSize.Height - 40),
                Text = "Previous"
            };
            previousButton.Click += new EventHandler(previousButton_Click);

            // 
            // writeFlash
            // 
            writeFlash = new System.Windows.Forms.Button
            {
                Size = new Size(150, 30),
                Location = new Point(10 , ClientSize.Height - 40),
                Text = "Write File"
            };
            this.writeFlash.Click += new System.EventHandler(writeFlash_Click);

            // 
            // toolBar
            // 
            toolBar = new System.Windows.Forms.ToolStrip();

            // Create a ToolStripDropDownButton
            ToolStripButton openFileButton = new ToolStripButton("Open a File");

            ToolStripDropDownButton consoleDropDown = new ToolStripDropDownButton("Console");
            consoleDropDown.DropDownItems.Add("Connect", null, startConsole);
            consoleDropDown.DropDownItems.Add("Log", null, displayLog);

            openFileButton.Click += readFile;

            // Add the ToolStripDropDownButton to the ToolStrip
            toolBar.Items.Add(openFileButton);
            toolBar.Items.Add(consoleDropDown);

            //
            // connectionStatus
            //
            this.connectionStatus = new System.Windows.Forms.Label();
            this.connectionStatus.AutoSize = true;
            this.connectionStatus.Location = new Point(10, ClientSize.Height - 60);
            this.connectionStatus.Name = "connectionStatus";
            this.connectionStatus.Size = new Size(82, 13);
            this.connectionStatus.TabIndex = 2;
            this.connectionStatus.Text = "Server Status: ";

            ClientSize = new Size(530, 560);
            Controls.Add(textBox1);
            Controls.Add(comboBox1);
            Controls.Add(nextButton);
            Controls.Add(previousButton);
            Controls.Add(writeFlash);
            Controls.Add(toolBar);
            Controls.Add(connectionStatus);


            this.Resize += new EventHandler(Form_Resize);
        }

        public void displayLog(object sender, EventArgs e)
        {
            logs = LogForm.getInstance();
            logs.Show();
            logs.BringToFront();
        }

        public void writeLog(string txt)
        {
            logs = LogForm.getInstance();
            logs.write(txt);
            logs.write(Environment.NewLine);
        }

        public void displayConnStatus(string txt)
        {
            this.connectionStatus.Text = txt;
        }

        private void startConsole(object sender, EventArgs e)
        {
            Form1 tmp = Form1.getInstance();
            tmp.mainForm = this;
            tmp.StartPosition = FormStartPosition.Manual;
            tmp.Location = new Point (this.Location.X + this.Width / 2 - tmp.Width / 2, this.Location.Y + this.Height/ 2 - tmp.Height / 2);
            tmp.ShowDialog();
            tmp.BringToFront();
        }

        private static byte[] IntToHexBytes(int num)
        {
            byte[] bytes = new byte[2];
            bytes[0] = (byte)((num >> 8) & 0xFF); // High byte
            bytes[1] = (byte)(num & 0xFF);        // Low byte
            return bytes;
        }

        private async Task<bool> finalizeBurn(int success, IPEndPoint mc)
        {
            int sum = 0;
            for (int i = 0; i < dataSections.Count; i++)
                foreach (var b in dataSections[i]) sum += b;

            byte[] sumBytes = BitConverter.GetBytes(sum);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(sumBytes);
            }

            byte[] hexBytes = BitConverter.GetBytes((ushort)success);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(hexBytes);
            }
                
            byte[] checkSumCmd = { 0x00, 0x4D };
            checkSumCmd = checkSumCmd.Concat(hexBytes).ToArray();
            handler.sendData(checkSumCmd, mc);

            sumBytes = (new byte[] { 0x00, 0x4F }).Concat(sumBytes).ToArray();
            Task<byte[]> recSum = WaitForResponseAsync(mc, sumBytes);
            Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
            Task completedTask = await Task.WhenAny(recSum, timeoutTask);

            if (completedTask == recSum)
            {
                if (recSum.Result.SequenceEqual(sumBytes))
                {
                    byte[] resetCmd = { 0x00, 0x50 };
                    handler.sendData(resetCmd, mc);
                    writeLog("File written successfully for " + mc.Address.ToString());
                    return true;
                }
                else
                {
                    writeLog("Failed to write the file for " + mc.Address.ToString() + " " + BitConverter.ToString(recSum.Result) + "::: calced " + BitConverter.ToString(sumBytes));
                    return false;
                }
            }
            else
            {
                writeLog("Timeout: Couldn't finalize the burn " + mc.Address.ToString());
                return false;
            }
        }

        private async Task<byte[]> WaitForResponseAsync(IPEndPoint endPoint, byte[] expectedResp)
        {
            // Check if a response has already been received for the specific endpoint
            if (!(handler.receivedIPs.ContainsKey(endPoint) && handler.receivedIPs[endPoint].Task.Result.SequenceEqual(expectedResp)))
            {
                if (handler.receivedIPs[endPoint].Task.IsCompleted)
                {
                    TaskCompletionSource<byte[]> responseReceivedTcs = new TaskCompletionSource<byte[]>();
                    // Add the response task to the dictionary for the specific endpoint
                    handler.receivedIPs[endPoint] = responseReceivedTcs;
                }
            }

            return await handler.receivedIPs[endPoint].Task;
        }

        private async void writeFlash_Click(object sender, EventArgs e)
        {
            if (connectionStatus.Text != "Server Status: ")
            {
                if (dataSections.Count > 0)
                {
                    foreach (var device in handler.receivedIPs)
                    {
                        currentWroteSection = 0;
                        int trials = 0;
                        int success = 0;
                        IPEndPoint deviceIP = device.Key;

                        byte[] bootCmd = { 0x00, 0x52 };
                        handler.sendData(bootCmd, deviceIP);

                        byte[] expectedRes = { 0x00, 0x48 };

                        Task<byte[]> receivedRes = WaitForResponseAsync(deviceIP, expectedRes);
                        Task timeoutT = Task.Delay(TimeSpan.FromSeconds(20));
                        Task completedT = await Task.WhenAny(receivedRes, timeoutT);

                        if (completedT == receivedRes)
                        {
                            if (!receivedRes.Result.SequenceEqual(expectedRes))
                            {
                                writeLog("Failed to boot the device " + deviceIP.Address.ToString());
                                continue;
                            }
                        }
                        else
                        {
                            writeLog("Timeout: Failed to boot the device " + deviceIP.Address.ToString());
                            continue;
                        }

                        while (currentWroteSection < dataSections.Count)
                        {
                            byte[] index = IntToHexBytes(currentWroteSection);

                            handler.write_flash_kbyte(dataSections[currentWroteSection], index, deviceIP);

                            trials++;

                            byte[] expectedResponse = { 0x00, 0x4A };
                            expectedResponse = expectedResponse.Concat(index).ToArray();

                            Task<byte[]> receivedResponse = WaitForResponseAsync(deviceIP, expectedResponse);
                            Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
                            Task completedTask = await Task.WhenAny(receivedResponse, timeoutTask);

                            if (completedTask == receivedResponse)
                            {
                                if (receivedResponse.Result.SequenceEqual(expectedResponse))
                                {
                                    currentWroteSection++;
                                    trials = 0;
                                    success++;

                                    if (currentWroteSection == dataSections.Count)
                                        await finalizeBurn(success, deviceIP);
                                }
                                else if (trials >= 4)
                                {
                                    writeLog("Failed to write to flash memory at index " + currentWroteSection);
                                    break;
                                }
                            }
                            else
                            {
                                writeLog("Timeout: Failed to write to flash memory at index " + currentWroteSection + " for device " + deviceIP.Address.ToString());
                                break; 
                            }
                        }

                        writeLog("Number of successful packets: " + success + " for " + deviceIP.Address.ToString());
                    }

                }
                else
                {
                    MessageBox.Show("Please open a file first.");
                }
            }
            else
            {
                MessageBox.Show("Please connect to the server first.");
            }
        }

        private void Form_Resize(object sender, EventArgs e)
        {
            // Adjust the size and position of the components based on the new form size
            textBox1.Size = new Size(ClientSize.Width - 30, ClientSize.Height - 140);
            comboBox1.Location = new Point(ClientSize.Width - 170, 30);
            nextButton.Location = new Point(ClientSize.Width - 115, ClientSize.Height - 40);
            previousButton.Location = new Point(ClientSize.Width - 215, ClientSize.Height - 40);
            writeFlash.Location = new Point(10, ClientSize.Height - 40);
            connectionStatus.Location = new Point(10, ClientSize.Height - 60);
        }

        // This function parses data of a line of a HEX file (unicode)
        private byte[] ParseRecord(string record,ref int lastAdress, ref int currUpperAddress, ref int lastUpperAddress)
        {
            if (record[0] != ':')
                throw new ArgumentException("Invalid record start character");

            int byteCount = Convert.ToInt32(record.Substring(1, 2), 16);
            int address = Convert.ToInt32(record.Substring(3, 4), 16);
            int recordType = Convert.ToInt32(record.Substring(7, 2), 16);
            byte[] empty = { };

            byte[] data = new byte[byteCount];
            for (int i = 0; i < byteCount; i++)
            {
                data[i] = Convert.ToByte(record.Substring(9 + i * 2, 2), 16);
            }

            int checksum = Convert.ToInt32(record.Substring(9 + byteCount * 2, 2), 16);

            // Validate checksum
            int sum = byteCount + (address >> 8) + (address & 0xFF) + recordType;
            foreach (var b in data) sum += b;
            sum = (sum & 0xFF);
            sum = (0x100 - sum) & 0xFF;

            if (sum != checksum)
                throw new Exception("Checksum does not match");

            if (recordType == 4)
            {
                currUpperAddress = Convert.ToInt32(record.Substring(9, 4), 16);
                return empty;
            }

            if (recordType != 0)
                return empty;

            if (lastAdress != -1)
            {
                // Fill gaps with FFs
                uint fullAddress = ((uint)currUpperAddress << 16) | (uint)address;
                uint lastFullAddress = ((uint)lastUpperAddress << 16) | (uint)lastAdress;

                long gapSize = fullAddress - lastFullAddress;
                if (gapSize > 0)
                {
                    byte[] gapData = new byte[gapSize];
                    for (int i = 0; i < gapSize; i++)
                    {
                        gapData[i] = 0xFF;
                    }
                    data = gapData.Concat(data).ToArray();
                }
            }

            lastAdress = address + byteCount;
            lastUpperAddress = currUpperAddress;
            return data;
        }

        private void InitializeFileSections()
        {
            if (openFileDialog1.FileName != "")
            {
                string fileContent = File.ReadAllText(openFileDialog1.FileName);
                int numLines = GetNumLinesFromComboBox();

                string[] lines = fileContent.Split(Environment.NewLine).Where(str => !string.IsNullOrEmpty(str)).ToArray();
                List<byte> data = new List<byte>();
                int lastAddress = -1;
                int lastUpperAddress = 0x0000;
                int upperAddress = 0x0000;

                dataSections.Clear();

                for (int i = 0; i < lines.Length; i++)
                {
                    byte[] tmp = ParseRecord(lines[i],ref lastAddress, ref upperAddress, ref lastUpperAddress);

                    data.AddRange(tmp);

                    while (data.Count >= 1024)
                    {
                        dataSections.Add(data.GetRange(0, 1024).ToArray());
                        data.RemoveRange(0, 1024);
                    }
                }

                if (data.Count > 0)
                {
                    while (data.Count < 1024)
                    {
                        data.Add(0xFF);
                    }
                    dataSections.Add(data.ToArray());
                }

                if (numLines == -1)
                {
                    string tmp = "";
                    foreach (var section in dataSections)
                    {
                        tmp += BitConverter.ToString(section);
                    }
                    fileSections = new string[] { tmp };
                }
                else
                {
                    int numSections = (int)Math.Ceiling((double)dataSections.Count / numLines);
                    fileSections = new string[numSections];
                    for (int i = 0; i < numSections; i++)
                    {
                        int startIndex = i * numLines;   // Index to start from
                        int count = Math.Min(dataSections.Count - startIndex, numLines);

                        // Get the range of byte arrays from the list
                        List<byte[]> range = dataSections.GetRange(startIndex, count);

                        fileSections[i] = "";
                        foreach (var byteArray in range)
                        {
                            fileSections[i] += BitConverter.ToString(byteArray) + "-";
                        }
                    }
                }

                currentSectionIndex = 0;
            }
        }

        private int GetNumLinesFromComboBox()
        {
            if (comboBox1.Text == "All")
            {
                return -1;
            }
            else if (int.TryParse(comboBox1.Text, out int numLines))
            {
                return numLines;
            }
            else
            {
                MessageBox.Show("Invalid number of lines selected.");
                return -1;
            }
        }

        private void SetText(string text)
        {
            textBox1.Text = text;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (fileSections != null && fileSections.Length > 0)
            {
                currentSectionIndex = 0;
                InitializeFileSections();
                DisplayCurrentSection();
            }
        }

        private void DisplayCurrentSection()
        {
            SetText(fileSections[currentSectionIndex]);
        }

        private void readFile(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    InitializeFileSections();
                    DisplayCurrentSection();
                }
                catch (SecurityException ex)
                {
                    MessageBox.Show($"Security error.\n\nError message: {ex.Message}\n\n" +
                    $"Details:\n\n{ex.StackTrace}");
                }
            }
        }

        private void nextButton_Click(object sender, EventArgs e)
        {
            if (fileSections != null && fileSections.Length > 0)
            {
                if (currentSectionIndex + 1 < fileSections.Length)
                {
                    currentSectionIndex = (currentSectionIndex + 1);
                    DisplayCurrentSection();
                }
            }
        }

        private void previousButton_Click(object sender, EventArgs e)
        {
            if (fileSections != null && fileSections.Length > 0)
            {
                if (currentSectionIndex - 1 >= 0)
                {
                    currentSectionIndex = (currentSectionIndex - 1);
                    DisplayCurrentSection();
                }
            }
        }
    }

}
