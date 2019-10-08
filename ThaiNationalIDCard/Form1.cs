using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Windows.Media.Imaging;
using System.Net;
using System.Diagnostics;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Threading;
using PCSC;
using System.Globalization;
using System.ServiceModel.Web;
using System.ServiceModel;

namespace ThaiNationalIDCard
{
    public partial class Form1 : Form
    {
        #region constant

        private const string STATUS_READING = " กำลังอ่านข้อมูล";
        private const string STATUS_READ_FAIL = " อ่านข้อมูลไม่สำเร็จ";
        private const string STATUS_NO_CARD_FOUND = " ไม่พบบัตร";
        private const string STATUS_NO_READER_CONNECTED = " ไม่พบเครื่องอ่าน";
        private const string STATUS_READ_SUCCESS = " อ่านข้อมูลสำเร็จ";
        private const string STATUS_READER_CONNECTED = " ตรวจพบอุปกรณ์ {0}";

        private const string SEX_MALE = "ชาย";
        private const string SEX_FEMALE = "หญิง";

        #endregion  

        #region members

        public static string id, THname, THlastname, ENname, ENlastname, THprefix, ENprefix, birthday,
            sex, issue, expire, address, SigBase64, SendBase64;
        byte[] byteImage;
        Image showPhoto;
        protected StatusBar mainStatusBar = new StatusBar();
        protected StatusBarPanel statusPanel = new StatusBarPanel();
        protected StatusBarPanel datetimePanel = new StatusBarPanel();

        private bool _isStartedMonitor;
        private ThaiIDCard _thaiIdCard;
        WebServiceHost json = new WebServiceHost(typeof(jsonService), new Uri("http://localhost:5555"));
        
        
        #endregion

        #region constructor
        public Form1()
        {
            InitializeComponent();
            CreateStatusBar();

            //Reset progress bar
            OnPhotoProgress(0, 100);

            InitializeProperties();
            RegisterReaderMonitor();
        }
        #endregion

        #region events

        public void OnPhotoProgress(int value, int maximum)
        {
            int percent = (int)(((double)(progressBar1.Value - progressBar1.Minimum) /
            (double)(progressBar1.Maximum - progressBar1.Minimum)) * 100);
            //using (Graphics gr = progressBar1.CreateGraphics())
            //{
            //    gr.DrawString(percent.ToString() + "%",
            //        SystemFonts.DefaultFont,
            //        Brushes.Black,
            //        new PointF(progressBar1.Width / 2 - (gr.MeasureString(percent.ToString() + "%",
            //            SystemFonts.DefaultFont).Width / 2.0F),
            //        progressBar1.Height / 2 - (gr.MeasureString(percent.ToString() + "%",
            //            SystemFonts.DefaultFont).Height / 2.0F)));
            //}
            //progressBar1.CreateGraphics().DrawString(percent.ToString() + "%", new Font("Arial", (float)8.25, FontStyle.Regular), Brushes.Black, new PointF(progressBar1.Width / 2 - 10, progressBar1.Height / 2 - 7));
            if (txtAddr.InvokeRequired)
            {
                if (progressBar1.Maximum != maximum)
                    progressBar1.BeginInvoke(new MethodInvoker(delegate { progressBar1.Maximum = maximum; }));

                // fix progress bar sync.
                if (progressBar1.Maximum > value)
                    progressBar1.BeginInvoke(new MethodInvoker(delegate { progressBar1.Value = value + 1; }));

                progressBar1.BeginInvoke(new MethodInvoker(delegate {
                    progressBar1.Value = value;
                    statusPanel.Text = STATUS_READING + " " + percent + "%";
                    //pg.Show();
                    //pg.Message = "กำลังอ่านข้อมูล, กรุณารอสักครู่.... " + percent + "%";
                    }));
            }
            else
            {
                if (progressBar1.Maximum != maximum)
                    progressBar1.Maximum = maximum;

                // fix progress bar sync.
                if (progressBar1.Maximum > value)
                    progressBar1.Value = value + 1;

                progressBar1.Value = value;
            }

        }

