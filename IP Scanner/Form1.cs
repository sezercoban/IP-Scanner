using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IP_Scanner
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            Control.CheckForIllegalCrossThreadCalls = false;
        }

        Thread ipSearchThread;

        int ipStart;
        int ipEnd;
        string ipSubnet;


        private void Form1_Load(object sender, EventArgs e)
        {
            btnStop.Enabled = false;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {

            //durdur
            btnTara.Enabled = true;
            btnStop.Enabled = false;
            txtRange.Enabled = true;

            ipSearchThread.Suspend();

            lblStatus.Text = "Tarama Durduruldu.";
        }

        private void btnTara_Click(object sender, EventArgs e)
        {

            if (parseRange() == false)
            {
                MessageBox.Show("Geçerli bir IP ya da IP aralığı girmediniz.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            btnTara.Enabled = false;
            btnStop.Enabled = true;
            txtRange.Enabled = false;


            startScan();

        }

        /// <summary>
        /// Girilen xxx.xxx.xxx.xxx-yyy aralığını parçalar, başlangıç,bitiş sayılarını oluşturur.
        /// </summary>
        /// <returns>Parçalama başarılı ise true.</returns>
        private bool parseRange()
        {
            string range = txtRange.Text;
            if (string.IsNullOrEmpty(range))
            {
                return false;
            }

            if (range.IndexOf('-') > 0)
            {
                try
                {
                    string[] parse = range.Split('-');
                    this.ipEnd = int.Parse(parse[1].Trim());

                    string[] ipParse = parse[0].Split('.');
                    this.ipStart = int.Parse(ipParse[3]);

                    this.ipSubnet = ipParse[0] + "." + ipParse[1] + "." + ipParse[2];

                }
                catch (Exception)
                {
                    return false;
                }

            }
            else
            {

                string[] parse = range.Split('.');
                this.ipStart = int.Parse(parse[3]);
                this.ipEnd = int.Parse(parse[3]);

                this.ipSubnet = parse[0] + "." + parse[1] + "." + parse[2];
            }

            return true;

        }


        /// <summary>
        /// İp taramayı başlatır.
        /// </summary>
        private void startScan()
        {

            ipSearchThread = new Thread(() => ipScanProcess(this.ipStart, this.ipEnd, this.ipSubnet));
            ipSearchThread.Start();

            if (ipSearchThread.IsAlive == true)
            {
                btnTara.Enabled = false;
                btnStop.Enabled = true;
                txtRange.Enabled = false;
            }

        }


        /// <summary>
        /// İP tarama  yapan process.
        /// </summary>
        /// <param name="ipRangeStart">Başlangıç ip</param>
        /// <param name="ipRangeEnd">Bitiş ip</param>
        /// <param name="ipSubnet">İp ilk 3 modülü</param>
        private void ipScanProcess(int ipRangeStart, int ipRangeEnd, string ipSubnet)
        {
            Ping myPing;
            PingReply reply;
            IPAddress addr;
            IPHostEntry host;

            prgWorkStatus.Maximum = ipRangeEnd - ipRangeStart;
            prgWorkStatus.Value = 0;
            dgvList.Rows.Clear();


            for (int i = ipRangeStart; i < ipRangeEnd; i++)
            {
                string subnetn = "." + i.ToString();
                myPing = new Ping();
                reply = myPing.Send(ipSubnet + subnetn, 900);


                lblStatus.Text = "Taranıyor : " + ipSubnet + subnetn;

                if (reply.Status == IPStatus.Success)
                {
                    try
                    {
                        addr = IPAddress.Parse(ipSubnet + subnetn);
                        host = Dns.GetHostEntry(addr);


                        string macAdress = getMacByIp(ipSubnet + subnetn, host.HostName);


                        Image image = imgList.Images[0];
                        dgvList.Rows.Add(image, ipSubnet + subnetn, host.HostName, macAdress);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Image image = imgList.Images[1];
                        dgvList.Rows.Add(image, ipSubnet + subnetn, "", "", "");
                    }
                }
                prgWorkStatus.Value += 1;
            }
            btnTara.Enabled = true;
            btnStop.Enabled = false;
            txtRange.Enabled = true;
            lblStatus.Text = dgvList.Rows.Count + " sonuç ile tarama tamamlandı!";


        }




        /// <summary>
        /// IP'nin mac adresi sorgulamasını yapar 
        /// </summary>
        /// <param name="ip">Sorgulanacak IP adresi</param>
        /// <param name="hostname">Sorgulanacak Hostname</param>
        /// <returns></returns>
        public string getMacByIp(string ip, string hostname)
        {
            if (Dns.GetHostName() == hostname)
            {
                string tempMac =
                     (
                         from nic in NetworkInterface.GetAllNetworkInterfaces()
                         where nic.OperationalStatus == OperationalStatus.Up
                         select nic.GetPhysicalAddress().ToString()
                     ).FirstOrDefault();
                return Regex.Replace(tempMac, ".{2}", "$0-").TrimEnd('-');
            }
            var macIpPairs = GetAllMacAddressesAndIppairs();
            int index = macIpPairs.FindIndex(x => x.IpAddress == ip);
            if (index >= 0)
            {
                return macIpPairs[index].MacAddress.ToUpper();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<MacIpPair> GetAllMacAddressesAndIppairs()
        {
            List<MacIpPair> mip = new List<MacIpPair>();
            System.Diagnostics.Process pProcess = new System.Diagnostics.Process();
            pProcess.StartInfo.FileName = "arp";
            pProcess.StartInfo.Arguments = "-a ";
            pProcess.StartInfo.UseShellExecute = false;
            pProcess.StartInfo.RedirectStandardOutput = true;
            pProcess.StartInfo.CreateNoWindow = true;
            pProcess.Start();
            string cmdOutput = pProcess.StandardOutput.ReadToEnd();
            string pattern = @"(?<ip>([0-9]{1,3}\.?){4})\s*(?<mac>([a-f0-9]{2}-?){6})";

            foreach (Match m in Regex.Matches(cmdOutput, pattern, RegexOptions.IgnoreCase))
            {
                mip.Add(new MacIpPair()
                {
                    MacAddress = m.Groups["mac"].Value,
                    IpAddress = m.Groups["ip"].Value
                });
            }

            return mip;
        }
        public struct MacIpPair
        {
            public string MacAddress;
            public string IpAddress;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            ipSearchThread.Suspend();
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {

            foreach (DataGridViewRow row in dgvList.Rows)
            {
                if (row.Cells[2].ToString().Trim().Contains(txtSearch.Text.Trim()))
                {
                    row.Visible = true;
                }
                else
                    row.Visible = false;
            }
        }

        private void txtSearch_KeyUp(object sender, KeyEventArgs e)
        {

            foreach (DataGridViewRow row in dgvList.Rows)
            {
                if (row.Cells[2].Value.ToString().Trim().Contains(txtSearch.Text.Trim()))
                {
                    row.Visible = true;
                }
                else
                    row.Visible = false;
            }
        }
    }
}
