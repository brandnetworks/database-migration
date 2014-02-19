using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Configuration;
using DatabaseTransferTool.Properties;
using System.IO;

namespace DatabaseTransferTool {

    /// <summary>
    /// The main window of the application
    /// </summary>
    public partial class Form1 : Form {

        // flag for the current error state of the application
        private static bool error = false;



        #region status event handler declarations

        delegate void PerformProgressBarUpdate();
        delegate void UpdateTableBar();
        delegate void UpdateQueryThreadsBar();
        delegate void UpdateTimeRemaining();
        delegate void UpdateHealth();

        PerformProgressBarUpdate Updater = null;
        UpdateTableBar TableUpdater = null;
        UpdateQueryThreadsBar QueryThreadsUpdater = null;
        UpdateTimeRemaining TimeRemainingUpdater = null;        

        delegate void UpdateCurrentHealthDelegate();
        private UpdateCurrentHealthDelegate UpdateCurrentHealthInstance = null;

        delegate void UpdateRecentHealthDelegate();
        private UpdateRecentHealthDelegate UpdateRecentHealthInstance = null;

        delegate void UpdateOverallHealthDelegate();
        private UpdateOverallHealthDelegate UpdateOverallHealthInstance = null;

        #endregion status event handler declarations


        // the time the transfer started
        private DateTime startTime = DateTime.MinValue;

        // a lock object for table data
        private object tablesLock = new object();

        // our handle for the transfer utility
        TransferUtils transferUtils = null;

        // whether the application can safely exit without improperly halting a background operation
        private bool safeToExit = true;

        /// <summary>
        /// Default constructor
        /// </summary>
        public Form1() {

            transferUtils = new TransferUtils();
            InitializeComponent();

            BuildGrid();

            #region initialize the display

            currentHealth.Visible = false;
            recentHealth.Visible = false;
            overallHealth.Visible = false;

            // set the parameters based on the App.config values
            source.Text = !string.IsNullOrWhiteSpace(ConfigurationSettings.AppSettings["SourceConnectionString"]) ?
                ConfigurationSettings.AppSettings["SourceConnectionString"] : Settings.Default.SourceConnectionStringText;
            destination.Text = !string.IsNullOrWhiteSpace(ConfigurationSettings.AppSettings["DestinationConnectionString"])
                ? ConfigurationSettings.AppSettings["DestinationConnectionString"] : Settings.Default.DestinationConnectionStringText;
            logBox.Text = ConfigurationSettings.AppSettings["LogPath"];
            olderBox.Text = ConfigurationSettings.AppSettings["BeforeDate"];
            newerBox.Text = ConfigurationSettings.AppSettings["AfterDate"];
            threadsBox.Text = ConfigurationSettings.AppSettings["ThreadPoolSize"];
            batchBox.Text = ConfigurationSettings.AppSettings["BatchSizeMB"]; ;

            updateTablesGrid(sourceTablesGrid, source.Text, GridType.Source);
            updateTablesGrid(destinationTablesGrid, destination.Text, GridType.Destination);
            Updater = UpdateStatus;
            TableUpdater = PerformTableProgressUpdate;
            QueryThreadsUpdater = PerformQueryThreadUpdate;
            TimeRemainingUpdater = PerformTimeRemainingUpdate;
            UpdateCurrentHealthInstance = UpdateCurrentHealth;
            UpdateRecentHealthInstance = UpdateRecentHealth;
            UpdateOverallHealthInstance = UpdateOverallHealth;

            statusStrip1.Text = "Time Remaining: N/A";

            #endregion initialize the display

            // cause the focus to leave the source and destination text boxes in order to
            // initialize the table grid data
            transferButton.Select();
        }

        #region helper methods for cross-thread status updates

        private void InvokeUpdateCurrentHealth() {
            currentHealth.Invoke(UpdateCurrentHealthInstance);
        }

