using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Player
{
    public int id = 0;
    public string name = "unnamed";
    public PlayerFaction faction = PlayerFaction.None;
    public int hqLevel = 1;
    public int sentSoldiers = 0;
    public int sentWorkers = 0;
    public int unitsSent = 0;
    public int tilesCaptured = 0;
    public DateTime lastActive = DateTime.MinValue;
    public bool awarded = false;
    public int rankBySentSoldiers;
    public int rankBySentWorkers;
    public int rankByHQLevel;

    public int recordedEloSoldiers = 0;
    public int recordedEloWorkers = 0;
    public List<float> scoreBlocks = new List<float>();
    public List<int> soldierBlocks = new List<int>();
    public List<int> workerBlocks = new List<int>();

    public Player(int id, string playerName)
    {
        this.id = id;
        this.name = playerName;
    }
    public Player(string name)
    {
        this.name = name;
        lastActive = DateTime.UtcNow;
    }

    public void Update(Player newData)
    {
        bool updateActivity = false;
        if (newData.hqLevel > hqLevel)
        {
            hqLevel = newData.hqLevel;
            updateActivity = true;
        }
        if (newData.sentSoldiers > sentSoldiers)
        {
            sentSoldiers = newData.sentSoldiers;
            updateActivity = true;
        }
        if (newData.sentWorkers > sentWorkers)
        {
            sentWorkers = newData.sentWorkers;
            updateActivity = true;
        }
        if (newData.tilesCaptured > tilesCaptured)
        {
            tilesCaptured = newData.tilesCaptured;
            updateActivity = true;
        }
        if(newData.awarded==true && awarded==false)
        {
            awarded = true;
            updateActivity = true;
        }
        if (updateActivity)
        {
            lastActive = DateTime.UtcNow;
        }
    }
}
