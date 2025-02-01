using System;
using System.Collections.Generic;
using System.Linq;

internal class Utils
{
    public static List<ResourceEntry> OrderByIdleness(List<ResourceEntry> anims)
    {
        return anims.OrderByDescending(x => x.Name.Contains("stand") && x.Name.Contains("idle"))
            .ThenByDescending(x => x.Name.Contains("stand"))
            .ThenByDescending(x => x.Name.Contains("idle"))
            .ThenByDescending(x => x.Name.Contains("unarmed")).ToList();
    }   
}