        public void OnReaderDisconnected()
        {
            _isStartedMonitor = false;

            BeginInvoke(new MethodInvoker(delegate {
                progressBar1.Visible = false;
                //listReader.Items.Clear();
                //listReader.ResetText();
                //listReader.Enabled = false;
                //listReader.AllowDrop = false;
                //btnRead.Enabled = false;
                statusPanel.Text = STATUS_NO_READER_CONNECTED;
            }));
            
        }

        public void OnCompleteReadCard(Personal personal)
        {
            CultureInfo ThaiCulture = new CultureInfo("th-TH");
            CultureInfo UsaCulture = new CultureInfo("en-US");
            Application.UseWaitCursor = false;
            if (personal == null)
                return;

            id = personal.Citizenid;
            birthday = personal.Birthday.ToString("dd/MM/yyyy", ThaiCulture);
            if (personal.Sex == "1") { sex = SEX_MALE; }
            else { sex = SEX_FEMALE; }
            THprefix = personal.Th_Prefix;
            THname = personal.Th_Firstname;
            THlastname = personal.Th_Lastname;
            ENprefix = personal.En_Prefix;
            ENname = personal.En_Firstname;
            ENlastname = personal.En_Lastname;
            issue = personal.Issue.ToString("dd/MM/yyyy", ThaiCulture);
            expire = personal.Expire.ToString("dd/MM/yyyy", ThaiCulture);
            address = personal.addrHouseNo + " " +
                      personal.addrVillageNo + " " +
                      personal.addrVillage + " " +
                      personal.addrLane + " " +
                      personal.addrRoad + " " +
                      personal.addrTambol + " " +
                      personal.addrAmphur + " " +
                      personal.addrProvince;
            byteImage = personal.PhotoRaw;
            showPhoto = personal.PhotoBitmap;
            BeginInvoke(new MethodInvoker(delegate { UpdateUIPersonal(); btnClear.Enabled = true;}));
        }

        public void OnBeginReadCard()
        {
            
            BeginInvoke(new MethodInvoker(delegate {
                OnPhotoProgress(0, 100);
                progressBar1.Visible = true;
                statusPanel.Text = STATUS_READING;
                Application.UseWaitCursor = true;
            }));
        }

        public void OnErrorReadCard(string message)
        {
            BeginInvoke(new MethodInvoker(delegate
            {
                statusPanel.Text = STATUS_READ_FAIL;
                MessageBox.Show("ไม่สามารถอ่านข้อมูลจากบัตรได้, กรุณาเสียบบัตร", "พบข้อผิดพลาดในการอ่านข้อมูล", MessageBoxButtons.OK, MessageBoxIcon.Error);
               // btnRead.Enabled = true;
                btnRefresh.Enabled = true;
                progressBar1.Visible = false;
            }));
        }
        private void btnConfig_Click(object sender, EventArgs e)
        {
            AllConfig ac = new AllConfig();
            ac.ShowDialog();
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtID.Text = "";
            txtName.Text = "";
            txtNameEN.Text = "";
            txtAddr.Text = "";
            txtBirthday.Text = "";
            txtSex.Text = "";
            txtIssue.Text = "";
            txtExpire.Text = "";
            pictureBox1.Image = null;
            btnClear.Enabled = false;
        }

        public void OnRemoveCard()
        {            
            BeginInvoke(new MethodInvoker(delegate {
                statusPanel.Text = STATUS_NO_CARD_FOUND;
               // btnRead.Enabled = false;
            }));
        }
        #endregion

        #region private methods
        
        private void InitializeProperties()
        {
            _thaiIdCard = new ThaiIDCard();
            _thaiIdCard.eventCardInserted += new handleCardInserted(OnCompleteReadCard);
            _thaiIdCard.eventPhotoProgress += new handlePhotoProgress(OnPhotoProgress);
            _thaiIdCard.eventBeforeCardInserted += new handleBeforeCardInserted(OnBeginReadCard);
            _thaiIdCard.eventCardReadError += new handleCardReadError(OnErrorReadCard);
            _thaiIdCard.eventCardRemoved += new handleCardRemoved(OnRemoveCard);
            _thaiIdCard.eventReaderDisconnected += new handleReaderDisconnected(OnReaderDisconnected);
            btnClear.Enabled = false;           
        }

