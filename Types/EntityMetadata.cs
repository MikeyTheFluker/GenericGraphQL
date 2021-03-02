using System.Collections.Generic;

namespace GenericGraphQL.Types
{
    public class EntityMetadata
    {
        public string TableName { get; set; }

        public string EntityName { get; set; }

        public string AssemblyFullName { get; set; }

        public IEnumerable<ColumnMetadata> Columns { get; set; }
    }
}
