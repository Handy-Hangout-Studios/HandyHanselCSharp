using System;

namespace HandyHansel.Attributes
{
    public class BotCategoryAttribute : Attribute
    {
        public string Name { get; set; }

        public BotCategoryAttribute(string name)
        {
            this.Name = name;
        }
    }
}
