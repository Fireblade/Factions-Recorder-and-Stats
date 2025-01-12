using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;



namespace PlayFlow
{
    public class PlayFlowBackend : MonoBehaviour
    {
        private static string API_URL = "https://api.cloud.playflow.app";
        private PlayFlowSession session;

        private static PlayFlowBackend _instance;

        public static PlayFlowBackend Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Create a new GameObject with PlayFlowSDK component
                    GameObject sdkObject = new GameObject("PlayFlowSDK");
                    _instance = sdkObject.AddComponent<PlayFlowBackend>();
                    DontDestroyOnLoad(sdkObject);
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
        
  

        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public PlayFlowSession InitSession(string clientToken, Action<PlayFlowSession> callback)
        {
            // Get a unique ID for this player 
            string playerId = SystemInfo.deviceUniqueIdentifier;
            // Register the player
            return session;

        }
        
        public static void GetActiveServersList(string token, bool includelaunchingservers, Action<ActiveServers> callback)
        {
            GetActiveServers(token, true, (response) =>
            {
                if (response != null)
                {
                    // Further processing of the response...
                    ActiveServers responseObj = JsonUtility.FromJson<ActiveServers>(response);
                    
                    // Manually parse the 'ports' field for each server
                    foreach (var server in responseObj.servers)
                    {
                        // Extract and parse the 'ports' part from the JSON string
                        // You will need to implement this part based on your JSON structure
                        string portsJson = ExtractPortsJson(response, server.match_id);
                        server.ports = ParsePorts(portsJson);
                    }
                    
                    callback?.Invoke(responseObj);
                }
                else
                {
                    Debug.Log("Error occurred while fetching the servers list.");
                }
            });
        }
        
        public static string ExtractPortsJson(string jsonResponse, string matchId)
        {
            // Find the starting index of the server object
            string serverStartToken = $"\"match_id\":\"{matchId}\"";
            int serverStartIndex = jsonResponse.IndexOf(serverStartToken);
            if (serverStartIndex == -1)
            {
                return "{}"; // Server not found or invalid matchId
            }

            // Find the starting index of the ports object within the server object
            string portsStartToken = "\"ports\":{";
            int portsStartIndex = jsonResponse.IndexOf(portsStartToken, serverStartIndex);
            if (portsStartIndex == -1)
            {
                return "{}"; // Ports object not found
            }

            // Find the closing bracket of the ports object
            int bracketCount = 1;
            int portsEndIndex = portsStartIndex + portsStartToken.Length;
            while (portsEndIndex < jsonResponse.Length && bracketCount > 0)
            {
                if (jsonResponse[portsEndIndex] == '{')
                {
                    bracketCount++;
                }
                else if (jsonResponse[portsEndIndex] == '}')
                {
                    bracketCount--;
                }
                portsEndIndex++;
            }

            // Extract the ports JSON string
            string portsJson = jsonResponse.Substring(portsStartIndex, portsEndIndex - portsStartIndex);

            return portsJson;
        }
        public static Dictionary<string, int> ParsePorts(string portsJson)
        {
            var ports = new Dictionary<string, int>();

            // Check if the portsJson string is not empty
            if (!string.IsNullOrEmpty(portsJson))
            {
                var matches = Regex.Matches(portsJson, "\"(\\d+)\":(\\d+)");
                foreach (Match match in matches)
                {
                    ports[match.Groups[1].Value] = int.Parse(match.Groups[2].Value);
                }
            }

            return ports;
        }
        
        public static IEnumerator CancelMatchmakingTicket(string token, string ticketId,Action<string> callback)
        {
            string url = $"{API_URL}/cancel_matchmaking_ticket/{ticketId}";
            UnityWebRequest www = UnityWebRequest.Delete(url);
            www.SetRequestHeader("token", token); // Replace with your client token

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                // Handle successful response
                callback?.Invoke($"Cancelled matchmaking ticket: {ticketId}");
            }
            else
            {
                // Handle error
                Debug.LogError($"Failed to cancel matchmaking ticket: {ticketId}, {www.error}");
            }
        }

        
        public static IEnumerator GetTicketStatus(string token, string ticketId, Action<string> callback)
        {
            string url = $"{API_URL}/get_ticket_status/{ticketId}";
            UnityWebRequest www = UnityWebRequest.Get(url);
            www.SetRequestHeader("token", token); // Use the provided token

            yield return Instance.StartCoroutine(WaitForRequest(www, callback));
        }


        
        
