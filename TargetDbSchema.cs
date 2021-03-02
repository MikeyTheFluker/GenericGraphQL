using System;
using GraphQL.Types;
using GraphQL.Utilities;
namespace GenericGraphQL
{
    public class TargetDbSchema : Schema
    {
        public TargetDbSchema(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            Query = serviceProvider.GetRequiredService<GenericQuery>();

        }
    }
}
