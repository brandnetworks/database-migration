using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DatabaseTransferTool {


    /// <summary>
    /// The model for the LogView view.
    /// </summary>
    public partial class LogView : Form {

        /// <summary>
        /// An event for updating the log view to add a new message
        /// </summary>
        /// <param name="text"></param>
        delegate void UpdateLogView(string text);

        private UpdateLogView UpdateLogs = null;
        
        /// <summary>
        /// Constructor
        /// </summary>
        public LogView() {
            InitializeComponent();
            resize();
            UpdateLogs = Update;
        }

        /// <summary>
        /// Handle resize events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LogView_Resize(object sender, EventArgs e) {
            resize();
        }

        /// <summary>
        /// Keep the text box in sync with the dimensions of the host window.
        /// </summary>
        private void resize() {
            textBox.Width = Width - (textBox.Margin.Left + textBox.Margin.Right);
            textBox.Height = Height - (textBox.Margin.Top + textBox.Margin.Bottom);
        }

        /// <summary>
        /// Add a new message to the log view along with a line break
        /// </summary>
        /// <param name="text"></param>
        private void Update(string text) {
            textBox.AppendText(text + "\r\n");
        }

        /// <summary>
        /// Notify observers that a new entry has been added.
        /// </summary>
        /// <param name="text"></param>
        public void LogEntryAdded(string text) {
            textBox.Invoke(UpdateLogs, text);
        }

    }
}
