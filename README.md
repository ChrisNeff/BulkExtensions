# BulkExtensions

   This project was built as an extension to add bulk operations functionality to the Entity Framework (EF6 and EFCore). 
It works as extension methods of the DbContext class and is very simple to use. The library uses the same connection your context created and if the context's database have a CurrentTransaction it will use it, otherwise it creates an internal one for the scope of the operation.
<br><br>
   It relies on the SqlBulkCopy class to perform all the operations, because of that, it can't handle navigation properties and will not persist relationships between entities, but there is a workaround for that if the foreign keys are being explicitly mapped in your model classes. See the workaround in the examples below.
   
### Overall features

- Bulk insert, update, insert or update, delete operations;
- Support context transaction (Uses the same connection and transaction of the context);
- If the context has no transaction it creates and uses an internal one for safety;
- Support tables with AutoIncrement key, not auto increment keys and composite keys;
- Output database generated Ids;
- Support Table-Per-Hierarchy(TPH);

#### Framework Targets

- For EF6 you can use it with .NetFramewok 4.5+;
- For EFCore you can use it with .NetFramewok 4.5.1+ or .NetCore1.0+;
   
### Changes in version 1.3
   
- Added support for EFCore;
- New feature InsertOrUpdate;
- Support composite keys;
   
### Installation
You can install it using the nuget package for your EF version:
- For EF6: <a href="https://www.nuget.org/packages/EntityFramework.BulkExtensions">EntityFramework.BulkExtensions</a><br>
- For EFCore: <a href="https://www.nuget.org/packages/EntityFramework.BulkExtensions.EFCore">EntityFramework.BulkExtensions.EFCore</a><br>

## How to use it
You just need to call the methods bellow for the feature you want to use passing the collection of entities to perform the operation.
```c#
context.BulkInsert(entities);
context.BulkUpdate(entities);
context.BulkInsertOrUpdate(entities);
context.BulkDelete(entities);

//Generated Ids are populated by adding the optional parammeter

context.BulkInsert(entities, InsertOptions.OutputIdentity);
context.BulkInsertOrUpdate(entities, InsertOptions.OutputIdentity);
```

## Examples

### Bulk insert
   There is two ways of using this method. By only using the list as parameters for this extension method it will perform a standard SqlBulkCopy operation, witch will not return the Ids of the inserted entities because of a limitation of the SqlBulkCopy class. 
   <br><br>
   By also selecting 'Identity.Output' as the second parameter, the method will fill the generated Ids for the entities inserted, using temporary tables to output and select the generated Ids under the hood. See the exemples below:
```c#
using EntityFramework.BulkExtensions.Operations

var entityList = new List<MyEntity>();

entityList.Add(new MyEntity());
entityList.Add(new MyEntity());
entityList.Add(new MyEntity());

//Bulk insert extension method
context.BulkInsert(entityList); 

/* Also, if you want the generated ids you can use the code below */

context.BulkInsert(entityList, InsertOptions.OutputIdentity);
entityList.First().Id //would return the id generated on the insert.

/* The ids generated by the database will be set for every inserted item
   in the entities collection */
```

#### Workaround for relationships
   You can explicitly set the foreign keys of your entity and insert it. See the example below.
   
```c#
using EntityFramework.BulkExtensions

var role = context.Set<Roles>()
   .Single(entity => entity.Name == "Admin")
   .ToList();

var entityList = new List<User>();

entityList.Add(new User{ RoleId = role.Id }); //Set the role id on the newly created user
entityList.Add(new User{ RoleId = role.Id });
entityList.Add(new User{ RoleId = role.Id });
entityList.Add(new User{ RoleId = role.Id });
entityList.Add(new User{ RoleId = role.Id });
entityList.Add(new User{ RoleId = role.Id });

//Bulk insert extension method
context.BulkInsert(entityList); 
/* By explicitly setting the foreing key the relationship will be persisted in the database. */
```
   
