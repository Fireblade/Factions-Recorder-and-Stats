using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public class CityData
{
    public int id = 0;
    public List<Building> buildings = new List<Building>();
    public Dictionary<Stat, double> stats = new Dictionary<Stat, double>();
    public string buildOrder = "";


    public CityData(int index)
    {
        id = index;
    }

    public double GetWoodStorage()
    {
        return stats[Stat.WoodStorage] * stats[Stat.WoodStorageMulti];
    }

    public double GetIronStorage()
    {
        return stats[Stat.IronStorage] * stats[Stat.IronStorageMulti];
    }

    public double GetWorkerStorage()
    {
        return stats[Stat.WorkerStorage] * stats[Stat.WorkerStorageMulti];
    }

    public double WoodProduction()
    {
        return stats[Stat.WoodProduction] * stats[Stat.WoodMulti];
    }

    public double IronProduction()
    {
        return (double) stats[Stat.IronProduction] * (double) stats[Stat.IronMulti];
    }

    public double WorkersProduction()
    {
        return stats[Stat.WorkerProduction] * stats[Stat.WorkerMulti];
    }

    public double SoldiersProduction()
    {
        return stats[Stat.SoldierProduction] * stats[Stat.SoldierMulti];
    }

    public bool AddProduction(int tickCount)
    {
        if(tickCount < 0)
        {
            Debug.LogError("tick count is negative");
            return false;
        }
        if (tickCount == 0) return false;

        stats[Stat.Ticks] += tickCount;

        if (WoodProduction() > 0)
        {
            double newWoodCount = stats[Stat.WoodCount] + (WoodProduction() * tickCount);
            stats[Stat.WoodCount] = Math.Min(newWoodCount, stats[Stat.WoodStorage]);
            if (newWoodCount < 0)
            {
                Debug.LogError("Wood count is negative");
                return false;
            }
        }

        if(IronProduction() > 0)
        {
            double startIronCount = stats[Stat.IronCount];
            stats[Stat.IronCount] += IronProduction() * tickCount;
            if(stats[Stat.IronCount] > stats[Stat.IronStorage])
            {
                stats[Stat.IronCount] = stats[Stat.IronStorage];
            }
            if (stats[Stat.IronCount] < 0)
            {
                Debug.LogError($"Iron count is negative." +
                    $"IronCount: {stats[Stat.IronCount]}, IronProduction: {IronProduction()}, TickCount: {tickCount}, IronStorage: {stats[Stat.IronStorage]}" +
                    $"\nStarting Iron Count: {startIronCount}");
                return false;
            }
        }

        if(WorkersProduction() > 0)
        {
            double startWorkerCount = stats[Stat.WorkerCount];
            if(WorkersProduction() * tickCount < 0)
            {
                Debug.LogError("Worker production is negative");
                return false;
            }
            stats[Stat.WorkerCount] += WorkersProduction() * tickCount;
            if (stats[Stat.WorkerCount] > stats[Stat.WorkerStorage])
            {
                stats[Stat.WorkerCount] = stats[Stat.WorkerStorage];
            }
            if (stats[Stat.WorkerCount] < 0)
            {
                Debug.LogError($"Worker count is negative." +
                    $"WorkerCount: {stats[Stat.WorkerCount]}, WorkerProduction: {IronProduction()}, TickCount: {tickCount}, WorkerStorage: {stats[Stat.WorkerStorage]}" +
                    $"\nStarting Worker Count: {startWorkerCount}");
                return false;
            }
        }

        if (SoldiersProduction() > 0)
        {
            double newSoldierCount = stats[Stat.SoldierCount] + (SoldiersProduction() * tickCount);
            stats[Stat.SoldierCount] = Math.Min(newSoldierCount, stats[Stat.SoldierStorage]);
            if (newSoldierCount < 0)
            {
                Debug.LogError("Soldier count is negative");
                return false;
            }
        }
        return true;
    }
}
