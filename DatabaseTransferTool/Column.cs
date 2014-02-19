using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseTransferTool {

    /// <summary>
    /// A class representing a column in a table
    /// </summary>
    internal class Column {

        /// <summary>
        /// The name of the column in the destination database
        /// </summary>
        public string ColumnName { get; private set; }

        /// <summary>
        /// The 1-based index of this column. This is currently only used in building
        /// the "order by" portion of queries. It obviously needs to be unique with
        /// respect to its associated table to prevent query errors or double
        /// inclusions/exclusions in query pagination.
        /// </summary>
        public int OrdinalPosition { get; private set; }

        /// <summary>
        /// The RDBMS data type of the column
        /// </summary>
        public string DataType { get; private set; }

        /// <summary>
        /// A helper method for determining whether the column is a date column or not
        /// </summary>
        public bool IsDateColumn { get { return DataType != null && DataType.ToLower() == "datetime"; } }

        /// <summary>
        /// Specifies any custom mapping between source and destination columns. This will
        /// be null except when a column name has changed between source and destination
        /// schemas or sometimes when virtual selects are used; in which case it will be
        /// the name of the column in the source database.
        /// </summary>
        public string CustomMapping { get; private set; }

        /// <summary>
        /// The name of this column in the destination database. This is simply an
        /// alias for ColumnName provided for added clarity.
        /// </summary>
        public string DestinationColumnName { get { return ColumnName; } }

        /// <summary>
        /// Denotes whether the column is populated from a single column in the source
        /// table or from a query against the source schema.
        /// </summary>
        public bool IsVirtualSelectColumn { get; set; }

        /// <summary>
        /// Specifies any custom query that is used in place of a column name to populate
        /// the data in this column in the destination database from source data
        /// </summary>
        public string SupportingSelectQuery { get; set; }

        /// <summary>
        /// The name of this column in the source database. This will always return
        /// the ColumnName except when a CustomMapping is specified.
        /// </summary>
        public string SourceColumnName { get { return CustomMapping ?? ColumnName; } }

        /// <summary>
        /// Generate and return the properly formatted SQL statement by which to refer to this column
        /// when querying against the source database. This can be as simple as the column name or as
        /// complex as a nested query depending on the data type of the column and the values of
        /// CustomMapping, ColumnName, and SupportingSelectQuery.
        /// </summary>
        public string EffectiveSourceColumnName
        {
            get
            {
                string _columnName = IsVirtualSelectColumn ? ("(" + SupportingSelectQuery + ")")
                    : ("[" + (CustomMapping ?? ColumnName).Replace("[", "").Replace("]", "") + "]");

                return string.IsNullOrWhiteSpace(ColumnName) ? null :
                    (IsDateColumn ? ("convert(VARCHAR, " + _columnName + ", 25)") : _columnName);
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="columnName">The name of the column in the destination database</param>
        /// <param name="ordinalPosition">The 1-based index of this column</param>
        /// <param name="dataType">The RDBMS data type of the column</param>
        /// <param name="customMapping">Specifies any custom mapping between source and destination columns</param>
        public Column(string columnName, int ordinalPosition, string dataType, string customMapping = null) {

            ColumnName = columnName;
            OrdinalPosition = ordinalPosition;
            DataType = dataType;
            CustomMapping = customMapping;
        }

        /// <summary>
        /// Return the column as a string. Currently returns the ColumnName.
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return ColumnName;
        }
    }
}
