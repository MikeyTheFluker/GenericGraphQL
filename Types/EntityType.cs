using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;
using JoinMonster;
using JoinMonster.Builders;
using JoinMonster.Configs;
using JoinMonster.Data;
using JoinMonster.Language.AST;

namespace GenericGraphQL.Types
{

    public class EntityType : ObjectGraphType<object>
    {
        public static List<EntityLevel> EntitiesWeWorkOn;
        public static Dictionary<string, EntityType> EntitiesAlreadyCreated;

        private Regex _checkForUnderscoreRemoval;

        private EntityLevel _entityLevel;
        private SQLiteDialect _dialect;
        public EntityType(EntityLevel level)
        {
            Name = level.EntityMetaData.TableName;
            _checkForUnderscoreRemoval = new Regex("[C]([A-Z]\\w+)");
            _entityLevel = level;
            this.SqlTable(level.EntityMetaData.TableName, "id");
            foreach (var tableColumn in level.EntityMetaData.Columns)
            {
                InitGraphTableColumn(tableColumn, Name);
            }
            _dialect = new SQLiteDialect();


            TableArgs.Add(new QueryArgument<IdGraphType> { Name = "number" });
            TableArgs.Add(new QueryArgument<IntGraphType> { Name = "first" });
            TableArgs.Add(new QueryArgument<IntGraphType> { Name = "offset" });
            TableArgs.Add(new QueryArgument<StringGraphType> { Name = "includes" });
            if (!EntitiesAlreadyCreated.ContainsKey(level.EntityMetaData.TableName))
            {
                EntitiesAlreadyCreated.Add(level.EntityMetaData.TableName, this);
            }
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

        
        private void InitGraphTableColumn(ColumnMetadata columnMetadata, string tableName)
        {
            var columnName = columnMetadata.FriendlyColumnName;


            var type = ResolveColumnMetaType(columnMetadata.DataType);
            var graphQLType = type.GetGraphTypeFromType(true);

            FieldType columnField;
            var targetEntityLevel = EntitiesWeWorkOn.GetLevel(columnMetadata.TargetEntityName);
            var isThisAnEntityInOurList = EntitiesWeWorkOn.Select(d => d.EntityMetaData.EntityName).Contains(columnMetadata.TargetEntityName);
            var isItLowerLevelThanUs = _entityLevel.Level < targetEntityLevel;
            if (columnMetadata.FkReference)
            {
                if (isThisAnEntityInOurList && isItLowerLevelThanUs)
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
                        columnField.SqlJoin(delegate (JoinBuilder @join, IReadOnlyDictionary<string, object> arguments,
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
                            Name = columnMetadata.ParentFkName, // columnName.Replace("Navigation", ""),
                            Type = typeof(EntityType),
                            ResolvedType = EntitiesAlreadyCreated.ContainsKey(columnMetadata.TargetTableName)
                                ? EntitiesAlreadyCreated[columnMetadata.TargetTableName] : null
                        };
                        var restoredColumn = RestoreUnderscore(columnMetadata.ParentFkName);
                        columnField.SqlJoin((join, arguments, context, node) => join.On(restoredColumn, columnMetadata.ChildFkName));
                        columnField.SqlColumn();
                        //columnField.Resolver = new Helpers.NameFieldResolver();
                        AddField(columnField);
                    }
                }
            }
            else
            {
                columnField = new FieldType
                {
                    Name = columnName,
                    Type = columnName == "id" ? typeof(IdGraphType) : graphQLType
                };
                columnField.SqlColumn();
                //columnField.Resolver = new Helpers.NameFieldResolver();
                AddField(columnField);
            }


            FillArgs(columnName);
        }

        /// <summary>
        /// Entity Framework doesn't like our underscores, maybe we can tell it to not do this,
        /// so we won't need to restore
        /// </summary>
        private string RestoreUnderscore(string columnName)
        {
            var restoredColumn = _checkForUnderscoreRemoval.IsMatch(columnName)
                ? $"c_{columnName.Remove(0, 1)}"
                : columnName;
            return restoredColumn;
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
