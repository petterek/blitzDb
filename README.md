# blitzDB
An easy way to do mapping between db and code.


## TODO:
- [x] Support for ASYNC the base interface IDBCommand does not support Async methods. Need to use SQLClient directly. Should we do this?
  - This is implemented in the SQLServer spesific version of BlitzDb
  - Not possible to do it in the generic version. 
- [ ] 



Name of fields / properties must match the name of coloums returned from the SQL query.

## Basic usage:
```
var  bdb = new blitzdb.DBAbstraction(new SqlConnection(ConnectionString()));

var cmd = new SqlCommand("Select Id,Name,Guid from tableOne where id =1");
var o = new DataObject();
bdb.Fill(cmd, o);

```

## Filling list of primitive values
If the generic type in the list is either "primitiv" or inherits from System.ValueType this will work

```
var  bdb = new blitzdb.DBAbstraction(new SqlConnection(ConnectionString()));
var cmd = new SqlCommand("Select Id from tableOne");
var o = new List<int>();
bdb.Fill(cmd, o);
```

## Can also work on immutable object
Name of parameters in the construcor must match with names in query.

```
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

var  bdb = new blitzdb.DBAbstraction(new SqlConnection(ConnectionString()));
var cmd = new SqlCommand("Select Id,Name,Guid,StringWithoutValue from tableOne where Id =@Id ");
cmd.Parameters.AddWithValue("Id", 1);

var o = bdb.Rehydrate<ImmutableObject>(cmd);

```

## Parameter spreading.
If you have an array of values you want to pass into a query, this can be done by the ExpandParameter extension method. 
```
    var cmd = new SqlCommand("Select Id,Name,Guid from tableOne where Id in(@Id) ");
    var o = new List<DataObject>();
    
    cmd.ExpandParameter(new SqlParameter("Id", DbType.Int32), new object[] { 1, 2, 4, 5, 6 });
    
    bdb.Fill(cmd, o);

```

## Automatic splitting of large parameter sets. 
SQLServer supports a maximum of 2000 parameters.. This should not be a problem, but in some cases it can. 
blitzDb has implemented an automatic way of splitting this into several queries.  

You can also take a look at the unit tests, they show the usage of the lib.

```
    var cmd = new SqlCommand("Select Id,Name,Guid from tableOne where Id in(@Id) ");
    var o = new List<DataObject>();
    cmd.ExpandParameter(new SqlParameter("Id", DbType.Int32), new object[] { 1, 2, 4, 5, 6 }, 3); <-The 3 here indicates max number of params pr request.
    bdb.splitSize = 2;
    bdb.Fill(cmd, o);

```