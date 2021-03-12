using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GenericGraphQL.Types;

namespace GenericGraphQL.Helpers
{
    public class OnlyRequiredEntitiesFeeder
    {
        private List<EntityMetadata> EntitiesToFeed;
        private List<string> EntitiesAlreadyFed;

        public OnlyRequiredEntitiesFeeder(IEnumerable<EntityMetadata> entitiesToFeed)
        {
            EntitiesToFeed = entitiesToFeed.ToList();
            EntitiesAlreadyFed = new List<string>();
        }

        public EntityMetadata GetNextEntity()
        {
            var nextEntity = EntitiesToFeed.FirstOrDefault(d => !EntitiesAlreadyFed.Contains(d.EntityName));
            if (nextEntity == null) return null;
            EntitiesAlreadyFed.Add(nextEntity.EntityName);
            return nextEntity;
        }

    }
}
