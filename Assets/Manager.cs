using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine.Networking;
using System.Text;
using static PlayFlow.PlayFlowManager;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class Manager : MonoBehaviour
{
    public static readonly string APIURL = "https://api.factions-online.com/api/game/";
    public static readonly string TESTAPIURL = "https://api.factions-online.com/api/game/";
    public static string factionsApiToken;
    public static string apiToken;

    public static readonly string PathLeaderboard = "/leaderboard";
    public static readonly string PathWorldMap = "/world/invasions_info";
    public static readonly string PathGet = "/get";

    public static Manager instance;


    public static readonly string siteUpdater = "https://mclama.com/Factions/TimeLapseUpdater.php";

    public bool runAgents = false;

    public List<GameHandler> gameHandlers = new List<GameHandler>();

    public float minuteTick = 0;
    public float updateGameListTick = 0; //every 5 minutes we poll the MySQL server and check for new games to add to the list. Or disable ones that have finished.

    public bool appendLocalFiles = false;
    public int activeGames = 0, gamesWaitingToStart = 0;



    // Start is called before the first frame update
    void Start()
    {
        instance = this;
        Application.runInBackground = true;
        Application.targetFrameRate = 20;

        LoadApiTokens();

        if (runAgents)
        {
            Building.InitializeBuildingData(null);
        }

        //Open source description - Uncomment line to pull leaderboard data from a game
        //PullLeaderboardFromGame(20);

        // Other initialization code...

        Debug.Log("Starting PHP update thread...");
        InvokeRepeating(nameof(StartPHPUpdateThread), 0f, 900f); // 900 seconds = 15 minutes

        //TestApi();


        //To force a game, Use the line below.
        //gameHandlers.Add(new GameHandler(20, false, false, "Volbadihr", 50, 50, true));
    }


    private void LoadApiTokens()
    {
        TextAsset configTextAsset = Resources.Load<TextAsset>("config");
        if (configTextAsset != null)
        {
            Debug.Log("Config file found."); // Log if the file is found
            string[] lines = configTextAsset.text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            Debug.Log("Config content: " + configTextAsset.text); // Log the config content

            foreach (string line in lines)
            {
                string[] keyValue = line.Split('=');
                if (keyValue.Length == 2)
                {
                    string key = keyValue[0].Trim();
                    string value = keyValue[1].Trim();

                    if (key == "factionsApiToken")
                    {
                        factionsApiToken = value;
                        Debug.Log("factionsApiToken: " + factionsApiToken);
                    }
                    else if (key == "apiToken")
                    {
                        apiToken = value;
                        Debug.Log("apiToken: " + apiToken);
                    }
                    else
                    {
                        Debug.LogError("Unknown key in config: " + key);
                    }
                }
                else
                {
                    Debug.LogError("Invalid line in config: " + line);
                }
            }
        }
        else
        {
            Debug.LogError("Config file not found.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        foreach (GameHandler gameHandler in gameHandlers)
        {
            gameHandler.Update();
        }

        minuteTick += Time.deltaTime;
        updateGameListTick += Time.deltaTime;

        if (minuteTick >= 60f)
        {
            minuteTick = 0f;
            Debug.Log("60 seconds passed. Requesting data.");
            foreach (GameHandler gameHandler in gameHandlers)
            {
                if(gameHandler.waitingToStart)
                {
                    gameHandler.CheckGameStart();
                }
                else
                {
                    if (gameHandler.isGameActive)
                    {
                        gameHandler.GetConnected();
                        gameHandler.CaptureGet();
                        gameHandler.CaptureLeaderboard();
                        gameHandler.requestMapData = true;
                    }
                }
            }
            //GetConnected();
        }
    }

    public async void PullLeaderboardFromGame(int gameID)
    {
        string fullUrl = APIURL + gameID + Manager.PathLeaderboard;
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

                List<Player> playerData = new List<Player>();

                // Get player data and update our local list
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

                    Player newPlayer = new Player(playerID, playerName)
                    {
                        id = playerID,
                        sentSoldiers = sentSoldiers,
                        sentWorkers = sentWorkers,
                        hqLevel = hqLevel,
                        faction = faction,
                        tilesCaptured = casesCaptured,
                        awarded = awarded
                    };

                    playerData.Add(newPlayer);
                }

                // Sort players by Sent Soldiers and assign ranks
                var sortedBySoldiers = playerData.OrderByDescending(p => p.sentSoldiers).ToList();
                for (int i = 0; i < sortedBySoldiers.Count; i++)
                {
                    sortedBySoldiers[i].rankBySentSoldiers = i + 1;
                }

                // Sort players by Sent Workers and assign ranks
                var sortedByWorkers = playerData.OrderByDescending(p => p.sentWorkers).ToList();
                for (int i = 0; i < sortedByWorkers.Count; i++)
                {
                    sortedByWorkers[i].rankBySentWorkers = i + 1;
                }

                // Sort players by HQ Level and assign ranks
                var sortedByHQLevel = playerData.OrderByDescending(p => p.hqLevel).ToList();
                for (int i = 0; i < sortedByHQLevel.Count; i++)
                {
                    sortedByHQLevel[i].rankByHQLevel = i + 1;
                }

                // Convert player data to JSON format for HTTP POST
                var contentData = new List<object>();
                var currentTime = DateTime.Now;

                foreach (var player in playerData)
                {
                    if (player.hqLevel >= 8)
                    {
                        contentData.Add(new
                        {
                            PlayerID = player.id,
                            PlayerName = player.name,
                            HQLevel = player.hqLevel,
                            Soldiers = player.sentSoldiers,
                            Workers = player.sentWorkers,
                            TilesCaptured = player.tilesCaptured,
                            Team = player.faction.ToString().ToUpper(),
                            RankBySoldiers = player.rankBySentSoldiers,
                            RankByWorkers = player.rankBySentWorkers,
                            RankByHQLevel = player.rankByHQLevel,
                            Awarded = player.awarded
                        });
                    }
                }

                var contentJson = JsonConvert.SerializeObject(contentData);

                using (HttpClient client = new HttpClient())
                {
                    // Add the API key header
                    client.DefaultRequestHeaders.Add("API_KEY", Manager.apiToken);

                    string requestBody = $"playerData={Uri.EscapeDataString(contentJson)}&gameID={gameID}";

                    StringContent content = new StringContent(requestBody, Encoding.UTF8, "application/x-www-form-urlencoded");

                    HttpResponseMessage response = await client.PostAsync("https://mclama.com/Factions/AddPlayerStats.php", content);

                    if (response.IsSuccessStatusCode)
                    {
                        Debug.Log("Leaderboard data sent successfully");
                    }
                    else
                    {
                        Debug.LogError("Error sending leaderboard data: " + response.StatusCode);
                    }
                }


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


    public async Task UpdateGameHandlersPHP()
    {
        Debug.Log("Updating game handlers from PHP data...");
        try
        {
            using (HttpClient client = new HttpClient())
            {
                string url = "https://mclama.com/Factions/GetGameList.php";
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string data = await response.Content.ReadAsStringAsync();

                if (data == "0 results")
                {
                    Debug.LogError("0 results returned from PHP script. Clearing game handlers list.");
                    gameHandlers.Clear();
                    activeGames = 0;
                }
                else
                {
                    string[] lines = data.Split(new[] { "<br>" }, StringSplitOptions.RemoveEmptyEntries);
                    Debug.Log("Number of lines: " + lines.Length); // Add this line to debug the number of lines
                    foreach (string line in lines)
                    {
                        Debug.Log("Processing line: " + line); // Add this line to debug each line
                        string[] parts = line.Split(':');
                        if (parts.Length < 7)
                        {
                            Debug.LogError("Invalid data format: " + line);
                            continue;
                        }

                        int gameId = int.Parse(parts[0]);
                        bool isTestGame = bool.Parse(parts[1].ToLower());
                        bool isGameActive = bool.Parse(parts[2].ToLower());
                        string mapName = parts[3];
                        int mapWidth = int.Parse(parts[4]);
                        int mapHeight = int.Parse(parts[5]);
                        bool waitingToStart = bool.Parse(parts[6]);

                        GameHandler existingGameHandler = gameHandlers.Find(g => g.gameID == gameId && g.isTestGame == isTestGame);
                        if (existingGameHandler != null)
                        {
                            if (existingGameHandler.waitingToStart != waitingToStart)
                            {
                                existingGameHandler.waitingToStart = waitingToStart;
                            }
                            if (existingGameHandler.isGameActive != isGameActive)
                            {
                                existingGameHandler.isGameActive = isGameActive;
                            }
                        }
                        else
                        {
                            Debug.Log($"New {(isGameActive ? "active " : "")}game found with ID: " + gameId + " and map: " + mapName + " with dimensions: " + mapWidth + "x" + mapHeight + ". Adding to list.");
                            gameHandlers.Add(new GameHandler(gameId, isTestGame, isGameActive, mapName, mapWidth, mapHeight, waitingToStart));
                            if (isGameActive) activeGames += 1;
                            if (waitingToStart) gamesWaitingToStart += 1;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("An error occurred: " + ex.Message);
        }
    }

    public void StartPHPUpdateThread()
    {
        Task.Run(() => UpdateGameHandlersPHP());
    }

    public async void TestApi()
    {
        string fullUrl = "https://factions-api.pilotsystems.net/api/game/17/hq/config";
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

                Debug.Log(json);

            }
            else
            {
                Debug.LogError("Error: " + request.error + "\n" + fullUrl);
            }
        }
    }

}

public enum PlayerFaction
{
    None,
    Red,
    Blue,
    Green,
    Yellow,
    Neutral
}
