using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Tilemaps;

public class GameHandler
{
    public bool requestMapData = false;
    public bool pulledFirstLeaderboardData = false;
    public float mapDataDelayTick = 5;

    public bool capturingMap = false;
    public bool capturingLeaderboard = false;
    public bool isTestGame = false;
    public bool isGameActive = false;
    public int gameID = 0;
    public string gameMap = "unknown";
    public int width = 50;
    public int height = 50;
    public bool waitingToStart = false;
    private int victoryGoal;
    private float hqIronCostMultiplier=1.5f;
    private float hqWoodCostMultiplier = 1.5f;
    private float hqWorkerCostMultiplier = 1.5f;
    private float buildingIronCostMultiplier = 1.5f;
    private float buildingWoodCostMultiplier = 1.5f;
    private float buildingWorkerCostMultiplier = 1.5f;
    public int gameMinute = 0;
    public int nextEloBlock = 480;
    public int eloBlockInterval = 480;
    public int totalBlocksChecked = 0;
    public int minuteSplit = 60;
    public GameMode gameMode = GameMode.Standard;

    public DateTime gameStart;

    public MapTile[,] currentMap;

    public Dictionary<string,Player> playerData = new Dictionary<string, Player>();
    public List<Player> playerDataToAdd = new List<Player>();
    public List<Player> playerDataToRemove = new List<Player>();

    public int[] factionPointGain = new int[]{ 0, 0, 0, 0, 0 };
    public int[] factionPoints = new int[] { 0, 0, 0, 0, 0 };
    public float[] factionAverageHqLevelTop20 = new float[] { 0, 0, 0, 0, 0 };
    public int[] factionCountOverHQ10 = new int[] { 0, 0, 0, 0, 0 };
    public int[] factionCountOverHQ15 = new int[] { 0, 0, 0, 0, 0 };
    public int[,] factionSentSoldiers = new int[60, 5]; //Records up to the last 60 minutes of soldier sent data
    public int[,] factionSentWorkers = new int[60, 5]; //records up to the last 60 minutes of worker sent data
    public int[] factionConnectedPlayers = new int[] { 0, 0, 0, 0, 0 };
    public int[] factionCastlePoints = new int[] { 0, 0, 0, 0, 0 };
    public int[] factionPlunder = new int[] { 0, 0, 0, 0, 0 };
    public int totalFactionPlundered = 0;


    public List<int> gameSoldierBlocks = new List<int>();
    public List<int> gameWorkerBlocks = new List<int>();

    private string winner = "NONE";

    public GameHandler(int gameID)
    {
        this.gameID = gameID;
        NewHandler();
    }

    public GameHandler(int gameId, bool isTestGame, bool isGameActive)
    {
        this.gameID = gameId;
        this.isGameActive = isTestGame;
        this.isGameActive = isGameActive;
        NewHandler();
    }

    public GameHandler(int gameId, bool isTestGame, bool isGameActive, string mapName, int mapWidth, int mapHeight, bool waitingToStart, int victoryGoal, float hqIronMulti, float hqWoodMulti, float hqWorkerMulti, float buildingIronMulti, float buildingWoodMulti, float buildingWorkerMulti, int gameMinute, string avgSoldierBlocks, string avgWorkerBlocks, int minuteSplit, GameMode gameMode)
    {
        this.gameID = gameId;
        this.isTestGame = isTestGame;
        this.isGameActive = isGameActive;
        this.gameMap = mapName;
        this.width = mapWidth;
        this.height = mapHeight;
        this.waitingToStart = waitingToStart;
        this.victoryGoal = victoryGoal;
        this.hqIronCostMultiplier = hqIronMulti;
        this.hqWoodCostMultiplier = hqWoodMulti;
        this.hqWorkerCostMultiplier = hqWorkerMulti;
        this.buildingIronCostMultiplier = buildingIronMulti;
        this.buildingWoodCostMultiplier = buildingWoodMulti;
        this.buildingWorkerCostMultiplier = buildingWorkerMulti;
        this.gameMinute = gameMinute;
        this.minuteSplit = minuteSplit;
        this.gameMode = gameMode;

        //set nextEloBlock to the next interval after current game minute.
        //these intervals happen at a specific 8 hour game minute every time.
        nextEloBlock = (gameMinute / eloBlockInterval) * eloBlockInterval + eloBlockInterval;

        try
        {
            if (!string.IsNullOrEmpty(avgSoldierBlocks) && avgSoldierBlocks != "")
            {
                gameSoldierBlocks = avgSoldierBlocks.Split(',').Select(int.Parse).ToList();
            }
            else
            {
                gameSoldierBlocks = new List<int>();
            }

            if (!string.IsNullOrEmpty(avgWorkerBlocks) && avgWorkerBlocks != "")
            {
                gameWorkerBlocks = avgWorkerBlocks.Split(',').Select(int.Parse).ToList();
            }
            else
            {
                gameWorkerBlocks = new List<int>();
            }

        }
        catch (Exception)
        {
            Debug.Log("Error on new handler. Unable to set avgBlocks");
        }
        try
        {
            NewHandler();
        }
        catch (Exception)
        {
            Debug.Log("Error initializing new handler, new handler()");
        }
    }

