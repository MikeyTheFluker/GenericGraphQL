using System.Collections.Generic;
using System.Linq;
using GenericGraphQL.Types;

namespace GenericGraphQL.Helpers
{
    /// <summary>
    /// We can't create entities that rely on entities that haven't been created yet
    /// This will feed us first all entities that have no Fks
    /// Then only ones entities that have already been fed
    /// </summary>
    public class ReverseEntityFeeder
    {
        private List<EntityMetadata> EntitiesToFeed;
        private List<string> EntitiesAlreadyFed;

        public ReverseEntityFeeder(IEnumerable<EntityMetadata> entitiesToFeed)
        {
            EntitiesToFeed = entitiesToFeed.ToList();
            EntitiesAlreadyFed = new List<string>();
        }
        public IEnumerable<string> TypesToFeed => EntitiesToFeed.Select(d => d.TableName);
        private bool NotAlreadyBeenFed(string entityName) => !AlreadyBeenFed(entityName);
        private bool AlreadyBeenFed(string entityName) => EntitiesAlreadyFed.Contains(entityName);

        public EntityMetadata GetNextEntity()
        {
            var fkLessEntity = EntitiesToFeed.FirstOrDefault(e =>
                !e.Columns.Any(c => c.IsOneToManyRelationship)
                && NotAlreadyBeenFed(e.EntityName));

            if (fkLessEntity != null)
            {
                EntitiesAlreadyFed.Add(fkLessEntity.EntityName);
                return fkLessEntity;
            }

            var entityThatOnlyReliesOnEntitiesFed =
                EntitiesToFeed.FirstOrDefault(d =>
                {
                    if (AlreadyBeenFed(d.EntityName)) return false;
                    var thingsReliedOn = d.Columns.Where(c => c.IsOneToManyRelationship)
                        .Select(c => c.TargetEntityName).Distinct().ToList();
                    var validSubject = CanWeFeedThisOneNext(thingsReliedOn, EntitiesAlreadyFed, d.EntityName);
                    return validSubject;
                });
            if (entityThatOnlyReliesOnEntitiesFed != null)
            {
                EntitiesAlreadyFed.Add(entityThatOnlyReliesOnEntitiesFed.EntityName);
            }
            else
            {
                var missingOnes = TypesToFeed.Except(EntitiesAlreadyFed).ToList();
            }
            return entityThatOnlyReliesOnEntitiesFed;
        }

        public static bool CanWeFeedThisOneNext(IEnumerable<string> entitiesReliedOn, IEnumerable<string> entitiesWeHaveAlreadyMigrated, string currentEntity)
        {
            //Don't exclude dependencies on yourself ie WorkOrder relying on itself
            var partsWeNeedButDoNotHave = entitiesReliedOn.Where(p => entitiesWeHaveAlreadyMigrated.All(p2 => p2 != p) && p != currentEntity);
            return !partsWeNeedButDoNotHave.Any();
        }

    }
}