        private void InvokeUpdateRecentHealth() {
            recentHealth.Invoke(UpdateRecentHealthInstance);
        }

        private void InvokeUpdateOverallHealth() {
            overallHealth.Invoke(UpdateOverallHealthInstance);
        }

        #endregion helper methods for cross-thread status updates

        /// <summary>
        /// Update the current health of the transfer. The current health is
        /// the instantaneous error status of the transfer operation.
        /// </summary>
        private void UpdateCurrentHealth() {
            
            currentHealth.Visible = true;
            SafeRetry.Health health = SafeRetry.GetCurrentHealth();
            currentHealth.ForeColor = health == SafeRetry.Health.Green ?
                Color.Green : health == SafeRetry.Health.Yellow ?
                Color.DarkOrange : health == SafeRetry.Health.Red ?
                Color.DarkRed : Color.Green;

        }

        /// <summary>
        /// Update the recent health of the transfer. The recent health is
        /// the error status of the transfer operation within the current
        /// "recent" window specified by the SafeRetry instance used by
        /// the TransferUtils object.
        /// </summary>
        private void UpdateRecentHealth() {

            recentHealth.Visible = true;
            SafeRetry.Health health = SafeRetry.GetRecentHealth();
            recentHealth.ForeColor = health == SafeRetry.Health.Green ?
                Color.Green : health == SafeRetry.Health.Yellow ?
                Color.DarkOrange : health == SafeRetry.Health.Red ?
                Color.DarkRed : Color.Green;
        }

        /// <summary>
        /// Update the overall health of the transfer. The overall health
        /// is the aggregate error status of the transfer from the beginning
        /// to the present.
        /// </summary>
        private void UpdateOverallHealth() {

            overallHealth.Visible = true;
            SafeRetry.Health health = SafeRetry.GetOverallHealth();
            overallHealth.ForeColor = health == SafeRetry.Health.Green ?
                Color.Green : health == SafeRetry.Health.Yellow ?
                Color.DarkOrange : health == SafeRetry.Health.Red ?
                Color.DarkRed : Color.Green;
        }

