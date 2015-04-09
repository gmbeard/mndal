## What is **mnDAL**? ##
**mnDAL** `[`_Emmental - see what I did there?_`]` is a C# data access layer framework that supports SQL Server databases. It is designed to remove the tedium of writing repetitive data access code in your application. mnDAL allows queries to be expressed in type-safe .NET code, using built in operators such as `==`, `<=`, and `%`.  Collections of entities can also be further queried and filtered in the client, independent of the database.

## Using mnDAL in your application ##
There are two main types of objects that make up mnDAL; **Entites** and **database adapters**.

### Entities ###
To make use of mnDAL in your application you must create entity classes that mirror your own database's entities/tables. Don't worry, though; These classes barely need to define any more than the fields within each table. The first step to creating your own entity class is to inherit the `mnDAL.EntityBase` class.

```
    public class CheeseEntity : EntityBase
    {
       ...
```

You must provide the name of your underlying database table to the base constructor. _NOTE: If your table is in a schema other than_ `dbo` _then you must also provide the schema name._

```
       public CheeseEntity()
           : base("dbo.Cheese") // CheeseEntity is based on the dbo.Cheese database table.
       {
          ...
```

You must also define `EntityDbField` objects for your entities' fields. These tell the framework which database fields your entity contains. I find the easiest way is to define an encapsulated, static class that defines the field objects as public, static members.

```
       public static class CheeseEntityFields
       {
           public static EntityDbField CheeseID = new EntityDbField("CheeseID", SqlDbType.Int, true);
           public static EntityDbField Name = new EntityDbField("[Name]", SqlDbType.NVarChar, 50);
       }

```

This design helps keep the fields within context of your entity (This helps when building query expressions, as you will discover later). You can define as many, or as few of the table's fields as you like.

In your class' constructor you must provide the field mappings. This is done by calling the `AddFieldMapping` base method for all the fields/property mappings you wish to define.

```
          AddFieldMapping(CheeseEntityFields.CheeseID, "m_CheeseID");
          AddFieldMapping(CheeseEntityFields.Name, "m_Name");
```

The `m_CheeseID` and `m_Name` values are members you must also define within your class. If the database fields you are mapping are nullable then you must map them to appropriate nullable data types within your class. E.g. for value types, use the `?` syntax (`DateTime?`, `int?`, etc). _IMPORTANT: You can only add a field mapping to concrete members of your class, not property definitions._

You can define your property accessors as normal. However, there is a useful base method that you can call in each property's `set` that will tell mnDAL to track changes to your entity: `SetFieldModified`

```
        public string Name
        {
            get { return m_Name; }
            set
            {
                // This will tell mnDAL that the value in 'Name' has changed.
                SetFieldModified(CheeseEntityFields.Name, GetDbFieldValueChanged(CheeseEntityFields.Name) | value != m_Name);
                m_Name = value;
            }
        }

```

In fact, if you want your entity to be updatable at all then you must call `SetFieldModified` for at least one of your classes properties. mnDAL will only update those fields whose that have changed.

Lastly, your entity class must implement the `GetIdentifierDbField` method. Within this method you simply return the EntityDbField object that acts as the entity's identity, or primary key.

```
        public override EntityDbField GetIdentifierDbField()
        {
            // The database field 'CheeseID' is the primary key.
            return CheeseEntityFields.CheeseID;
        }
```

If your entity doesn't implement a primary key (or your do not wish to make your entity updatable), this method can either return `null`, or throw an exception. _NOTE: mnDAL, currently, does not support composite keys._

You now have a fully functioning mnDAL entity of your own! You may, of course, add your own logic to your class as you please.


### Accessing data with your entity classes, Expressions, and `DatabaseAdapter` ###

mnDAL uses the `DatabaseAdapter` class to provide the final bridge between your application and your data. To create a `DatabaseAdapter` you simply provide a standard `SqlConnection` instance to its constructor.

```
   m_Connection = new SqlConnection(
      "Data Source=.\\SQLEXPRESS;" +
      "AttachDbFilename=\"db\\mnDAL.mdf\";" +
      "Integrated Security=True;" +
      "User Instance=True");
   m_Connection.Open();
   DatabaseAdapter adapter = new DatabaseAdapter(m_Connection);
```

To fetch entities from your database, you call the `DatabaseAdapter.FetchEntities` method. The key to this method is the `EntityFetcher<>` parameter. If you create an `EntityFetcher` with its empty constructor, `FetchEntities` will return all the records in the underlying table. However, you can construct an `EntityFetcher` using mnDAL's expression syntax. An expression is created by using various operator overloads with an `EntityDbField` object.

```
   // Expression: WHERE CheeseID = 1
   CheeseEntity.CheeseEntityFields.CheeseID == 1

   // Expression: WHERE CheeseID >= 1
   CheeseEntity.CheeseEntityFields.CheeseID >= 1

   // Expression: WHERE CheeseID != 1
   CheeseEntity.CheeseEntityFields.CheeseID != 1

   // Expression: WHERE [Name] LIKE 'Emmental%'
   CheeseEntity.CheeseEntityFields.Name % "Emmental"

   // Expression: WHERE CheeseID IN (1, 2, 3, 4)
   CheeseEntity.CheeseEntityFields.CheeseID == (new int[] {1, 2, 3, 4})
```

These expressions can also be combined using the binary `|` (or) and `&` (and) operators. You should infer significance with parentheses, as you would with any and/or expression.

```
   // Expression: WHERE CheeseID > 2 AND [Name] LIKE 'Emmental%'
   CheeseEntity.CheeseEntityFields.CheeseID > 2 & CheeseEntity.CheeseEntityFields.Name % "Emmental"
```

So, using our mnDAL's expression syntax you can construct queries like so...

```
   CheeseEntity[] entities = adapter.FetchEntities(new EntityFetcher<CheeseEntity>(CheeseEntity.CheeseEntityFields.CheeseID == 1));
```

### Filtering results ###

Even though the `FetchEntities` method returns a simple array of entities, you can use mnDAL's expression syntax and the `Array.ForEach` algorithm to filter your results, quite effectively.

```
   mnDAL.Expression expr = (CheeseEntity.CheeseEntityFields.Name % "Edam" | CheeseEntity.CheeseEntityFields.Name == "Roquefort");
   
   List<CheeseEntity> filtered = new List<CheeseEntity>();
   Array.ForEach(entities, delegate(CheeseEntity item)
   {
      if(expr.Eval(item))
      {
         filtered.Add(item);
      }
   });
```

Using .NET's anonymous delegates makes this a fairly trivial task.

### Inserting, Updating, and Deleting ###

You can make changes to your entities (and, ultimately, your data) by calling the  `DatabaseAdapter.CommitEntity` with an `EntityUpdater` object.

```
   EntityUpdater insert = new EntityUpdater(entity, UpdateAction.Insert);
   adapter.CommitEntity<CheeseEntity>(ref insert);
```

The operation is specified by the `UpdateAction` enum - `Insert`, `Update`, `Delete`.