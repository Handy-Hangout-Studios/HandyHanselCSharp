using System;
using System.Collections.Generic;
using System.Text;

namespace HandyHansel.Attributes
{
    public class BotCategoryAttribute : Attribute
    {
        public string Name { get; set; }

        public BotCategoryAttribute(string name)
        {
            Name = name;
        }
    }
}
