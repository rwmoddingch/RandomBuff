using System;

namespace RandomBuff.Core.Game.Settings.CustomMissions;

public class ElementPropertyAttribute : Attribute
{
    public ElementPropertyAttribute(string name)
    {
        Name = name;
    }


    public ElementPropertyAttribute(string name, string category)
    {
        Name = name;
        Category = category;
    }
    
    public string Name { get; set; }

    public string Category { get; set; } = "Detail";
}