using System;
using System.Collections.Generic;
using GraphQL;
using GraphQL.Types;
using JoinMonster;
using JoinMonster.Data;

namespace GenericGraphQL.Types
{
    public class EntityType : ObjectGraphType<object>
    {
        private SQLiteDialect _dialect;
        public EntityType(EntityMetadata tableMetadata)
        {
            Name = tableMetadata.TableName;
            this.SqlTable(tableMetadata.TableName, "id");
            foreach (var tableColumn in tableMetadata.Columns)
            {
                InitGraphTableColumn(tableColumn);
            }
            _dialect = new SQLiteDialect();


            TableArgs.Add(new QueryArgument<IdGraphType> { Name = "number" });
            TableArgs.Add(new QueryArgument<IntGraphType> { Name = "first" });
            TableArgs.Add(new QueryArgument<IntGraphType> { Name = "offset" });
            TableArgs.Add(new QueryArgument<StringGraphType> { Name = "includes" });
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
                        { "bit", typeof(bool) }
                    };
                }
                return _databaseTypeToSystemType;
            }
        }
        
        private void InitGraphTableColumn(ColumnMetadata columnMetadata)
        {
            var columnName = columnMetadata.FriendlyColumnName;


            var type = ResolveColumnMetaType(columnMetadata.DataType);
            var graphQLType = type.GetGraphTypeFromType(true);

            FieldType columnField;
            if (columnMetadata.FkReference)
            {
                columnField = new FieldType { Name = columnName,
                    Type = columnMetadata.IsOneToManyRelationship ? typeof(ListGraphType<EntityType>) : typeof(EntityType) };
                columnField.SqlJoin((join, arguments, context, node) => join.Raw(
                    $"{_dialect.Quote(columnName)}.{_dialect.Quote(columnMetadata.ChildFkName)} = {_dialect.Quote(columnMetadata.SourceTableName)}.{_dialect.Quote(columnMetadata.ParentFkName)}",
                            null,
                        $"LEFT JOIN {_dialect.Quote(columnMetadata.TargetTableName)} as {_dialect.Quote(columnName)}"));
                columnField.SqlColumn();
                AddField(columnField);
            }
            else
            {
                columnField = new FieldType
                {
                    Name = columnName,
                    Type = columnName == "id" ? typeof(IdGraphType) : graphQLType,
                    
                };
                columnField.SqlColumn();
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
