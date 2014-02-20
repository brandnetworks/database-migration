database-migration
==================

Purpose and Background
----------------------
The database migration tool was initially developed to support the migration of a client's SQL Server databases from Azure into Rackspace. There are a few traditional approaches to this challenge that work well with small amounts of data:

* Exporting the databases to bacpac files within Azure and then importing those files into SQL Server Management Studio
* Using the database migration wizard [here](http://msdn.microsoft.com/en-us/library/windowsazure/jj156144.aspx)
* Other tools using a combination of approaches similar to those above

Because of the amount of data in the database, we found several severe scaling issues with all of the above approaches:

* Some routinely encountered memory exhaustion issues which would crash the transfer process
* Azure would at times refuse to export the databases to bacpac files or fail after several hours
* Azure's random transport errors, throttling, timeouts, and connection severing would cause transfer/import tools to crash or fail
* All required the entire database, or at least all tables that were to be included, to be transferred at once (problematic when trying to minimize the downtime window during the transfer of a large database)
* None provided an ETA or logged the total time consumed (which we required in order to properly schedule our migration window)

Shortly after that migration was completed successfully using this database migration tool, another team needed to migrate its client's production data from an existing database to a new production database with a new, significantly different schema matching its new data model. Similar challenges were faced in that transfer along with new challenges in populating destination tables from more complicated queries against a variety of source tables.

As a result, a new transfer tool was developed with these features:

* Automatic, randomly staggered retry logic for transactions that fail as a result of transport issues or other exceptions
* Initial batching and thread pooling based on user-configured parameters to remain within service provider, RDBMS, network, and local machine constraints
* Automatic live batch and thread pool resizing at a tabular level based on the original parameters and the number of retries attempted
* Incorporation of custom query filters to allow incremental table transfers (i.e. first transfer includes all data older than a month while the next includes only new data)
* Options for automatic query filters to allow "dated" data to be incrementally transferred
* Live ETA and total time consumption metrics along with separate logging
* Ability to specify which tables and which portions of those tables should be included in a transfer via inclusion of custom where clauses
* Ability to specify custom mapping between source and destination tables and columns
* Ability to specify custom virtual select queries that are used to populate destination columns from source data
* Ability to specify whether the queries against the source tables should return distinct results or not
* Support for all variants of SQL Server (i.e. standard, web, 2008, 2012, etc)
* Live event logging

The tool is still a work in progress but is stable enough to reliably use and has already been used for multiple production migrations.

Usage Instructions
------------------

1. Make sure your destination database is exists and its schema has been created. To do this, create a schema-only backup within SQL Server Management Studio of the source database and run the schema creation script against the destination database if you are just copying the database without making schema changes. If you are making schema changes, make sure you run your database scripts against the destination database first. Also make sure there exists a valid user with sufficient privileges to log in and modify data. You can do this by including logins and object permissions in the schema-only backup.
2. Check out the solution, build it, and run it.
3. Note that if you wish to have the same configuration options persist across application runs, you should modify the [App.config](https://github.com/brandnetworks/database-migration/blob/master/DatabaseTransferTool/App.config) as you progress through the following steps; otherwise you will need to manually fill out the fields before each run except for the source and destination connection strings which are automatically filled in based on the last ones used if they are not specified in the [App.config](https://github.com/brandnetworks/database-migration/blob/master/DatabaseTransferTool/App.config).
4. You will be greeted with the main window of the application:
![image](https://raw2.github.com/brandnetworks/database-migration/master/readme%20images/DTT1.png)
5. Fill out the connection strings to match your databases. The connection strings can likely be directly copied from your codebase (if any). Additional parameters will be automatically added as needed later to optimize the connection strings for the transfer and prevent unnecessary timeouts/failures.
6. You should be able to leave the thread pool and batch sizes as they are, but you might be able to bump them up if you are running on a reasonably powerful machine and are not dealing with connection throttling/etc; however, the defaults were experimentally determined to be the best for transfers from Azure, so if you are transferring from another service with even higher volatility, you might have to experimentally decrease these values. Retry logic will automatically optimize them over time, but it is faster to start with good values rather than go through a number of failures during a transfer before arriving at appropriate parameters.
7. Fill out the log path to point to the directory you want the log files to be stored. A new log file will be created each time the application runs and any parent directories that do not yet exist will be created.
8. Once the connection strings are specified, the source and destination tables view will be populated with all the tables in the source database (except for the tables marked for exclusion in the [App.config](https://github.com/brandnetworks/database-migration/blob/master/DatabaseTransferTool/App.config)) ordered by their size along with all the tables in the destination database that don't already exist in the source database. These additional tables can be useful for newly created join tables in the destination database.
9. Select the tables you want to transfer. By default, all tables are checked, but you can decide to transfer only certain tables by leaving only those tables checked. If you want to always exclude certain tables, add their fully-qualified name as it is displayed to the [App.config](https://github.com/brandnetworks/database-migration/blob/master/DatabaseTransferTool/App.config) ExcludedTables entity (comma separated).
10. If you are not transferring the entire table at once, you can choose to enter the query filters for the source tables (the column is out of view in the above screenshot but could be seen by scrolling the source tables grid to the right). For instance, if you want to transfer only System_Log entries with a Timestamp > '1-1-13', you would set the filter for that table to ```Timestamp > '1-1-13'```. The filter will be directly inserted into a where clause, so nested queries within a filter like ```LogId not in (select LogId from SomeTable where Condition)``` are valid. Since it can be cumbersome to set the same filters for the same tables each time you run the application (you may retry a few times to debug connection strings or tweak parameters, for instance), you can also specify the filters to apply to each table in the [App.config](https://github.com/brandnetworks/database-migration/blob/master/DatabaseTransferTool/App.config) (see the example contained in that file). The application will automatically load any filters from that file for the appropriate tables.
11. If you are not transferring the entire table at once but do not want to manually enter query filters, you can select to transfer only data newer/older than a particular date and specify the behavior when data without a date is encountered. The "Newer Than" option is equivalent to ">=" and the "Older Than" option is equivalent to "<", so running one after the other will always result in a complete transfer so long as the "Include Non-Dated Tables" box is checked during one of the transfers (this also applies to rows within a dated table that have only null date values). Note that any data already existing in the destination tables within the range of data being copied will be cleared at the start of the table's transfer.
12. If a column name has changed between a table in the source schema and the corresponding table in the destination schema, you can specify custom column mappings for the table as shown below with the syntax ```NewColumnName=>OldColumnName,NewColumnName1=>OldColumnName1,...``` where NewColumnName is the name of the column in the destination database and OldColumnName is the name of the column in the source database. Custom column mappings can also be specified in the [App.config](https://github.com/brandnetworks/database-migration/blob/master/DatabaseTransferTool/App.config); the process for doing so is detailed in the default [App.config](https://github.com/brandnetworks/database-migration/blob/master/DatabaseTransferTool/App.config) you will receive when you check out the project.
13. If you happen to be migrating data between a source table and destination table with backwards-compatible schema changes that cannot be resolved by column mapping (i.e. new, resolvable columns added to the destination table's schema), you can account for these changes with virtual select columns. A virtual select column in this sense is a column that exists only in the destination schema (not the source schema) but will be added into the queries against the source schema and populated with the results of a user-specified query. These queries can be as simple as ```null```/```convert(somedatatype, null)```/```someOtherColumn``` or as complicated as ```select min(WishListId) from Schema.Wishlist w where w.UserId = wishlist.UserId```. It should be noted that the table queried against is labeled with its name in the query, so in the previous example, it was valid to refer to the UserId of "wishlist" since that virtual select query was specified for the Wishlist table. It is simpler to specify these queries in the [App.config](https://github.com/brandnetworks/database-migration/blob/master/DatabaseTransferTool/App.config) (documentation is included), but they can also be specified in the table grid directly using the following format: ```DestinationColumnName=>{some query to resolve it},OtherDestinationColumnName=>{other query},...```.
14. If you need account for tables that have been renamed between the source and destination database, find the table in the destination tables grid and enter the name from its source schema counterpart into the Source Table Mapping field.
15. If you need to account for tables that are newly created in the destination schema but can be populated from source data, you can use a combination of the above techniques. First, specify a base source table to query from (i.e. Source Table Mapping). Next, specify how the columns for each row will be populated (i.e. Virtual Select Columns). For something like a join table, you may need to indicate that only distinct results are to be returned from the source queries to avoid issues with conflicting/duplicate primary keys in the destination inserts.
16. If you have already attempted transfers and run into errors, you may wish to enable query logging to determine if any failures are caused by queries as opposed to transport or other errors.
17. At this point you are ready to begin your transfer. You may wish to set the source database to read-only since the transfer tool uses dirty reads against the source database and leaving the source database in read-write mode could result in an internally inconsistent  data.
![image](https://raw2.github.com/brandnetworks/database-migration/master/readme%20images/MainWindow.png)
18. Click "Begin Transfer".You will be able to monitor the progress of the transfer from both the application and the logs. While the specified log file will be written to, a log viewer will also be displayed during the transfer.
The current, recent, and overall health metrics indicate the query success rates over different intervals. Green means there has been a <= 5% failure rate, yellow/orange means there has been a <= 66.67% failure rate, and red means  there has been a 67 - 100% failure rate. The current health operates across just the last query, the recent health operates across the last 5 queries, and the overall health operates over all queries.
The time spent and time required metrics are updated every half second, and the time remaining metric is updated whenever progress is made. This metric is based on the total time spent so far on the total amount of data transferred with respect to the amount of data left to transfer. It should be noted that this estimate is as accurate as possible based on the known information, but it can error in the following manners: 1) the estimate may be low near the beginning of a transfer since the tables are transferred in order of size descending (larger tables first) and larger tables transfer more efficiently than smaller ones due to a smaller proportion of overhead, and 2) the estimate may be high near the end of the transfer since smaller tables transfer much more quickly, especially when the sum of the table data does not completely fill a batch.
The following is an example of a successful transfer log. The first section reports the configuration used and the rest indicates actions taken by the tool and their results along with some status metrics.

>[ 10/16/2013 4:07:03 PM ] ===== Settings =====

>[ 10/16/2013 4:07:03 PM ] Maximum number of threads: 12

>[ 10/16/2013 4:07:03 PM ] Batch size: 15 MB

>[ 10/16/2013 4:07:03 PM ] Source: Server=tcp:...,1433;Database=...;User ID=...;Password=...;Trusted_Connection=False;Encrypt=True;Connect Timeout=150;Max Pool Size=1000;Timeout=99999;

>[ 10/16/2013 4:07:03 PM ] Destination: Server=tcp:...,1433;Database=...;User ID=...;Password=...;Connect Timeout=150;Max Pool Size=1000;Timeout=99999

>[ 10/16/2013 4:07:03 PM ] Transfer mode: All

>[ 10/16/2013 4:07:03 PM ] Before date: 9-1-13

>[ 10/16/2013 4:07:03 PM ] After date: 9-1-13

>[ 10/16/2013 4:07:03 PM ] Check existence: False

>[ 10/16/2013 4:07:03 PM ] Include nondated: True

>[ 10/16/2013 4:07:03 PM ] ===== End Settings =====

>[ 10/16/2013 4:07:03 PM ] Creating schema

>[ 10/16/2013 4:07:03 PM ] Processing table dbo.[...]; custom filter is Id in (select distinct Id from [...].Items where Status in ('A', 'B')

>[ 10/16/2013 4:07:04 PM ] Total rows: 48836, total size: 10 MB

>[ 10/16/2013 4:07:04 PM ] Batch size is 73254

>[ 10/16/2013 4:07:04 PM ] Clearing table dbo.[...] with filter: Id in (select distinct Id from [...].Items where Status in ('A', 'B')

>[ 10/16/2013 4:07:14 PM ] Transferred batch 1 of 1 batches for dbo.[...]to destination database

>[ 10/16/2013 4:07:14 PM ] Transferred 10 of 56 total MB

>[ 10/16/2013 4:07:14 PM ] Estimate of total time remaining: 00:00:49.0570000

>[ 10/16/2013 4:07:14 PM ] Processing table dbo.[...]

>[ 10/16/2013 4:07:14 PM ] Total rows: 119911, total size: 8 MB

>[ 10/16/2013 4:07:14 PM ] Batch size is 224833

>[ 10/16/2013 4:07:14 PM ] Clearing table dbo.[...]

>[ 10/16/2013 4:07:19 PM ] Transferred batch 1 of 1 batches for dbo.[...] to destination database

>[ 10/16/2013 4:07:19 PM ] Transferred 18 of 56 total MB

>[ 10/16/2013 4:07:19 PM ] Estimate of total time remaining: 00:00:33.7370000

>[ 10/16/2013 4:07:19 PM ] Processing table dbo.[...]

>[ 10/16/2013 4:07:19 PM ] Total rows: 4564, total size: 7 MB

>[ 10/16/2013 4:07:19 PM ] Batch size is 9780

>[ 10/16/2013 4:07:19 PM ] Clearing table dbo.[...]

>[ 10/16/2013 4:07:25 PM ] Transferred batch 1 of 1 batches for dbo.[...]to destination database

>[ 10/16/2013 4:07:25 PM ] Transferred 25 of 56 total MB

>[ 10/16/2013 4:07:25 PM ] Estimate of total time remaining: 00:00:26.9980000

>[ 10/16/2013 4:07:25 PM ] Processing table dbo.[...]

>[ 10/16/2013 4:07:25 PM ] Total rows: 64649, total size: 7 MB

>[ 10/16/2013 4:07:25 PM ] Batch size is 138534

>[ 10/16/2013 4:07:25 PM ] Clearing table dbo.[...]

>[ 10/16/2013 4:07:28 PM ] Transferred batch 1 of 1 batches for dbo.[...]to destination database

>[ 10/16/2013 4:07:28 PM ] Transferred 32 of 56 total MB

>[ 10/16/2013 4:07:28 PM ] Estimate of total time remaining: 00:00:18.7750000

>[ 10/16/2013 4:07:28 PM ] Processing table dbo.[...]

>[ 10/16/2013 4:07:28 PM ] Total rows: 23540, total size: 6 MB

>[ 10/16/2013 4:07:28 PM ] Batch size is 58850

>[ 10/16/2013 4:07:28 PM ] Clearing table dbo.[...]

>[ 10/16/2013 4:07:37 PM ] Transferred batch 1 of 1 batches for dbo.[...]to destination database

>[ 10/16/2013 4:07:37 PM ] Transferred 38 of 56 total MB

>[ 10/16/2013 4:07:37 PM ] Estimate of total time remaining: 00:00:15.8270000

>[ 10/16/2013 4:07:37 PM ] Processing table dbo.[...]

>[ 10/16/2013 4:07:37 PM ] Total rows: 175508, total size: 6 MB

>[ 10/16/2013 4:07:37 PM ] Batch size is 438770

>[ 10/16/2013 4:07:37 PM ] Clearing table dbo.[...]

>[ 10/16/2013 4:07:40 PM ] Transferred batch 1 of 1 batches for dbo.[...] to destination database

>[ 10/16/2013 4:07:40 PM ] Transferred 44 of 56 total MB

>[ 10/16/2013 4:07:40 PM ] Estimate of total time remaining: 00:00:10.0420000

>[ 10/16/2013 4:07:40 PM ] Processing table dbo.[...]

>[ 10/16/2013 4:07:40 PM ] Total rows: 37909, total size: 6 MB

>[ 10/16/2013 4:07:40 PM ] Batch size is 94772

>[ 10/16/2013 4:07:40 PM ] Clearing table dbo.[...]

>[ 10/16/2013 4:07:45 PM ] Transferred batch 1 of 1 batches for dbo.[...]to destination database

>[ 10/16/2013 4:07:45 PM ] Transferred 50 of 56 total MB

>[ 10/16/2013 4:07:45 PM ] Estimate of total time remaining: 00:00:05.0270000

>[ 10/16/2013 4:07:45 PM ] Processing table dbo.[...]

>[ 10/16/2013 4:07:45 PM ] Total rows: 55667, total size: 6 MB

>[ 10/16/2013 4:07:45 PM ] Batch size is 139168

>[ 10/16/2013 4:07:45 PM ] Clearing table dbo.[...]

>[ 10/16/2013 4:07:51 PM ] Transferred batch 1 of 1 batches for dbo.[...] to destination database

>[ 10/16/2013 4:07:51 PM ] Transferred 56 of 56 total MB

>[ 10/16/2013 4:07:51 PM ] Total elapsed time: 00:00:47.5077173

>[ 10/16/2013 4:07:51 PM ] Finished

The following screenshot shows how the live log view will look when retry logic kicks in:
![image](https://raw2.github.com/brandnetworks/database-migration/master/readme%20images/retry.png)
19. Table rows in the grid view will turn green upon successful transfer completion. You may wish to monitor the metrics displayed along with the logs to make sure everything is operating optimally. The above log indicates a successful transfer with no issues. However, if the log contains a large volume of retry indications across several tables, you may wish to decrease the default batch size and/or thread pool size to speed up the transfer; these values will be automatically decreased and rerun for the table in the event of a detected transfer failure, but every failure wastes time.
![image](https://raw2.github.com/brandnetworks/database-migration/master/readme%20images/inprogress.png)
20. Wait for the transfer tool to complete and then verify the data in the new database (check row counts especially across tables that would require pagination, sp_spaceused output if applicable, nulls, empty values, datetime values, ids and primary keys, new join/etc tables, mapped columns, etc) to make sure no errors have occurred. Any errors in the resulting data would be considered bugs that should be reported and fixed.

Hints and Gotchas
-----------------

* When building virtual select queries, the type of the data is very important. You might need to perform type conversions in order for the queries to work (i.e. convert(nvarchar(50), columnName)).
* Transfer times will vary to some extent with the source database load, the load of the server hosting the destination database, and the bandwidth of the networks in between. If you are going to make a production migration and are trying to determine how long it will take based on a trial run, you should schedule the trial run during hours of usage matching the usage levels you expect during the actual migration. Not only will things slow down, but you might encounter more throttling or transport errors which will trigger retry logic and potentially a dynamic changing of parameters, which will also influence the overall time.
* It is strongly recommended to utilize the [App.config](https://github.com/brandnetworks/database-migration/blob/master/DatabaseTransferTool/App.config) as much as possible since it is can help reduce unnecessary syntax/parameter errors and will save time between runs.
* If you decide to specify a threadpool and batch size other than the default, it will help to open your performance monitor to see how much memory, CPU, and network bandwidth you are using. If you notice a severe peaking in any of those, you can try starting with a smaller batch size. Increasing the threadpool size can help level out the bandwidth usage over time as threads begin to accumulate offsets. The ideal parameters are the ones that will perform the entire transfer without running into too many throttling issues but not cause the transfer to be slower than necessary.
* Turning on query logging when failures other than transport-level errors occur can be useful in determining whether there is an error in query syntax or not. You do not need to restart the transfer for this setting to take effect. This is a useful setting when you are first entering any custom mappings/queries/etc you need into the tool. You can transfer only the tables you need in order to test them before by unchecking the other tables.
* Don't manually set the timeout values in the connection strings unless you don't expect any connection problems whatsoever. Doing so can prevent the tool from properly timing out when it tries to populate the information in the user interface on first load when a database is unreachable. The tool will automatically add those values before it begins the transfer.

Future Enhancements
-------------------

* It would be nice to have a feature that allows the user to browse for a sql script responsible for creating the schema (i.e. the exported schema from the original database) and run it against the destination database before the rest of the transfer occurs. This is currently challenged by the inability to use the "go" keyword within a query.
* Currently, all tables are processed in order and one at a time. This is good for larger tables, but near the end of a transfer when only small tables are processed, it would be much faster to allow tables to be transferred in parallel so long as the overall batch size is not exceeded.
* It would be nice to have an option to run arbitrary sql commands/scripts after the transfer has completed. For instance, after copying a prod database to a dev database, it would be convenient to have the tool automatically update all the place ids, access tokens, place links, etc to use non-prod values.
* Proper tests should be added at some point if active development work is going to continue.
* The large volume of event handling and polling between the transfer backend services and the UI could be cleaned up using reactive programming (http://msdn.microsoft.com/en-us/data/gg577609.aspx).

Known Bugs
----------

* There are times that some columns will not turn green right away after they have been successfully processed (or potentially ever depending on the column attributes and their position in the transfer).
* There are times when the tool attempts to delete newly added data from a failed table and start over with more optimized parameters. This sometimes fails because of key conflicts even when constraints have been disabled. It would be nice to automatically drop and add constraints on all affected tables in order to reliably be able to delete tables when required. This would get complicated of those tables influenced a cascading deletion of tables that were already populated. It might be OK if the tables were populated in order of their dependencies, but the tool does not currently have any dependency determination built in.
* Transfers that complete within less than a 10th of a second might cause the tool to not show overall progress or other status indicators correctly after the transfer has completed. This has no impacts on performance and is a result of the polling nature of some of the status monitors. The remaining polling logic that still exists should be changed to observer/etc logic at some point.

Support
-------
Create an issue here.
