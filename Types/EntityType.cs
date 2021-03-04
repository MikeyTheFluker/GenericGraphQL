using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;
using JoinMonster;
using JoinMonster.Builders;
using JoinMonster.Configs;
using JoinMonster.Data;
using JoinMonster.Language.AST;
using System.Linq;

namespace GenericGraphQL.Types
{

    public class EntityType : ObjectGraphType<Entity>
    {
        public static Dictionary<string, EntityType> EntitiesAlreadyCreated;
        private readonly SQLiteDialect _dialect;

        public EntityType(EntityType entityType, IEnumerable<FieldType> fields)
        {
            Name = entityType.Name;
            this.SqlTable(Name, "id");
            _dialect = new SQLiteDialect();

            fields.ToList().ForEach(f =>
            {
                FillArgs(f.Name);
                this.AddField(f);
            });

            TableArgs.Add(new QueryArgument<IdGraphType> { Name = "number" });
            TableArgs.Add(new QueryArgument<IntGraphType> { Name = "first" });
            TableArgs.Add(new QueryArgument<IntGraphType> { Name = "offset" });
            TableArgs.Add(new QueryArgument<StringGraphType> { Name = "includes" });


        }

        public EntityType(EntityMetadata tableMetadata)
        {
            Name = tableMetadata.TableName;
            this.SqlTable(tableMetadata.TableName, "id");
            foreach (var tableColumn in tableMetadata.Columns)
            {
                InitGraphTableColumn(tableColumn, Name);
            }
            _dialect = new SQLiteDialect();


            TableArgs.Add(new QueryArgument<IdGraphType> { Name = "number" });
            TableArgs.Add(new QueryArgument<IntGraphType> { Name = "first" });
            TableArgs.Add(new QueryArgument<IntGraphType> { Name = "offset" });
            TableArgs.Add(new QueryArgument<StringGraphType> { Name = "includes" });
            if (EntitiesAlreadyCreated.ContainsKey(tableMetadata.TableName))
            {
                EntitiesAlreadyCreated.Remove(tableMetadata.TableName);
            }

            EntityType x = this;
            var y = (EntityType)this.Fields.FirstOrDefault(f => !string.IsNullOrEmpty(f.Description))?.ResolvedType;
            if (y != null)
            {
                var yy = (EntityType)y.Fields.FirstOrDefault(f => !string.IsNullOrEmpty(f.Description))?.ResolvedType;
                var zz = yy?.Fields.FirstOrDefault(f => f.Name == tableMetadata.EntityName);
                if (zz != null)
                {
                    var list = yy.Fields.Where(f => f.Name != tableMetadata.EntityName);
                    x = new EntityType(this, list);
                }
            }
            
            EntitiesAlreadyCreated.Add(tableMetadata.TableName, x);

        }

        public QueryArguments TableArgs
        {
            get; set;
        }

        private IDictionary<string, Type> _databaseTypeToSystemType;
        protected IDictionary<string, Type> DatabaseTypeToSystemType
        {
            get
            {
                if (_databaseTypeToSystemType == null)
                {
                    _databaseTypeToSystemType = new Dictionary<string, Type> {
                        { "uniqueidentifier", typeof(string) },
                        { "char", typeof(string) },
                        { "nvarchar", typeof(string) },
                        { "int", typeof(int) },
                        { "decimal", typeof(decimal) },
                        { "float", typeof(decimal) },
                        { "bit", typeof(bool) }
                    };
                }
                return _databaseTypeToSystemType;
            }
        }

        public void UpdateField()
        {
            var fieldsThatNeedUpdating = Fields.Where(d => 
                d.Type == typeof(ListGraphType<EntityType>) && d.ResolvedType == null);
            fieldsThatNeedUpdating.ToList().ForEach(d=> d.ResolvedType = EntitiesAlreadyCreated[d.Description]);
        }
        
        private void InitGraphTableColumn(ColumnMetadata columnMetadata, string tableName)
        {
            var columnName = columnMetadata.FriendlyColumnName;


            var type = ResolveColumnMetaType(columnMetadata.DataType);
            var graphQLType = type.GetGraphTypeFromType(true);

            FieldType columnField;
            if (columnMetadata.FkReference)
            {
                if (columnMetadata.IsOneToManyRelationship)
                {
                    columnField = new FieldType
                    {
                        Name = columnName,
                        Description = columnMetadata.TargetTableName,       //Using description to store the key to the dictionary for later
                        Type = typeof(ListGraphType<EntityType>),
                        ResolvedType = EntitiesAlreadyCreated.ContainsKey(columnMetadata.TargetTableName) 
                            ? EntitiesAlreadyCreated[columnMetadata.TargetTableName] : null
                    };
                    columnField.SqlJoin(delegate(JoinBuilder @join, IReadOnlyDictionary<string, object> arguments,
                        IResolveFieldContext context, SqlTable node)
                    {
                        @join.Raw(
                            $"{_dialect.Quote(columnName)}.{_dialect.Quote(columnMetadata.ChildFkName)} = {_dialect.Quote(columnMetadata.SourceTableName)}.{_dialect.Quote(columnMetadata.ParentFkName)}",
                            null,
                            $"LEFT JOIN {_dialect.Quote(columnMetadata.TargetTableName)} as {_dialect.Quote(columnName)}");
                    });
                    //columnField.Resolver = new Helpers.NameFieldResolver();
                    AddField(columnField);
                }
                else
                {
                    columnField = new FieldType
                    {
                        Name = columnName,
                        Type = typeof(EntityType),
                        ResolvedType = EntitiesAlreadyCreated.ContainsKey(columnMetadata.TargetTableName)
                            ? EntitiesAlreadyCreated[columnMetadata.TargetTableName] : null
                    };
                    columnField.SqlJoin((join, arguments, context, node) => join.On(columnMetadata.ParentFkName, columnMetadata.ChildFkName));
                    columnField.SqlColumn();
                    //columnField.Resolver = new Helpers.NameFieldResolver();
                    AddField(columnField);
                }
                
            }
            else
            {
                columnField = new FieldType
                {
                    Name = columnName,
                    Type = columnName == "id" ? typeof(IdGraphType) : graphQLType,
                    //ResolvedType = new ObjectGraphType()


                };
                columnField.SqlColumn();
                //columnField.Resolver = new Helpers.NameFieldResolver();
                AddField(columnField);
            }


            FillArgs(columnName);
        }

        private void FillArgs(string columnName)
        {
            if (TableArgs == null)
            {
                TableArgs = new QueryArguments(
                    new QueryArgument<StringGraphType>()
                    {
                        Name = columnName
                    }
                );
            }
            else
            {
                TableArgs.Add(new QueryArgument<StringGraphType> { Name = columnName });
            }
        }

        private Type ResolveColumnMetaType(string dbType)
        {
            if (DatabaseTypeToSystemType.ContainsKey(dbType))
                return DatabaseTypeToSystemType[dbType];

            return typeof(string);
        }
    }
}
