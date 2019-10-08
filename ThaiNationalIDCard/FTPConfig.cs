using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace ThaiNationalIDCard
{
    public partial class FTPConfig : Form
    {
        public FTPConfig()
        {
            InitializeComponent();
            txtFTP.Text = Properties.Settings.Default.FTP;
            txtPort.Text = Properties.Settings.Default.Port;
            txtUser.Text = Properties.Settings.Default.User;
            txtFTP.KeyPress += new System.Windows.Forms.KeyPressEventHandler(CheckEnter);
            txtPort.KeyPress += new System.Windows.Forms.KeyPressEventHandler(CheckEnter);
            txtUser.KeyPress += new System.Windows.Forms.KeyPressEventHandler(CheckEnter);
            txtPassword.KeyPress += new System.Windows.Forms.KeyPressEventHandler(CheckEnter);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.URL = txtFTP.Text;
            Properties.Settings.Default.Port = txtPort.Text;
            Properties.Settings.Default.User = txtUser.Text;
            Properties.Settings.Default.Save();
            if (txtFTP.Text == "" || txtPort.Text == "" || txtUser.Text == "" || txtPassword.Text == "")
                MessageBox.Show("กรุณากำหนดข้อมูลการตั้งค่าให้ถูกต้อง", "พบข้อผิดพลาดในการตั้งค่า", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
                this.Hide();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            txtFTP.Text = "";
            txtPort.Text = "";
            txtUser.Text = "";
            txtPassword.Text = "";
            this.Hide();
        }
        private void CheckEnter(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                Properties.Settings.Default.URL = txtFTP.Text;
                Properties.Settings.Default.Port = txtPort.Text;
                Properties.Settings.Default.User = txtUser.Text;
                Properties.Settings.Default.Save();
                if (txtFTP.Text == "" || txtPort.Text == "" || txtUser.Text == "" || txtPassword.Text == "")
                    MessageBox.Show("กรุณากำหนดข้อมูลการตั้งค่าให้ถูกต้อง", "พบข้อผิดพลาดในการตั้งค่า", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                    this.Hide();
            }
        }
    }
}
