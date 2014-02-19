namespace DatabaseTransferTool {
    partial class Form1 {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.transferButton = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.logBox = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.olderRadio = new System.Windows.Forms.RadioButton();
            this.newerRadio = new System.Windows.Forms.RadioButton();
            this.allRadio = new System.Windows.Forms.RadioButton();
            this.olderBox = new System.Windows.Forms.TextBox();
            this.newerBox = new System.Windows.Forms.TextBox();
            this.includeUndated = new System.Windows.Forms.CheckBox();
            this.label6 = new System.Windows.Forms.Label();
            this.threadsBox = new System.Windows.Forms.TextBox();
            this.batchBox = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.toggleCheckBox = new System.Windows.Forms.CheckBox();
            this.label14 = new System.Windows.Forms.Label();
            this.tableProgressBar = new System.Windows.Forms.ProgressBar();
            this.label16 = new System.Windows.Forms.Label();
            this.sourceTablesGrid = new System.Windows.Forms.DataGridView();
            this.queryThreadsBar = new System.Windows.Forms.ProgressBar();
            this.label17 = new System.Windows.Forms.Label();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.currentHealth = new System.Windows.Forms.Label();
            this.recentHealth = new System.Windows.Forms.Label();
            this.overallHealth = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.destinationTablesGrid = new System.Windows.Forms.DataGridView();
            this.queryLogging = new System.Windows.Forms.CheckBox();
            this.destination = new System.Windows.Forms.TextBox();
            this.source = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.sourceTablesGrid)).BeginInit();
            this.statusStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.destinationTablesGrid)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 14);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(128, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Source Connection String";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(16, 57);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(147, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Destination Connection String";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(16, 97);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(126, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Source Tables to Include";
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(114, 527);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(819, 23);
            this.progressBar.TabIndex = 6;
            // 
            // transferButton
            // 
            this.transferButton.Location = new System.Drawing.Point(19, 498);
            this.transferButton.Name = "transferButton";
            this.transferButton.Size = new System.Drawing.Size(914, 23);
            this.transferButton.TabIndex = 7;
            this.transferButton.Text = "Begin Transfer";
            this.transferButton.UseVisualStyleBackColor = true;
            this.transferButton.Click += new System.EventHandler(this.transferButton_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(19, 365);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(50, 13);
            this.label4.TabIndex = 9;
            this.label4.Text = "Log Path";
            // 
            // logBox
            // 
            this.logBox.Location = new System.Drawing.Point(19, 382);
            this.logBox.Name = "logBox";
            this.logBox.Size = new System.Drawing.Size(253, 20);
            this.logBox.TabIndex = 10;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(19, 409);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(96, 13);
            this.label5.TabIndex = 11;
            this.label5.Text = "Transfer Only Data";
            // 
            // olderRadio
            // 
            this.olderRadio.AutoSize = true;
            this.olderRadio.Location = new System.Drawing.Point(22, 426);
            this.olderRadio.Name = "olderRadio";
            this.olderRadio.Size = new System.Drawing.Size(78, 17);
            this.olderRadio.TabIndex = 12;
            this.olderRadio.Text = "Older Than";
            this.olderRadio.UseVisualStyleBackColor = true;
            // 
            // newerRadio
            // 
            this.newerRadio.AutoSize = true;
            this.newerRadio.Location = new System.Drawing.Point(22, 450);
            this.newerRadio.Name = "newerRadio";
            this.newerRadio.Size = new System.Drawing.Size(84, 17);
            this.newerRadio.TabIndex = 13;
            this.newerRadio.Text = "Newer Than";
            this.newerRadio.UseVisualStyleBackColor = true;
            // 
            // allRadio
            // 
            this.allRadio.AutoSize = true;
            this.allRadio.Checked = true;
            this.allRadio.Location = new System.Drawing.Point(22, 474);
            this.allRadio.Name = "allRadio";
            this.allRadio.Size = new System.Drawing.Size(62, 17);
            this.allRadio.TabIndex = 14;
            this.allRadio.TabStop = true;
            this.allRadio.Text = "All Data";
            this.allRadio.UseVisualStyleBackColor = true;
            // 
            // olderBox
            // 
            this.olderBox.Location = new System.Drawing.Point(107, 426);
            this.olderBox.Name = "olderBox";
            this.olderBox.Size = new System.Drawing.Size(165, 20);
            this.olderBox.TabIndex = 15;
            // 
            // newerBox
            // 
            this.newerBox.Location = new System.Drawing.Point(107, 450);
            this.newerBox.Name = "newerBox";
            this.newerBox.Size = new System.Drawing.Size(165, 20);
            this.newerBox.TabIndex = 16;
            // 
            // includeUndated
            // 
            this.includeUndated.AutoSize = true;
            this.includeUndated.Checked = true;
            this.includeUndated.CheckState = System.Windows.Forms.CheckState.Checked;
            this.includeUndated.Location = new System.Drawing.Point(107, 475);
            this.includeUndated.Name = "includeUndated";
            this.includeUndated.Size = new System.Drawing.Size(151, 17);
            this.includeUndated.TabIndex = 18;
            this.includeUndated.Text = "Include Non-Dated Tables";
            this.includeUndated.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(278, 385);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(145, 13);
            this.label6.TabIndex = 19;
            this.label6.Text = "Maximum Number of Threads";
            // 
            // threadsBox
            // 
            this.threadsBox.Location = new System.Drawing.Point(429, 382);
            this.threadsBox.Name = "threadsBox";
            this.threadsBox.Size = new System.Drawing.Size(79, 20);
            this.threadsBox.TabIndex = 20;
            // 
            // batchBox
            // 
            this.batchBox.Location = new System.Drawing.Point(429, 409);
            this.batchBox.Name = "batchBox";
            this.batchBox.Size = new System.Drawing.Size(79, 20);
            this.batchBox.TabIndex = 21;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(514, 412);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(23, 13);
            this.label7.TabIndex = 23;
            this.label7.Text = "MB";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(365, 412);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(58, 13);
            this.label9.TabIndex = 25;
            this.label9.Text = "Batch Size";
            // 
            // toggleCheckBox
            // 
            this.toggleCheckBox.AutoSize = true;
            this.toggleCheckBox.Checked = true;
            this.toggleCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.toggleCheckBox.Location = new System.Drawing.Point(827, 97);
            this.toggleCheckBox.Name = "toggleCheckBox";
            this.toggleCheckBox.Size = new System.Drawing.Size(106, 17);
            this.toggleCheckBox.TabIndex = 27;
            this.toggleCheckBox.Text = "Toggle Selection";
            this.toggleCheckBox.UseVisualStyleBackColor = true;
            this.toggleCheckBox.CheckedChanged += new System.EventHandler(this.toggleCheckBox_CheckedChanged);
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(19, 527);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(84, 13);
            this.label14.TabIndex = 34;
            this.label14.Text = "Overall Progress";
            // 
            // tableProgressBar
            // 
            this.tableProgressBar.Location = new System.Drawing.Point(114, 556);
            this.tableProgressBar.Name = "tableProgressBar";
            this.tableProgressBar.Size = new System.Drawing.Size(819, 23);
            this.tableProgressBar.TabIndex = 36;
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(23, 556);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(78, 13);
            this.label16.TabIndex = 37;
            this.label16.Text = "Table Progress";
            // 
            // sourceTablesGrid
            // 
            this.sourceTablesGrid.AllowUserToAddRows = false;
            this.sourceTablesGrid.AllowUserToDeleteRows = false;
            this.sourceTablesGrid.AllowUserToResizeRows = false;
            this.sourceTablesGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.sourceTablesGrid.Location = new System.Drawing.Point(22, 120);
            this.sourceTablesGrid.MultiSelect = false;
            this.sourceTablesGrid.Name = "sourceTablesGrid";
            this.sourceTablesGrid.RowHeadersVisible = false;
            this.sourceTablesGrid.ShowCellErrors = false;
            this.sourceTablesGrid.ShowCellToolTips = false;
            this.sourceTablesGrid.ShowEditingIcon = false;
            this.sourceTablesGrid.ShowRowErrors = false;
            this.sourceTablesGrid.Size = new System.Drawing.Size(911, 109);
            this.sourceTablesGrid.TabIndex = 38;
            this.sourceTablesGrid.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.tablesGrid_DataError);
            // 
            // queryThreadsBar
            // 
            this.queryThreadsBar.Location = new System.Drawing.Point(114, 585);
            this.queryThreadsBar.Name = "queryThreadsBar";
            this.queryThreadsBar.Size = new System.Drawing.Size(819, 23);
            this.queryThreadsBar.TabIndex = 40;
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(23, 585);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(79, 13);
            this.label17.TabIndex = 41;
            this.label17.Text = "Active Threads";
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusLabel});
            this.statusStrip1.Location = new System.Drawing.Point(0, 615);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(945, 22);
            this.statusStrip1.TabIndex = 42;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // statusLabel
            // 
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(0, 17);
            // 
            // currentHealth
            // 
            this.currentHealth.AutoSize = true;
            this.currentHealth.Location = new System.Drawing.Point(858, 440);
            this.currentHealth.Name = "currentHealth";
            this.currentHealth.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
            this.currentHealth.Size = new System.Drawing.Size(75, 18);
            this.currentHealth.TabIndex = 43;
            this.currentHealth.Text = "Current Health";
            this.currentHealth.Visible = false;
            // 
            // recentHealth
            // 
            this.recentHealth.AutoSize = true;
            this.recentHealth.Location = new System.Drawing.Point(858, 458);
            this.recentHealth.Name = "recentHealth";
            this.recentHealth.Size = new System.Drawing.Size(76, 13);
            this.recentHealth.TabIndex = 44;
            this.recentHealth.Text = "Recent Health";
            this.recentHealth.Visible = false;
            // 
            // overallHealth
            // 
            this.overallHealth.AutoSize = true;
            this.overallHealth.Location = new System.Drawing.Point(860, 477);
            this.overallHealth.Name = "overallHealth";
            this.overallHealth.Size = new System.Drawing.Size(74, 13);
            this.overallHealth.TabIndex = 45;
            this.overallHealth.Text = "Overall Health";
            this.overallHealth.Visible = false;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(19, 241);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(145, 13);
            this.label8.TabIndex = 46;
            this.label8.Text = "Destination Tables to Include";
            // 
            // destinationTablesGrid
            // 
            this.destinationTablesGrid.AllowUserToAddRows = false;
            this.destinationTablesGrid.AllowUserToDeleteRows = false;
            this.destinationTablesGrid.AllowUserToResizeRows = false;
            this.destinationTablesGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.destinationTablesGrid.Location = new System.Drawing.Point(19, 257);
            this.destinationTablesGrid.MultiSelect = false;
            this.destinationTablesGrid.Name = "destinationTablesGrid";
            this.destinationTablesGrid.RowHeadersVisible = false;
            this.destinationTablesGrid.ShowCellErrors = false;
            this.destinationTablesGrid.ShowCellToolTips = false;
            this.destinationTablesGrid.ShowEditingIcon = false;
            this.destinationTablesGrid.ShowRowErrors = false;
            this.destinationTablesGrid.Size = new System.Drawing.Size(915, 105);
            this.destinationTablesGrid.TabIndex = 47;
            this.destinationTablesGrid.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.tablesGrid_DataError);
            // 
            // queryLogging
            // 
            this.queryLogging.AutoSize = true;
            this.queryLogging.Location = new System.Drawing.Point(802, 381);
            this.queryLogging.Name = "queryLogging";
            this.queryLogging.Size = new System.Drawing.Size(131, 17);
            this.queryLogging.TabIndex = 48;
            this.queryLogging.Text = "Enable Query Logging";
            this.queryLogging.UseVisualStyleBackColor = true;
            this.queryLogging.CheckedChanged += new System.EventHandler(this.queryLogging_CheckedChanged);
            // 
            // destination
            // 
            this.destination.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::DatabaseTransferTool.Properties.Settings.Default, "DestinationConnectionStringText", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.destination.Location = new System.Drawing.Point(19, 74);
            this.destination.Name = "destination";
            this.destination.Size = new System.Drawing.Size(914, 20);
            this.destination.TabIndex = 3;
            this.destination.Text = global::DatabaseTransferTool.Properties.Settings.Default.DestinationConnectionStringText;
            this.destination.Leave += new System.EventHandler(this.destination_Leave);
            // 
            // source
            // 
            this.source.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::DatabaseTransferTool.Properties.Settings.Default, "SourceConnectionStringText", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.source.Location = new System.Drawing.Point(16, 30);
            this.source.Name = "source";
            this.source.Size = new System.Drawing.Size(917, 20);
            this.source.TabIndex = 1;
            this.source.Text = global::DatabaseTransferTool.Properties.Settings.Default.SourceConnectionStringText;
            this.source.Leave += new System.EventHandler(this.source_Leave);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(945, 637);
            this.Controls.Add(this.queryLogging);
            this.Controls.Add(this.destinationTablesGrid);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.overallHealth);
            this.Controls.Add(this.recentHealth);
            this.Controls.Add(this.currentHealth);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.label17);
            this.Controls.Add(this.queryThreadsBar);
            this.Controls.Add(this.sourceTablesGrid);
            this.Controls.Add(this.label16);
            this.Controls.Add(this.tableProgressBar);
            this.Controls.Add(this.label14);
            this.Controls.Add(this.toggleCheckBox);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.batchBox);
            this.Controls.Add(this.threadsBox);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.includeUndated);
            this.Controls.Add(this.newerBox);
            this.Controls.Add(this.olderBox);
            this.Controls.Add(this.allRadio);
            this.Controls.Add(this.newerRadio);
            this.Controls.Add(this.olderRadio);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.logBox);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.transferButton);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.destination);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.source);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Text = "Database Transfer Tool";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.sourceTablesGrid)).EndInit();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.destinationTablesGrid)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox source;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox destination;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Button transferButton;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox logBox;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.RadioButton olderRadio;
        private System.Windows.Forms.RadioButton newerRadio;
        private System.Windows.Forms.RadioButton allRadio;
        private System.Windows.Forms.TextBox olderBox;
        private System.Windows.Forms.TextBox newerBox;
        private System.Windows.Forms.CheckBox includeUndated;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox threadsBox;
        private System.Windows.Forms.TextBox batchBox;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.CheckBox toggleCheckBox;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.ProgressBar tableProgressBar;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.DataGridView sourceTablesGrid;
        private System.Windows.Forms.ProgressBar queryThreadsBar;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel;
        private System.Windows.Forms.Label currentHealth;
        private System.Windows.Forms.Label recentHealth;
        private System.Windows.Forms.Label overallHealth;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.DataGridView destinationTablesGrid;
        private System.Windows.Forms.CheckBox queryLogging;
    }
}

