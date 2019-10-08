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
    public partial class URLForm : Form
    {
        public URLForm()
        {
            InitializeComponent();
            textBox1.Text = Properties.Settings.Default.URL;
            textBox1.KeyPress += new System.Windows.Forms.KeyPressEventHandler(CheckEnter);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.URL = textBox1.Text;
            Properties.Settings.Default.Save();
            if (textBox1.Text == "")
                MessageBox.Show("กรุณากำหนด URL", "ไม่สามารถส่งข้อมูล", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
                this.Hide();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            this.Hide();
        }
        private void CheckEnter(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                Properties.Settings.Default.URL = textBox1.Text;
                Properties.Settings.Default.Save();
                if (textBox1.Text == "")
                    MessageBox.Show("กรุณากำหนด URL", "ไม่สามารถส่งข้อมูลได้", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                    this.Hide();
            }
        }
    }
}
