using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace camera_show {
    public partial class KeyPassword : Form {
        public KeyPassword() {
            InitializeComponent();
        }
        /// <summary>
        /// For input key password
        /// </summary>
        public string inputValue { get; set; }
        private void textBox1_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode != Keys.Enter) {
                return;
            }
            inputValue = textBox1.Text;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
