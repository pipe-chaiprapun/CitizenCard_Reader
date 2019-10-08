using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ThaiNationalIDCard
{
    public partial class AllConfig : Form
    {
        public AllConfig()
        {
            InitializeComponent();
            txtUrl.Text = Properties.Settings.Default.URL;
            txtFtp.Text = Properties.Settings.Default.FTP;
            txtPort.Text = Properties.Settings.Default.Port;
            txtUser.Text = Properties.Settings.Default.User;
            txtPass.Text = Properties.Settings.Default.Pass;
        }

        private void button1_Click(object sender, EventArgs e)
        {
                Properties.Settings.Default.URL = txtUrl.Text;
                Properties.Settings.Default.FTP = txtFtp.Text;
                Properties.Settings.Default.Port = txtPort.Text;
                Properties.Settings.Default.User = txtUser.Text;
                Properties.Settings.Default.Pass = txtPass.Text;
                Properties.Settings.Default.Save();
                this.Hide();
        }
    }
}
