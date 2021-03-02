namespace GenericGraphQL.Types
{
    public class ColumnMetadata
    {
        public string ColumnName { get; set; }
        //This might need a better approach, GraphQL hates dollar sign in a column name, SQL Server is fine with it
        public string FriendlyColumnName => ColumnName.Replace("$", "S");
        public string DataType { get; set; }
        public bool FkReference { get; set; }
        public string ParentFkName { get; set; }
        public string ChildFkName { get; set; }
        public bool IsOneToManyRelationship { get; set; }
        public string SourceTableName { get; set; }
        public string TargetTableName { get; set; }
        public string TargetEntityName { get; set; }
    }
}
