using GraphQL.Types;
using JoinMonster;

namespace GenericGraphQL.Types
{
    public class Entity : ObjectGraphType
    {
        public Entity(string entityName)
        {
            this.SqlTable(entityName, "id");

        }
    }
}
