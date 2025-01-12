using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.VisualScripting;
using System.Linq;
using UnityEngine.UIElements;

public class CityManager : Agent
{
    [SerializeField] private double myWood=50, myIron=50, myWorkers=0, tickNumber=0, score=0, actionCode=0, totalLevel=6,buildingCount=4, maxBuildings=4, bestScore=0;

    [SerializeField] private string actionCodeHistory = "";

    [SerializeField]
    [TextArea(3, 15)] // Make the text area 3 lines tall
    private string m_TextArea = ""; // The string itself

    private List<Building> buildings = new List<Building>();
    private ResourceManager myResources = new ResourceManager();

    public Building HQ;
    public Dictionary<Stat, double> stats = new Dictionary<Stat, double>
        {
            { Stat.WoodStorage, 140 },
            { Stat.WoodProduction, 1 },
            { Stat.WoodMulti, 1 },
            { Stat.WoodCount, 50 },
            { Stat.IronStorage, 140 },
            { Stat.IronProduction, 0 },
            { Stat.IronMulti, 1 },
            { Stat.IronCount, 0 },
            { Stat.SoldierStorage, 50 },
            { Stat.SoldierProduction, 0  },
            { Stat.SoldierMulti, 1 },
            { Stat.SoldierCount, 0 },
            { Stat.WorkerStorage, 50 },
            { Stat.WorkerProduction, 0 },
            { Stat.WorkerMulti, 1 },
            { Stat.WorkerCount, 0 },
            { Stat.MaxBuildings, 1 },
            { Stat.BuildingCount, 0 },
        };

    void Start()
    {
        if (!Manager.instance.runAgents)
        {
            Destroy(gameObject);
            return;
        }
        myResources.resources.Add(ResourceType.Wood, new Resource("Wood", ResourceType.Wood, 50));
        myResources.resources.Add(ResourceType.Iron, new Resource("Iron", ResourceType.Iron, 50));
        myResources.resources.Add(ResourceType.Workers, new Resource("Workers", ResourceType.Workers, 0));
        for (int i = 0; i < 15; i++)
        {
            buildings.Add(new Building(this,"None", BuildingType.None, 0)); //Empty slots
        }
        buildings[0] = new Building(this, "HQ", BuildingType.HQ, 5);
        HQ = buildings[0];
        buildings[1] = new Building(this, "Wood Cutter", BuildingType.WoodCutter, 2);
        buildings[2] = new Building(this, "Mine", BuildingType.Mine, 1);
        buildings[3] = new Building(this, "Storage", BuildingType.Storage, 1);
        totalLevel = 9;
        UpdateStats();
        buildingCount = stats[Stat.BuildingCount];
        maxBuildings = stats[Stat.MaxBuildings];
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation((float) myResources.resources[ResourceType.Wood].Amount);
        sensor.AddObservation((float) myResources.resources[ResourceType.Iron].Amount);
        sensor.AddObservation((float) myResources.resources[ResourceType.Workers].Amount);

        sensor.AddObservation((float)stats[Stat.WoodStorage]);
        sensor.AddObservation((float)stats[Stat.IronStorage]);
        sensor.AddObservation((float)stats[Stat.WorkerStorage]);

        sensor.AddObservation((float)stats[Stat.WoodProduction]);
        sensor.AddObservation((float)stats[Stat.IronProduction]);
        sensor.AddObservation((float)stats[Stat.WorkerProduction]);

        for (int i = 0; i < 15; i++)
        {
            //sensor.AddOneHotObservation((int)buildings[i].myBuilding, 7);
            sensor.AddObservation((int)buildings[i].myBuilding);
            sensor.AddObservation((float)buildings[i].Level);
        }

        //sensor.AddObservation((float)stats[Stat.WoodCount]);
        //sensor.AddObservation((float)stats[Stat.IronCount]);
        //sensor.AddObservation((float)stats[Stat.WorkerCount]);
    }

