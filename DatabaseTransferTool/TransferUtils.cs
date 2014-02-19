using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace DatabaseTransferTool {

    /// <summary>
    /// The transfer utility responsible for migrating data from a source database
    /// to a destination database.
    /// </summary>
    public class TransferUtils {

        /// <summary>
        /// The mode to use for the transfer.
        /// OlderThan: Only data dated before a specified timestamp will be included
        /// NewerThan: Only data dated after a specified timestamp will be included
        /// All: All data will be included
        /// </summary>
        public enum TransferMode {OlderThan, NewerThan, All};

        /// <summary>
        /// The status indicator handle for the transfer
        /// </summary>
        internal StatusIndicator Status { get; private set; }

        // the time the transfer started
        private DateTime startTime = DateTime.MinValue;

        /// <summary>
        /// The number of query threads currently running
        /// </summary>
        public int NumQueryThreads { get; private set; }

        /// <summary>
        /// Whether the transfer utility is currently in an error state
        /// </summary>
        public bool IsErrorState { get; set; }

        /// <summary>
        /// The total amount of time remaining in the transfer
        /// </summary>
        public TimeSpan TimeRemaining { get; private set; }

        /// <summary>
        /// Whether queries should be included in the logging or not. This is not
        /// thread-safe but doesn't currently need to be.
        /// </summary>
        public static bool QueryLoggingEnabled = false;

        /// <summary>
        /// Constructor
        /// </summary>
        public TransferUtils() {
            Status = new StatusIndicator();
            IsErrorState = false;
        }

        /// <summary>
        /// A reference to the table currently being transferred. Tables are transferred
        /// one at a time.
        /// </summary>
        public Table CurrentTable { get; private set; }

        /// <summary>
        /// A list of tables that have been successfully transferred.
        /// </summary>
        public List<Table> CompletedTables { get; private set; }

        /// <summary>
        /// A list of all tables to transfer agnostic to their transfer status.
        /// </summary>
        public List<Table> AllTables { get; private set; }

        /// <summary>
        /// Reset the status of the transfer utility
        /// </summary>
        public void Reset() {
            CompletedTables = new List<Table>();
            AllTables = new List<Table>();
            Status.Maximum = 1;
            Status.Minimum = 0;
            Status.Step = 1;
            Status.Value = 0;
            TimeRemaining = TimeSpan.MinValue;
            IsErrorState = false;
            NumQueryThreads = 0;
        }

        
        /// <summary>
        /// Perform the database transfer. Note that the source database should be marked as 
        /// read-only before running this to avoid new data being excluded from the transfer 
        /// once a table transfer has begun in order to ensure an internally consistent
        /// destination database.
        /// </summary>
        /// <param name="sourceConnectionString"></param>
        /// <param name="destinationConnectionString"></param>
        /// <param name="tables"></param>
        /// <param name="logFile"></param>
        /// <param name="mode"></param>
        /// <param name="beforeDate"></param>
        /// <param name="afterDate"></param>
        /// <param name="includeUndated">whether to include data that is not associated with a date or not</param>
        /// <param name="maxThreads">the maximum size of the worker threadpool</param>
        /// <param name="batchSize">the number of megabytes to include in a single batch</param>
        public void TransferTables(string sourceConnectionString, string destinationConnectionString, 
            List<Table> tables, string logFile, TransferMode mode, string beforeDate = null, 
            string afterDate = null, bool includeUndated = true,
            int maxThreads = int.MaxValue, int batchSize = 1) {

            // set the start time
            startTime = DateTime.Now;

            // reset the status
            Reset();

            AllTables = tables;

            // set the log file to use based on the timestamp and specified log directory
            Logger.LogFile = Path.Combine(logFile, 
                "Database_Transfer_" + DateTime.Now.ToString().Replace("/", "-").Replace(":", "-").Replace(" ", "_") + ".txt");


            // prepend the run settings to the log
            Logger.Log("===== Settings =====");
            Logger.Log("Maximum number of threads: " + maxThreads);
            Logger.Log("Batch size: " + batchSize + " MB");
            Logger.Log("Source: " + sourceConnectionString);
            Logger.Log("Destination: " + destinationConnectionString);
            Logger.Log("Transfer mode: " + mode.ToString());
            Logger.Log("Before date: " + beforeDate);
            Logger.Log("After date: " + afterDate);
            Logger.Log("Include nondated: " + includeUndated);
            Logger.Log("===== End Settings =====");

            // reset the progress bar
            Status.Maximum = tables.Sum(x => x.SizeInMB);
            Status.Minimum = 0;
            Status.Step = 1;
            Status.Value = 0;

            Logger.Log("Creating schema");

            // process each table
            foreach (Table table in tables) {

                CheckState();

                table.TransferStatus = new StatusIndicator() {
                    Minimum = 0,
                    Maximum = 1,
                    Value = 0,
                    Step = 1
                };
                
                CurrentTable = table;

                // Table creation is included in schema dumps and would prevent multiple iterations of date-based
                // transfers or retries after batch resizes if done here
                #region not currently used
                /*
                Logger.Log("Creating table " + (progressbar.Value + 1) + " of " + progressbar.Maximum + ": " + table);
                string tableGenerationScript = GenerateTableCreationScript(sourceConnectionString, table);
                ExecuteCommand(destinationConnectionString, tableGenerationScript);
                */
                #endregion not currently used

                // retrieve the columns
                table.Columns = EnumerateColumns(destinationConnectionString, table, TableType.Destination);

                Logger.Log("Processing table " + table + " " + table.TableOptions);

                DateTime tableStartTime = DateTime.Now;
                List<int> skips = new List<int>();


                // determine if the table will be processed
                if (table.Columns.Count > 0 && (mode == TransferMode.All || table.Columns.Where(x => x.IsDateColumn).Count() > 0 || includeUndated)) {

                    CheckState();

                    string dateColumn = null;

                    // determine what the age column will be if applicable
                    if (mode == TransferMode.NewerThan || mode == TransferMode.OlderThan) {
                        dateColumn = table.Columns.Where(x => x.IsDateColumn).OrderBy(x => x.OrdinalPosition).Select(x => x.ColumnName).FirstOrDefault();
                    }

                    table.TotalRows = DetermineTotalRows(sourceConnectionString, table, table.CustomFilter, mode, dateColumn,
                        mode == TransferMode.NewerThan ? afterDate : mode == TransferMode.OlderThan ? beforeDate : null,
                        includeUndated);

                    Logger.Log("Total rows: " + table.TotalRows + ", total size: " + table.SizeInMB + " MB");



                    // determine the batch size
                    int batch = DetermineBatchSize(table, batchSize);
                    Logger.Log("Batch size is " + batch);


                    // only process the table if it has data
                    if (table.TotalRows > 0) {

                        int effectiveBatch = batch;
                        int retries = 0;
                        bool success = false;
                        int effectiveMaxThreads = maxThreads;

                        while (!success && retries <= SafeRetry.MAX_RETRIES) {

                            // if we've been here before, wipe the table and try to start fresh
                            if (mode == TransferMode.All || retries > 0) {
                                string deleteFilter = BuildDateBasedWhereClause(includeUndated, mode == TransferMode.NewerThan ? afterDate :
                                                mode == TransferMode.OlderThan ? beforeDate : null, dateColumn, mode, table.CustomFilter);

                                DeleteTable(table.Name, destinationConnectionString, deleteFilter);
                            }

                            try {
                                
                                // prepare constructs for parallelization

                                int skip = 0;
                                table.TotalBatchesRequired = (int) Math.Ceiling((double) table.TotalRows / (double) effectiveBatch);

                                for (skip = 0; skip < table.TotalBatchesRequired; ++skip) {
                                    skips.Add(skip);
                                }

                                table.TransferStatus.Maximum = table.TotalBatchesRequired;

                                table.TableCompleted += UpdateTableCompletion;

                                CheckState();

                                // in batches, select the appropriate data and insert it (disabling triggers/constraints first)
                                int completedBatches = 0;
                                object batchLock = new object();
                                object queryCountLock = new object();

                                // process the batches in parallel
                                Parallel.ForEach(skips,
                                    new ParallelOptions { MaxDegreeOfParallelism = effectiveMaxThreads },
                                    currSkip => {

                                        lock (queryCountLock) {
                                            ++NumQueryThreads;
                                        }

                                        CheckState();

                                        // copy the batch of data from the source to the destination
                                        BulkCopy(sourceConnectionString, destinationConnectionString, table,
                                            table.CustomFilter, mode, mode == TransferMode.NewerThan ? afterDate :
                                                mode == TransferMode.OlderThan ? beforeDate : null,
                                            dateColumn, currSkip, effectiveBatch, includeUndated);

                                        int batchNum = 0;
                                        lock (batchLock) {
                                            batchNum = ++completedBatches;
                                        }

                                        Logger.Log("Transferred batch " + batchNum + " of " + skips.Count + " batches for " +
                                            table + " to destination database");

                                        // notify the table that a batch has been completed
                                        table.BatchCompleted();

                                        lock (queryCountLock) {
                                            --NumQueryThreads;
                                        }
                                    }
                                );

                                CompletedTables.Add(table);
                                table.TableCompleted -= UpdateTableCompletion;
                                success = true;

                            }
                            catch (MaxRetriesLimitExceededException) {

                                // Try to wipe the destination table of data we've newly inserted since we were unable to 
                                // successfully transfer it. This might fail if it conflicts with contraints and triggers 
                                // that exist in other tables.
                                string deleteFilter = BuildDateBasedWhereClause(includeUndated, mode == TransferMode.NewerThan ? afterDate :
                                                mode == TransferMode.OlderThan ? beforeDate : null, dateColumn, mode, table.CustomFilter);

                                DeleteTable(table.Name, destinationConnectionString, deleteFilter);

                                --effectiveBatch;
                                if (effectiveBatch < 1) {
                                    effectiveBatch = 1;
                                }

                                effectiveMaxThreads -= (int) (Math.Round((double) maxThreads / 10.0) > 0 ? 
                                    Math.Round((double) maxThreads / 10.0) : 1);
                                if (effectiveMaxThreads < Environment.ProcessorCount) {
                                    effectiveMaxThreads = Environment.ProcessorCount;
                                }

                                ++retries;
                                UndoTableCompletion(table, table.TotalBatchesCompleted);
                                Logger.Log("Deleting table and trying again with more conservative parameters");
                            }
                        }

                        if (!success) {
                            throw new MaxRetriesLimitExceededException("Failed to transfer table " + table);
                        }


                    }
                    else {
                        CompletedTables.Add(table);
                        UpdateTableCompletion(null, null);
                    }

                }
                else {
                    CompletedTables.Add(table);
                    UpdateTableCompletion(null, null);
                }


            }

            CheckState();

            // we've finished
            Status.Value = Status.Maximum;

            DateTime endTime = DateTime.Now;
            TimeSpan interval = endTime - startTime;

            Logger.Log("Total elapsed time: " + interval.ToString());
            Logger.Log("Finished");
        }

        /// <summary>
        /// Abort the transfer if the transfer utility is in an error state or has been signaled to stop
        /// </summary>
        private void CheckState() {
            if (IsErrorState) {
                throw new Exception("Error state detected, halting");
            }
        }

        /// <summary>
        /// Delete all relevant data from a table
        /// </summary>
        /// <param name="table"></param>
        /// <param name="connectionString"></param>
        /// <param name="customFilter"></param>
        private void DeleteTable(string table, string connectionString, string customFilter =  null) {

            // log before clearing
            Logger.Log("Clearing destination table " + table + (string.IsNullOrWhiteSpace(customFilter) ? "" : (" with filter: " + customFilter)));
            
            // drop the data from the table
            ExecuteCommand(connectionString, "alter table " + table + " nocheck constraint all; delete from " + table + 
                (string.IsNullOrWhiteSpace(customFilter) ? "" : (customFilter.TrimStart().StartsWith("where ") ? (" " + customFilter) : (" where " + customFilter))) + 
                "; alter table " + table + " check constraint all");
        }

        /// <summary>
        /// Undo progress made earlier as a result of a partially completed but recently failed table
        /// </summary>
        /// <param name="table"></param>
        /// <param name="numSteps"></param>
        void UndoTableCompletion(Table table, int numSteps) {
            Status.Step = (int) Math.Round((double) table.SizeInMB / (double) table.TotalBatchesRequired);

            for (int i = 0; i < numSteps; ++i) {
                Status.Value -= Status.Step;
            }

            DateTime now = DateTime.Now;
            TimeSpan totalTimeSpent = now - startTime;
            double percent = (double) Status.Value / (double) Status.Maximum;
            TimeRemaining = TimeSpan.FromMilliseconds(((double) totalTimeSpent.TotalMilliseconds /
                (100 * percent)) * (100 - (100 * percent)));
        }

        /// <summary>
        /// Update the status to reflect progress made in transferring a table
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void UpdateTableCompletion(object sender, EventArgs e) {

            try {
                Table table = sender as Table;

                // log a status update for the batch completed
                Status.Step = (int) Math.Round((double) table.SizeInMB / (double) table.TotalBatchesRequired);
                Status.PerformStep();

                DateTime now = DateTime.Now;
                TimeSpan totalTimeSpent = now - startTime;
                double percent = (double) Status.Value / (double) Status.Maximum;
                TimeRemaining = TimeSpan.FromMilliseconds(((double) totalTimeSpent.TotalMilliseconds /
                    (100 * percent)) * (100 - (100 * percent)));



                if (table.TotalBatchesCompleted == table.TotalBatchesRequired) {

                    Logger.Log("Transferred " + Status.Value + " of " + Status.Maximum + " total MB");

                    if (table != null) {
                        table.TableCompleted -= UpdateTableCompletion;
                    }

                    if (Status.Value != Status.Maximum) {

                        Logger.Log("Estimate of total time remaining: " + TimeRemaining);
                    }
                }
            }
            catch (Exception) { }

        }

        /// <summary>
        /// Generate a string of columns to be used in queries
        /// </summary>
        /// <param name="columns"></param>
        /// <returns></returns>
        private string GenerateColumnsString(IList<Column> columns) {

            string results = null;

            if (columns != null)
            {

                string[] effectiveColumns = new string[columns.Count];

                for (int i = 0; i < effectiveColumns.Count(); ++i)
                {

                    effectiveColumns[i] = columns[i].EffectiveSourceColumnName + " as " + columns[i].DestinationColumnName;

                }
                results = string.Join(", ", effectiveColumns);

            }

            return results;

        }

        /// <summary>
        /// Generate an "order by" clause to be used in a query
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="useNames">whether to use column names or indices</param>
        /// <returns></returns>
        private string GenerateOrderByString(IList<Column> columns, bool useNames = false) {

            List<string> indices = new List<string>();


            for (int i = 1; i <= columns.Count; ++i) {

                if (!columns[i - 1].IsVirtualSelectColumn)
                {
                    indices.Add(useNames ? ("\"" + columns[i - 1].SourceColumnName + "\"") : i + " asc");
                }
            }

            return string.Join(", ", indices);

        }

        /// <summary>
        /// Prepare a SQL command for execution. This sets the timeout value along with
        /// the command type and opens the connection
        /// </summary>
        /// <param name="command"></param>
        /// <param name="query"></param>
        /// <param name="type"></param>
        private void PrepareCommand(SqlCommand command, string query, System.Data.CommandType type = System.Data.CommandType.Text) {

            command.CommandText = query;
            command.CommandTimeout = Int32.MaxValue;
            command.CommandType = System.Data.CommandType.Text;
            command.Connection.Open();

        }

        /// <summary>
        /// Build a where claused based on date inclusion parameters
        /// </summary>
        /// <param name="includeUndated">whether to include data not associated with a date or not</param>
        /// <param name="cutoffDate">the date at which to include data before/after</param>
        /// <param name="dateColumn">the column to use as the data date</param>
        /// <param name="mode">the transfer mode to use</param>
        /// <param name="customFilter">the custom query filter to include</param>
        /// <returns>a SQL WHERE clause excluding the WHERE keyword</returns>
        private string BuildDateBasedWhereClause(bool includeUndated, string cutoffDate, 
            string dateColumn, TransferMode mode, string customFilter) {
            string whereClause = string.Empty;

            if (!includeUndated || (!string.IsNullOrWhiteSpace(cutoffDate) && 
                !string.IsNullOrWhiteSpace(dateColumn))) {

                switch (mode) {
                    case TransferMode.NewerThan:
                        whereClause = "and (" + dateColumn + " >= '" + cutoffDate + "'" + (includeUndated ?
                            (" or " + dateColumn + " is null") : string.Empty) + ") " + customFilter + " ";
                        break;
                    case TransferMode.OlderThan:
                        whereClause = "and (" + dateColumn + " < '" + cutoffDate + "'" + (includeUndated ?
                            (" or " + dateColumn + " is null") : string.Empty) + ") " + customFilter + " ";
                        break;
                    case TransferMode.All:
                        if (!string.IsNullOrWhiteSpace(customFilter)) {
                            whereClause = "and " + (customFilter.TrimStart().StartsWith("where ") ? 
                                customFilter.Substring(customFilter.IndexOf("where ") + 6) : customFilter) + " ";
                        }
                        break;
                    default:
                        break;
                }

            }
            else if (!string.IsNullOrWhiteSpace(customFilter) && !customFilter.TrimStart().StartsWith("where ")) {
                whereClause = " where " + customFilter;
            }

            return whereClause;
        }

        /// <summary>
        /// Execute a SQL query and return the results
        /// </summary>
        /// <param name="command"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        [SafeRetry]
        private SqlDataReader ExecuteQuery(SqlCommand command, string query) {

            PrepareCommand(command, query);
            if (QueryLoggingEnabled)
            {
                Logger.Log("Executing query: " + query);
            }
            return command.ExecuteReader();

        }

        /// <summary>
        /// Generate a query for a batch of data from a source table
        /// </summary>
        /// <param name="table">the source table</param>
        /// <param name="customFilter">the custom WHERE clause</param>
        /// <param name="mode">the transfer mode to use</param>
        /// <param name="cutoffDate">The date before/after which data will be included.
        /// Not required when the transfer mode of ALL is used.</param>
        /// <param name="dateColumn">The column to use for determining the age of data.
        /// Not required when the transfer mode of ALL is used.</param>
        /// <param name="currSkip">the number of rows to skip</param>
        /// <param name="batch">the number of rows to include in a batch</param>
        /// <param name="columns">the columns in the table</param>
        /// <param name="includeUndated">whether to include data that has no age associated with it or not</param>
        /// <returns>SQL query</returns>
        private string GenerateQuery(Table table, string customFilter, TransferMode mode, string cutoffDate,
            string dateColumn, int currSkip, int batch, IList<Column> columns, bool includeUndated) {

            // start with a basic WHERE clause
            string whereClause = BuildDateBasedWhereClause(includeUndated, cutoffDate, dateColumn, mode, customFilter);

            StringBuilder query = new StringBuilder();

            // Perform dirty reads. This is only risky of the source database is not set to read-only (which
            // should not be the case for a production-level transfer), but this can be useful when using 
            // this tool against a live database for benchmark/preparation purposes in which the internal 
            // consistency of the destination database is not important.

            query.Append("set transaction isolation level read uncommitted; ");

            // we'll use our own pagination to prevent dependencies on a specific version of SQL Server
            if (table.UseDistinctSourceSelect)
            {
                query.Append("select distinct ").Append(GenerateColumnsString(columns))
                    .Append(" from ( select top ").Append(((currSkip + 1) * batch) - (currSkip * batch))
                    .Append(" *")
                    .Append(" from (select row_number() over(order by ").Append(GenerateOrderByString(columns, true))
                    .Append(") as rownumber, ").Append(GenerateColumnsString(columns))
                    .Append(" from ").Append(table.EffectiveSourceTableName).Append(" ")
                    .Append(table.EffectiveSourceTableName.Substring(table.Name.LastIndexOf('.') + 1))
                    .Append(") nolock where rownumber > ").Append(currSkip * batch)
                    .Append(" and rownumber <= ").Append((currSkip + 1) * batch).Append(whereClause.TrimStart().StartsWith("where ") ?
                        (" and " + whereClause.Substring(whereClause.IndexOf("where ") + 6)) : whereClause)
                    .Append(" order by ").Append(GenerateOrderByString(columns))
                    .Append(") ").AppendLine(table.EffectiveSourceTableName.Substring(table.Name.LastIndexOf('.') + 1));
            }
            else
            {
                query.Append("select *")
                    .Append(" from (select row_number() over(order by ").Append(GenerateOrderByString(columns, true))
                    .Append(") as rownumber, ").Append(GenerateColumnsString(columns))
                    .Append(" from ").Append(table.EffectiveSourceTableName).Append(" ")
                    .Append(table.EffectiveSourceTableName.Substring(table.Name.LastIndexOf('.') + 1))
                    .Append(") nolock where rownumber > ").Append(currSkip * batch)
                    .Append(" and rownumber <= ").Append((currSkip + 1) * batch).Append(whereClause.TrimStart().StartsWith("where ") ?
                        (" and " + whereClause.Substring(whereClause.IndexOf("where ") + 6)) : whereClause)
                    .Append(" order by ").Append(GenerateOrderByString(columns)).AppendLine();
            }

            return query.ToString();
        }

        /// <summary>
        /// Copy a batch of data from a source database to a destination database
        /// </summary>
        /// <param name="sourceConnectionString"></param>
        /// <param name="destinationConnectionString"></param>
        /// <param name="table"></param>
        /// <param name="customFilter"></param>
        /// <param name="mode"></param>
        /// <param name="cutoffDate"></param>
        /// <param name="dateColumn"></param>
        /// <param name="currSkip"></param>
        /// <param name="batch"></param>
        /// <param name="includeUndated"></param>
        [SafeRetry]
        private void BulkCopy(string sourceConnectionString, string destinationConnectionString, Table table, string customFilter,
            TransferMode mode, string cutoffDate, string dateColumn, int currSkip, int batch, bool includeUndated) {

            using (SqlConnection connection = new SqlConnection(sourceConnectionString)) {
                using (SqlCommand command = connection.CreateCommand()) {
                    PerformBulkInsert(destinationConnectionString,
                        RetrieveRowsAsStream(command, table, customFilter,
                        mode, cutoffDate, dateColumn, currSkip, batch, includeUndated),
                        batch, table);
                }
            }   

            

        }

        /// <summary>
        /// Insert a batch of already-queried data into the destination database
        /// </summary>
        /// <param name="destinationConnectionString">the destination connection string</param>
        /// <param name="rows">the data to insert</param>
        /// <param name="batchSize">the number of rows to include in a batch</param>
        /// <param name="table">the destination table</param>
        private void PerformBulkInsert(string destinationConnectionString, SqlDataReader rows, int batchSize, Table table) {

            // the SqlBulkCopy utility makes this operation significantly faster than manually managing
            // the copy
            using (SqlBulkCopy copier = new SqlBulkCopy(destinationConnectionString,
                SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.KeepNulls |
                SqlBulkCopyOptions.UseInternalTransaction)) {

                copier.DestinationTableName = table.Name;
                copier.BulkCopyTimeout = 99999;
                copier.EnableStreaming = true;
                copier.NotifyAfter = batchSize;
                copier.BatchSize = batchSize;

                // Specify the column mapping even though we do that properly elsewhere. This is just
                // to satisfy a dependency of the bulk copier since it can fail when these are not specified.
                foreach (Column column in table.Columns)
                {
                    copier.ColumnMappings.Add(column.DestinationColumnName, column.DestinationColumnName);
                }

                // insert the rows into the destination database
                copier.WriteToServer(rows);

                copier.Close();

            }
            

        }

        /// <summary>
        /// Query the source database for a batch of data
        /// </summary>
        /// <param name="command">the source query</param>
        /// <param name="table">the source table</param>
        /// <param name="customFilter">the custom WHERE clause</param>
        /// <param name="mode">the transfer mode to use</param>
        /// <param name="cutoffDate">The date before/after which to include data. This is ignored when
        /// the transfer mode is ALL.</param>
        /// <param name="dateColumn">The column to use in determining the age of data. This is ignored
        /// when the transfer mode is ALL.</param>
        /// <param name="currSkip">the number of rows to skip</param>
        /// <param name="batch">the number of rows to include in a batch</param>
        /// <param name="includeUndated">whether to include data not associated with an age or not</param>
        /// <returns></returns>
        [SafeRetry]
        private SqlDataReader RetrieveRowsAsStream(SqlCommand command, Table table, string customFilter,
            TransferMode mode, string cutoffDate, string dateColumn, int currSkip, int batch,
            bool includeUndated) {

            // generate the query to run against the source database
            string query = GenerateQuery(table, customFilter, mode, cutoffDate, dateColumn,
                currSkip, batch, table.Columns, includeUndated);

            // run the query and return the results
            return ExecuteQuery(command, query);
        }

        /// <summary>
        /// Determing the total number of rows in a table
        /// </summary>
        /// <param name="sourceConnectionString">the connection string to the database</param>
        /// <param name="table">the source table</param>
        /// <param name="customFilter">the custom WHERE clause</param>
        /// <param name="mode">the transfer mode to use</param>
        /// <param name="dateColumn">The column to use in determining the age of data. This is ignored
        /// when the transfer mode is ALL.</param>
        /// <param name="cutoffDate">The date before/after which to include data. This is ignored when
        /// the transfer mode is ALL.</param>
        /// <param name="includeUndated">whether to include data not associated with an age or not</param>
        /// <returns></returns>
        [SafeRetry]
        private long DetermineTotalRows(string sourceConnectionString, Table table, string customFilter, TransferMode mode, 
            string dateColumn, string cutoffDate, bool includeUndated) {

            long totalRows = 0;

            // only incude relevant rows based on the supplied custom WHERE clause and dates
            string whereClause = BuildDateBasedWhereClause(includeUndated, cutoffDate, dateColumn, mode, customFilter);

            using (SqlConnection connection = new SqlConnection(sourceConnectionString)) {
                using (SqlCommand command = connection.CreateCommand()) {

                    // build the query based on the correct table name to use and the supplied WHERE clause
                    string sqlCommand = "select count(*) from " + table.EffectiveSourceTableName + " " + whereClause;

                    PrepareCommand(command, sqlCommand);

                    // run the query
                    using (SqlDataReader reader = command.ExecuteReader()) {
                        reader.Read();
                        totalRows = long.Parse(reader[0].ToString());
                    }

                }
                
            }

            return totalRows;
        }

        /// <summary>
        /// Determine the appropriate number of rows to use in a batch based on the
        /// batch size in megabytes and the average size in MB per row of the table data.
        /// </summary>
        /// <param name="table">the table for which to calcaulte a batch size</param>
        /// <param name="batchSizeMB">number of megabytes to include in a batch</param>
        /// <returns>number of rows to include in a batch</returns>
        private int DetermineBatchSize(Table table, int batchSizeMB) {

            int batchSize = 0;

            if (table.TotalRows > 0 && table.SizeInMB > 0) {

                double sizePerRow = (double) table.SizeInMB / (double) table.TotalRows;

                if (sizePerRow > 0)
                {
                    int numRowsPerBatch = (int) Math.Round((double) batchSizeMB / (double) sizePerRow);

                    batchSize = numRowsPerBatch > 0 ? numRowsPerBatch : 1;
                }
                else
                {
                    batchSize = (int) table.TotalRows;
                }
            }
            else {
                // default to a single batch for the table
                batchSize = (int) table.TotalRows;
            }

            return batchSize;
        }

        /// <summary>
        /// An enumeration for the type of table (source or destination)
        /// </summary>
        private enum TableType { Source, Destination };

        /// <summary>
        /// Retrieve the columns from a table
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="table"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        [SafeRetry]
        private IList<Column> EnumerateColumns(string connectionString, Table table, TableType type = TableType.Source) {

            // build the query
            string script = "select column_name, ordinal_position, data_type from information_schema.columns where table_name = '"
                + table.Name.Substring(table.Name.LastIndexOf(".") + 1) + "' and table_schema = '" + table.Name.Substring(0, table.Name.IndexOf(".")) + "'";

            IList<Column> columns = new List<Column>();

            using (SqlConnection connection = new SqlConnection(connectionString)) {
                using (SqlCommand command = connection.CreateCommand()) {

                    PrepareCommand(command, script);

                    // execute the query
                    using (SqlDataReader reader = command.ExecuteReader()) {

                        // create a column for each result
                        while (reader.Read()) {
                            columns.Add(new Column(reader["column_name"].ToString(), 
                                int.Parse(reader["ordinal_position"].ToString()), reader["data_type"].ToString(),
                                table.CustomColumnMappings != null && 
                                (
                                    (type == TableType.Source &&  table.CustomColumnMappings.Any(x => x.Value == reader["column_name"].ToString())) ||
                                    (type == TableType.Destination && table.CustomColumnMappings.ContainsKey(reader["column_name"].ToString()))
                                ) ?
                                table.CustomColumnMappings[reader["column_name"].ToString()] : null));
                        }

                    }

                }
                
            }

            return columns;
        }

        /// <summary>
        /// Retrieve all tables from a database ordered by their size
        /// from largest to smallest
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public string[][] EnumerateTables(string connectionString) {


            string script = @"select table_schema, table_name, MB 
from information_schema.tables a
	left outer join 
	(
		select sys.objects.name, (sum(reserved_page_count) * 8.0 / 1024 ) as MB
		from sys.dm_db_partition_stats, sys.objects 
		where sys.dm_db_partition_stats.object_id = sys.objects.object_id 
		group by sys.objects.name
	) b on a.table_name = name
where table_type = 'BASE TABLE' order by MB desc";


            List<string[]> tables = new List<string[]>();

            using (SqlConnection connection = new SqlConnection(connectionString)) {
                using (SqlCommand command = connection.CreateCommand()) {

                    PrepareCommand(command, script);

                    using (SqlDataReader reader = command.ExecuteReader()) {

                        while (reader.Read()) {
                            // Currently custom WHERE filters/etc are not incorporated into the size
                            // determination. Not sure if this is actually desirable or not, so leaving
                            // them out for now.
                            tables.Add(new string[] {reader["table_schema"] + "." + reader["table_name"], 
                                reader["MB"].ToString()});
                        }

                    }

                }
                
            }

            return tables.ToArray();
        }

        /// <summary>
        /// Execute a SQL command against a specified database
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="command"></param>
        [SafeRetry]
        internal void ExecuteCommand(string connectionString, string command) {

            using (SqlConnection connection = new SqlConnection(connectionString)) {
                using (SqlCommand sqlCommand = connection.CreateCommand()) {

                    PrepareCommand(sqlCommand, command);
                    if (QueryLoggingEnabled)
                    {
                        Logger.Log("Executing command: " + command);
                    }
                    sqlCommand.ExecuteNonQuery();
                }
                
            }

        }

        // The following code is currently deprecated since table creation and deletion is
        // no longer performed by this utility.
        #region not currently used
        /*
        [SafeRetry]
        private string GenerateTableCreationScript(string sourceConnectionString, string table) {

            // http://stackoverflow.com/questions/706664/generate-sql-create-scripts-for-existing-tables-with-query
            string script = @"DECLARE @table_name SYSNAME
SELECT @table_name = '" + table + @"'

DECLARE 
      @object_name SYSNAME
    , @object_id INT

SELECT 
      @object_name = '[' + s.name + '].[' + o.name + ']'
    , @object_id = o.[object_id]
FROM sys.objects o WITH (NOWAIT)
JOIN sys.schemas s WITH (NOWAIT) ON o.[schema_id] = s.[schema_id]
WHERE s.name + '.' + o.name = @table_name
    AND o.[type] = 'U'
    AND o.is_ms_shipped = 0

DECLARE @SQL NVARCHAR(MAX) = ''

;WITH index_column AS 
(
    SELECT 
          ic.[object_id]
        , ic.index_id
        , ic.is_descending_key
        , ic.is_included_column
        , c.name
    FROM sys.index_columns ic WITH (NOWAIT)
    JOIN sys.columns c WITH (NOWAIT) ON ic.[object_id] = c.[object_id] AND ic.column_id = c.column_id
    WHERE ic.[object_id] = @object_id
),
fk_columns AS 
(
     SELECT 
          k.constraint_object_id
        , cname = c.name
        , rcname = rc.name
    FROM sys.foreign_key_columns k WITH (NOWAIT)
    JOIN sys.columns rc WITH (NOWAIT) ON rc.[object_id] = k.referenced_object_id AND rc.column_id = k.referenced_column_id 
    JOIN sys.columns c WITH (NOWAIT) ON c.[object_id] = k.parent_object_id AND c.column_id = k.parent_column_id
    WHERE k.parent_object_id = @object_id
)
SELECT @SQL = 'CREATE TABLE ' + @object_name + CHAR(13) + '(' + CHAR(13) + STUFF((
    SELECT CHAR(9) + ', [' + c.name + '] ' + 
        CASE WHEN c.is_computed = 1
            THEN 'AS ' + cc.[definition] 
            ELSE UPPER(tp.name) + 
                CASE WHEN tp.name IN ('varchar', 'char', 'varbinary', 'binary', 'text')
                       THEN '(' + CASE WHEN c.max_length = -1 THEN 'MAX' ELSE CAST(c.max_length AS VARCHAR(5)) END + ')'
                     WHEN tp.name IN ('nvarchar', 'nchar', 'ntext')
                       THEN '(' + CASE WHEN c.max_length = -1 THEN 'MAX' ELSE CAST(c.max_length / 2 AS VARCHAR(5)) END + ')'
                     WHEN tp.name IN ('datetime2', 'time2', 'datetimeoffset') 
                       THEN '(' + CAST(c.scale AS VARCHAR(5)) + ')'
                     WHEN tp.name = 'decimal' 
                       THEN '(' + CAST(c.[precision] AS VARCHAR(5)) + ',' + CAST(c.scale AS VARCHAR(5)) + ')'
                    ELSE ''
                END +
                CASE WHEN c.collation_name IS NOT NULL THEN ' COLLATE ' + c.collation_name ELSE '' END +
                CASE WHEN c.is_nullable = 1 THEN ' NULL' ELSE ' NOT NULL' END +
                CASE WHEN dc.[definition] IS NOT NULL THEN ' DEFAULT' + dc.[definition] ELSE '' END + 
                CASE WHEN ic.is_identity = 1 THEN ' IDENTITY(' + CAST(ISNULL(ic.seed_value, '0') AS CHAR(1)) + ',' + CAST(ISNULL(ic.increment_value, '1') AS CHAR(1)) + ')' ELSE '' END 
        END + CHAR(13)
    FROM sys.columns c WITH (NOWAIT)
    JOIN sys.types tp WITH (NOWAIT) ON c.user_type_id = tp.user_type_id
    LEFT JOIN sys.computed_columns cc WITH (NOWAIT) ON c.[object_id] = cc.[object_id] AND c.column_id = cc.column_id
    LEFT JOIN sys.default_constraints dc WITH (NOWAIT) ON c.default_object_id != 0 AND c.[object_id] = dc.parent_object_id AND c.column_id = dc.parent_column_id
    LEFT JOIN sys.identity_columns ic WITH (NOWAIT) ON c.is_identity = 1 AND c.[object_id] = ic.[object_id] AND c.column_id = ic.column_id
    WHERE c.[object_id] = @object_id
    ORDER BY c.column_id
    FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, CHAR(9) + ' ')
    + ISNULL((SELECT CHAR(9) + ', CONSTRAINT [' + k.name + '] PRIMARY KEY (' + 
                    (SELECT STUFF((
                         SELECT ', [' + c.name + '] ' + CASE WHEN ic.is_descending_key = 1 THEN 'DESC' ELSE 'ASC' END
                         FROM sys.index_columns ic WITH (NOWAIT)
                         JOIN sys.columns c WITH (NOWAIT) ON c.[object_id] = ic.[object_id] AND c.column_id = ic.column_id
                         WHERE ic.is_included_column = 0
                             AND ic.[object_id] = k.parent_object_id 
                             AND ic.index_id = k.unique_index_id     
                         FOR XML PATH(N''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, ''))
            + ')' + CHAR(13)
            FROM sys.key_constraints k WITH (NOWAIT)
            WHERE k.parent_object_id = @object_id 
                AND k.[type] = 'PK'), '') + ')'  + CHAR(13)
    + ISNULL((SELECT (
        SELECT CHAR(13) +
             'ALTER TABLE ' + @object_name + ' WITH' 
            + CASE WHEN fk.is_not_trusted = 1 
                THEN ' NOCHECK' 
                ELSE ' CHECK' 
              END + 
              ' ADD CONSTRAINT [' + fk.name  + '] FOREIGN KEY(' 
              + STUFF((
                SELECT ', [' + k.cname + ']'
                FROM fk_columns k
                WHERE k.constraint_object_id = fk.[object_id]
                FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '')
               + ')' +
              ' REFERENCES [' + SCHEMA_NAME(ro.[schema_id]) + '].[' + ro.name + '] ('
              + STUFF((
                SELECT ', [' + k.rcname + ']'
                FROM fk_columns k
                WHERE k.constraint_object_id = fk.[object_id]
                FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '')
               + ')'
            + CASE 
                WHEN fk.delete_referential_action = 1 THEN ' ON DELETE CASCADE' 
                WHEN fk.delete_referential_action = 2 THEN ' ON DELETE SET NULL'
                WHEN fk.delete_referential_action = 3 THEN ' ON DELETE SET DEFAULT' 
                ELSE '' 
              END
            + CASE 
                WHEN fk.update_referential_action = 1 THEN ' ON UPDATE CASCADE'
                WHEN fk.update_referential_action = 2 THEN ' ON UPDATE SET NULL'
                WHEN fk.update_referential_action = 3 THEN ' ON UPDATE SET DEFAULT'  
                ELSE '' 
              END 
            + CHAR(13) + 'ALTER TABLE ' + @object_name + ' CHECK CONSTRAINT [' + fk.name  + ']' + CHAR(13)
        FROM sys.foreign_keys fk WITH (NOWAIT)
        JOIN sys.objects ro WITH (NOWAIT) ON ro.[object_id] = fk.referenced_object_id
        WHERE fk.parent_object_id = @object_id
        FOR XML PATH(N''), TYPE).value('.', 'NVARCHAR(MAX)')), '')
    + ISNULL(((SELECT
         CHAR(13) + 'CREATE' + CASE WHEN i.is_unique = 1 THEN ' UNIQUE' ELSE '' END 
                + ' NONCLUSTERED INDEX [' + i.name + '] ON ' + @object_name + ' (' +
                STUFF((
                SELECT ', [' + c.name + ']' + CASE WHEN c.is_descending_key = 1 THEN ' DESC' ELSE ' ASC' END
                FROM index_column c
                WHERE c.is_included_column = 0
                    AND c.index_id = i.index_id
                FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '') + ')'  
                + ISNULL(CHAR(13) + 'INCLUDE (' + 
                    STUFF((
                    SELECT ', [' + c.name + ']'
                    FROM index_column c
                    WHERE c.is_included_column = 1
                        AND c.index_id = i.index_id
                    FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '') + ')', '')  + CHAR(13)
        FROM sys.indexes i WITH (NOWAIT)
        WHERE i.[object_id] = @object_id
            AND i.is_primary_key = 0
            AND i.[type] = 2
        FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)')
    ), '')

select @SQL as CreationScript";



            string outputScript = null;

            using (SqlConnection connection = new SqlConnection(sourceConnectionString)) {
                using (SqlCommand command = connection.CreateCommand()) {

                    PrepareCommand(command, script);

                    using (SqlDataReader reader = command.ExecuteReader()) {
                        reader.Read();
                        outputScript = reader["CreationScript"].ToString();
                    }

                }
                
            }

            return outputScript;
        }
        */
        #endregion not currently used


    }

}
