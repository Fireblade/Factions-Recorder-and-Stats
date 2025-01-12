using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using Unity.MLAgents.Sensors.Reflection;
using Unity.VisualScripting;
using UnityEngine;

public class Building
{
    public static bool dataInitalized = false;
    public static Dictionary<BuildingType, BuildingData> buildingData = new Dictionary<BuildingType, BuildingData>();

    public CityManager manager;
    public CityPlannerManager cityManager;
    public string Name { get; set; }
    public BuildingType myBuilding { get; set; } = BuildingType.None;
    public int Level { get; set; } = 1;

    public Building()
    {

    }

    public Building(CityManager manager, string name, BuildingType buildingType, int lvl)
    {
        this.manager = manager;
        Name = name;
        this.myBuilding = buildingType;
        Level = lvl;
    }

    // Check if the building can be constructed or upgraded
    public bool CanUpgrade(ResourceManager resourceManager)
    {
        foreach (var cost in CostsForLevel(Level+1).ToList())
        {
            if (!resourceManager.CanAfford(cost.Key, cost.Value))
                return false;
        }
        return true;
    }

    public bool CanBuild(ResourceManager resourceManager)
    {
        foreach (var cost in CostsForLevel(1).ToList())
        {
            if (!resourceManager.CanAfford(cost.Key, cost.Value))
                return false;
        }
        return true;
    }

    // Upgrade the building (costs scale with level)
    public bool Upgrade(ResourceManager resourceManager)
    {
        if (myBuilding == BuildingType.None)
        {
            return false;
        }
        if (manager.HQ.Level >= buildingData[myBuilding].requiredHQLevel)
        {
            if (CanUpgrade(resourceManager))
            {

                Dictionary<ResourceType, double> ResourceCosts = CostsForLevel(Level + 1);
                resourceManager.SpendResources(ResourceCosts);
                Level++;
                manager.needUpdateStats = true;
                return true;
            }
        }
        return false;
    }

    public Dictionary<ResourceType,double> CostsForLevel(int level)
    {
        return buildingData[myBuilding].costs[level - 1];
    }

    // Produce resources per tick
    public void ProduceResources(ResourceManager resourceManager)
    {
        if (myBuilding == BuildingType.None)
        {
            return;
        }
        //foreach (var output in buildingData[myBuilding].outputs.ToList())
        //{
        //    resourceManager.IncrementResource(output.Key, output.Value * Level);
        //}
    }

    public void UpdateStats()
    {
        if(myBuilding == BuildingType.None)
        {
            return;
        }
        foreach (var effect in buildingData[myBuilding].effects)
        {
            manager.stats[effect.Key] += (effect.Value * (double) Level);
        }
    }

