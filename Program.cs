using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.SystemTextJson;
using JoinMonster;
using JoinMonster.Data;
using JoinMonster.Language;
using Microsoft.Data.SqlClient;
using NestHydration;

namespace GenericGraphQL
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var connectionToDb = await File.ReadAllTextAsync("connectionString.txt");

            var dbContext = ContextBuilder.BuildContext(connectionToDb);
            var jm = new JoinMonsterExecuter(
                new QueryToSqlConverter(new DefaultAliasGenerator()),
                new SqlCompiler(new SQLiteDialect()),
                new Hydrator()
            );
            var serviceProvider = new FuncServiceProvider(type =>
            {
                var swQuery = new GenericQuery(dbContext, jm);

                if (type == typeof(GenericQuery))
                    return swQuery;

                return Activator.CreateInstance(type);
            });

            var queryToRun = await File.ReadAllTextAsync("query.txt");

            GenericQuery.QueryToRun = queryToRun;

            var schema = new TargetDbSchema(serviceProvider);
            Console.WriteLine("Built a schema");

            
 

            Console.WriteLine("Running GraphQL Query" );
            Console.WriteLine(queryToRun);

            await using var connection = new SqlConnection(connectionToDb);

            await connection.OpenAsync();

            var options = new ExecutionOptions
            {
                ThrowOnUnhandledException = true,
                Schema = schema,
                Query = queryToRun,
                UserContext = new Dictionary<string, object>
                {
                    {nameof(IDbConnection), connection}
                }
            };
            var result = await new DocumentExecuter().ExecuteAsync(
                options
            ).ConfigureAwait(true);

            //data.EnrichWithApolloTracing(start);
            Console.WriteLine("Json returned to caller:");
            var writer = new DocumentWriter(true);

            Console.WriteLine(await writer.WriteToStringAsync(result));
        }
    }
}
