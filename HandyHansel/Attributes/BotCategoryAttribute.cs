using System;

namespace HandyHansel.Attributes
{
    [AttributeUsage(validOn: AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class BotCategoryAttribute : Attribute
    {
        public string Name { get; set; }

        public BotCategoryAttribute(string name)
        {
            this.Name = name;
        }
    }
}
