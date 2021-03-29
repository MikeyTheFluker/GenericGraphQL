using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GenericGraphQL.Types;
using Microsoft.EntityFrameworkCore;

namespace GenericGraphQL.Helpers
{
    public class MetadataConnector
    {
        private readonly List<EntityLevel> _entitiesLevels;
        private List<EntityMetadata> _metadata;

        public MetadataConnector(DbContext context, List<EntityLevel> entitiesLevels)
        {
            _entitiesLevels = entitiesLevels;
            var dbMetadata = new DatabaseMetadata(context);
            _metadata = dbMetadata.GetEntityMetadatas().ToList();
        }

        public void AttachMetaDataToEntities()
        {
            var maxLevel = _entitiesLevels.Max(d => d.Level);

            for (var i = 1; i <= maxLevel; i++)
            {
                var currentLevel = _entitiesLevels.Where(d => d.Level == i).ToList();
                currentLevel.ForEach(d => AddEntityMetaDataToLevel(d, _metadata));
            }
        }

        private void AddEntityMetaDataToLevel(EntityLevel l, List<EntityMetadata> metaData)
        {
            if (l.Level == 1)
            {
                l.EntityMetaData = metaData.Single(e =>
                    e.TableName.Equals(l.Name, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                var target = l.PreviousEntityLevel.EntityMetaData.Columns.FirstOrDefault(e =>
                    e.FriendlyColumnName != null &&
                    e.FriendlyColumnName.Equals(l.Name, StringComparison.OrdinalIgnoreCase));

                if (target == null)
                {
                    //Try to find the column with Navigation
                    var modifiedName = $"{l.Name}Navigation";   //Entity Framework attaches this word for relationship virtual columns
                    target = l.PreviousEntityLevel.EntityMetaData.Columns.FirstOrDefault(e =>
                        e.FriendlyColumnName != null &&
                        e.FriendlyColumnName.Equals(modifiedName, StringComparison.OrdinalIgnoreCase));

                    if (target == null)
                    {
                        throw
                            new Exception($"Couldn't find a matching Database Entity for Query entity: {l.Name}");
                    }
                    
                }
                    
                l.EntityMetaData = metaData.FirstOrDefault(d => d.EntityName == target.TargetEntityName);
            }
        }
    }
}
