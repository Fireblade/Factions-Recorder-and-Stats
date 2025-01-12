// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Text;
// using System.Text.RegularExpressions;
// using UnityEngine;
// using UnityEngine.Networking;
//
//
//
// namespace PlayFlow
// {
//     public class PlayFlowMatchMaker : MonoBehaviour
//     {
//         private static string API_URL = "https://88c2-100-34-136-100.ngrok-free.app";
//         public static PlayFlowMatchMaker Instance;
//
//         private void Awake()
//         {
//             if (Instance == null)
//             {
//                 Instance = this;
//                 DontDestroyOnLoad(gameObject);
//             }
//             else
//             {
//                 Destroy(gameObject);
//             }
//         }
//
//         void Start()
//         {
//
//         }
//
//         // Update is called once per frame
//         void Update()
//         {
//
//         }
//
//        
//
//         public static void GetActiveServers(string token, bool includelaunchingservers, Action<string> callback)
//         {
//             string actionUrl = API_URL + "/list_servers";
//
//             UnityWebRequest www = new UnityWebRequest(actionUrl, "POST");
//             www.downloadHandler = new DownloadHandlerBuffer();
//
//             // Set headers
//             www.SetRequestHeader("Content-Type", "application/json");
//             www.SetRequestHeader("token", token);
//             www.SetRequestHeader("includelaunchingservers", includelaunchingservers.ToString());
//
//             if (PlayFlowSDK.Instance != null)
//             {
//                 PlayFlowSDK.Instance.StartCoroutine(WaitForRequest(www, callback));
//             }
//             else
//             {
//                 Debug.LogError("PlayFlowSDK instance is null.");
//             }
//         }
//
//
//
//         private static IEnumerator WaitForRequest(UnityWebRequest www, Action<string> callback)
//         {
//             yield return www.SendWebRequest();
//
//             bool hasError;
//
// #if UNITY_2019_3_OR_NEWER
//             hasError = www.result != UnityWebRequest.Result.Success;
// #else
//     hasError = www.isNetworkError || www.isHttpError;
// #endif
//
//             if (!hasError)
//             {
//                 callback?.Invoke(www.downloadHandler.text);
//             }
//             else
//             {
//                 // Print error and response body
//                 Debug.Log("WWW Error: " + www.error);
//                 Debug.Log("WWW Error Body: " + www.downloadHandler.text);
//                 callback?.Invoke(null); // or you can pass the error message or any other string indicating an error
//             }
//
//             www.Dispose(); // Dispose of the UnityWebRequest object manually
//         }
//     }
//
//     [System.Serializable]
//     public class PlayFlowSession
//     {
//         public string client_token;
//         public string session_token;
//         public string player_id;
//         public string player_name;
//
//         // Override the ToString method to print the session data
//         public override string ToString()
//         {
//             return "clientToken: " + client_token + "\n" +
//                    "sessionToken: " + session_token + "\n" +
//                    "playerId: " + player_id + "\n" +
//                    "playerName: " + player_name + "\n";
//         }
//     }
//
//     
//     //"ports":{"443":443,"7778":30542,"7777":30747,"8383":31969}
//     [Serializable]
//     public class PortMapping
//     {
//         public string key;
//         public int value;
//     }
//
//     [Serializable]
//     public class Server
//     {
//         public string match_id;
//         public string status;
//         public string region;
//         public string instance_type;
//         public string server_arguments;
//         public bool ssl_enabled;
//         public string ip;
//         public string start_time;
//         public string server_url;
//         public int playflow_api_version;
//         public string server_tag;
//         //"ports":{"443":443,"7778":30542,"7777":30747,"8383":31969}
//         //Create a new class for ports
//         public Dictionary<string, int> ports = new Dictionary<string, int>();
//
//
//
//         
//
//
//         // Override the ToString method for better debugging
//         public override string ToString()
//         {
//             return $"Match ID: {match_id}, Status: {status}, IP: {ip}, Server URL: {server_url}";
//         }
//     }
//
//     [Serializable]
//     public class ActiveServers
//     {
//         public int total_servers;
//         public Server[] servers;
//
//         // Override the ToString method to print the session data
//         public override string ToString()
//         {
//             string serversString = "";
//             foreach (Server server in servers)
//             {
//                 serversString += server.ToString() + "\n";
//             }
//
//             return "totalServers: " + total_servers + "\n" +
//                    "servers: " + serversString;
//         }
//     }
// }
//   
//     
//