        /// <summary>
        /// Construct the source and destination table grids.
        /// </summary>
        private void BuildGrid()
        {

            #region construction of source tables grid

            sourceTablesGrid.Columns.Clear();

            sourceTablesGrid.Columns.Add(new DataGridViewCheckBoxColumn() {
                Name = " ",
                HeaderText = " ",
                FillWeight = 1,
                ReadOnly = false,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                Width = 20,
                Resizable = DataGridViewTriState.False,
                MinimumWidth = 20
            });

            sourceTablesGrid.Columns.Add(new DataGridViewTextBoxColumn() {
                Name = "Table",
                HeaderText = "Table",
                FillWeight = 2,
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                MinimumWidth = 50,
                Resizable = DataGridViewTriState.False,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            });

            sourceTablesGrid.Columns.Add(new DataGridViewTextBoxColumn() {
                Name = "Size (MB)",
                HeaderText = "Size (MB)",
                FillWeight = 2,
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                MinimumWidth = 30,
                Resizable = DataGridViewTriState.False,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            });

            sourceTablesGrid.Columns.Add(new DataGridViewTextBoxColumn()
            {
                Name = "Column Mappings",
                HeaderText = "Column Mappings",
                FillWeight = 2,
                ReadOnly = false,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                MinimumWidth = 150,
                Resizable = DataGridViewTriState.True,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            });

            sourceTablesGrid.Columns.Add(new DataGridViewTextBoxColumn()
            {
                Name = "Virtual Select Columns",
                HeaderText = "Virtual Select Columns",
                FillWeight = 2,
                ReadOnly = false,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                MinimumWidth = 150,
                Resizable = DataGridViewTriState.True,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            });

            sourceTablesGrid.Columns.Add(new DataGridViewTextBoxColumn() {
                Name = "Filter",
                HeaderText = "Filter",
                FillWeight = 100,
                ReadOnly = false,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                MinimumWidth = 100,
                Width = 100,
                Resizable = DataGridViewTriState.False,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            #endregion construction of source tables grid

            #region construction of the destination tables grid

            destinationTablesGrid.Columns.Clear();

            destinationTablesGrid.Columns.Add(new DataGridViewCheckBoxColumn()
            {
                Name = " ",
                HeaderText = " ",
                FillWeight = 1,
                ReadOnly = false,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                Width = 20,
                Resizable = DataGridViewTriState.False,
                MinimumWidth = 20
            });

            destinationTablesGrid.Columns.Add(new DataGridViewTextBoxColumn()
            {
                Name = "Table",
                HeaderText = "Table",
                FillWeight = 2,
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                MinimumWidth = 50,
                Resizable = DataGridViewTriState.False,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            });

            destinationTablesGrid.Columns.Add(new DataGridViewTextBoxColumn()
            {
                Name = "Source Table Mapping",
                HeaderText = "Source Table Mapping",
                FillWeight = 2,
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                MinimumWidth = 150,
                Resizable = DataGridViewTriState.False,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            });

            destinationTablesGrid.Columns.Add(new DataGridViewCheckBoxColumn()
            {
                Name = "Use Distinct Selects",
                HeaderText = "Use Distinct Selects",
                FillWeight = 1,
                ReadOnly = false,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                Width = 20,
                Resizable = DataGridViewTriState.False,
                MinimumWidth = 20,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            });

            destinationTablesGrid.Columns.Add(new DataGridViewTextBoxColumn()
            {
                Name = "Virtual Select Columns",
                HeaderText = "Virtual Select Columns",
                FillWeight = 2,
                ReadOnly = false,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                MinimumWidth = 150,
                Resizable = DataGridViewTriState.True,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            #endregion construction of the destination tables grid
        }

        /// <summary>
        /// Handle the click event of the transferButton and initiate the transfer if possible.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void transferButton_Click(object sender, EventArgs e) {

            try
            {
                // save the user-specified parameters for the next run in case they are not already
                // specified in the App.config
                Settings.Default.Save();

                // optimize the destination connection string for the transfer
                destination.Text = OptimizeConnectionStringForTransfer(destination.Text);

                // optimize the source destination connection string for the transfer
                source.Text = OptimizeConnectionStringForTransfer(source.Text);

                // reset the transfer utility so we start with a clean slate
                transferUtils.Reset();

                // note the time the transfer started
                startTime = DateTime.Now;

                // reset the progress
                progressBar.Value = 0;
                tableProgressBar.Value = 0;
                transferButton.Enabled = false;
                safeToExit = false;

                // A list for tables to include in the transfer. This will be populated
                // with all checked tables in the source and destination grids.
                List<Table> include = new List<Table>();

                // populate the included tables list
                foreach (DataGridViewRow row in sourceTablesGrid.Rows)
                {

                    DataGridViewCheckBoxCell checkbox = row.Cells[0] as DataGridViewCheckBoxCell;

                    if (checkbox.Value == checkbox.TrueValue)
                    {
                        IDictionary<string, string> columnMappings = new Dictionary<string, string>();
                        IDictionary<string, string> virtualSelectColumns = new Dictionary<string, string>();

                        if (!string.IsNullOrWhiteSpace(row.Cells[3].Value as string))
                        {
                            // column mappings are in the form of col=>col1,col2=>col3,...
                            foreach (string mapping in (row.Cells[3].Value as string).Replace(" ", "").Split(new string[] { "," },
                                StringSplitOptions.RemoveEmptyEntries))
                            {
                                string[] columns = mapping.Split(new string[] { "=>" }, StringSplitOptions.RemoveEmptyEntries);
                                if (columns.Length != 2)
                                {
                                    throw new Exception("Invalid column mapping string: " + mapping);
                                }
                                else
                                {
                                    columnMappings.Add(columns[0], columns[1]);
                                }
                            }
                        }

                        // handle virtual select columns
                        if (!string.IsNullOrWhiteSpace(row.Cells[4].Value as string))
                        {
                            // column mappings are in the form of col=>{select query col1},col2=>{select query col3},...
                            foreach (string column in (row.Cells[4].Value as string).Split(new string[] { "}," },
                                StringSplitOptions.RemoveEmptyEntries))
                            {
                                string[] columns = column.Split(new string[] { "=>{" }, StringSplitOptions.RemoveEmptyEntries);
                                if (columns.Length != 2)
                                {
                                    throw new Exception("Invalid virtual select column string: " + column);
                                }
                                else
                                {
                                    virtualSelectColumns.Add(columns[0], columns[1].Replace("}", ""));
                                }
                            }
                        }

                        // create a table object from the selected row and add it to the included list
                        Table table = new Table(0, row.Cells[1].Value as string,
                            row.Cells[5].Value as string, (int) Math.Round(double.Parse(row.Cells[2].Value as string)),
                            columnMappings, true, virtualSelectColumns);

                        include.Add(table);                        
                    }

                }

                // handle destination tables grid in the same manner
                foreach (DataGridViewRow row in destinationTablesGrid.Rows)
                {
                    DataGridViewCheckBoxCell checkbox = row.Cells[0] as DataGridViewCheckBoxCell;

                    if (checkbox.Value == checkbox.TrueValue && !string.IsNullOrWhiteSpace(row.Cells[4].Value as string))
                    {
                        IDictionary<string, string> virtualSelectColumns = new Dictionary<string, string>();
                        DataGridViewCheckBoxCell distinct = row.Cells[3] as DataGridViewCheckBoxCell;

                        // handle virtual select columns
                        if (!string.IsNullOrWhiteSpace(row.Cells[4].Value as string))
                        {
                            // column mappings are in the form of col=>{select query col1},col2=>{select query col3},...
                            foreach (string column in (row.Cells[4].Value as string).Split(new string[] { "}," },
                                StringSplitOptions.RemoveEmptyEntries))
                            {
                                string[] columns = column.Split(new string[] { "=>{" }, StringSplitOptions.RemoveEmptyEntries);
                                if (columns.Length != 2)
                                {
                                    throw new Exception("Invalid virtual select column string: " + column);
                                }
                                else
                                {
                                    virtualSelectColumns.Add(columns[0], columns[1].Replace("}", ""));
                                }
                            }
                        }

                        // create a table object for the selected row and add it to the included list
                        Table table = new Table(0, row.Cells[1].Value as string, null, 0, null, false, virtualSelectColumns, 
                            row.Cells[2].Value as string, distinct.Value == distinct.TrueValue);

                        include.Add(table);    

                    }
                }

                // grab a reference to the transfer utility's status indicator and tie it
                // to the status controls in the view
                StatusIndicator status = transferUtils.Status;
                status.Update += UpdateProgressBar;

                // prepare the live logging view for use
                LogView logView = new LogView();
                Logger.EntryAdded += logView.LogEntryAdded;
                logView.SetDesktopLocation(Location.X + 600, Location.Y - 100);
                logView.Visible = true;

                // reset the error state
                error = false;

                // run the transfer in a background thread
                new Thread(() =>
                {

                    Thread.CurrentThread.IsBackground = true;

                    try
                    {
                        // no longer safe to exit; the transfer is running
                        safeToExit = false;

                        // perform the transfer
                        transferUtils.TransferTables(source.Text, destination.Text, include, logBox.Text,
                            olderRadio.Checked ? TransferUtils.TransferMode.OlderThan :
                            newerRadio.Checked ? TransferUtils.TransferMode.NewerThan :
                            TransferUtils.TransferMode.All, olderBox.Text, newerBox.Text,
                            includeUndated.Checked, int.Parse(threadsBox.Text),
                            int.Parse(batchBox.Text));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message + ", " + ex.StackTrace);
                        error = true;
                    }
                    finally
                    {
                        // post-transfer/post-failure housekeeping
                        status.Update -= UpdateProgressBar;
                        safeToExit = true;
                        Logger.EntryAdded -= logView.LogEntryAdded;
                        transferButton.Enabled = true;
                    }


                }).Start();

                // Run a polling-based status updater in a background thread. It would
                // be nice to replace this with a responsive programming approach like 
                // RX.
                Thread memoryUpdater = new Thread(() =>
                {
                    while (!safeToExit)
                    {
                        try
                        {
                            UpdateTableProgress();
                            UpdateQueryThreads();
                            UpdateETA();
                            InvokeUpdateCurrentHealth();
                            InvokeUpdateRecentHealth();
                            InvokeUpdateOverallHealth();
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message + ", " + ex.StackTrace);
                            error = true;
                        }
                    }
                });

                memoryUpdater.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
            
        }

        /// <summary>
        /// Recalculate and display the time left in the transfer
        /// </summary>
        private void PerformTimeRemainingUpdate() {
            
            TimeSpan timeSpent = DateTime.Now - startTime;
            TimeSpan timeRemaining = transferUtils.TimeRemaining;
            TimeSpan timeRequired = timeSpent + timeRemaining;

            if (timeSpent.TotalMilliseconds >= 0 &&
                timeRemaining.TotalMilliseconds >= 0 &&
                timeRequired.TotalMilliseconds >= 0) {

                statusLabel.Text = "Time Remaining: " + formatTimeSpan(timeRemaining) +
                    "   Time Spent: " + formatTimeSpan(timeSpent) +
                    "   Total Time Required: " + formatTimeSpan(timeRequired);
            }
        }

        /// <summary>
        /// Format a time span for display
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        private string formatTimeSpan(TimeSpan time) {

            StringBuilder formatted = new StringBuilder();

            if (time.Days > 0) {
                formatted.Append(time.Days.ToString("D2")).Append(".");
            }
            formatted.Append(time.Hours.ToString("D2")).Append(":")
                .Append(time.Minutes.ToString("D2")).Append(":").Append(time.Seconds.ToString("D2"));

            return formatted.ToString();
        }

        /// <summary>
        /// Recalculate the time remaining by calling PerformTimeRemainingUpdate
        /// (currently a passthrough)
        /// </summary>
        private void UpdateETA() {
            PerformTimeRemainingUpdate();
        }

        /// <summary>
        /// Update the progress of the current table
        /// </summary>
        private void PerformTableProgressUpdate() {
            if (transferUtils != null && transferUtils.CurrentTable != null && 
                transferUtils.CurrentTable.TransferStatus != null) {

                tableProgressBar.Minimum = transferUtils.CurrentTable.TransferStatus.Minimum;
                tableProgressBar.Maximum = transferUtils.CurrentTable.TransferStatus.Maximum;
                tableProgressBar.Value = transferUtils.CurrentTable.TransferStatus.Value;
            }

        }

        /// <summary>
        /// Update the current number of query threads running
        /// </summary>
        private void PerformQueryThreadUpdate() {
            if (transferUtils != null) {
                queryThreadsBar.Minimum = 0;
                queryThreadsBar.Maximum = int.Parse(threadsBox.Text);
                queryThreadsBar.Value = transferUtils.NumQueryThreads;
            }
        }

        /// <summary>
        /// Invoke the TableUpdater on the tableProgressBar
        /// </summary>
        private void UpdateTableProgress() {
            tableProgressBar.Invoke(TableUpdater);
        }

        /// <summary>
        /// Invoke the QueryThreadsUpdater on the queryThreadsBar
        /// </summary>
        private void UpdateQueryThreads() {
            queryThreadsBar.Invoke(QueryThreadsUpdater);
        }

        /// <summary>
        /// Invoke the Updater on the progressBar
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void UpdateProgressBar(object sender, EventArgs e)
        {
            progressBar.Invoke(Updater);
        }

        /// <summary>
        /// Update the status controls
        /// </summary>
        private void UpdateStatus() {

            // overall progress bar
            progressBar.Minimum = transferUtils.Status.Minimum;
            progressBar.Maximum = transferUtils.Status.Maximum;
            progressBar.Step = transferUtils.Status.Step;
            progressBar.Value = transferUtils.Status.Value;


            if ((transferUtils.CompletedTables.Count == transferUtils.AllTables.Count && transferUtils.AllTables.Count > 0) || error) {

                transferButton.Enabled = true;
                queryThreadsBar.Value = 0;

                setCompletionRowColors(sourceTablesGrid);
                setCompletionRowColors(destinationTablesGrid);
            }

            if (transferUtils != null && transferUtils.CompletedTables != null) {

                updateRowColors(sourceTablesGrid);
                updateRowColors(destinationTablesGrid);                

                if (transferUtils.CompletedTables.Count == transferUtils.AllTables.Count) {
                    progressBar.Value = progressBar.Maximum;
                }
            }

            UpdateTableProgress();

        }

        /// <summary>
        /// Cause grid rows associated with tables that have been successfully transferred to turn green
        /// </summary>
        /// <param name="grid"></param>
        private void setCompletionRowColors(DataGridView grid)
        {
            foreach (DataGridViewRow row in grid.Rows)
            {

                DataGridViewCheckBoxCell checkbox = row.Cells[0] as DataGridViewCheckBoxCell;

                if (checkbox.Value == checkbox.TrueValue)
                {
                    row.DefaultCellStyle.BackColor = Color.LightGreen;
                }

            }
        }

        /// <summary>
        /// Synchronize grid rows with their appropriate colors based on the status of the transfers of 
        /// their associated tables
        /// </summary>
        /// <param name="grid"></param>
        private void updateRowColors(DataGridView grid)
        {
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (transferUtils.CompletedTables.Where(x => x.Name == row.Cells[1].Value.ToString()).Count() > 0)
                {
                    row.DefaultCellStyle.BackColor = Color.LightGreen;
                }
                else
                {
                    row.DefaultCellStyle.BackColor = Color.White;
                }
            }
        }

        /// <summary>
        /// Update the destination tables grid after the destination connection string has changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void destination_Leave(object sender, EventArgs e) {
            updateTablesGrid(destinationTablesGrid, destination.Text, GridType.Destination);
        }

