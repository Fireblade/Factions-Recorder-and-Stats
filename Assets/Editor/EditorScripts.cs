using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public class EditorScripts : EditorWindow
{
    private int gameId;
    private string apiToken;
    private string factionsApiToken;
    private int playerCount;
    private int blockCount;
    private int blockLength;
    private int maxBlock;

    private Dictionary<int, List<Player>> pData = new Dictionary<int, List<Player>>();

    [MenuItem("Custom/EditorScripts")]
    public static void ShowWindow()
    {
        GetWindow<EditorScripts>("EditorScripts");
    }

    private void OnGUI()
    {
        GUILayout.Label("EditorScripts", EditorStyles.boldLabel);

        gameId = EditorGUILayout.IntField("Game ID", gameId);
        blockLength = EditorGUILayout.IntField("Block Length (minutes)", blockLength);
        maxBlock = EditorGUILayout.IntField("Max Block (minutes)", maxBlock);

        if (GUILayout.Button("Load API Token"))
        {
            LoadApiToken();
        }

        if (GUILayout.Button("Get Connected Players"))
        {
            GetConnectedPlayers();
        }

        if (GUILayout.Button("Process Player Data"))
        {
            ProcessPlayerData();
        }

        if (GUILayout.Button("Analyze Player Activity"))
        {
            AnalyzePlayerActivity();
        }

        GUILayout.Label($"Players Tracked: {playerCount}");
        GUILayout.Label($"Blocks Counted: {blockCount}");
    }

    private void LoadApiToken()
    {
        LoadApiTokens();
        Debug.Log("API Token Loaded: " + apiToken);
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

    private async void GetConnectedPlayers()
    {
        if (gameId <= 0)
        {
            Debug.LogError("Invalid Game ID");
            return;
        }

        string fullUrl = Manager.APIURL + gameId + "/connected";
        using (UnityWebRequest request = UnityWebRequest.Get(fullUrl))
        {
            request.SetRequestHeader("Authorization", "Bearer " + factionsApiToken);

            UnityWebRequestAsyncOperation operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                JObject jsonData = JObject.Parse(json);

                int red = jsonData["RED"]?.Value<int>() ?? 0;
                int green = jsonData["GREEN"]?.Value<int>() ?? 0;
                int blue = jsonData["BLUE"]?.Value<int>() ?? 0;
                int yellow = jsonData["YELLOW"]?.Value<int>() ?? 0;

                Debug.Log($"Connected Players - Red: {red}, Green: {green}, Blue: {blue}, Yellow: {yellow}");
            }
            else
            {
                Debug.LogError("Error: " + request.error + "\n" + fullUrl);
            }
        }
    }

    private async void ProcessPlayerData()
    {
        if (gameId <= 0)
        {
            Debug.LogError("Invalid Game ID");
            return;
        }

        if (blockLength <= 0 || maxBlock <= 0)
        {
            Debug.LogError("Invalid Block Length or Max Block");
            return;
        }

        pData = new Dictionary<int, List<Player>>(); //reset data

        for (int urlMinute = 0; urlMinute <= maxBlock; urlMinute += blockLength)
        {
            string url = "https://www.mclama.com/Factions/GameData/" + gameId + "/" + urlMinute + ".txt";
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
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
                        int playerID = newPlayerData["id"].Value<int>();
                        int sentSoldiers = newPlayerData["sentSoldiers"].Value<int>();
                        int sentWorkers = newPlayerData["sentWorkers"].Value<int>();
                        int hqLevel = newPlayerData["hqLevel"].Value<int>();
                        PlayerFaction faction = GetFaction(newPlayerData["faction"].ToString());
                        DateTime lastActive = newPlayerData["lastActive"].Value<DateTime>();

                        Player newPlayer = new Player(playerID, playerName)
                        {
                            id = playerID,
                            sentSoldiers = sentSoldiers,
                            sentWorkers = sentWorkers,
                            unitsSent = (sentSoldiers * 2) + sentWorkers,
                            hqLevel = hqLevel,
                            faction = faction,
                            lastActive = lastActive
                        };

                        playerData.Add(newPlayer);
                    }

                    pData.Add(urlMinute, playerData);
                    Debug.Log($"minute {urlMinute} data received.");
                }
                else
                {
                    Debug.LogError("Error: " + request.error);
                }
            }
        }

        playerCount = pData.Values.SelectMany(players => players).Select(player => player.id).Distinct().Count();
        blockCount = maxBlock / blockLength + 1;

        Repaint(); // Update the GUI
        Debug.Log($"Players Tracked: {playerCount}");
        Debug.Log($"Blocks Counted: {blockCount}");
    }

    private void AnalyzePlayerActivity()
    {
        // Initial check for data
        if (pData == null || pData.Count == 0) // Added null check
        {
            Debug.LogError("No player data available (pData is null or empty).");
            return;
        }

        // --- Step 1: Determine the definitive startTime from the first block (key 0) ---
        DateTime startTime = DateTime.MaxValue;
        if (pData.TryGetValue(0, out var initialPlayers))
        {
            // Find the minimum lastActive time among players in the first block
            foreach (var player in initialPlayers)
            {
                // Skip invalid dates like MinValue
                if (player.lastActive == DateTime.MinValue) continue;

                if (player.lastActive < startTime)
                {
                    startTime = player.lastActive;
                }
            }
        }

        // Check if a valid startTime was found from block 0
        if (startTime == DateTime.MaxValue)
        {
            Debug.LogError("Could not determine start time from block 0. Ensure pData[0] exists and contains players with valid lastActive times.");
            return;
        }

        // Dictionaries to store results
        Dictionary<int, string> playerActivity = new Dictionary<int, string>();
        Dictionary<int, string> playerNames = new Dictionary<int, string>();

        // --- Step 2: Iterate through all blocks in reverse order and determine activity based on the fixed startTime ---
        for (int i = maxBlock; i >= 0; i -= blockLength)
        {
            DateTime blockThresholdTime;
            try
            {
                // Calculate the time threshold for the *current* block interval being checked
                blockThresholdTime = startTime.AddMinutes(i - blockLength);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Debug.LogError($"Error calculating blockThresholdTime for i={i}. StartTime={startTime}, i-blockLength={i - blockLength}. This might indicate startTime is too close to MinValue/MaxValue or i/maxBlock is excessively large. Skipping block. Exception: {ex.Message}");
                continue; // Skip processing for this specific block 'i'
            }

            // Process players listed within this specific block index 'i' from pData
            if (pData.TryGetValue(i, out var playersInBlock))
            {
                foreach (var player in playersInBlock)
                {
                    // Ensure player is registered in our tracking dictionaries the first time we see them
                    if (!playerActivity.ContainsKey(player.id))
                    {
                        playerActivity[player.id] = ""; // Initialize (leading comma will be added below)
                        playerNames[player.id] = player.name;
                    }

                    // Check if the player's last activity meets the threshold for this block
                    if (player.lastActive >= blockThresholdTime) // Compare against the calculated threshold
                    {
                        playerActivity[player.id] = ",A" + playerActivity[player.id]; // Prepend activity status for this block
                    }
                    else
                    {
                        playerActivity[player.id] = ",I" + playerActivity[player.id]; // Prepend activity status for this block
                    }
                }
            }

            // For players not in the current block, prepend "-"
            foreach (var playerId in playerActivity.Keys.ToList())
            {
                if (!playersInBlock.Any(p => p.id == playerId))
                {
                    playerActivity[playerId] = ",-" + playerActivity[playerId];
                }
            }
        }


        // --- Step 3: Format and Log Results ---
        string result = "";
        foreach (KeyValuePair<int, string> pair in playerActivity)
        {
            // Defensive check for name existence
            if (playerNames.TryGetValue(pair.Key, out string name))
            {
                // Original format: "ID Name,A,I,A..."
                result += $"{pair.Key} {name}{pair.Value}\n";
            }
            else
            {
                result += $"{pair.Key} UnknownName{pair.Value}\n"; // Fallback
            }
        }

        string outputPath = Path.Combine(Application.dataPath, $"output{gameId}.txt");
        File.WriteAllText(outputPath, result);
        Debug.Log($"Player Activity Analysis Result written to: {outputPath}");
    }

    private PlayerFaction GetFaction(string text)
    {
        return text.ToUpper() switch
        {
            "GREEN" => PlayerFaction.Green,
            "RED" => PlayerFaction.Red,
            "NEUTRAL" => PlayerFaction.Neutral,
            "BLUE" => PlayerFaction.Blue,
            "YELLOW" => PlayerFaction.Yellow,
            _ => PlayerFaction.None,
        };
    }
}

