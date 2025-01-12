using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class MapTile
{
    public int soldiers;
    public PlayerFaction owner = PlayerFaction.None;
    public DateTime captureDate;
    public Player capturePlayer;
    public bool supplied = true;

    public MapTile()
    {
        soldiers = 0;
        owner = PlayerFaction.None;
        captureDate = DateTime.MinValue;
        capturePlayer = null;
        supplied = true;
    }

    public MapTile(int soldiers, PlayerFaction owner, DateTime captureDate, Player capturePlayer)
    {
        this.soldiers = soldiers;
        this.owner = owner;
        this.captureDate = captureDate;
        this.capturePlayer = capturePlayer;
    }
}