    public override void OnEpisodeBegin()
    {
        //Reset all resource costs in myResources
        myResources.resources[ResourceType.Wood].SetAmount(50);
        myResources.resources[ResourceType.Iron].SetAmount(50);
        myResources.resources[ResourceType.Workers].SetAmount(0);
        score = 0;
        myWood = 50;
        myIron = 50;
        myWorkers = 0;
        tickNumber = 0;
        buildings = new List<Building>();
        for (int i = 0; i < 15; i++)
        {
            buildings.Add(new Building(this, "None", BuildingType.None, 0)); //Empty slots
        }
        buildings[0] = new Building(this, "HQ", BuildingType.HQ, 5);
        HQ = buildings[0];
        buildings[1] = new Building(this, "Wood Cutter", BuildingType.WoodCutter, 2);
        buildings[2] = new Building(this, "Mine", BuildingType.Mine, 1);
        buildings[3] = new Building(this, "Storage", BuildingType.Storage, 1);
        totalLevel = 9;
        UpdateStats();
        buildingCount = stats[Stat.BuildingCount];
        maxBuildings = stats[Stat.MaxBuildings];

    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        actionCode = actions.DiscreteActions[0];
        actionCodeHistory += actionCode;
        if (actionCodeHistory.Length > 20)
        {
            actionCodeHistory = actionCodeHistory.Substring(actionCodeHistory.Length - 20);
        }
        //Debug.Log($"Action received: {actionCode}"); // Log the action received

        bool buyOrUpgrade = false;
        BuildingType buildingType = BuildingType.None;
        switch (actionCode)
        {
            case 0: // Do nothing
                break;
            case 1: // Upgrade HQ
                if (HQ.Upgrade(myResources))
                {
                    Debug.Log("Upgraded HQ to level " + HQ.Level); //Debug
                    float reward = 10f + (HQ.Level*0.5f);
                    if (tickNumber < MaxStep)
                    {
                        reward *= 1f + ((MaxStep - (float) tickNumber) / MaxStep);
                    }
                    SetReward(reward);
                    needUpdateStats = true;
                    totalLevel += 1;

                    if (HQ.Level > 8)
                    {
                        EndEpisode();
                    } //End the episode at level 9 for now as a great position to be in, before moving to the next steps.
                }
                break;
            case 2: // Upgrade a hut
                buyOrUpgrade = true;
                buildingType = BuildingType.WoodCutter;
                break;
            case 3: // upgrade a mine
                buyOrUpgrade = true;
                buildingType = BuildingType.Mine;
                break;
            case 4: // upgrade a tavern
                buyOrUpgrade = true;
                buildingType = BuildingType.Tavern;
                if(HQ.Level <= 9)
                {
                    SetReward(-7f);
                    EndEpisode();
                } //penalize for trying to build a tavern before level 10 (HQ level 10 begins to require workers.)
                break;
            case 5: // upgrade a house
                buyOrUpgrade = true;
                buildingType = BuildingType.House;
                if (HQ.Level <= 10)
                {
                    SetReward(-15f);
                    EndEpisode();
                } //penalize for trying to build a tavern before level 10 (HQ level 10 begins to require workers.)
                break;
            case 6: // upgrade a storage
                buyOrUpgrade = true;
                buildingType = BuildingType.Storage;
                break;
            case 7: // upgrade a barracks
                buyOrUpgrade = true;
                buildingType = BuildingType.Barracks;
                break;

        }
        if (buyOrUpgrade)
        {
            bool canUpgrade = true;
            if (stats[Stat.BuildingCount] < stats[Stat.MaxBuildings])
            {
                //Find first available empty builting slot.

                Dictionary < ResourceType, double> BuildCost = Building.buildingData[buildingType].costs[0]; // build cost for level 0
                if (CanBuild(buildingType, BuildCost))
                {
                    foreach (var building in buildings)
                    {
                        if (building.myBuilding == BuildingType.None)
                        {
                            myResources.SpendResources(BuildCost);
                            building.myBuilding = buildingType;
                            building.Level = 1;
                            building.Name = buildingType.ToString();
                            SetReward(0.5f);
                            //Debug.Log("Built a level 1 " + buildingType.ToString() + " at slot " + buildings.IndexOf(building)); //Debug
                            canUpgrade = false;
                            needUpdateStats = true;
                            totalLevel++;
                            if(buildingType == BuildingType.Storage)
                            {
                                SetReward(-15f);
                                EndEpisode();
                                //penalize for building another storage when we only need 1 until hq level 15.
                            }
                            break;
                        }
                    }
                    if (buildingType == BuildingType.WoodCutter || buildingType == BuildingType.Mine)
                    {
                        int wc = 0; //woodcutters
                        int m = 0; //mines
                        foreach (var building in buildings)
                        {
                            if (building.myBuilding == BuildingType.WoodCutter)
                            {
                                wc++;
                            }
                            else if (building.myBuilding == BuildingType.Mine)
                            {
                                m++;
                            }
                        }
                        if( (wc/2) > m+1)
                        {
                            SetReward(-2f); //penalize for having more woodcutters than mines (2:1 ratio)
                        }
                        else if ((wc / 2) < m + 1)
                        {
                            SetReward(-2f); //penalize for having more mines than woodcutters (2:1 ratio)
                        }
                    } 

                }
            } //End of buy new building. Can't upgrade if we built a new building.
            else
            if (canUpgrade)
            {
                // Didn't find an empty slot, so lets find the first woodcutter building that we can afford, and upgrade it.
                Building b = null;
                int lowest = 999;
                for (int i = 0; i < buildings.Count; i++)
                {
                    if (buildings[i].myBuilding == buildingType && buildings[i].Level < lowest)
                    {
                        b = buildings[i];
                        lowest = buildings[i].Level;
                    }
                }
                if (lowest != 999)
                {
                    if (b.CanUpgrade(myResources))
                    {
                        if (b.Upgrade(myResources))
                        {
                            if(b.Level > HQ.Level+1)
                            {
                                SetReward(-1f);
                            }
                            else
                            {
                                SetReward(b.Level * 0.1f);
                            }
                            needUpdateStats = true;
                            totalLevel++;
                            //Debug.Log("Upgraded a " + buildingType.ToString() + " to level " + b.Level + " at slot " + buildings.IndexOf(b)); //Debug
                        }
                    }
                }
            }
        }
        ProcessTick();
        
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;

        if (Input.GetKey(KeyCode.Alpha1))
        {
            discreteActions[0] = 1;
        }
        else if (Input.GetKey(KeyCode.Alpha2))
        {
            discreteActions[0] = 2;
        }
        else if (Input.GetKey(KeyCode.Alpha3))
        {
            discreteActions[0] = 3;
        }
        else if (Input.GetKey(KeyCode.Alpha4))
        {
            discreteActions[0] = 4;
        }
        else if (Input.GetKey(KeyCode.Alpha5))
        {
            discreteActions[0] = 5;
        }
        else if (Input.GetKey(KeyCode.Alpha6))
        {
            discreteActions[0] = 6;
        }
        else if (Input.GetKey(KeyCode.Alpha7))
        {
            discreteActions[0] = 7;
        }
        else if (Input.GetKey(KeyCode.Alpha8))
        {
            discreteActions[0] = 8;
        }
        else if (Input.GetKey(KeyCode.Alpha9))
        {
            discreteActions[0] = 9;
        }
        else
        {
            discreteActions[0] = 0; // Do nothing if nothing is pressed.
        }
    }


