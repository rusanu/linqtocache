A caching extension for IQueryable with Query Notifications invalidation support.

# How to Use

Download the appropiate .dll for your target platform (.Net 3.5 or .Net 4.0) and add it as a reference to your project. Now you can use the `AsCached()` method extension on any IQueryable:

```C#
var queryTags = from t in ctx.Tags select t;
var tags = queryTags.AsCached("Tags");
foreach (Tag t in tags)
{
  ...
}
```

# How do Query Notification work

Query Notifications are a SQL Server 2005 and forward feature that allows the client to subscribe for receiving notifications when the result of a query has changed due to updates on the server. Say user Alice submits a query like 

```SQL
SELECT PersonID, FirstName, LastName 
FROM dbo.People
```

and receives back a result set containing the selected records from the dbo.People table. If the query submitted was also marked for notifications, then the SQL Server will remember this query. Later user Bob, using a different application, makes an update to the dbo.People table, say it add a new person in the table. SQL Server detects that the query submitted by Alice has now changed the result (it has become *invalid*) and will send a notification to Alice informing her that the result set she got back when she run the query has become invalid. Alice has to re-run the query to see *how* did the result set change.

The typical way of using Query Notification is to use the [`SqlDependency`](http://msdn.microsoft.com/en-us/library/system.data.sqlclient.sqldependency.aspx) class.

For a more detailed explanation how Query Notification work inside SQL Server I recommend this blog article [The Mysterious Notification](http://rusanu.com/2006/06/17/the-mysterious-notification).

# How does LinqToCache Work

LinqToCache adds the `AsCached` extension method to the `IQueryable` interface. When the `IQueryable<T>` is first iterated, the LinqToCache library adds a SqlDependency object on the query submitted to SQL Server. Subsequent requests for the same type T with the same caching keys will be returned from a cached copy of the result. When the SqlDependency receives an invalidation notification the cached result is invalidated. The next iteration of an `IQueryable` will again submit the query to the SQL Server and fetch a new result.

# Getting notified on invalidation

If you wish to get a callback in the application when the LinqToCache query result is notified you can submit an optional `CachedQueryOptions` argument to the `AsCached` call. The `OnInvalidated` event on this optional argument will be raised when the SqlDependency has notification has fired, in other words when the query was notified by the SQL Server because data was updated by another user:

```C#
var queryTags = from t in ctx.Tags select t;
var tags = queryTags.AsCached("Tags", new QueryCachedOptions () {
  OnInvalidate = (sender, args) => {
    // This code here is invoked when the query result is invalidated
    // Always check the args parameter to understand the type of invalidation that occured
    if (SqlNotificationSource.Data == args.NotificationEventArgs.Source
       && SqlNotificationType.Change == args.NotificationEventargs.Type) {
       // This is a data change notificaiton, the result set has changed
    }
  }
 });
```

In general it is better to simply request the data using the `AsCached` method and don't handle explicitly the invalidation notifications. But some applications may choose to actively monitor the change notifications so that they notify the user via some GUI interaction (eg. a System Tray popup).

## Query Notification Restrictions

Not any query can be marked for Query Notifications. The full list of restrictions and conditions is documented at [Creating a Query for Notification](http://msdn.microsoft.com/en-us/library/ms181122.aspx). Some of the important restrictions that apply to LINQ queries are:

  - Table names must be qualified with two-part names.
  - The statement must reference a base table (not a view or UDF).
  - The statement must not use the TOP expression.
  - The statement must not use any nondeterministic functions, including ranking and windowing functions.

If the query is invalid for notifications the `OnInvalidated` event will be called immediately with an `Source` of value `SqlNotificationSource.Statement` and an `Info` of value `SqlNotificationInfo.Invalid`.

Most LinqToSQL queries are valid for notifications as long as the table name is the schema qualified two-part name: that is the `TableAttribute` on the class is `schema.TableName`, not only `TableName`. However, some popular LINQ operators are not supported for notifications,  like `Skip()` and `Take()`.
