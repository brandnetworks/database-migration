using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseTransferTool {


    /// <summary>
    /// A class representing a database table.
    /// </summary>
    public class Table {

        /// <summary>
        /// The number of batches that will be required in order to transfer this table
        /// </summary>
        public int TotalBatchesRequired { get; set; }

        /// <summary>
        /// The fully-qualified name of this table
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The current number of batches transfered
        /// </summary>
        public int TotalBatchesCompleted { get; private set; }

        /// <summary>
        /// The number of rows the table has
        /// </summary>
        public long TotalRows { get; set; }

        /// <summary>
        /// Determine whether the table has been fully transferred or not
        /// </summary>
        public bool IsCompleted {get {return TotalBatchesCompleted == TotalBatchesRequired;}}

        public event EventHandler TableCompleted;

        /// <summary>
        /// A string containing any user-specified select filter (i.e. custom where clauses)
        /// to be used in querying against the table
        /// </summary>
        public string CustomFilter { get; set; }

        /// <summary>
        /// The total space in MB consumed by the table in its source database
        /// </summary>
        public int SizeInMB { get; set; }

        /// <summary>
        /// The status indicator associated with the transfer of this table
        /// </summary>
        public StatusIndicator TransferStatus { get; set; }

        /// <summary>
        /// The table name to use when querying against the source database. This is usually
        /// the same as the table name except when the table has changed names between databases
        /// or the source table is only used as a base for the destination table.
        /// </summary>
        public string EffectiveSourceTableName { get { return PreviousTableName ?? Name; } }

        /// <summary>
        /// Denotes whether the source database contains this table or not.
        /// </summary>
        public bool IsSourceTable { get; set; }

        /// <summary>
        /// A data structure specifying any custom mappings between source
        /// and destination columns.
        /// key: source column name, value: destination column name
        /// </summary>
        public IDictionary<string, string> CustomColumnMappings = null;

        /// <summary>
        /// A data structure specifying any virtual select columns. Virtual
        /// select columns are columns present in the destination database but
        /// not in the source database that can be populated by queries against
        /// the source database.
        /// key: source virtual column name, value: supporting query
        /// </summary>
        public IDictionary<string, string> VirtualSelectColumns = null;

        /// <summary>
        /// The name of the source table mapped to this destination table. This will be
        /// null except when the table has changed between source and destination schemas
        /// or when the source table is only used as a base for the destination table.
        /// </summary>
        public string PreviousTableName { get; set; }

        /// <summary>
        /// Specifies whether the data queries against the source table will return only distinct
        /// results or not. False by default. This will normally only be true if a new destination
        /// table acts as a join between portions of old tables and redundant join entries are
        /// not desired.
        /// </summary>
        public bool UseDistinctSourceSelect { get; set; }

        private IList<Column> _columns = null;

        /// <summary>
        /// The columns associated with this table
        /// </summary>
        internal IList<Column> Columns { 
            get {
                return _columns;
            }
            set
            {
                _columns = value;

                if (VirtualSelectColumns != null)
                {
                    foreach (string column in VirtualSelectColumns.Keys)
                    {
                        if (!_columns.Any(c => c.ColumnName.ToLower() == column.ToLower()))
                        {
                            _columns.Add(new Column(column, _columns.Max(c => c.OrdinalPosition) + 1, null));
                        }
                    }

                    foreach (Column column in _columns)
                    {
                        if (VirtualSelectColumns.Keys.Any(c => c.ToLower() == column.ColumnName.ToLower()))
                        {
                            foreach (KeyValuePair<string, string> mapping in VirtualSelectColumns)
                            {
                                if (mapping.Key.ToLower() == column.ColumnName.ToLower())
                                {
                                    column.IsVirtualSelectColumn = true;
                                    column.SupportingSelectQuery = VirtualSelectColumns[mapping.Key];
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// A helper attribute for formatting the options applied to the transfer of this table as a string
        /// </summary>
        public string TableOptions { 
            get { 
                return string.Join(string.Empty, 
                    new string[] { 
                        !string.IsNullOrWhiteSpace(CustomFilter) ? ("; custom filter is " + CustomFilter) : null,
                        CustomColumnMappings != null && CustomColumnMappings.Count > 0 ? 
                            ("; custom column mappings are " + (string.Join(",", CustomColumnMappings
                            .Select(x => "'" + x.Key + "' => '" + x.Value + "'" ).ToList()))) : 
                            null
                    }
                ); 
            } 
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="batchesRequired">The number of batches that will be required in order to transfer this table</param>
        /// <param name="name">The name of the table in the destination database</param>
        /// <param name="customFilter"></param>
        /// <param name="sizeInMB">The total space in MB consumed by the table in its source database</param>
        /// <param name="customColumnMappings">Custom mappings between source and destination columns.</param>
        /// <param name="isSourceTable">Whether this table exists in the source database or not</param>
        /// <param name="virtualSelectColumns">Virtual select columns are columns present in the destination database but
        /// not in the source database that can be populated by queries against the source database.</param>
        /// <param name="previousTableName">The name of the source table mapped to this destination table.</param>
        /// <param name="useDistinctSourceSelect">Specifies whether the data queries against the source table will return only distinct
        /// results or not.</param>
        public Table(int batchesRequired, string name, string customFilter, int sizeInMB, 
            IDictionary<string,string> customColumnMappings = null, bool isSourceTable = true,
            IDictionary<string, string> virtualSelectColumns = null, string previousTableName = null,
            bool useDistinctSourceSelect = false) {

            TotalBatchesRequired = batchesRequired;
            TotalBatchesCompleted = 0;
            Name = name;
            CustomFilter = customFilter;
            SizeInMB = sizeInMB;

            CustomColumnMappings = customColumnMappings ?? new Dictionary<string, string>();
            VirtualSelectColumns = virtualSelectColumns ?? new Dictionary<string, string>();

            IsSourceTable = isSourceTable;

            PreviousTableName = previousTableName;
            UseDistinctSourceSelect = useDistinctSourceSelect;
        }

        /// <summary>
        /// Perform the status updates required when a batch is completed
        /// </summary>
        public void BatchCompleted() {

            ++TotalBatchesCompleted;
            TransferStatus.PerformStep();

            if (TableCompleted != null) {
                TableCompleted(this, null);
            }
            
        }

        /// <summary>
        /// Return the table as a string. This currently returns the table name.
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return Name;
        }

    }
}