    public static void InitializeBuildingData(CityPlannerManager cityManager)
    {
        if(dataInitalized)
        {
            return;
        }
        // Multipliers for HQ costs

        double HQwoodMultiplier = 1.5;
        double HQironMultiplier = 1.5;
        double HQworkerMultiplier = 1.5;

        // Multipliers for building costs

        double woodMultiplier = 1.5;
        double ironMultiplier = 1.5;
        double workerMultiplier = 1.5;

        if (cityManager != null)
        {
            HQwoodMultiplier *= cityManager.HQWoodMulti;
            HQironMultiplier *= cityManager.HQIronMulti;
            HQworkerMultiplier *= cityManager.HQWorkerMulti;

            woodMultiplier *= cityManager.woodMulti;
            ironMultiplier *= cityManager.ironMulti;
            workerMultiplier *= cityManager.workerMulti;
        }

        dataInitalized = true;
        //Start with HQ and continue level scaling to level 30.
        buildingData.Add(BuildingType.HQ, new BuildingData(0));
        buildingData[BuildingType.HQ].effects.Add(Stat.MaxBuildings, 1);
        buildingData[BuildingType.HQ].costs.Add(new Dictionary<ResourceType, double> { { ResourceType.Wood, 50 } }); // level 1 - Build cost
        string costString = "HQ costs: \n";
        for (int level = 2; level <= 30; level++)
        {
            Dictionary<ResourceType, double> costs = new Dictionary<ResourceType, double>();

            // Calculate cost scaling for each resource
            double woodCostScaling = System.Math.Pow(HQwoodMultiplier, level - 1);
            double ironCostScaling = System.Math.Pow(HQironMultiplier, level - 1);
            double workerCostScaling = System.Math.Pow(HQworkerMultiplier, level - 1);

            // Add resource costs
            costs.Add(ResourceType.Wood, 50 * woodCostScaling);
            costs.Add(ResourceType.Iron, 50 * ironCostScaling);

            // Add workers cost at level 9
            if (level >= 9)
            {
                costs.Add(ResourceType.Workers, 3 * workerCostScaling);
            }
            costString += (int)costs.GetValueOrDefault(ResourceType.Wood, 0) + ", " + (int)costs.GetValueOrDefault(ResourceType.Iron, 0) + ", " + (int)costs.GetValueOrDefault(ResourceType.Workers, 0) + "\n";

            // Add the building data entry
            buildingData[BuildingType.HQ].costs.Add(costs);
        }
        Debug.Log(costString);

        //Woodcutter Upgrade costs. Level 9 introduces Workers cost at 12. Continues to level 30
        buildingData.Add(BuildingType.WoodCutter, new BuildingData(0));
        buildingData[BuildingType.WoodCutter].effects.Add(Stat.WoodProduction, 0.5d);
        buildingData[BuildingType.WoodCutter].costs.Add(new Dictionary<ResourceType, double> { { ResourceType.Wood, 40 } }); // level 1
        costString = "WoodCutter costs: \n";
        for (int level = 2; level <= 30; level++)
        {
            Dictionary<ResourceType, double> costs = new Dictionary<ResourceType, double>();

            // Calculate cost scaling for each resource
            double woodCostScaling = System.Math.Pow(woodMultiplier, level - 1);
            double ironCostScaling = System.Math.Pow(ironMultiplier, level - 1);
            double workerCostScaling = System.Math.Pow(workerMultiplier, level - 1);

            // Add resource costs
            costs.Add(ResourceType.Wood, 40 * woodCostScaling);

            // Add Iron cost at level 2
            if (level >= 2)
            {
                costs.Add(ResourceType.Iron, 10 * ironCostScaling);
            }

            // Add workers cost at level 9
            if (level >= 9)
            {
                costs.Add(ResourceType.Workers, 3 * workerCostScaling);
            }
            costString += (int)costs.GetValueOrDefault(ResourceType.Wood, 0) + ", " + (int)costs.GetValueOrDefault(ResourceType.Iron, 0) + ", " + (int)costs.GetValueOrDefault(ResourceType.Workers, 0) + "\n";

            // Add the building data entry
            buildingData[BuildingType.WoodCutter].costs.Add(costs);
        }
        Debug.Log(costString);

        //mine upgrade costs. level 9 introduces workers cost at 12 and upgrades to level 30.
        buildingData.Add(BuildingType.Mine, new BuildingData(0));
        buildingData[BuildingType.Mine].effects.Add(Stat.IronProduction, 0.5d);
        //buildingData[BuildingType.Mine].outputs = new Dictionary<ResourceType, double> { { ResourceType.Iron, 0.5d } };
        buildingData[BuildingType.Mine].costs.Add(new Dictionary<ResourceType, double> { { ResourceType.Wood, 40 } }); // level 1, Also the cost to build
        costString = "Mine costs: \n";
        for (int level = 2; level <= 30; level++)
        {
            Dictionary<ResourceType, double> costs = new Dictionary<ResourceType, double>();

            // Calculate cost scaling for each resource
            double woodCostScaling = System.Math.Pow(woodMultiplier, level - 1);
            double ironCostScaling = System.Math.Pow(ironMultiplier, level - 1);
            double workerCostScaling = System.Math.Pow(workerMultiplier, level - 1);

            // Add resource costs
            costs.Add(ResourceType.Wood, 40 * woodCostScaling);
            costs.Add(ResourceType.Iron, 10 * ironCostScaling);

            // Add workers cost at level 9
            if (level >= 9)
            {
                costs.Add(ResourceType.Workers, 3 * workerCostScaling);
            }
            costString += (int)costs.GetValueOrDefault(ResourceType.Wood, 0) + ", " + (int)costs.GetValueOrDefault(ResourceType.Iron, 0) + ", " + (int)costs.GetValueOrDefault(ResourceType.Workers, 0) + "\n";

            // Add the building data entry
            buildingData[BuildingType.Mine].costs.Add(costs);
        }
        Debug.Log(costString);


        buildingData.Add(BuildingType.Storage, new BuildingData(3));
        buildingData[BuildingType.Storage].effects.Add(Stat.WoodStorage, 1000);
        buildingData[BuildingType.Storage].effects.Add(Stat.IronStorage, 1000);
        buildingData[BuildingType.Storage].costs.Add(new Dictionary<ResourceType, double> { { ResourceType.Wood, 80 } , { ResourceType.Iron, 20 } }); // level 1 - Build cost
        costString = "Storage costs: \n";
        for (int level = 2; level <= 30; level++)
        {
            Dictionary<ResourceType, double> costs = new Dictionary<ResourceType, double>();

            // Calculate cost scaling for each resource
            double woodCostScaling = System.Math.Pow(woodMultiplier, level - 1);
            double ironCostScaling = System.Math.Pow(ironMultiplier, level - 1);
            double workerCostScaling = System.Math.Pow(workerMultiplier, level - 1);

            // Add resource costs
            costs.Add(ResourceType.Wood, 80 * woodCostScaling);
            costs.Add(ResourceType.Iron, 20 * ironCostScaling);

            // Add workers cost at level 4
            if (level >= 4)
            {
                costs.Add(ResourceType.Workers, 3 * workerCostScaling);
            }
            costString += (int)costs.GetValueOrDefault(ResourceType.Wood, 0) + ", " + (int)costs.GetValueOrDefault(ResourceType.Iron, 0) + ", " + (int)costs.GetValueOrDefault(ResourceType.Workers, 0) + "\n";

            // Add the building data entry
            buildingData[BuildingType.Storage].costs.Add(costs);
        }
        Debug.Log(costString);

        buildingData.Add(BuildingType.TrainingCenter, new BuildingData(4));
        buildingData[BuildingType.TrainingCenter].costs.Add(new Dictionary<ResourceType, double> { { ResourceType.Wood, 100 }, { ResourceType.Iron, 150 } }); // level 1 - Build cost
        //buildingData[BuildingType.TrainingCenter].outputs = new Dictionary<ResourceType, double> { { ResourceType.Soldiers, 0.05d } };
        for (int level = 2; level <= 30; level++)
        {
            Dictionary<ResourceType, double> costs = new Dictionary<ResourceType, double>();

            // Calculate cost scaling for each resource
            double woodCostScaling = System.Math.Pow(woodMultiplier, level - 1);
            double ironCostScaling = System.Math.Pow(ironMultiplier, level - 1);
            double workerCostScaling = System.Math.Pow(workerMultiplier, level - 1);

            // Add resource costs
            costs.Add(ResourceType.Wood, 100 * woodCostScaling);
            costs.Add(ResourceType.Iron, 150 * ironCostScaling);

            // Add workers cost at level 4
            if (level >= 4)
            {
                costs.Add(ResourceType.Workers, 27 * workerCostScaling);
            }

            // Add the building data entry
            buildingData[BuildingType.TrainingCenter].costs.Add(costs);
        }

        buildingData.Add(BuildingType.Barracks, new BuildingData(5));
        buildingData[BuildingType.Barracks].effects.Add(Stat.SoldierStorage, 250);
        buildingData[BuildingType.Barracks].costs.Add(new Dictionary<ResourceType, double> { { ResourceType.Wood, 500 }, { ResourceType.Iron, 500 } }); // level 1 - Build cost
        for (int level = 2; level <= 30; level++)
        {
            Dictionary<ResourceType, double> costs = new Dictionary<ResourceType, double>();

            // Calculate cost scaling for each resource
            double woodCostScaling = System.Math.Pow(woodMultiplier, level - 1);
            double ironCostScaling = System.Math.Pow(ironMultiplier, level - 1);
            double workerCostScaling = System.Math.Pow(workerMultiplier, level - 1);

            // Add resource costs
            costs.Add(ResourceType.Wood, 500 * woodCostScaling);
            costs.Add(ResourceType.Iron, 500 * ironCostScaling);

            // Add workers cost at level 4
            if (level >= 4)
            {
                costs.Add(ResourceType.Workers, 27 * workerCostScaling);
            }

            // Add the building data entry
            buildingData[BuildingType.Barracks].costs.Add(costs);
        }

        buildingData.Add(BuildingType.Tavern, new BuildingData(5));
        buildingData[BuildingType.Tavern].effects.Add(Stat.WorkerProduction, 0.01d);
        buildingData[BuildingType.Tavern].costs.Add(new Dictionary<ResourceType, double> { { ResourceType.Wood, 150d }, { ResourceType.Iron, 100d } }); // level 1 - Build cost
        costString = "Tavern costs: \n";
        //buildingData[BuildingType.Tavern].outputs = new Dictionary<ResourceType, double> { { ResourceType.Workers, 0.01d } };
        for (int level = 2; level <= 30; level++)
        {
            Dictionary<ResourceType, double> costs = new Dictionary<ResourceType, double>();

            // Calculate cost scaling for each resource
            double woodCostScaling = System.Math.Pow(woodMultiplier, level - 1);
            double ironCostScaling = System.Math.Pow(ironMultiplier, level - 1);
            double workerCostScaling = System.Math.Pow(workerMultiplier, level - 1);

            // Add resource costs
            costs.Add(ResourceType.Wood, 150d * woodCostScaling);
            costs.Add(ResourceType.Iron, 100d * ironCostScaling);

            // Add workers cost at level 4
            if (level >= 4)
            {
                costs.Add(ResourceType.Workers, 33d * workerCostScaling);
            }
            costString += (int)costs.GetValueOrDefault(ResourceType.Wood, 0) + ", " + (int)costs.GetValueOrDefault(ResourceType.Iron, 0) + ", " + (int)costs.GetValueOrDefault(ResourceType.Workers, 0) + "\n";

            // Add the building data entry
            buildingData[BuildingType.Tavern].costs.Add(costs);
        }
        Debug.Log(costString);

        buildingData.Add(BuildingType.House, new BuildingData(6));
        buildingData[BuildingType.House].effects.Add(Stat.WorkerStorage, 500d);
        buildingData[BuildingType.House].costs.Add(new Dictionary<ResourceType, double> { { ResourceType.Wood, 500d }, { ResourceType.Iron, 400d } }); // level 1 - Build cost
        costString = "House costs: \n";
        for (int level = 2; level <= 30; level++)
        {
            Dictionary<ResourceType, double> costs = new Dictionary<ResourceType, double>();

            // Calculate cost scaling for each resource
            double woodCostScaling = System.Math.Pow(woodMultiplier, level - 1);
            double ironCostScaling = System.Math.Pow(ironMultiplier, level - 1);
            double workerCostScaling = System.Math.Pow(workerMultiplier, level - 1);

            // Add resource costs
            costs.Add(ResourceType.Wood, 500d * woodCostScaling);
            costs.Add(ResourceType.Iron, 400d * ironCostScaling);

            // Add workers cost at level 4
            if (level >= 4)
            {
                costs.Add(ResourceType.Workers, 3d * workerCostScaling);
            }
            costString += (int)costs.GetValueOrDefault(ResourceType.Wood, 0) + ", " + (int)costs.GetValueOrDefault(ResourceType.Iron, 0) + ", " + (int)costs.GetValueOrDefault(ResourceType.Workers, 0) + "\n";

            // Add the building data entry
            buildingData[BuildingType.House].costs.Add(costs);
        }
        Debug.Log(costString);

        //Command center
        //Requires a level 10 village
        //Wood 500
        //Iron 1,000
        //Worker 15

        buildingData.Add(BuildingType.GuardTower, new BuildingData(12));
        //buildingData[BuildingType.Barracks].effects.Add(Stat.SoldierStorage, 250);
        for (int level = 2; level <= 30; level++)
        {
            Dictionary<ResourceType, double> costs = new Dictionary<ResourceType, double>();

            // Calculate cost scaling for each resource
            double woodCostScaling = System.Math.Pow(woodMultiplier, level - 1);
            double ironCostScaling = System.Math.Pow(ironMultiplier, level - 1);
            double workerCostScaling = System.Math.Pow(workerMultiplier, level - 1);

            // Add resource costs
            costs.Add(ResourceType.Wood, 500 * woodCostScaling);
            costs.Add(ResourceType.Iron, 1000 * ironCostScaling);
            costs.Add(ResourceType.Workers, 10 * workerCostScaling);

            // Add the building data entry
            buildingData[BuildingType.GuardTower].costs.Add(costs);
        }

        buildingData.Add(BuildingType.TownHall, new BuildingData(15));
        buildingData[BuildingType.TownHall].effects.Add(Stat.WoodProduction, 0.03d);
        buildingData[BuildingType.TownHall].effects.Add(Stat.IronProduction, 0.03d);
        for (int level = 2; level <= 30; level++)
        {
            Dictionary<ResourceType, double> costs = new Dictionary<ResourceType, double>();

            // Calculate cost scaling for each resource
            double woodCostScaling = System.Math.Pow(woodMultiplier, level - 1);
            double ironCostScaling = System.Math.Pow(ironMultiplier, level - 1);
            double workerCostScaling = System.Math.Pow(workerMultiplier, level - 1);

            // Add resource costs
            costs.Add(ResourceType.Wood, 1000 * woodCostScaling);
            costs.Add(ResourceType.Iron, 600 * ironCostScaling);
            costs.Add(ResourceType.Workers, 15 * workerCostScaling);

            // Add the building data entry
            buildingData[BuildingType.TownHall].costs.Add(costs);
        }

        buildingData.Add(BuildingType.MerceneryOffice, new BuildingData(18));
        for (int level = 2; level <= 30; level++)
        {
            Dictionary<ResourceType, double> costs = new Dictionary<ResourceType, double>();

            // Calculate cost scaling for each resource
            double woodCostScaling = System.Math.Pow(woodMultiplier, level - 1);
            double ironCostScaling = System.Math.Pow(ironMultiplier, level - 1);
            double workerCostScaling = System.Math.Pow(workerMultiplier, level - 1);

            // Add resource costs
            costs.Add(ResourceType.Wood, 300 * woodCostScaling);
            costs.Add(ResourceType.Iron, 300 * ironCostScaling);
            costs.Add(ResourceType.Workers, 10 * workerCostScaling);

            // Add the building data entry
            buildingData[BuildingType.MerceneryOffice].costs.Add(costs);
        }

    }
}

public enum BuildingType
{
    None,
    HQ,
    WoodCutter,
    Mine,
    Storage,
    House,
    Tavern,
    TrainingCenter,
    Barracks,
    GuardTower,
    Arena,
    TownHall,
    MerceneryOffice,
}



