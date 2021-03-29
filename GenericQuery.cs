using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using GenericGraphQL.Helpers;
using GenericGraphQL.Types;
using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;
using JoinMonster;
using JoinMonster.Builders;
using JoinMonster.Language.AST;
using Microsoft.EntityFrameworkCore;

namespace GenericGraphQL
{
    public sealed class GenericQuery : ObjectGraphType<object>
    {
        public static string QueryToRun;
        public GenericQuery(DbContext dbContext, JoinMonsterExecuter jm)
        {
            Name = "MyQuery";

            object Resolve(IResolveFieldContext<object> context) =>
                jm.ExecuteAsync(context, async (sql, parameters) =>
                {
                    Console.WriteLine("SQL sent to DB");
                    sql = sql.Replace("SELECT", "SELECT TOP 20");
                    sql = sql.Replace("\"\"", "\"");
                    Console.WriteLine(sql);
                    Console.WriteLine();
                    Console.WriteLine("Parameters:");
                    parameters.ToList().ForEach(d => Console.Write($"{d.Key}-{d.Value}"));
                    Console.WriteLine();

                    var dbConnection = (IDbConnection)context.UserContext[nameof(IDbConnection)];
                    await using var command = (DbCommand)dbConnection.CreateCommand();
                    command.CommandText = sql;
                    foreach (var (key, value) in parameters)
                    {
                        var sqlParameter = command.CreateParameter();
                        sqlParameter.ParameterName = key;
                        sqlParameter.Value = value;
                        command.Parameters.Add(sqlParameter);
                    }

                    return await command.ExecuteReaderAsync();
                });


            EntityType.EntitiesWeWorkOn = new List<EntityLevel>();
            var levelBuilder = new EntityLevelBuilder(QueryToRun);
            EntityType.EntitiesWeWorkOn = levelBuilder.BuildEntityLevelsFromQuery();


            EntityType.EntitiesAlreadyCreated = new Dictionary<string, EntityType>();

            var metaConnector = new MetadataConnector(dbContext, EntityType.EntitiesWeWorkOn);
            metaConnector.AttachMetaDataToEntities();

            var orderedList = EntityType.EntitiesWeWorkOn
                .OrderByDescending(d => d.Level).ToList();


            foreach(var e in orderedList)
            {
                var tableType = new EntityType(e);
                var newListType = new ListGraphType(tableType);

                var newField = new FieldType
                {
                    Name = e.EntityMetaData.TableName,
                    Type = typeof(ListGraphType<Entity>),
                    ResolvedType = newListType,
                    Resolver =
                        new FuncFieldResolver<object, object>(Resolve), //new MyFieldResolver(metaTable, dbContext),
                    Arguments = new QueryArguments(
                        tableType.TableArgs
                    )
                };
                newField.SqlWhere(ApplyParameters);
                AddField(newField);
            }

            
        }






        private void ApplyParameters(WhereBuilder where, IReadOnlyDictionary<string, object> args, IResolveFieldContext _, SqlTable __)
        {
            var userArguments = args.Where(d => d.Value != null);
            foreach (var (key, _) in userArguments)
            {
                where.Column(key, args[key]);
            }
            
        }
    }
}