        // Override to catch DeviceChange signal, for Registering reader monitor
        protected override void WndProc(ref Message m)
        {
            var WM_DEVICECHANGE = 0x219;

            base.WndProc(ref m);

            if (m.Msg == WM_DEVICECHANGE)
            {
                RegisterReaderMonitor();
            }

        }

        private void RegisterReaderMonitor()
        {
            try
            {
                var readers = _thaiIdCard.GetReaders();
                if (readers != null && !_isStartedMonitor)
                {
                    statusPanel.Text = string.Format(STATUS_READER_CONNECTED, readers.First() + " (กำลังเชื่อมต่อ)");
                    Console.WriteLine("Connected with " + readers.First());
                    //btnRead.Enabled = true;
                    //SetReadersToCombo(readers);

                    _isStartedMonitor = _thaiIdCard.MonitorStart(readers.First());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Card reader is removed !");
            }
        }

        //private void listReader_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    btnRead.Enabled = true;
        //    statusPanel.Text = " ตรวจพบอุปรกรณ์ " + listReader.SelectedItem;
        //}

        private void CreateStatusBar()
        {
            // Set first panel properties and add to StatusBar
            statusPanel.BorderStyle = StatusBarPanelBorderStyle.Sunken;
            statusPanel.Text = "  Application พร้อมทำงาน (กรุุณาเชื่อมต่ออุปกรณ์)";
            statusPanel.ToolTipText = "กิจกรรมครั้งล่าสุด";
            statusPanel.AutoSize = StatusBarPanelAutoSize.Spring;
            mainStatusBar.Panels.Add(statusPanel);

            // Set second panel properties and add to StatusBar
            datetimePanel.BorderStyle = StatusBarPanelBorderStyle.Raised;
            datetimePanel.ToolTipText = "เวลาปัจจุบัน " + System.DateTime.Today.ToString();


            datetimePanel.Text = System.DateTime.Today.ToLongDateString();
            datetimePanel.AutoSize = StatusBarPanelAutoSize.Contents;
            mainStatusBar.Panels.Add(datetimePanel);

            mainStatusBar.ShowPanels = true;
            mainStatusBar.Height = 30;
            // Add StatusBar to Form controls
            Controls.Add(mainStatusBar);

        }

        private void UpdateUIPersonal()
        {
            txtID.Text = id;
            txtBirthday.Text = birthday;
            txtSex.Text = sex;
            txtName.Text = THprefix + THname + "  " + THlastname;
            txtNameEN.Text = ENprefix + ENname + "  " + ENlastname;
            txtIssue.Text = issue;
            txtExpire.Text = expire;
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.Image = showPhoto;
            txtAddr.Text = "";
            txtAddr.AppendText(address);
            statusPanel.Text = STATUS_READ_SUCCESS;

            btnRefresh.Enabled = true;
           // btnRead.Enabled = true;
            btnPost.Enabled = true;
            btnFTP.Enabled = true;
            btnRefresh.Enabled = true;
            progressBar1.Visible = false;

            if (pictureBox1.Image != null)
            {
                SigBase64 = Convert.ToBase64String(byteImage);
                SendBase64 = SigBase64.Replace("+", @"%2B");
                SendBase64 = SigBase64.Replace("/", @"%2F");
                SendBase64 = SigBase64.Replace("=", @"%3D");
            }
            else if(pictureBox1.Image == null)
            {
                SigBase64 = null;
            }
            
            Console.WriteLine("json state before open => " + json.State);
            try
            {
                json.Open();  
      
            }
            catch(System.InvalidOperationException e)
            {
                Console.WriteLine("exception error => " + e);
            }         
            Console.WriteLine("json state after open => " + json.State);
            
        }

        //private void btnread_click(object sender, eventargs e)
        //{
        //    btnread.enabled = false;
        //    var t = new thread(new threadstart(delegate {
        //        if (!_thaiidcard.readcard())
        //            onerrorreadcard(null);
        //    }));
        //    btnread.enabled = true;
        //   t.start();
        //}
        private void bntRefresh_Click(object sender, EventArgs e)
        {
            //listReader.Items.Clear();

            //btnRead.Enabled = false;

            var readers = _thaiIdCard.GetReaders();
            if (readers == null)
            {
                MessageBox.Show("กรุณาเชื่อมต่อเครื่องอ่านและเสียบบัตร", "ไม่พบอุปกรณ์เชื่อมต่อ", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
               // btnRead.Enabled = false;
                return;
            }
            else
            {
               // btnRead.Enabled = true;
                statusPanel.Text = string.Format(STATUS_READER_CONNECTED, readers.First() + " (กำลังเชื่อมต่อ)");
            }
        }

        private void bntPost_Click(object sender, EventArgs e)
        {
            /*var SigBase64 = Convert.ToBase64String(byteImage);
            var sendBase64 = SigBase64.Replace("+", @"%2B");
            sendBase64 = SigBase64.Replace("/", @"%2F");
            sendBase64 = SigBase64.Replace("=", @"%3D");*/
 
            string URL = Properties.Settings.Default.URL;
            // uform.ShowDialog();
            if (URL != "")
            {
                WebRequest request = WebRequest.Create(URL);
                request.Method = "POST";
                string postData = "thaiID=" + txtID.Text + "&picture=" + SendBase64;
                byte[] bytePost = Encoding.UTF8.GetBytes(postData);
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = bytePost.Length;
                try
                {
                    Stream dataStream = request.GetRequestStream();
                    dataStream.Write(bytePost, 0, bytePost.Length);
                    dataStream.Close();

                    WebResponse response = request.GetResponse();
                    dataStream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(dataStream);
                    reader.Close();
                    dataStream.Close();
                    response.Close();
                    string registResult;

                    using (WebClient client = new WebClient())
                    {
                        byte[] RP =
                        client.UploadValues(URL, new NameValueCollection()
                                       {
                                   { "Picture", SendBase64 },
                                   { "thaiID", txtID.Text },
                                       });
                        registResult = System.Text.Encoding.UTF8.GetString(RP);
                        if (registResult.ToLower() == "success")
                        {
                            string getPath = URL +
                            "?thaiID=" + txtID.Text +
                            "&thName=" + THprefix + THname +
                            "&thSurname=" + THlastname +
                            "&enName=" + ENprefix + ENname +
                            "&enSurname=" + ENlastname +
                            "&address=" + address +
                            "&birthdate=" + birthday +
                            "&date_issue=" + issue +
                            "&date_expire=" + expire +
                            "&version="
                            ;
                            statusPanel.Text = " ส่งข้อมูลถึง " + "[" + URL + "]" + " สำเร็จ! , เวลา: " + System.DateTime.Now.ToString("h:mm") + " น.";
                            ProcessStartInfo sInfo = new ProcessStartInfo(getPath);
                            Process.Start(sInfo);
                            //Properties.Settings.Default.URL = URL;
                        }
                        else
                        {
                            MessageBox.Show("ไม่สามารถแนบข้อมูลสำหรับการลงทะเบียนได้", "Getting Path", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                catch(Exception error)
                {
                    MessageBox.Show("ไม่ได้รับการตอบสนองจาก " + request.RequestUri.Host, "พบปัญหาการร้องขอ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
                MessageBox.Show("กรุณากำหนด URL ในหน้าตั้งค่า", "ไม่สามารถส่งข้อมูล", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void btnFTP_Click(object sender, EventArgs e)
        {
            //FTPConfig fc = new FTPConfig();
           // fc.ShowDialog();
            string fileName = id + ".csv";
            string ftpAddress = Properties.Settings.Default.FTP + ":" + Properties.Settings.Default.Port + "/";
            string user = Properties.Settings.Default.User;
            string pass = Properties.Settings.Default.Pass;
            string fileContent = string.Join(",", id, THprefix, THname, THlastname, ENprefix,
                    ENname, ENlastname, sex, address, birthday, issue, expire);

            if (Properties.Settings.Default.FTP != "" && Properties.Settings.Default.Port != "" && Properties.Settings.Default.User != "" && Properties.Settings.Default.Pass != "")
            {
                byte[] buffer = Encoding.UTF8.GetBytes(fileContent);
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://" + ftpAddress + fileName);
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential(user, pass);

                //fc.txtFTP.ReadOnly = true;
                //fc.txtPort.ReadOnly = true;
                //fc.txtUser.ReadOnly = true;
                //fc.txtPassword.ReadOnly = true;
                try
                {
                    Stream reqStream = request.GetRequestStream();
                    reqStream.Write(buffer, 0, buffer.Length);
                    reqStream.Close();
                    MessageBox.Show("เขียนข้อมูลสำเร็จ", "ตอบกลับการร้องขอ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    statusPanel.Text = " เขียนไฟล์ " + fileName + " สำเร็จ" + ", เวลา: " + System.DateTime.Now.ToString("h:mm") + " น.";
                    //Properties.Settings.Default.FTP = fc.txtFTP.Text;
                    //Properties.Settings.Default.Port = fc.txtPort.Text;
                    //Properties.Settings.Default.User = fc.txtUser.Text;
                    //Properties.Settings.Default.Save();
                    //fc.Hide();
                }
                catch
                {
                    MessageBox.Show("ไม่สามารถเขียนข้อมูลลง Server!", "พบข้อผิดพลาดในการร้องขอการเขียนข้อมูล", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    //fc.txtFTP.ReadOnly = false;
                    //fc.txtPort.ReadOnly = false;
                    //fc.txtUser.ReadOnly = false;
                    //fc.txtPassword.ReadOnly = false;
                    //fc.txtPassword.Text = "";
                }
            }
            else
                MessageBox.Show("กรุณากำหนดการตั้งค่าสำหรับการเขียนข้อมูล", "ไม่สามารถเขียนข้อมูล", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
                
        private void SetReadersToCombo(string[] readers)
        {
            readers[0] = readers[0]+" (กำลังเชื่อมต่อ)";
            //listReader.Items.Clear();
            ////listReader.Items.AddRange(readers);
            //listReader.Items.AddRange(readers);
            //listReader.AllowDrop = false;
            //listReader.SelectedItem = listReader.Items[0];
            //listReader.Enabled = true;

        }
        private string FixThaiCodePage(string str)
        {
            byte[] raw = Encoding.Default.GetBytes(str);
            string res = Encoding.GetEncoding("TIS-620").GetString(raw);
            return res;
        }
        [System.ServiceModel.ServiceContract]
        public interface ijsonService
        {
            [OperationContract, WebGet(ResponseFormat = WebMessageFormat.Json)]
            personal_info person();
        }
        public class jsonService : ijsonService
        {
            public personal_info person()
            {
                WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");
                return new personal_info() {
                    id = Form1.id,
                    EN_prefix = Form1.ENprefix,
                    TH_prefix = Form1.THprefix,
                    EN_firstname = Form1.ENname,
                    EN_lastname = Form1.ENlastname,
                    TH_firstname = Form1.THname,
                    TH_lastname = Form1.THlastname,
                    birthdate = Form1.birthday,
                    addr = Form1.address,
                    gender = Form1.sex,
                    iss = Form1.issue,
                    exp = Form1.expire,
                    img = Form1.SigBase64                
                };
            }
        }
        public class personal_info
        {
            public string id { get; set; }
            public string EN_prefix { get; set; }
            public string TH_prefix { get; set; }
            public string EN_firstname { get; set; }
            public string EN_lastname { get; set; }
            public string TH_firstname { get; set; }
            public string TH_lastname { get; set; }
            public string birthdate { get; set; }
            public string addr { get; set; }
            public string gender { get; set; }
            public string iss { get; set; }
            public string exp { get; set; }
            public string img { get; set; }
        }
        #endregion
    }    
        
}