        /// <summary>
        /// Toggle all checkboxes in the table grids
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toggleCheckBox_CheckedChanged(object sender, EventArgs e) {
            toggleCheckBoxChangeForTable(sourceTablesGrid);
            toggleCheckBoxChangeForTable(destinationTablesGrid);
        }

        /// <summary>
        /// Toggle all checkboxes in the specified tables grid
        /// </summary>
        /// <param name="grid"></param>
        private void toggleCheckBoxChangeForTable(DataGridView grid)
        {
            lock (tablesLock)
            {
                for (int index = 0; index < grid.Rows.Count; ++index)
                {

                    DataGridViewCheckBoxCell cell = grid.Rows[index].Cells[0] as DataGridViewCheckBoxCell;

                    if (cell != null)
                    {
                        if (Convert.ToBoolean(cell.Value))
                        {
                            cell.Value = cell.FalseValue;
                        }
                        else
                        {
                            cell.Value = cell.TrueValue;
                        }
                    }

                }
            }
        }

        /// <summary>
        /// Respond to the form closing. Wait until it's safe to close before exiting.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e) {
            Settings.Default.Save();
            if (transferUtils != null && !safeToExit) {
                transferUtils.IsErrorState = true;
                while (!safeToExit) {
                    Thread.Sleep(500);
                }
            }
        }

