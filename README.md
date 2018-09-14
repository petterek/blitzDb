# blitzDB
An easy way to do mapping between db and code.


## TODO:
- [x] ~~Support for ASYNC the base interface IDBCommand does not support Async methods. Need to use SQLClient directly. Should we do this?~~
  - This is implemented in the SQLServer spesific version of BlitzDb
  - Not possible to do it in the generic version. 
- [x] ~~Spreading of paramenters~~     
- [x] ~~Automatic splitting of large parameter sets~~
- [x] ~~Support for tuples~~
- [ ]  



Name of fields / properties must match the name of coloums returned from the SQL query.


This is assumed to exist in all examples

```csharp
var  bdb = new blitzdb.DBAbstraction(new SqlConnection(ConnectionString()));
```


## Basic usage:

```csharp
var cmd = new SqlCommand("Select Id,Name,Guid from tableOne where id =1");
var o = new DataObject();
bdb.Fill(cmd, o);

```

## Filling list of primitive values
If the generic type in the list is either "primitiv" or inherits from System.ValueType this will work

```csharp
var cmd = new SqlCommand("Select Id from tableOne");
var o = new List<int>();
bdb.Fill(cmd, o);
```
OR 
```csharp
var cmd = new SqlCommand("Select Guid from tableOne");
var o = bdb.Fill<List<Guid>>(cmd);
```

## Can also work on immutable object
Name of parameters in the construcor must match with names in query.

```csharp
 class ImmutableObject
{
    public int Id { get; }
    public Guid? Guid { get; }
    public string Name { get; }

    public ImmutableObject(int Id, Guid? guid, string name)
    {
        Name = name;
        Guid = guid;
        this.Id = Id;
    }
}


var cmd = new SqlCommand("Select Id,Name,Guid,StringWithoutValue from tableOne where Id =@Id ");
cmd.Parameters.AddWithValue("Id", 1);

var o = bdb.Rehydrate<ImmutableObject>(cmd);

```

## Parameter spreading.
If you have an array of values you want to pass into a query, this can be done by the ExpandParameter extension method. 
```csharp
    var cmd = new SqlCommand("Select Id,Name,Guid from tableOne where Id in(@Id) ");
    var o = new List<DataObject>();
    
    cmd.ExpandParameter(new SqlParameter("Id", DbType.Int32), new object[] { 1, 2, 4, 5, 6 });
    bdb.Fill(cmd, o);

```

## Automatic splitting of large parameter sets. 
SQLServer supports a maximum of 2000 parameters.. This should not be a problem, but in some cases it can. 
blitzDb has implemented an automatic way of splitting this into several queries.  

In this example the query will be run 2 times against the db, and the result of the queries will be aggregated in the result object.

```csharp
    var cmd = new SqlCommand("Select Id,Name,Guid from tableOne where Id in(@Id) ");
    var o = new List<DataObject>();
    cmd.ExpandParameter(new SqlParameter("Id", DbType.Int32), new object[] { 1, 2, 4, 5, 6 }, 3);// <-The 3 here indicates max number of params pr request.
    bdb.Fill(cmd, o);
```


## To use tuples, sepperate commands with ; number of commands must be the same as number of types to fill
```csharp
	var cmd = new SqlCommand(@"Select Id,Name,Guid from tableOne;Select Id,Name,Guid from tableOne where Id = 1;");
	var (o,o2) = bdb.Fill<List<DataObject>,DataObject>(cmd);
```
#### You can also take a look at the unit tests, they show the usage of the lib.
