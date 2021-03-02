using System.Collections.Generic;
using System.Linq;
using GenericGraphQL.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GenericGraphQL.Helpers
{
    public interface IDatabaseMetadata
    {
        void ReloadMetadata();
        IEnumerable<EntityMetadata> GetEntityMetadatas();
    }

    public sealed class DatabaseMetadata : IDatabaseMetadata
    {
        private readonly DbContext _dbContext;
        
        private IEnumerable<EntityMetadata> _tables;
        private Dictionary<string, string> tableEntityLookup;

        public DatabaseMetadata(DbContext dbContext)
        {
            _dbContext = dbContext;
            tableEntityLookup = new Dictionary<string, string>();

            if (_tables == null)
                ReloadMetadata();
        }

        public IEnumerable<EntityMetadata> GetEntityMetadatas()
        {
            if (_tables == null)
                return new List<EntityMetadata>();

            return _tables;
        }

        public void ReloadMetadata()
        {
            _tables = FetchEntityMetadata();
        }

        private IReadOnlyList<EntityMetadata> FetchEntityMetadata()
        {
            var metaTables = new List<EntityMetadata>();
            var entityTypes = _dbContext.Model.GetEntityTypes();


            foreach (var entityType in entityTypes)
            {
                var tableName = entityType.GetTableName();
                tableEntityLookup.Add(entityType.ClrType.Name, tableName);
            }

            foreach (var entityType in entityTypes)
            {
                var tableName = entityType.GetTableName();

                metaTables.Add(new EntityMetadata
                {
                    TableName = tableName,
                    EntityName = entityType.ClrType.Name,
                    AssemblyFullName = entityType.ClrType.FullName,
                    Columns = GetColumnsMetadata(entityType, tableName)
                });
                
            }

            return metaTables;
        }

        private IReadOnlyList<ColumnMetadata> GetColumnsMetadata(IEntityType entityType, string tableName)
        {
            var tableColumns = entityType.GetProperties().Select(propertyType => new ColumnMetadata {ColumnName = propertyType.GetColumnName(), DataType = propertyType.GetColumnType()}).ToList();
            var navigations = entityType.GetNavigations();
            foreach (var nav in navigations)
            {
                var parentFk = nav.ForeignKey.Properties.FirstOrDefault();
                var childFk = nav.ForeignKey.PrincipalKey.Properties.FirstOrDefault();

                var isOneToMany = nav.ClrType.IsGenericType &&
                                  nav.ClrType.GetGenericTypeDefinition() == typeof(ICollection<>);
                if (isOneToMany)
                {
                    tableColumns.Add(new ColumnMetadata
                    {
                        ColumnName = nav.Name,
                        DataType = nav.ForeignKey.DeclaringEntityType.Name,
                        FkReference = true,
                        ParentFkName = childFk?.Name,           //Flip
                        ChildFkName = parentFk?.Name,
                        IsOneToManyRelationship = isOneToMany,
                        SourceTableName = tableName,
                        TargetTableName = tableEntityLookup[nav.ForeignKey.DeclaringEntityType.ClrType.Name],  //Enity Name, needs to be Table Name, maybe lookup
                        TargetEntityName = nav.ForeignKey.DeclaringEntityType.ClrType.Name
                    });
                }
                else
                {
                    tableColumns.Add(new ColumnMetadata
                    {
                        ColumnName = nav.Name,
                        DataType = nav.DeclaringEntityType.Name,
                        FkReference = true,
                        ParentFkName = parentFk?.Name,
                        ChildFkName = childFk?.Name,
                        IsOneToManyRelationship = isOneToMany,
                        TargetTableName = tableEntityLookup[nav.ForeignKey.DeclaringEntityType.ClrType.Name],
                        TargetEntityName = nav.ForeignKey.PrincipalEntityType.ClrType.Name
                    });
                }
                
            }
            return tableColumns;
        }
    }
}
