using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Resource
{
    public string name { get; private set; } // Name
    public ResourceType type { get; private set; } // Name or type of resource (e.g. Gold, Wood)
    public double Amount { get; private set; }       // Current amount of the resource

    public Resource(string name, ResourceType type, double amount)
    {
        this.name = name;
        this.type = type;
        Amount = amount;
    }

    public void SetAmount(double amount)
    {
        Amount = amount;
    }

    // Increment the resource amount with production multiplier
    public void Increment(double amount)
    {
        Amount += amount;
    }

    // Decrement the resource amount
    public bool Decrement(double amount)
    {
        if (Amount >= amount)
        {
            Amount -= amount;
            return true;
        }
        return false;
    }

}

public enum ResourceType
{
    Wood,
    Iron,
    Soldiers,
    Workers,
    Knight,
    Guardian
}