    public bool needUpdateStats = false; // Set to true when a building is added, removed, or upgraded.
    public void AddBuilding(Building building)
    {
        buildings.Add(building);
    }

    public void ProcessTick()
    {
        if(needUpdateStats)
        {
            UpdateStats();
            needUpdateStats = false;
        }
        myResources.IncrementResource(ResourceType.Wood, stats[Stat.WoodProduction] * stats[Stat.WoodMulti]);
        myResources.IncrementResource(ResourceType.Iron, stats[Stat.IronProduction] * stats[Stat.IronMulti]);
        myResources.IncrementResource(ResourceType.Workers, stats[Stat.WorkerProduction] * stats[Stat.WorkerMulti]);

        if (myResources.resources[ResourceType.Wood].Amount >= stats[Stat.WoodStorage])
        {
            myResources.resources[ResourceType.Wood].SetAmount(stats[Stat.WoodStorage]);
            SetReward(-0.05f); //Penalize having max resources and not spending them.
        }
        if (myResources.resources[ResourceType.Iron].Amount >= stats[Stat.IronStorage])
        {
            myResources.resources[ResourceType.Iron].SetAmount(stats[Stat.IronStorage]);
            SetReward(-0.15f); //Penalize having max resources and not spending them. Iron is more valuable.
        }
        if (myResources.resources[ResourceType.Workers].Amount >= stats[Stat.WorkerStorage])
        {
            myResources.resources[ResourceType.Workers].SetAmount(stats[Stat.WorkerStorage]);
            SetReward(-0.02f); //Penalize having max resources and not spending them.
        }

        myWood = myResources.resources[ResourceType.Wood].Amount;
        myIron = myResources.resources[ResourceType.Iron].Amount;
        myWorkers = myResources.resources[ResourceType.Workers].Amount;
        score = GetCumulativeReward();
        buildingCount = stats[Stat.BuildingCount];
        maxBuildings = stats[Stat.MaxBuildings];

        tickNumber++;
        if(tickNumber % 100 == 0)
        {
            string str = "";
            for(int i=0; i<maxBuildings; i++)
            {
                str += buildings[i].myBuilding.ToString() + ", Lv: " + buildings[i].Level + "\n";
            }
            m_TextArea = str;
            
            if(score > bestScore)
            {
                bestScore = score;
            }
        }
        if (score <= -2)
        {
            EndEpisode();
        }
    }