        public static IEnumerator GetServerStatus(string clientToken, string match_id, Action<Server> callback)
        {
            yield return PlayFlowBackend.CheckServerStatus(clientToken, match_id, statusResponse =>
            {
                try
                {
                    Debug.Log($"Get Status: {statusResponse}");
                    Server server = JsonUtility.FromJson<Server>(statusResponse);
                    
                    if (server.status == "running")
                    {
                        string portsJson = PlayFlowBackend.ExtractPortsJson(statusResponse, server.match_id);
                        server.ports = PlayFlowBackend.ParsePorts(portsJson);
                        callback?.Invoke(server);
                    }
                    
                    Debug.Log($"Server Status: {server}");
                    callback?.Invoke(server);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error parsing server status: {e.Message}");
                    // Handle the error appropriately
                }
            });
        }
        


        public static void GetActiveServers(string token, bool includelaunchingservers, Action<string> callback)
        {
            string actionUrl = API_URL + "/list_servers";

            UnityWebRequest www = new UnityWebRequest(actionUrl, "POST");
            www.downloadHandler = new DownloadHandlerBuffer();

            // Set headers
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("token", token);
            www.SetRequestHeader("includelaunchingservers", includelaunchingservers.ToString());

            if (PlayFlowBackend.Instance != null)
          
                PlayFlowBackend.Instance.StartCoroutine(WaitForRequest(www, callback));
         
            else
            {
                Debug.LogError("PlayFlowSDK instance is null.");
            }
        }

        public static IEnumerator CreateMatchmakingTicket(string token, string playerJson, Action<string> callback)
        {
            string url = $"{API_URL}/create_matchmaking_ticket";
            Debug.Log(playerJson);
            UnityWebRequest www = UnityWebRequest.Put(url, playerJson);
            www.method = "POST";
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("token", token);

            yield return Instance.StartCoroutine(WaitForRequest(www, callback));
        }
        
        
        public static IEnumerator CheckServerStatus(string token, string match_id, Action<string> callback)
        {
            string url = $"{API_URL}/get_server_status";
            UnityWebRequest www = UnityWebRequest.Get(url);
            www.method = "GET";
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("token", token);
            www.SetRequestHeader("match-id", match_id);

            yield return Instance.StartCoroutine(WaitForRequest(www, callback));
        }




        private static IEnumerator WaitForRequest(UnityWebRequest www, Action<string> callback)
        {
            yield return www.SendWebRequest();

            bool hasError;

#if UNITY_2019_3_OR_NEWER
            hasError = www.result != UnityWebRequest.Result.Success;
#else
    hasError = www.isNetworkError || www.isHttpError;
#endif

            if (!hasError)
            {
                callback?.Invoke(www.downloadHandler.text);
            }
            else
            {
                // Print error and response body
                Debug.Log("WWW Error: " + www.error);
                Debug.Log("WWW Error Body: " + www.downloadHandler.text);
                callback?.Invoke(null); // or you can pass the error message or any other string indicating an error
            }

            www.Dispose(); // Dispose of the UnityWebRequest object manually
        }
    }

    [System.Serializable]
    public class PlayFlowSession
    {
        public string client_token;
        public string session_token;
        public string player_id;
        public string player_name;

        // Override the ToString method to print the session data
        public override string ToString()
        {
            return "clientToken: " + client_token + "\n" +
                   "sessionToken: " + session_token + "\n" +
                   "playerId: " + player_id + "\n" +
                   "playerName: " + player_name + "\n";
        }
    }

    
    //"ports":{"443":443,"7778":30542,"7777":30747,"8383":31969}
    [Serializable]
    public class PortMapping
    {
        public string key;
        public int value;
    }

    [Serializable]
    public class Server
    {
        public string match_id;
        public string status;
        public string region;
        public string instance_type;
        public string server_arguments;
        public bool ssl_enabled;
        public string ip;
        public string start_time;
        public string server_url;
        public int playflow_api_version;
        public string server_tag;
        //Create a new class for ports
        public Dictionary<string, int> ports = new Dictionary<string, int>();



        


        // Override the ToString method for better debugging
        public override string ToString()
        {
            return $"Match ID: {match_id}, Status: {status}, IP: {ip}, Server URL: {server_url}";
        }
    }

    [Serializable]
    public class ActiveServers
    {
        public int total_servers;
        public Server[] servers;

        // Override the ToString method to print the session data
        public override string ToString()
        {
            string serversString = "";
            foreach (Server server in servers)
            {
                serversString += server.ToString() + "\n";
            }

            return "totalServers: " + total_servers + "\n" +
                   "servers: " + serversString;
        }
    }
}
  
    