        /// <summary>
        /// Determine whether the source tables grid contains a specified table.
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        private bool sourceGridContainsTable(string table)
        {
            bool containsTable = false;

            if (sourceTablesGrid != null && sourceTablesGrid.Rows != null &&
                sourceTablesGrid.Rows.Count > 0)
            {

                foreach (DataGridViewRow row in sourceTablesGrid.Rows)
                {
                    if (row.Cells != null && row.Cells.Count > 2 && row.Cells[1].Value as string == table)
                    {
                        containsTable = true;
                        break;
                    }
                }

            }

            return containsTable;
        }

        /// <summary>
        /// An enumeration for data grid types.
        /// </summary>
        private enum GridType {Source, Destination};

        /// <summary>
        /// Update the contents of a data grid based on its grid type and associated database
        /// connection string.
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="connectionString"></param>
        /// <param name="type"></param>
        private void updateTablesGrid(DataGridView grid, string connectionString, GridType type)
        {
            // read the excluded tables list from the App.config
            string excludedTablesConfig = ConfigurationSettings.AppSettings["ExcludedTables"];
            string[] excludedTables = null;

            if (!string.IsNullOrWhiteSpace(excludedTablesConfig))
            {
                excludedTables = excludedTablesConfig.ToLower().Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            }

            lock (tablesLock)
            {

                // clear the grid first
                if (grid != null && grid.Rows != null)
                {
                    grid.Rows.Clear();
                }

                try
                {
                    // only operate on a valid connection string
                    if (!string.IsNullOrWhiteSpace(connectionString))
                    {

                        foreach (string[] table in transferUtils.EnumerateTables(connectionString))
                        {
                            // ignore tables specified to be excluded
                            if (excludedTables == null || !excludedTables.Contains(table[0].ToLower()))
                            {
                                if (type == GridType.Source)
                                {
                                    grid.Rows.Add(
                                        new DataGridViewCheckBoxCell() { TrueValue = true, FalseValue = false },
                                        table[0],
                                        table[1],
                                        getCustomColumnMappings(table[0]),
                                        getVirtualSelectQueryString(table[0]),
                                        ConfigurationSettings.AppSettings.AllKeys.Contains(table[0]) ?
                                        ConfigurationSettings.AppSettings[table[0]] :
                                        string.Empty
                                    );
                                }
                                else // we're operating on the destination grid
                                {
                                    // make sure the table does not already exist in the source grid
                                    if (!sourceGridContainsTable(table[0]))
                                    {
                                        string mapping = ConfigurationSettings.AppSettings["Table_Mapping_Distinct:" + table[0]] ??
                                            ConfigurationSettings.AppSettings["Table_Mapping:" + table[0]];

                                        grid.Rows.Add(
                                            new DataGridViewCheckBoxCell() { TrueValue = true, FalseValue = false },
                                            table[0],
                                            mapping,
                                            new DataGridViewCheckBoxCell() { TrueValue = true, FalseValue = false },
                                            getVirtualSelectQueryString(table[0])
                                        );
                                    }
                                }

                            }
                        }

                        // initialize the checkboxes
                        for (int index = 0; index < grid.Rows.Count; ++index)
                        {
                            DataGridViewCheckBoxCell cell = grid.Rows[index].Cells[0] as DataGridViewCheckBoxCell;
                            if (cell != null)
                            {
                                cell.TrueValue = true;
                                cell.FalseValue = false;
                                cell.Value = cell.TrueValue;
                            }
                            cell = grid.Rows[index].Cells[3] as DataGridViewCheckBoxCell;
                            if (cell != null) {
                                cell.TrueValue = true;
                                cell.FalseValue = false;
                                cell.Value = ConfigurationSettings.AppSettings.AllKeys.Contains("Table_Mapping_Distinct:" + (grid.Rows[index].Cells[1].Value as string)) ? 
                                    cell.TrueValue : cell.FalseValue;
                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    // show a generic error message at first
                    DialogResult result = MessageBox.Show("Unable to connect to " + type.ToString().ToLower() +" database. Do you wish to see more information?", 
                        "Connection Error", MessageBoxButtons.YesNo);

                    // show additional error information if the user requests it
                    if (result == System.Windows.Forms.DialogResult.Yes)
                    {
                        MessageBox.Show(ex.Message);
                    }

                }
            }
        }

        /// <summary>
        /// Retrieve the App.config values specified for the table formatted as a string.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="prefix">Currently only "Custom_Mappings" and "Virtual_Selects" are valid.</param>
        /// <param name="valuePrefix">The parsing prefix to use. 
        /// This should be null for Custom_Mappings and "{" for Virtual_Selects.</param>
        /// <param name="valueSuffix">The parsing suffix to use. 
        /// This should be null for Custom_Mappings and "}" for Virtual_Selects.</param>
        /// <returns></returns>
        private string getConfigAggregate(string tableName, string prefix, string valuePrefix, string valueSuffix)
        {
            string result = null;

            if (ConfigurationSettings.AppSettings.AllKeys.Any(k => k.StartsWith(prefix + ":" + tableName + ".")))
            {
                IDictionary<string, string> virtualColumns = new Dictionary<string, string>();

                foreach (string setting in ConfigurationSettings.AppSettings.AllKeys.Where(
                    k => k.StartsWith(prefix + ":" + tableName + ".")))
                {
                    virtualColumns.Add(setting.Replace(prefix + ":" + tableName + ".", ""),
                        ConfigurationSettings.AppSettings[setting]);
                }

                result = string.Join(",", virtualColumns.Select(c => c.Key + "=>" + valuePrefix + c.Value + valueSuffix));
            }

            return result;
        }
        /// <summary>
        /// Retrieve the string associated with the custom column mappings for a table
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        private string getCustomColumnMappings(string tableName)
        {
            return getConfigAggregate(tableName, "Custom_Mappings", null, null);
        }

        /// <summary>
        /// Retrieve the string associated with the virtual select query for a table
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        private string getVirtualSelectQueryString(string tableName)
        {
            return getConfigAggregate(tableName, "Virtual_Selects", "{", "}");
        }

        /// <summary>
        /// Update the source tables grid after the user has finished modifying the connection string
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void source_Leave(object sender, EventArgs e) {

            updateTablesGrid(sourceTablesGrid, source.Text, GridType.Source);            
        }

        /// <summary>
        /// Tweak a connection string for a transfer. This involves updating the
        /// ConnectTimeout, Timeout, and MaxPoolSize to values friendlier to a 
        /// resilient transfer.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        private string OptimizeConnectionStringForTransfer(string connectionString) {

            string optimized = connectionString;

            if (!string.IsNullOrWhiteSpace(optimized)) {
                if (optimized.Last() != ';') {
                    optimized += ";";
                }

                if (!optimized.Replace(" ", "").Contains("ConnectTimeout=")) {
                    optimized += "Connect Timeout=150;";
                }

                if (!optimized.Replace(" ", "").Contains(";Timeout=") && optimized.IndexOf("Timeout=") != 0) {
                    optimized += "Timeout=99999;";
                }

                if (!optimized.Replace(" ", "").Contains("MaxPoolSize=")) {
                    optimized += "Max Pool Size=1000;";
                }
            }

            return optimized;
        }

        /// <summary>
        /// Handle data grid errors
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tablesGrid_DataError(object sender, DataGridViewDataErrorEventArgs e) {
        }

        /// <summary>
        /// Update the transfer utility option for logging queries to match the checkbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void queryLogging_CheckedChanged(object sender, EventArgs e)
        {
            TransferUtils.QueryLoggingEnabled = queryLogging.Checked;
        }


    }
}
