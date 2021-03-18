using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using GenericGraphQL.Types;

namespace GenericGraphQL.Helpers
{
    public class EntityLevelBuilder
    {
        private readonly string _query;
        private readonly Regex _regex;

        public EntityLevelBuilder(string query)
        {
            _regex = new Regex(@"(\s+)(\w+)(?:\(.*?\))?({.*?(?:\1)})", RegexOptions.Singleline);
            _query = query;
        }

        public List<EntityLevel> BuildEntityLevelsFromQuery()
        {
            var returned = new List<EntityLevel>();
            var noWhiteSpace = Regex.Replace(_query, @"\s+", "");
            int level = 0;
            string previousWord = string.Empty;
            var currentWord = new StringBuilder();
            var spaceless = _query.Replace(" ", "");

            var newRegex = new Regex("\\(.*\\)");
            EntityLevel newEntity = null;
            foreach (char c in spaceless)
            {
                switch (c)
                {
                    case '{':
                        if (level != 0)
                        {
                            var current = currentWord.ToString();
                            var newName = string.IsNullOrEmpty(current)
                                ? previousWord
                                : current;


                            var finalName = newRegex.Replace(newName, string.Empty);
                            newEntity = new EntityLevel {Name = finalName, Level = level, PreviousEntityLevel = newEntity};
                            returned.Add(newEntity);
                        }
                        level++;
                        break;
                    case '}':
                        level--;
                        break;
                    case '\n':
                        break;
                    case '\r':  //Environment.NewLine
                        previousWord = currentWord.ToString();
                        currentWord.Clear();
                        break;

                    default:
                        currentWord.Append(c);
                        break;
                }
   


            }
            return returned;
        }

        public List<EntityLevel> BuildEntityLevelsFromQuery2()
        {
            var returned = new List<EntityLevel>();
            int level = 1;
            if (_regex.IsMatch(_query))
            {
                var x = _regex.Match(_query);
                var entity = x.Groups[2];
                returned.Add(new EntityLevel { Name = entity.Value, Level = level });
                var insideGroup = x.Groups[3].Value;
                if (_regex.IsMatch(insideGroup))
                {
                    x = _regex.Match(insideGroup);
                    var g = x.Groups;

                    var secondLayer = insideGroup.Replace(x.Groups[0].Value, string.Empty);

                    if (_regex.IsMatch(secondLayer))
                    {
                        x = _regex.Match(secondLayer);
                        var gg = x.Groups[2];
                    }
                }
            }

            return returned;
        }

        private void AddEntityLevel(List<EntityLevel> entities, string insideGroup)
        {
            if (_regex.IsMatch(insideGroup))
            {
                var x = _regex.Match(insideGroup);
                var g = x.Groups;
                //entities.Add(g[2]);
                var secondLayer = insideGroup.Replace(x.Groups[0].Value, string.Empty);

                if (_regex.IsMatch(secondLayer))
                {
                    x = _regex.Match(secondLayer);
                    var gg = x.Groups[2];
                }
            }
        }
    }
}
