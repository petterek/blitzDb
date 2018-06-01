# blitzDB
An easy way to do mapping between db and code.


Name of fields / properties must match the name of coloums returned from the SQL query.

## Basic usage:
```
var  bdb = new blitzdb.DBAbstraction(new SqlConnection(ConnectionString()));

var cmd = new SqlCommand("Select Id,Name,Guid from tableOne where id =1");
var o = new DataObject();
bdb.Fill(cmd, o);

```

## Fillilng list of primitive values
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


You can also take a look at the unit tests, they show the usage of the lib.