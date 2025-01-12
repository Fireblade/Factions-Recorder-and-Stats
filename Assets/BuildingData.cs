using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingData 
{
    public List<Dictionary<ResourceType, double>> costs;
    public Dictionary<Stat, double> effects = new();
    public int requiredHQLevel;

    public BuildingData(int level)
    {
        requiredHQLevel = level;
        costs = new List<Dictionary<ResourceType, double>>();
    }
}
