using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

public class CityPlannerManager : MonoBehaviour
{
    [Header("Planning")]
    public int planningDepth = 0;
    public int maxTicks = 12960; //3 days. 1 tick = 20 seconds.
    [Header("City Data")]
    public int totalCitiesCreated = 1;
    public int duplicateCitiesDeleted = 0;
    public int citiesInQueue = 0;

    public List<CityData> cityDataList = new List<CityData>();
    public List<CityData> cityQueue = new List<CityData>();
    public List<CityData> newQueue = new List<CityData>();

    public CityData bestCity = null;

    [Header("Multipliers")]
    public double HQIronMulti = 1;
    public double HQWoodMulti = 1, HQWorkerMulti = 1;
    public double ironMulti = 1, woodMulti = 1, workerMulti = 1;

    [Header("Specialization")]
    public int hqBuyMinLevelForTavern = 7;
    public int hqBuyMinLevelForHouse = 14;
    public int hqUpgradeMinLevelForTavern = 7;
    public int hqUpgradeMnLevelForHouse = 14;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Building.InitializeBuildingData(this);
        CityData cityData = new CityData(cityDataList.Count+1);
        cityData.stats = new Dictionary<Stat, float>
        {
            { Stat.Ticks, 0 },
            { Stat.WoodStorage, 1140 },
            { Stat.WoodStorageMulti, 1.0f },
            { Stat.WoodProduction, 1.5f },
            { Stat.WoodMulti, 1.0f },
            { Stat.WoodCount, 260 },
            { Stat.IronStorage, 1140 },
            { Stat.IronStorageMulti, 1.0f },
            { Stat.IronProduction, 0.5f },
            { Stat.IronMulti, 1.0f },
            { Stat.IronCount, 300 },
            { Stat.SoldierStorage, 50 },
            { Stat.SoldierProduction, 0  },
            { Stat.SoldierMulti, 1 },
            { Stat.SoldierCount, 0 },
            { Stat.WorkerStorage, 50 },
            { Stat.WorkerStorageMulti, 1.0f },
            { Stat.WorkerProduction, 0 },
            { Stat.WorkerMulti, 1.0f },
            { Stat.WorkerCount, 0 },
            { Stat.MaxBuildings, 5 },
            { Stat.BuildingCount, 4 },
        };
        cityData.buildings = new List<Building>
        {
            new Building { myBuilding = BuildingType.HQ, Level = 5 },
            new Building { myBuilding = BuildingType.WoodCutter, Level = 2 },
            new Building { myBuilding = BuildingType.Mine, Level = 1 },
            new Building { myBuilding = BuildingType.Storage, Level = 1 }
        };
        //cityData.buildOrder = "Do Tutorial\n";
        cityDataList.Add(cityData);
        cityQueue.Add(cityData);
        bestCity = cityData;
        StartCoroutine(ProcessCityQueue());
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            string stats = string.Join("\n", bestCity.stats.Select(stat => $"{stat.Key}: {stat.Value}"));
            //Debug.Log($"BestCity id: {bestCity.id}\nBuilding count: {bestCity.buildings.Count}\nTicks Spent: {bestCity.stats[Stat.Ticks]}\nBuild order:\n{bestCity.buildOrder}\n\nStats:\n{stats}");
            Debug.Log($"BestCity id: {bestCity.id}\nBuilding count: {bestCity.buildings.Count}\nTicks Spent: {bestCity.stats[Stat.Ticks]}\n\nStats:\n{stats}");
        }
    }

    private IEnumerator ProcessCityQueue()
    {
        while (true)
        {
            if (cityQueue.Count > 0)
            {
                newQueue = new List<CityData>();
                for (int i = 0; i < cityQueue.Count; i++)
                {
                    CityData city = cityQueue[i];
                    // Ensure the HQ building is at index 0
                    if (city.buildings[0].myBuilding != BuildingType.HQ)
                    {
                        Debug.Log("HQ is not in the first slot. Reordering buildings...");
                        Building hqBuilding = city.buildings.FirstOrDefault(b => b.myBuilding == BuildingType.HQ);
                        if (hqBuilding == null)
                        {
                            Debug.LogError("City does not have an HQ building. Skipping city.");
                            continue;
                        }
                        int hqIndex = city.buildings.IndexOf(hqBuilding);
                        city.buildings[hqIndex] = city.buildings[0];
                        city.buildings[0] = hqBuilding;
                    }

                    int HQLevel = city.buildings[0].Level;
                    if (city.stats[Stat.MaxBuildings] > city.stats[Stat.BuildingCount])
                    {
                        AddNewBuildingToCity(city, BuildingType.WoodCutter);
                        AddNewBuildingToCity(city, BuildingType.Mine);
                        if (HQLevel >= hqBuyMinLevelForTavern && city.buildings.Count(b => b.myBuilding == BuildingType.Tavern) < 1)
                            AddNewBuildingToCity(city, BuildingType.Tavern);
                        if (HQLevel >= hqBuyMinLevelForHouse && city.buildings.Count(b => b.myBuilding == BuildingType.House) < 1)
                            AddNewBuildingToCity(city, BuildingType.House);
                        if (HQLevel >= 13 && city.buildings.Count(b => b.myBuilding == BuildingType.Storage) < 2)
                            AddNewBuildingToCity(city, BuildingType.Storage);
                    }
                    else
                    {
                        UpgradeLowestLevelBuildings(city);
                    }

                    void UpgradeLowestLevelBuildings(CityData city)
                    {
                        var buildingsToUpgrade = city.buildings
                            .Where(b => b.myBuilding == BuildingType.WoodCutter || b.myBuilding == BuildingType.Mine || b.myBuilding == BuildingType.Storage || b.myBuilding == BuildingType.Tavern || b.myBuilding == BuildingType.House || b.myBuilding == BuildingType.HQ)
                            .OrderBy(b => b.Level)
                            .ToList();

                        foreach (var building in buildingsToUpgrade)
                        {
                            int buildingIndex = city.buildings.IndexOf(building);
                            UpgradeBuildingForCity(city, buildingIndex);
                        }
                    }

                    if (i % 1000 == 0)
                    {


                        yield return new WaitForSeconds(0.001f);
                    }
                }

                planningDepth += 1;
                //if (planningDepth == 15)
                //{
                //    //if (newQueue.Count > 100000)
                //    //{
                //        newQueue = new List<CityData> { bestCity };
                //    //}
                //}


                // Remove duplicate cities from newQueue
                //                    .GroupBy<CityData, List<object>>(city => city.buildings
                int initialQueueCount = newQueue.Count;
                newQueue = newQueue
                    .GroupBy(city => new
                    {
                        Buildings = string.Join(",", city.buildings
                            .OrderBy(b => b.myBuilding)
                            .ThenBy(b => b.Level)
                            .Select(b => $"{b.myBuilding}-{b.Level}")),
                        Stats = string.Join(",", city.stats
                            .OrderBy(s => s.Key)
                            .Select(s => $"{s.Key}-{s.Value}"))
                    })
                    .Select(g => g.First())
                    .ToList();

                duplicateCitiesDeleted += initialQueueCount - newQueue.Count;

                citiesInQueue = newQueue.Count;

                // Log the results
                Debug.Log("Finished processing queue. New queue size: " + newQueue.Count + ", From: " + cityQueue.Count);

                cityQueue = newQueue;
            }
            yield return null;
        }

    }

    private void AddNewBuildingToCity(CityData city, BuildingType newBuilding)
    {
        if (city == null)
        {
            Debug.Log("[Build] City is null");
            return;
        }


        // Check the ratio of WoodCutters to Mines
        int woodCutterCount = 0;
        int mineCount = 0;
        foreach (var building in city.buildings)
        {
            if (building.myBuilding == BuildingType.WoodCutter)
                woodCutterCount++;
            else if (building.myBuilding == BuildingType.Mine)
                mineCount++;
        }

        ////For default values, dev testing
        //if (newBuilding == BuildingType.WoodCutter && (woodCutterCount > 5 || woodCutterCount > 2 * mineCount + 1))
        //{
        //    return;
        //}
        //if (newBuilding == BuildingType.Mine && (mineCount > 5 || mineCount > (woodCutterCount + 1) / 2))
        //{
        //    return;
        //}

        float cityWoodStorage = city.GetWoodStorage();
        float cityIronStorage = city.GetIronStorage();
        float cityWorkerStorage = city.GetWorkerStorage();

        var buildingCosts = Building.buildingData[newBuilding].costs[0];

        float woodCost = buildingCosts.ContainsKey(ResourceType.Wood) ? (float) buildingCosts[ResourceType.Wood] : 0;
        float ironCost = buildingCosts.ContainsKey(ResourceType.Iron) ? (float) buildingCosts[ResourceType.Iron] : 0;
        float workersCost = buildingCosts.ContainsKey(ResourceType.Workers) ? (float) buildingCosts[ResourceType.Workers] : 0;

        if (workersCost > 0 && city.WorkersProduction() == 0)
        {
            return; //do not build if we have no workers.
        }

        float woodRequired = Math.Max(0, woodCost - city.stats[Stat.WoodCount]);
        float ironRequired = Math.Max(0, ironCost - city.stats[Stat.IronCount]);
        float workersRequired = Math.Max(0, workersCost - city.stats[Stat.WorkerCount]);

        if (woodCost < 0 || ironCost < 0 || workersCost < 0)
        {
            Debug.LogError("[Build] Building costs cannot be negative.");
            return;
        }

        if (woodRequired > cityWoodStorage || ironRequired > cityIronStorage || workersRequired > cityWorkerStorage)
        {
            return;
        }

        float woodTime = woodRequired > 0 ? woodRequired / city.WoodProduction() : 0;
        float ironTime = ironRequired > 0 ? ironRequired / city.IronProduction() : 0;
        float workerTime = workersRequired > 0 ? workersRequired / city.WorkersProduction() : 0;


        float maxTime = Mathf.Ceil(Mathf.Max(woodTime, Mathf.Max(ironTime, workerTime)) + 1f);

        if (maxTime < 0) maxTime = 0;
        if(city.stats[Stat.Ticks] + maxTime > maxTicks)
        {
            return; //do not build if we run out of time.
        }

        CityData newCity = new CityData(totalCitiesCreated)
        {
            stats = new Dictionary<Stat, float>(city.stats.ToDictionary(entry => entry.Key, entry => entry.Value)),
            buildings = city.buildings.Select(b => new Building { myBuilding = b.myBuilding, Level = b.Level }).ToList(),
            //buildOrder = city.buildOrder
        };

        newCity.stats[Stat.WoodCount] = Mathf.Min(city.stats[Stat.WoodCount] + city.WoodProduction() * maxTime, cityWoodStorage) - woodCost;
        newCity.stats[Stat.IronCount] = Mathf.Min(city.stats[Stat.IronCount] + city.IronProduction() * maxTime, cityIronStorage) - ironCost;
        newCity.stats[Stat.WorkerCount] = Mathf.Min(city.stats[Stat.WorkerCount] + city.WorkersProduction() * maxTime, cityWorkerStorage) - workersCost;
        newCity.stats[Stat.Ticks] = city.stats[Stat.Ticks] + maxTime;
        newCity.stats[Stat.BuildingCount] += 1;

        //newCity.buildOrder += "Build " + newBuilding + ". Ticks: " + newCity.stats[Stat.Ticks] + "\n";

        newCity.buildings.Add(new Building { myBuilding = newBuilding, Level = 1 });

        foreach (var keyValuePair in Building.buildingData[newBuilding].effects)
        {
            if (newCity.stats.ContainsKey(keyValuePair.Key))
            {
                newCity.stats[keyValuePair.Key] += (float) keyValuePair.Value;
            }
            else
            {
                newCity.stats[keyValuePair.Key] = (float) keyValuePair.Value;
            }
        }

        if (newCity.stats[Stat.Ticks] < maxTicks)
        {
            totalCitiesCreated += 1;
            newQueue.Add(newCity);
            if (newCity.buildings[0].Level >= bestCity.buildings[0].Level && (newCity.buildings[0].Level > bestCity.buildings[0].Level || newCity.stats[Stat.Ticks] < bestCity.stats[Stat.Ticks]))
            {
                bestCity = newCity;
                Debug.Log($"New best city found. ID: {bestCity.id} with HQ level: {bestCity.buildings[0].Level} in {bestCity.stats[Stat.Ticks]} ticks");
                //Debug.Log($"City id: {bestCity.id}\nBuilding count: {bestCity.buildings.Count}\nTicks Spent: {bestCity.stats[Stat.Ticks]}\nBuild order:\n{bestCity.buildOrder}");
                Debug.Log($"City id: {bestCity.id}\nBuilding count: {bestCity.buildings.Count}\nTicks Spent: {bestCity.stats[Stat.Ticks]}");
            }
        }
    }

    private void UpgradeBuildingForCity(CityData city, int buildingIndex)
    {
        if (city == null)
        {
            Debug.Log("[Upgrade] City is null");
            return;
        }

        Building building = city.buildings[buildingIndex];

        int hqLevel = city.buildings[0].Level;

        float cityWoodStorage = city.GetWoodStorage();
        float cityIronStorage = city.GetIronStorage();
        float cityWorkerStorage = city.GetWorkerStorage();

        // Check if the building can be upgraded based on HQ level
        if ((building.myBuilding == BuildingType.Mine || building.myBuilding == BuildingType.WoodCutter) && building.Level >= hqLevel)
            return;
        //if (building.myBuilding == BuildingType.Tavern && building.Level >= hqUpgradeMinLevelForTavern)
        //    return;
        //if (building.myBuilding == BuildingType.House && building.Level >= hqUpgradeMnLevelForHouse)
        //    return;

        //This section of code is designed so that it allows upgrading the storage, Only if the cost of the next HQ is higher than one of our resource costs.
        if (building.myBuilding == BuildingType.Storage)
        {
            var nextHqLevelCosts = city.buildings[0].CostsForLevel(hqLevel + 1);
            if ((!nextHqLevelCosts.ContainsKey(ResourceType.Wood) || cityWoodStorage >= nextHqLevelCosts[ResourceType.Wood]) &&
                (!nextHqLevelCosts.ContainsKey(ResourceType.Iron) || cityIronStorage >= nextHqLevelCosts[ResourceType.Iron]) &&
                (!nextHqLevelCosts.ContainsKey(ResourceType.Workers) || cityWorkerStorage >= nextHqLevelCosts[ResourceType.Workers]))
            {
                return;
            }
        }




        var buildingCosts = building.CostsForLevel(building.Level + 1);

        float woodCost = (float) buildingCosts.GetValueOrDefault(ResourceType.Wood, 0f);
        float ironCost = (float) buildingCosts.GetValueOrDefault(ResourceType.Iron, 0f);
        float workersCost = (float) buildingCosts.GetValueOrDefault(ResourceType.Workers, 0f);

        if (workersCost > 0 && city.stats[Stat.WorkerProduction] == 0)
            return; // Do not upgrade if we produce no workers

        //Calculates the difference between the cost of the building and the resources we have.
        float woodRequired = Mathf.Max(0f, woodCost - city.stats[Stat.WoodCount]);
        float ironRequired = Mathf.Max(0f, ironCost - city.stats[Stat.IronCount]);
        float workersRequired = Mathf.Max(0f, workersCost - city.stats[Stat.WorkerCount]);

        if (woodCost > cityWoodStorage || ironCost > cityIronStorage || workersCost > cityWorkerStorage)
            return;

        // Calculate the time to produce the difference (required) resources.
        float woodTime = woodRequired > 0f ? woodRequired / city.WoodProduction() : 0f;
        float ironTime = ironRequired > 0f ? ironRequired / city.IronProduction() : 0f;
        float workerTime = workersRequired > 0f ? workersRequired / city.WorkersProduction() : 0f;

        float maxTime = Mathf.Ceil(Mathf.Max(woodTime, Mathf.Max(ironTime, workerTime)) + 1f);
        if (city.stats[Stat.Ticks] + maxTime > maxTicks || maxTime > 750)
        {
            return; //do not upgrade if we run out of time.
        }
        //if (city.buildings[0].Level > 8 && (building.myBuilding == BuildingType.Tavern || building.myBuilding == BuildingType.House))
        //{
        //    Debug.Log($"Building {building.myBuilding} is not being built/upgraded even though HQ level is higher than 8.");
        //}
        float maxWaitTime = 200;
        if (building.myBuilding == BuildingType.HQ)
        {
            maxWaitTime = 350;
            if (hqLevel > 10)
                maxWaitTime += ( 60 * (hqLevel-10)); // 10 minutes per HQ level
                //maxWaitTime = 50 + (20 * 3 * hqLevel); // 10 minutes per HQ level
            //else
            //    maxWaitTime = 15 + (12 * 3 * hqLevel); // 10 minutes per HQ level
        }
        else if (building.myBuilding == BuildingType.WoodCutter || building.myBuilding == BuildingType.Mine)
        {
            maxWaitTime = 20 + (5 * building.Level); // 3 minutes per HQ level
        }
        else if (building.myBuilding == BuildingType.Tavern || building.myBuilding == BuildingType.House)
        {
            maxWaitTime = 15 + (3 * building.Level); // 3 minutes per HQ level
        }
        else if (building.myBuilding == BuildingType.Storage)
        {
            maxWaitTime = 15 + (3 * building.Level); // 12 minutes per HQ level
        }

        if (maxTime > maxWaitTime)
        {
            return; //do not waste time on upgrades that take too long.
        }

        CityData newCity = new CityData(totalCitiesCreated)
        {
            stats = new Dictionary<Stat, float>(city.stats.ToDictionary(entry => entry.Key, entry => entry.Value)),
            buildings = city.buildings.Select(b => new Building { myBuilding = b.myBuilding, Level = b.Level }).ToList(),
            //buildOrder = city.buildOrder
        };

        newCity.buildings[buildingIndex].Level += 1;

        newCity.stats[Stat.WoodCount] = Mathf.Min(city.stats[Stat.WoodCount] + city.WoodProduction() * maxTime, cityWoodStorage) - woodCost;
        newCity.stats[Stat.IronCount] = Mathf.Min(city.stats[Stat.IronCount] + city.IronProduction() * maxTime, cityIronStorage) - ironCost;
        newCity.stats[Stat.WorkerCount] = Mathf.Min(city.stats[Stat.WorkerCount] + city.WorkersProduction() * maxTime, cityWorkerStorage) - workersCost;
        newCity.stats[Stat.Ticks] = city.stats[Stat.Ticks] + maxTime;

        //newCity.buildOrder += $"Upgrade {building.myBuilding} to level {newCity.buildings[buildingIndex].Level}. Ticks: {newCity.stats[Stat.Ticks]}";
        //newCity.buildOrder += $", Wood: {Math.Floor(newCity.stats[Stat.WoodCount])}, Iron: {Math.Floor(newCity.stats[Stat.IronCount])}, Workers: {Math.Floor(newCity.stats[Stat.WorkerCount])}\n";

        foreach (var keyValuePair in Building.buildingData[building.myBuilding].effects)
        {
            if (newCity.stats.ContainsKey(keyValuePair.Key))
            {
                newCity.stats[keyValuePair.Key] += (float) keyValuePair.Value;
            }
            else
            {
                newCity.stats[keyValuePair.Key] = (float) keyValuePair.Value;
            }
        }

        if (newCity.stats[Stat.Ticks] <= maxTicks)
        {
            totalCitiesCreated += 1;
            newQueue.Add(newCity);
            if (newCity.buildings[0].Level >= bestCity.buildings[0].Level && (newCity.buildings[0].Level > bestCity.buildings[0].Level || newCity.stats[Stat.Ticks] < bestCity.stats[Stat.Ticks]))
            {
                bestCity = newCity;
                Debug.Log($"New best city found. ID: {bestCity.id} with HQ level: {bestCity.buildings[0].Level} in {bestCity.stats[Stat.Ticks]} ticks");
                //Debug.Log($"City id: {bestCity.id}, Depth: {planningDepth}\nBuilding count: {bestCity.buildings.Count}\nTicks Spent: {bestCity.stats[Stat.Ticks]}\nBuild order:\n{bestCity.buildOrder}");
                Debug.Log($"City id: {bestCity.id}, Depth: {planningDepth}\nBuilding count: {bestCity.buildings.Count}\nTicks Spent: {bestCity.stats[Stat.Ticks]}");
            }
        }
        else
        {
            Debug.Log("Redundant code, this code shouldnt be executed since we return the same check earlier");
        }
    }
}

public class SequenceEqualityComparer : IEqualityComparer<List<object>>
{
    public bool Equals(List<object> x, List<object> y)
    {
        if (x == null || y == null)
            return false;
        if (x.Count != y.Count)
            return false;
        for (int i = 0; i < x.Count; i++)
        {
            if (!x[i].Equals(y[i]))
                return false;
        }
        return true;
    }

    public int GetHashCode(List<object> obj)
    {
        if (obj == null)
            return 0;
        int hash = 17;
        foreach (var item in obj)
        {
            hash = hash * 31 + (item == null ? 0 : item.GetHashCode());
        }
        return hash;
    }
}