### Bulk update
```c#
using EntityFramework.BulkExtensions

Random rnd = new Random();

//Read some entities from database.
var entityList = context.Set<MyEntity>()
   .Where(entity => entity.Owner == "Steve")
   .ToList();
foreach(var entity in entityList) 
{
    //Replace the old value with some random new value.
    entity.Value = rnd.Next(1000); 
}

//Bulk update extension method
context.BulkUpdate(entityList); 

/* Under the hood, this operation will create a mirror table of your entity's table, 
   bulk insert the updated entities using the SqlBulkCopy class, use the MERGE sql 
   command to transfer the data to the original entity table using the primary keys 
   to match entries and then drop the mirror table. The original course of action of 
   the entity framework would be create an UPDATE command for each entity, wich suffers 
   a big performance hit with an increased number of entries to update. */
```

### Bulk insert or update
```c#
using EntityFramework.BulkExtensions

Random rnd = new Random();

//Read some entities from database.
var entityList = context.Set<MyEntity>()
   .Where(entity => entity.Owner == "Steve")
   .ToList();
foreach(var entity in entityList) 
{
    //Replace the old value with some random new value.
    entity.Value = rnd.Next(1000);    
}
//Add some new entities.
for(var i = 0; i < 10; i++)
{
   entityList.Add(new MyEntity
   {
      Value = rnd.Next(1000);
   });
}

//Bulk update extension method
context.BulkInsertOrUpdate(entityList); 

/* Also, if you want the generated ids for the
   newly added entitites you can use the code below*/

context.BulkInsertOrUpdate(entityList, InsertOptions.OutputIdentity);

/* Under the hood, this operation will create a mirror table of your entity's table, 
   bulk insert the updated entities using the SqlBulkCopy class, use the MERGE sql 
   command to transfer the data to the original entity table using the primary keys 
   to match entries, the ones not matched are new and will be inserted, and then drop 
   the mirror table. The original course of action of the entity framework would be 
   create an UPDATE command for each entity, wich suffers a big performance hit with 
   an increased number of entries to update. */
```

### Bulk delete
```c#
using EntityFramework.BulkExtensions

//Read some entities from database.
var entityList = context.Set<MyEntity>()
      .Where(entity => entity.Owner == "Steve")
      .toList();

//Bulk delete extension method
context.BulkDelete(entityList); 

/* This operation will delete all the entities in the list from the database. */
```

### Transactions
The work with transactions is pretty straightforward and flexible. If you are performing multiple operations on the context using a transaction it is safe to use any bulk operation, the operations use the transaction of the context to perform database manipulation.

```c#
using EntityFramework.BulkExtensions
   //Begin a transaction on your context.
using(var transaction = context.Database.BeginTransaction())
{
   var rnd = new Random();

   //Read some entities from database.
   var updateList = context.Set<MyEntity>()
      .Where(entity => entity.Owner == "Steve")
      .ToList();
   foreach(var entity in updateList) 
   {
       //Replace the old value with some random new value.
       entity.Value = rnd.Next(1000);
       entity.OtherProperty = "some random string";
   }

   //Bulk update extension method
   context.BulkUpdate(updateList); // 1st operation

   //Read other entities from database.
   var deleteList = context.Set<MyEntity>()
         .Where(entity => entity.Owner == "Bob")
         .toList();

   //Bulk delete extension method
   context.BulkDelete(deleteList); // 2nd operation

   //Commit the transaction
   transaction.Commit();
}

/* The two operations will run on the same transaction, if something goes worng the rollback would
undo the changes made by the two bulk operations.*/
```

If you are not using transaction, each bulk operations creates a transaction for the scope of the operation.

```c#
using EntityFramework.BulkExtensions

var rnd = new Random();

//Read some entities from database.
var updateList = context.Set<MyEntity>()
   .Where(entity => entity.Owner == "Steve")
   .ToList();
foreach(var entity in updateList) 
{
    //Replace the old value with some random new value.
    entity.Value = rnd.Next(1000); 
    entity.OtherProperty = "some random string";
}

//Bulk update extension method
context.BulkUpdate(updateList); // 1st operation

//Read other entities from database.
var deleteList = context.Set<MyEntity>()
      .Where(entity => entity.Owner == "Bob")
      .toList();

//Bulk delete extension method
context.BulkDelete(deleteList); // 2nd operation

/* Each operations will run on it's own transaction. For example, if something 
goes worng with the delete operation the changes made by it would be undone but 
the changes made by the update before would persist.*/
```

## Credits
This library is based on the SqlBulkTools by Greg Taylor.


