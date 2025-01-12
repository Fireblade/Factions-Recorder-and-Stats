using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager
{
    public Dictionary<ResourceType, Resource> resources = new Dictionary<ResourceType, Resource>();

    // Add a resource to the manager
    public void AddResource(Resource resource)
    {
        resources[resource.type] = resource;
    }

    // Increment resource amount
    public void IncrementResource(ResourceType resourceType, double amount)
    {
        if (resources.ContainsKey(resourceType))
        {
            resources[resourceType].Increment(amount);
        }
    }

    // Check if you can afford a resource cost
    public bool CanAfford(ResourceType resource, double cost)
    {
        return resources.ContainsKey(resource) && resources[resource].Amount >= cost;
    }

    // Spend resources
    public void SpendResources(Dictionary<ResourceType, double> resourceCosts)
    {
        foreach (var cost in resourceCosts)
        {
            resources[cost.Key].Decrement(cost.Value);
        }
    }

    // Get current value of a resource
    public double GetResourceValue(ResourceType resourceType)
    {
        if (resources.ContainsKey(resourceType))
        {
            return resources[resourceType].Amount;
        }
        return 0;
    }
}
