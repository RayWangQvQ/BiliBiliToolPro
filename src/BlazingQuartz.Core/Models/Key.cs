using System;

namespace BlazingQuartz.Core.Models
{
    public class Key
    {
        public string Name { get; set; }
        public string? Group { get; set; }

        public Key(string name)
        {
            Name = name;
        }

        public Key(string name, string group)
            : this(name)
        {
            Group = group;
        }

        public Key(Key key)
        {
            Name = key.Name;
            Group = key.Group;
        }

        public bool Equals(string name, string? group)
        {
            return Name == name && Group == group;
        }
    }
}