    public void NewHandler()
    {
        if (isGameActive)
        {
            gameStart = DateTime.UtcNow;
            currentMap = new MapTile[width, height];
            //populate every tile in tileDataArray
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    currentMap[x, y] = new MapTile();
                }
            }
            //now initialize factionSentSoldiers and factionSentWorkers
            for (int i = 0; i < 60; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    factionSentSoldiers[i, j] = 0;
                    factionSentWorkers[i, j] = 0;
                }
            }
            CaptureGet();
            CaptureLeaderboard();
            requestMapData = true;
        }
    }

    public void Update()
    {
        if (requestMapData)
        {
            mapDataDelayTick -= Time.deltaTime;
            if (mapDataDelayTick <= 0)
            {
                requestMapData = false;
                CaptureMap();
                mapDataDelayTick = 5;
                if(gameMode == GameMode.Standard)
                    if (gameMinute == nextEloBlock)
                    {
                        CalculateEloBlock();
                    }

                if (gameMinute % minuteSplit == 0)
                {
                    //every minuteSplit do this.
                    RecordSegment();
                }
            }
        }
        if (!capturingMap && !capturingLeaderboard)
        {
            CheckPlayerDataToAdd();
        }
    }

    private async void RecordSegment()
    {
        try
        {
            var contentData = playerData.Values.ToList();
            var contentJson = JsonConvert.SerializeObject(contentData);

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("API_KEY", Manager.apiToken);
                string requestBody = $"playerData={Uri.EscapeDataString(contentJson)}&gameID={gameID}&gameMinute={Uri.EscapeDataString(gameMinute.ToString())}";

                StringContent content = new StringContent(requestBody, Encoding.UTF8, "application/x-www-form-urlencoded");

                HttpResponseMessage response = await client.PostAsync("https://mclama.com/Factions/RecordSegment.php", content);

                if (response.IsSuccessStatusCode)
                {
                    //Debug.Log("leaderboard data sent successfully");
                }
                else
                {
                    // Debug.LogError("Error sending leaderboard data: " + response.StatusCode);
                }
            }
        }
        catch (Exception)
        {
            Debug.Log($"Game minute {gameMinute} failed to record segment");
        }
    }

    private async void CalculateEloBlock()
    {
        nextEloBlock += eloBlockInterval;
        totalBlocksChecked++;

        //initialize fields
        int eligibleCount = 0;
        int totalBlockSoldiers = 0;
        int totalBlockWorkers = 0;

        //Sort through every player and get the data we need.
        foreach (KeyValuePair<string, Player> player in playerData)
        {
            if (player.Value.hqLevel > 5)
            {
                eligibleCount++;
                totalBlockSoldiers += player.Value.sentSoldiers - player.Value.recordedEloSoldiers;
                totalBlockWorkers += player.Value.sentWorkers - player.Value.recordedEloWorkers;
            }
        }

        //Specify our averages.
        int avgBlockSoldiers = eligibleCount > 0 ? totalBlockSoldiers / eligibleCount : 0;
        int avgBlockWorkers = eligibleCount > 0 ? totalBlockWorkers / eligibleCount : 0;

        gameSoldierBlocks.Add(avgBlockSoldiers);
        gameWorkerBlocks.Add(avgBlockWorkers);

        var contentData = new List<object>();

        foreach (var playerEntry in playerData)
        {
            var player = playerEntry.Value;

            int blockSoldiers = player.sentSoldiers - player.recordedEloSoldiers;
            float soldierScore = Mathf.Sqrt(blockSoldiers / avgBlockSoldiers);

            int blockWorkers = player.sentWorkers - player.recordedEloWorkers;
            float workerScore = Mathf.Sqrt(blockWorkers / avgBlockWorkers);

            float totalScore = (float)Math.Round(soldierScore + workerScore, 5);
            while (player.scoreBlocks.Count < (totalBlocksChecked-1))
            {
                player.scoreBlocks.Add(0);
                player.soldierBlocks.Add(0);
                player.workerBlocks.Add(0);
            }
            player.scoreBlocks.Add(totalScore);
            player.soldierBlocks.Add(blockSoldiers);
            player.workerBlocks.Add(blockWorkers);

            string scoreString = string.Join(",", player.scoreBlocks);
            string soldierString = string.Join(",", player.soldierBlocks);
            string workerString = string.Join(",", player.workerBlocks);


            contentData.Add(new
            {
                PlayerID = player.id,
                ScoreBlocks = scoreString,
                SoldierBlocks = soldierString,
                WorkerBlocks = workerString,
            });

            //Save state of their sent soldiers/workers so we can tell the difference between blocks.
            player.recordedEloSoldiers = player.sentSoldiers;
            player.recordedEloWorkers = player.sentWorkers;

        }

        string gameSoldierString = string.Join(",", gameSoldierBlocks);
        string gameWorkerString = string.Join(",", gameWorkerBlocks);

        var contentJson = JsonConvert.SerializeObject(contentData);

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("API_KEY", Manager.apiToken);
            string requestBody = $"playerData={Uri.EscapeDataString(contentJson)}&gameID={gameID}&averageSoldierBlocks={Uri.EscapeDataString(gameSoldierString)}&averageWorkerBlocks={Uri.EscapeDataString(gameWorkerString)}";

            StringContent content = new StringContent(requestBody, Encoding.UTF8, "application/x-www-form-urlencoded");

            HttpResponseMessage response = await client.PostAsync("https://mclama.com/Factions/AddScoresToGame.php", content);

            if (response.IsSuccessStatusCode)
            {
                //Debug.Log("leaderboard data sent successfully");
            }
            else
            {
                // Debug.LogError("Error sending leaderboard data: " + response.StatusCode);
            }
        }
    }

    private void CheckPlayerDataToAdd()
    {
        if (playerDataToAdd.Count > 0)
        {
            //we have to iterate through the list backwards to avoid index out of range errors
            //we also need to remove the player from the list after adding them to the dictionary
            //we need to check if the player is already in the dictionary to avoid duplicate keys
            //we need to compare the data to make sure we don't overwrite existing data that may be newer than the data we have
            for (int i = playerDataToAdd.Count - 1; i >= 0; i--)
            {
                Player data = playerDataToAdd[i];
                if (!playerData.ContainsKey(data.name))
                {
                    playerData.Add(data.name, data);
                }
                else
                {
                    data.Update(playerData[data.name]);
                }
                playerDataToAdd.RemoveAt(i);
            }
        }
    }

    public async void CaptureMap()
    {
        capturingMap = true;
        string fullUrl = GameAPIURL + gameID + Manager.PathWorldMap;
        using (UnityWebRequest request = UnityWebRequest.Get(fullUrl))
        {
            request.SetRequestHeader("Authorization", "Bearer " + Manager.factionsApiToken);

            UnityWebRequestAsyncOperation operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                //Debug.Log("WorldMap Response: " + request.downloadHandler.text);
                Debug.Log("Map data captured successfully");
                string json = request.downloadHandler.text;

                totalFactionPlundered = 0; //reset for this minute

                // Parse the JSON response
                //WorldData mapData = JsonUtility.FromJson<WorldData>(json);

                // Clear the currentMap list
                //tileDataArray.Clear();

                JObject jsonData = JObject.Parse(json);

                // Extract the "world" data (assuming it's a 50x50 array)
                JArray worldArray = (JArray)jsonData["world"];
                //Debug.Log("WorldArray: " + worldArray.Count);

                // Loop through the world array to populate tileDataArray
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        JObject tileObject = (JObject)worldArray[y][x];
                        currentMap[x, y] = ParseTileData(tileObject);
                    }
                }

                string mapPrint = DateTime.UtcNow.ToString() + "_";
                //string cleanMapPrint = "";
                //Loop through every tile in tileDataArray and add print
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        if (currentMap[x,y].owner == PlayerFaction.None)
                        {
                            mapPrint += "0"; //Empty tile, no owner
                        }
                        else if (currentMap[x,y].owner == PlayerFaction.Neutral)
                        {
                            mapPrint += "N"; //Neutral tile
                        }
                        else
                        if (currentMap[x, y].supplied)
                            mapPrint += (int)currentMap[x, y].owner;
                        else
                            mapPrint += ((int)currentMap[x, y].owner + 4);
                        //cleanMapPrint += (int)currentMap[x, y].owner;
                    }
                    //cleanMapPrint += "\n";
                }

                //Now we need to add various other data to the mapPrint string
                //lets add the factions points, Faction Point Gains, faction Send Soldiers, factionSendSoldiersGain (in the last minute), factionSentSoldiersGainHour(in the last hour), factionSentWorkers, factionSentWorkersGain, factionSentWorkersGainHour, teamAverageHq, teamAverageHQOver10, teamAverageHQOver15. Each type of data is seperated by a '_'. with each team being seperated by a '='.
                //first we do Faction points. an example is "100=200=1312=31" by using factions RED, GREEN, BLUE, YELLOW. We skip NEUTRAL and NONE.
                //we start the for loop at 1 because we skip NEUTRAL and NONE.
                mapPrint += "_";
                for (int i = 1; i < 5; i++)
                {
                    mapPrint += factionPoints[i] + "=";
                }
                //now we do faction point gains. an example is "100=200=1312=31"
                mapPrint += "_";
                for (int i = 1; i < 5; i++)
                {
                    mapPrint += factionPointGain[i] + "=";
                }
                //now we do faction sent soldiers. an example is "100=200=1312=31"
                mapPrint += "_";
                for (int i = 1; i < 5; i++)
                {
                    mapPrint += factionSentSoldiers[0, i] + "=";
                }
                //now we do faction sent soldiers gain in the last minute. an example is "100=200=1312=31"
                mapPrint += "_";
                for (int i = 1; i < 5; i++)
                {
                    mapPrint += (factionSentSoldiers[0, i] - factionSentSoldiers[1, i]) + "=";
                }
                //now we do faction sent soldiers gain in the last hour. an example is "100=200=1312=31"
                mapPrint += "_";
                for (int i = 1; i < 5; i++)
                {
                    mapPrint += (factionSentSoldiers[0, i] - factionSentSoldiers[59, i]) + "=";
                }
                //now we do faction sent workers. an example is "100=200=1312=31"
                mapPrint += "_";
                for (int i = 1; i < 5; i++)
                {
                    mapPrint += factionSentWorkers[0, i] + "=";
                }
                //now we do faction sent workers gain in the last minute. an example is "100=200=1312=31"
                mapPrint += "_";
                for (int i = 1; i < 5; i++)
                {
                    mapPrint += (factionSentWorkers[0, i] - factionSentWorkers[1, i]) + "=";
                }
                //now we do faction sent workers gain in the last hour. an example is "100=200=1312=31"
                mapPrint += "_";
                for (int i = 1; i < 5; i++)
                {
                    mapPrint += (factionSentWorkers[0, i] - factionSentWorkers[59, i]) + "=";
                }
                //now we do faction average hq level top 20. an example is "100=200=1312=31"
                mapPrint += "_";
                for (int i = 1; i < 5; i++)
                {
                    mapPrint += factionAverageHqLevelTop20[i] + "=";
                }
                //now we do faction count over hq 10. an example is "100=200=1312=31"
                mapPrint += "_";
                for (int i = 1; i < 5; i++)
                {
                    mapPrint += factionCountOverHQ10[i] + "=";
                }
                //now we do faction count over hq 15. an example is "100=200=1312=31"
                mapPrint += "_";
                for (int i = 1; i < 5; i++)
                {
                    mapPrint += factionCountOverHQ15[i] + "=";
                }
                //Now lets add a section for each factions connected players. an example is "4=5=2=5"
                mapPrint += "_";
                for (int i = 1; i < 5; i++)
                {
                    mapPrint += factionConnectedPlayers[i] + "=";
                }

                using (HttpClient client = new HttpClient())
                {
                    string requestBody = $"mapData={Uri.EscapeDataString(mapPrint)}&gameID={gameID}&isTestGame={isTestGame.ToString()}";

                    StringContent content = new StringContent(requestBody, Encoding.UTF8, "application/x-www-form-urlencoded");

                    HttpResponseMessage response = await client.PostAsync(Manager.siteUpdater, content);

                    if (response.IsSuccessStatusCode)
                    {
                        //Debug.Log("Map data sent successfully");
                    }
                    else
                    {
                        //Debug.LogError("Error sending map data: " + response.StatusCode);
                    }
                }

                //Now get the mysql leaderboards set up correctly.

                var contentData = new List<object>();
                var currentTime = DateTime.Now;

                foreach (var playerEntry in playerData)
                {
                    var player = playerEntry.Value;
                    if ((currentTime - player.lastActive).TotalMinutes <= 5)
                    {
                        contentData.Add(new
                        {
                            PlayerID = player.id,
                            PlayerName = playerEntry.Key,
                            HQLevel = player.hqLevel,
                            Soldiers = player.sentSoldiers,
                            Workers = player.sentWorkers,
                            TilesCaptured = player.tilesCaptured,
                            Team = (int)player.faction,
                            LastActive = player.lastActive.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
                        });
                    }
                }

                var contentJson = JsonConvert.SerializeObject(contentData);

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("API_KEY", Manager.apiToken);
                    string requestBody = $"playerData={Uri.EscapeDataString(contentJson)}&gameID={gameID}";

                    StringContent content = new StringContent(requestBody, Encoding.UTF8, "application/x-www-form-urlencoded");

                    HttpResponseMessage response = await client.PostAsync("https://mclama.com/Factions/UpdateGameLeaderboards.php", content);

                    if (response.IsSuccessStatusCode)
                    {
                        //Debug.Log("leaderboard data sent successfully");
                    }
                    else
                    {
                        // Debug.LogError("Error sending leaderboard data: " + response.StatusCode);
                    }
                }


            }
            else
            {
                Debug.LogError("Error: " + request.error + "\n" + fullUrl);
            }
        }

        MapTile ParseTileData(JObject tileObject)
        {
            MapTile tileData = new MapTile();

            // Check if the "f" field exists and populate accordingly
            if (tileObject["f"] != null)
            {
                PlayerFaction newOwner = PlayerFaction.None;
                switch (tileObject["f"].ToString())
                {
                    case "GREEN":
                        newOwner = PlayerFaction.Green;
                        break;
                    case "RED":
                        newOwner  = PlayerFaction.Red;
                        break;
                    case "NEUTRAL":
                        newOwner = PlayerFaction.Neutral;
                        break;
                    case "BLUE":
                        newOwner = PlayerFaction.Blue;
                        break;
                    case "YELLOW":
                        newOwner = PlayerFaction.Yellow;
                        break;
                }
                if(tileData.owner != newOwner)
                {
                    tileData.tileHeat += 1;
                    tileData.owner = newOwner;
                }
            }

            if (tileObject["v"] != null)
            {
                try
                {
                    tileData.victoryPoints = tileObject["v"].Value<int>();
                    if (tileData.victoryPoints < factionCastlePoints[(int)tileData.owner])
                    {
                        //Faction has lost VP on their castle and has been plundered.
                        totalFactionPlundered += factionCastlePoints[(int)tileData.owner] - tileData.victoryPoints;
                    }
                    factionCastlePoints[(int)tileData.owner] = tileData.victoryPoints;
                }
                catch (Exception)
                {
                }
            }

            if (tileObject["cd"] != null)
            {
                try
                {
                    if (tileObject["cd"].Value<int>() > 0)
                    {
                        tileData.supplied = false;
                    }
                }
                catch (Exception)
                {
                }
            }

            // Check if the "p" field exists (player name)
            if (tileObject.ContainsItem("p") && tileObject["p"] != null)
            {
                string playerName = tileObject["p"].ToString();
                if(playerData.ContainsKey(playerName))
                {
                    tileData.capturePlayer = playerData[playerName];
                }

                int soldierCount = tileObject["s"].Value<int>();
                tileData.soldiers = soldierCount;
            }

            // Add more field checks as necessary

            return tileData;
        }

        gameMinute++;
        capturingMap = false;
    }


    public async void CaptureGet()
    {
        string fullUrl = GameAPIURL.Replace("game","games") + gameID + Manager.PathGet;
        using (UnityWebRequest request = UnityWebRequest.Get(fullUrl))
        {
            request.SetRequestHeader("Authorization", "Bearer " + Manager.factionsApiToken);

            UnityWebRequestAsyncOperation operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;

                JObject jsonData = JObject.Parse(json);

                // Extract points data
                int newRedPoints = jsonData["points"]["RED"].Value<int>();
                int newGreenPoints = jsonData["points"]["GREEN"].Value<int>();
                int newBluePoints = jsonData["points"]["BLUE"].Value<int>();
                int newYellowPoints = jsonData["points"]["YELLOW"].Value<int>();

                // Calculate point gains
                factionPointGain[(int)PlayerFaction.Red] = newRedPoints - factionPoints[(int)PlayerFaction.Red];
                factionPointGain[(int)PlayerFaction.Green] = newGreenPoints - factionPoints[(int)PlayerFaction.Green];
                factionPointGain[(int)PlayerFaction.Blue] = newBluePoints - factionPoints[(int)PlayerFaction.Blue];
                factionPointGain[(int)PlayerFaction.Yellow] = newYellowPoints - factionPoints[(int)PlayerFaction.Yellow];

                // Update faction points
                factionPoints[(int)PlayerFaction.Red] = newRedPoints;
                factionPoints[(int)PlayerFaction.Green] = newGreenPoints;
                factionPoints[(int)PlayerFaction.Blue] = newBluePoints;
                factionPoints[(int)PlayerFaction.Yellow] = newYellowPoints;

                //Print out a debug of the factions points.
                string debugPrint = "[GET]Faction Points: ";
                for (int i = 0; i < 5; i++)
                {
                    debugPrint += factionPoints[i] + " ";
                }
                debugPrint += "\nFaction Point Gains: ";
                for (int i = 0; i < 5; i++)
                {
                    debugPrint += factionPointGain[i] + " ";
                }
                Debug.Log(debugPrint);

                if (jsonData["status"].ToString() == "PLAYING")
                {
                    isGameActive = true;
                    waitingToStart = false;
                }
                else
                if (jsonData["status"].ToString() == "COMPLETED")
                {
                    if (isGameActive)
                    {
                        //Game is still running and we discovered the game is completed.
                        //Run one last data recording of the leaderboards.
                        RecordSegment();
                        isGameActive = false;
                        winner = jsonData["winner"].ToString();
                        //We then need to send a message to our website so it updates the mysql and inform the game has ended.
                        SendUpdateGameStatus();
                    }
                }
            }
            else
            {
                Debug.LogError("Error: " + request.error + "\n" + fullUrl);
            }
        }
    }

    public async void GrabGameData()
    {
        if (gameMinute > 2) return;
        try
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("API_KEY", Manager.apiToken);
                string url = "https://mclama.com/Factions/GetGameData.php";
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string data = await response.Content.ReadAsStringAsync();

                if (data == "0 results")
                {
                    Debug.Log("0 results. No game data");
                }
                else
                {
                    // Parse the JSON data
                    JObject jsonData = JObject.Parse(data);

                    // Extract the gameMinute
                    gameMinute = jsonData["GameMinute"].Value<int>();

                    // Extract the AverageSoldierBlocks and AverageWorkerBlocks
                    string averageSoldierBlocks = jsonData["AverageSoldierBlocks"].Value<string>();
                    string averageWorkerBlocks = jsonData["AverageWorkerBlocks"].Value<string>();

                    // Convert averageSoldierBlocks to a list of integers
                    gameSoldierBlocks = averageSoldierBlocks.Split(',')
                        .Select(int.Parse)
                        .ToList();

                    // Convert averageWorkerBlocks to a list of integers
                    gameWorkerBlocks = averageWorkerBlocks.Split(',')
                        .Select(int.Parse)
                        .ToList();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("An error occurred: " + ex.Message);
        }
    }



    public async void CaptureLeaderboard()
    {
        string fullUrl = GameAPIURL + gameID + Manager.PathLeaderboard;
        using (UnityWebRequest request = UnityWebRequest.Get(fullUrl))
        {
            request.SetRequestHeader("Authorization", "Bearer " + Manager.factionsApiToken);

            UnityWebRequestAsyncOperation operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;

                // Parse the JSON array
                JArray jsonData = JArray.Parse(json);

                //Get player data and update out local list
                // Iterate through the player data
                foreach (var newPlayerData in jsonData)
                {
                    string playerName = newPlayerData["name"].ToString();
                    int playerID = newPlayerData["playerId"].Value<int>();
                    int sentSoldiers = newPlayerData["sentSoldiers"].Value<int>();
                    int sentWorkers = newPlayerData["sentWorkers"].Value<int>();
                    int hqLevel = newPlayerData["hqLevel"].Value<int>();
                    int casesCaptured = newPlayerData["casesCaptured"].Value<int>();
                    bool awarded = newPlayerData["awarded"].Value<bool>();
                    PlayerFaction faction = GetFaction(newPlayerData["faction"].ToString());

                    Player newPlayer = new Player(playerName);
                    newPlayer.id = playerID;
                    newPlayer.sentSoldiers = sentSoldiers;
                    newPlayer.sentWorkers = sentWorkers;
                    newPlayer.hqLevel = hqLevel;
                    newPlayer.faction = faction;
                    newPlayer.tilesCaptured = casesCaptured;
                    newPlayer.awarded = awarded;

                    if (playerData.ContainsKey(playerName))
                    {
                        playerData[playerName].Update(newPlayer);
                    }
                    else
                    {
                        playerData.Add(playerName, newPlayer);
                    }
                }

                // Initialize faction data
                int[] totalSentSoldiers = new int[5];
                int[] totalSentWorkers = new int[5];
                int[] hqLevelCountOver10 = new int[5];
                int[] hqLevelCountOver15 = new int[5];
                List<Player>[] topPlayersByFaction = new List<Player>[5];
                for (int i = 0; i < 5; i++)
                {
                    topPlayersByFaction[i] = new List<Player>();
                }

                // Iterate through the player data
                foreach (KeyValuePair<string, Player> pair in playerData)
                {
                    Player player = pair.Value;

                    int factionIndex = (int)player.faction;

                    totalSentSoldiers[factionIndex] += player.sentSoldiers;
                    totalSentWorkers[factionIndex] += player.sentWorkers;

                    if (player.hqLevel >= 10)
                    {
                        hqLevelCountOver10[factionIndex]++;
                    }
                    if (player.hqLevel >= 15)
                    {
                        hqLevelCountOver15[factionIndex]++;
                    }

                    topPlayersByFaction[factionIndex].Add(player);
                }

                // Calculate average HQ level of top 20 players by HQ level for each faction
                double[] averageHqLevelTop20 = new double[5];
                for (int i = 0; i < 5; i++)
                {
                    topPlayersByFaction[i].Sort((p1, p2) => p2.hqLevel.CompareTo(p1.hqLevel));
                    int topCount = Math.Min(20, topPlayersByFaction[i].Count);
                    if (topCount > 0)
                    {
                        averageHqLevelTop20[i] = topPlayersByFaction[i].Take(topCount).Average(p => p.hqLevel);
                    }
                }

                // Rotate the old data so we can enter the latest new data.
                for (int i = 59; i > 0; i--)
                {
                    for (int j = 0; j < 5; j++)
                    {
                        factionSentSoldiers[i, j] = factionSentSoldiers[i - 1, j];
                        factionSentWorkers[i, j] = factionSentWorkers[i - 1, j];
                    }
                }

                // Enter the data for fields
                for (int i = 0; i < 5; i++)
                {
                    factionAverageHqLevelTop20[i] = (float)averageHqLevelTop20[i];
                    factionCountOverHQ10[i] = hqLevelCountOver10[i];
                    factionCountOverHQ15[i] = hqLevelCountOver15[i];
                    factionSentSoldiers[0, i] = totalSentSoldiers[i];
                    factionSentWorkers[0, i] = totalSentWorkers[i];
                }

                if (!pulledFirstLeaderboardData)
                {
                    pulledFirstLeaderboardData = true;
                    //we need to set the first 59 minutes of the sent soldiers and workers data to the current data
                    for (int i = 1; i < 60; i++)
                    {
                        for (int j = 0; j < 5; j++)
                        {
                            factionSentSoldiers[i, j] = factionSentSoldiers[0, j];
                            factionSentWorkers[i, j] = factionSentWorkers[0, j];
                        }
                    }
                }

                // Print out a debug of all the data we just captured.
                string debugPrint = "[Leaderboard] Faction Points: ";
                for (int i = 1; i < 5; i++)
                {
                    debugPrint += factionPoints[i] + " ";
                }
                debugPrint += "\nFaction Point Gains: ";
                for (int i = 1; i < 5; i++)
                {
                    debugPrint += factionPointGain[i] + " ";
                }
                debugPrint += "\nFaction Average HQ Level Top 20: ";
                for (int i = 1; i < 5; i++)
                {
                    debugPrint += factionAverageHqLevelTop20[i] + " ";
                }
                debugPrint += "\nFaction Count Over HQ 10: ";
                for (int i = 1; i < 5; i++)
                {
                    debugPrint += factionCountOverHQ10[i] + " ";
                }
                debugPrint += "\nFaction Count Over HQ 15: ";
                for (int i = 1; i < 5; i++)
                {
                    debugPrint += factionCountOverHQ15[i] + " ";
                }
                debugPrint += "\nFaction Sent Soldiers: ";
                for (int i = 1; i < 5; i++)
                {
                    debugPrint += factionSentSoldiers[0, i] + " ";
                }
                debugPrint += "\nFaction Sent Workers: ";
                for (int i = 1; i < 5; i++)
                {
                    debugPrint += factionSentWorkers[0, i] + " ";
                }
                Debug.Log(debugPrint);

            }
            else
            {
                Debug.LogError("Error: " + request.error);
            }
        }

        PlayerFaction GetFaction(string text)
        {
            switch (text)
            {
                case "GREEN":
                    return PlayerFaction.Green;
                case "RED":
                    return PlayerFaction.Red;
                case "NEUTRAL":
                    return PlayerFaction.Neutral;
                case "BLUE":
                    return PlayerFaction.Blue;
                case "YELLOW":
                    return PlayerFaction.Yellow;
            }
            return PlayerFaction.None;
        }
    }

    public async void GetConnected()
    {
        string fullUrl = GameAPIURL + gameID + "/connected";
        using (UnityWebRequest request = UnityWebRequest.Get(fullUrl))
        {
            request.SetRequestHeader("Authorization", "Bearer " + Manager.factionsApiToken);

            UnityWebRequestAsyncOperation operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;

                JObject jsonData = JObject.Parse(json);

                // Extract connected data
                int red = jsonData["RED"].Value<int>();
                int green = jsonData["GREEN"].Value<int>();
                int blue = jsonData["BLUE"].Value<int>();
                int yellow = jsonData["YELLOW"].Value<int>();

                //Debug.Log("Connected data: Red: " + red + ", Green: " + green + ", Blue: " + blue + ", Yellow: " + yellow);

                //set the factionConnectedPlayers array to the new data
                factionConnectedPlayers[(int)PlayerFaction.Red] = red;
                factionConnectedPlayers[(int)PlayerFaction.Green] = green;
                factionConnectedPlayers[(int)PlayerFaction.Blue] = blue;
                factionConnectedPlayers[(int)PlayerFaction.Yellow] = yellow;


                //send data to google sheet for graphs
                SendDataToGoogleForm(red, green, blue, yellow);

            }
            else
            {
                Debug.LogError("Error: " + request.error + "\n" + fullUrl);
            }
        }
    }

    public async void SendDataToGoogleForm(int red, int green, int blue, int yellow)
    {
        string url = "https://docs.google.com/forms/d/e/1FAIpQLSeXVckl1h3R8kAplMcMB8X3A6qBIbBBR3tecl3e234R4Jbo0g/formResponse";
        WWWForm form = new WWWForm();
        form.AddField("entry.1240471921", red);
        form.AddField("entry.1810503733", blue);
        form.AddField("entry.1408056110", green);
        form.AddField("entry.2073771277", yellow);

        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Data successfully sent to Google Form.");
            }
            else
            {
                Debug.LogError("Error sending data to Google Form: " + request.error);
            }
        }
    }

    public async void CheckGameStart()
    {
        if (!waitingToStart)
        {
            return;
        }
        // https://api.factions-online.com/api/games/19/get
        string fullUrl = GameAPIURL.Replace("game", "games") + gameID + Manager.PathGet;
        using (UnityWebRequest request = UnityWebRequest.Get(fullUrl))
        {
            request.SetRequestHeader("Authorization", "Bearer " + Manager.factionsApiToken);

            UnityWebRequestAsyncOperation operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;

                JObject jsonData = JObject.Parse(json);

                if (jsonData["status"].ToString() == "PLAYING")
                {
                    isGameActive = true;
                    waitingToStart = false;
                    gameMap = CapitalizeFirstLetter(jsonData["map"].ToString().ToLower());
                    NewHandler();
                }

            }
            else
            {
                Debug.LogError("Error: " + request.error + "\n" + fullUrl);
            }
        }
    }

    public async void CheckGameEnd()
    {
        if (waitingToStart)
        {
            return;
        }
        // https://api.factions-online.com/api/games/19/get
        string fullUrl = GameAPIURL.Replace("game", "games") + gameID + Manager.PathGet;
        using (UnityWebRequest request = UnityWebRequest.Get(fullUrl))
        {
            request.SetRequestHeader("Authorization", "Bearer " + Manager.factionsApiToken);

            UnityWebRequestAsyncOperation operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;

                JObject jsonData = JObject.Parse(json);

                

            }
            else
            {
                Debug.LogError("Error: " + request.error + "\n" + fullUrl);
            }
        }
    }

    private async void SendUpdateGameStatus()
    {
        var contentData = new
        {
            gameID,
            active = isGameActive,
            winner
        };

        var jsonContent = JsonConvert.SerializeObject(contentData);
        var requestBody = $"content={Uri.EscapeDataString(jsonContent)}";

        StringContent content = new StringContent(requestBody, Encoding.UTF8, "application/x-www-form-urlencoded");

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("API_KEY", Manager.apiToken);

            HttpResponseMessage response = await client.PostAsync("https://mclama.com/Factions/UpdateGameStatus.php", content);

            if (response.IsSuccessStatusCode)
            {
                Debug.Log("Game status updated successfully.");
            }
            else
            {
                Debug.LogError("Failed to update game status: " + response.StatusCode);
            }
        }
    }


    public async void CaptureConfig()
    {
        string fullUrl = GameAPIURL + gameID + Manager.PathConfig;
        using (UnityWebRequest request = UnityWebRequest.Get(fullUrl))
        {
            request.SetRequestHeader("Authorization", "Bearer " + Manager.factionsApiToken);

            UnityWebRequestAsyncOperation operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                JObject jsonData = JObject.Parse(json);

                float newHqIronCostMultiplier = (float)jsonData["misc"]["parameters"]["hq_iron_cost_multiplier"] * 1.5f;
                float newHqWoodCostMultiplier = (float)jsonData["misc"]["parameters"]["hq_wood_cost_multiplier"] * 1.5f;
                float newHqWorkerCostMultiplier = (float)jsonData["misc"]["parameters"]["hq_worker_cost_multiplier"] * 1.5f;
                float newBuildingIronCostMultiplier = (float)jsonData["misc"]["parameters"]["building_iron_cost_multiplier"] * 1.5f;
                float newBuildingWoodCostMultiplier = (float)jsonData["misc"]["parameters"]["building_wood_cost_multiplier"] * 1.5f;
                float newBuildingWorkerCostMultiplier = (float)jsonData["misc"]["parameters"]["building_worker_cost_multiplier"] * 1.5f;

                bool shouldUpdate = false;

                if (hqIronCostMultiplier != newHqIronCostMultiplier)
                {
                    hqIronCostMultiplier = newHqIronCostMultiplier;
                    shouldUpdate = true;
                }
                if (hqWoodCostMultiplier != newHqWoodCostMultiplier)
                {
                    hqWoodCostMultiplier = newHqWoodCostMultiplier;
                    shouldUpdate = true;
                }
                if (hqWorkerCostMultiplier != newHqWorkerCostMultiplier)
                {
                    hqWorkerCostMultiplier = newHqWorkerCostMultiplier;
                    shouldUpdate = true;
                }
                if (buildingIronCostMultiplier != newBuildingIronCostMultiplier)
                {
                    buildingIronCostMultiplier = newBuildingIronCostMultiplier;
                    shouldUpdate = true;
                }
                if (buildingWoodCostMultiplier != newBuildingWoodCostMultiplier)
                {
                    buildingWoodCostMultiplier = newBuildingWoodCostMultiplier;
                    shouldUpdate = true;
                }
                if (buildingWorkerCostMultiplier != newBuildingWorkerCostMultiplier)
                {
                    buildingWorkerCostMultiplier = newBuildingWorkerCostMultiplier;
                    shouldUpdate = true;
                }

                if (shouldUpdate)
                {
                    UpdateSiteMultipliers(gameID); // Ensure gameID is available in the context
                    Debug.Log("Multipliers updated successfully.");
                }
            }
            else
            {
                Debug.LogError("Error: " + request.error + "\n" + fullUrl);
            }
        }
    }

    private async void UpdateSiteMultipliers(int gameID)
    {
        var contentData = new
        {
            hqIronCostMultiplier,
            hqWoodCostMultiplier,
            hqWorkerCostMultiplier,
            buildingIronCostMultiplier,
            buildingWoodCostMultiplier,
            buildingWorkerCostMultiplier,
            gameID
        };

        var jsonContent = JsonConvert.SerializeObject(contentData);
        var requestBody = $"content={Uri.EscapeDataString(jsonContent)}";

        StringContent content = new StringContent(requestBody, Encoding.UTF8, "application/x-www-form-urlencoded");

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("API_KEY", Manager.apiToken);

            HttpResponseMessage response = await client.PostAsync("https://mclama.com/Factions/UpdateGameMultipliers.php", content);

            if (response.IsSuccessStatusCode)
            {
                Debug.Log("Data sent successfully.");
            }
            else
            {
                Debug.LogError("Failed to send data: " + response.StatusCode);
            }
        }
    }

    public string CapitalizeFirstLetter(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return char.ToUpper(input[0]) + input.Substring(1).ToLower();
    }

    private string GameAPIURL
    {
        get
        {
            return isTestGame ? Manager.TESTAPIURL : Manager.APIURL;
        }
    }

}

public enum GameMode
{
    Flash,
    Short,
    Standard,
}
