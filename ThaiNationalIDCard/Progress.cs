using System;
using System.Drawing;
using System.Windows.Forms;

namespace ThaiNationalIDCard
{
    public partial class Progress : Form
    {

        #region constructor
        public Progress()
        {
            InitializeComponent();
            FormBorderStyle = FormBorderStyle.None;
        }
        #endregion
        #region Mutator
        public string Message
        {
            set { label1.Text = value; }
        }
        #endregion
        /*#region EVENTS

        public event EventHandler<EventArgs> Canceled;
        private void btnCancel_Click(object sender, EventArgs e)
        {
            EventHandler<EventArgs> ea = Canceled;
            if (ea != null)
            {
                ea(this, e);
                this.Close();
            }
        }
        #endregion*/
    }
}
