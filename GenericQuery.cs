using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
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
    public sealed class GenericQuery : ObjectGraphType<EntityType>
    {
 
        public GenericQuery(DbContext dbContext, JoinMonsterExecuter jm)
        {
            FieldType BuildEntity(EntityMetadata entityMetadata)
            {
                var tableType = new EntityType(entityMetadata);
                var newListType = new ListGraphType(tableType);

                var newField = new FieldType
                {
                    Name = entityMetadata.TableName,
                    Type = typeof(ListGraphType<Entity>),
                    ResolvedType = newListType,
                    Resolver =
                        new FuncFieldResolver<object, object>(Resolve), //new MyFieldResolver(metaTable, dbContext),
                    Arguments = new QueryArguments(
                        tableType.TableArgs
                    )
                };
               
                return newField;
            }

            Name = "MyQuery";

            object Resolve(IResolveFieldContext<object> context) =>
                jm.ExecuteAsync(context, async (sql, parameters) =>
                {
                    Console.WriteLine("SQL sent to DB");
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

            var dbMetadata = new DatabaseMetadata(dbContext);
            var allEntities = dbMetadata.GetEntityMetadatas().ToList();
            EntityType.EntitiesAlreadyCreated = new Dictionary<string, EntityType>();
            var feeder = new EntityFeeder(allEntities);
            var entityToMap = feeder.GetNextEntity();
            do
            {
                var newField = BuildEntity(entityToMap);
                // newField.SqlWhere(ApplyParameters);
                // AddField(newField);
                entityToMap = feeder.GetNextEntity();
            } while (entityToMap != null);


            // //do it again
            var f = new ReverseEntityFeeder(allEntities);
            var e = f.GetNextEntity();
            do
            {
                var newField = BuildEntity(e);
                newField.SqlWhere(ApplyParameters);
                AddField(newField);
                e = f.GetNextEntity();
            } while (e != null);

            //ResolveOneToManyColumnRelationships();
        }

        //private void ResolveOneToManyColumnRelationships()
        //{
        //    Fields.ToList().ForEach(d =>
        //    {
        //        var x = d.Arguments
        //            .Where(a => a.Type == typeof(ListGraphType<EntityType>) && a.ResolvedType == null);
        //        x.ToList().ForEach(a => a.ResolvedType = EntityType.EntitiesAlreadyCreated[a.Description]);
        //    });
        //}

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
