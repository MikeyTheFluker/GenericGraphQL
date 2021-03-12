﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GenericGraphQL.Types
{
    public class EntityLevel
    {
        public string Name { get; set; }
        public int Level { get; set; }
    }

    public static class EntityExtensions
    {
        public static int GetLevel(this List<EntityLevel> l, string name)
        {
            var found = l.FirstOrDefault(d => d.Name == name);
            return found?.Level ?? 0;
        }
    }
}
