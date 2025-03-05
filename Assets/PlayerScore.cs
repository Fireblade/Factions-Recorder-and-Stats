using UnityEngine;

public class PlayerScore
{
    public int id = 0;
    public string name = "unnamed";
    public int soldiers = 0;
    public int workers = 0;
    public int units;
    public float soldierScore = 0;
    public float workerScore = 0;
    public float unitScore = 0;

    public PlayerScore(int id, string playerName, int soldiers, int workers, float soldierScore, float workerScore)
    {
        this.id = id;
        this.name = playerName;
        this.soldiers = soldiers;
        this.workers = workers;
        this.soldierScore = soldierScore;
        this.workerScore = workerScore;
    }

    public PlayerScore(int id, string playerName, int units, float unitScore)
    {
        this.id = id;
        this.name = playerName;
        this.units = units;
        this.unitScore = unitScore;
    }
}
