using System;
using System.Collections.Generic;
using System.Text;
using GraphQL;
using GraphQL.Resolvers;

namespace GenericGraphQL.Helpers
{
    public class NameFieldResolver : IFieldResolver
    {
        public object Resolve(IResolveFieldContext context)
        {
            var source = context.Source;

            if (source == null)
            {
                return null;
            }

            var name = Char.ToUpperInvariant(context.FieldAst.Name[0]) + context.FieldAst.Name.Substring(1);
            var value = GetPropValue(source, name);

            value = value != null ? value : string.Empty;

            /*if (value == null)
            {
                throw new InvalidOperationException($"Expected to find property {context.FieldAst.Name} on {context.Source.GetType().Name} but it does not exist.");
            } */

            return value;
        }

        private static object GetPropValue(object src, string propName)
        {
            return src.GetType().GetProperty(propName.ToLower()).GetValue(src, null);
        }

    }
}