    // Calculate stats and produce resources after building changes
    public void ProduceResources(ResourceManager resourceManager)
    {
        foreach (var building in buildings)
        {
            building.ProduceResources(resourceManager);
            // Update other stats as needed (e.g. increase resource caps, boost production, etc.)
        }
    }

    public void UpdateStats()
    {
        //Reset default stats.
        stats = new Dictionary<Stat, double>
        {
            { Stat.WoodStorage, 140 },
            { Stat.WoodProduction, 1 },
            { Stat.WoodMulti, 1 },
            { Stat.IronStorage, 140 },
            { Stat.IronProduction, 0 },
            { Stat.IronMulti, 1 },
            { Stat.SoldierStorage, 50 },
            { Stat.SoldierProduction, 0  },
            { Stat.SoldierMulti, 1 },
            { Stat.WorkerStorage, 50 },
            { Stat.WorkerProduction, 0 },
            { Stat.WorkerMulti, 1 },
            { Stat.MaxBuildings, 1 },
            { Stat.BuildingCount, 0 },
        };
        //stats[Stat.MaxBuildings] = HQ.Level;
        foreach (var building in buildings)
        {
            if (building.myBuilding == BuildingType.None)
            {
                return;
            }
            building.UpdateStats();
            stats[Stat.BuildingCount] += 1;
        }
    }
    public bool CanBuild(BuildingType building, Dictionary<ResourceType, double> BuildCosts)
    {
        foreach (var cost in BuildCosts)
        {
            if (!myResources.CanAfford(cost.Key, cost.Value))
                return false;
        }
        
        return true;
    }
    public Dictionary<ResourceType, double> CostToBuildFor(BuildingType building)
    {
        return Building.buildingData[building].costs[0];
    }
}

public enum Stat
{
    Ticks,
    WoodStorage,
    WoodStorageMulti,
    WoodProduction,
    WoodMulti,
    WoodCount,
    IronStorage,
    IronStorageMulti,
    IronProduction,
    IronMulti,
    IronCount,
    SoldierStorage,
    SoldierStorageMulti,
    SoldierProduction,
    SoldierMulti,
    SoldierCount,
    WorkerStorage,
    WorkerStorageMulti,
    WorkerProduction,
    WorkerMulti,
    WorkerCount,
    MaxBuildings,
    BuildingCount,
}
