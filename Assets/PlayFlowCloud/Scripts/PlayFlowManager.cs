using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

//PlayFlow Namespace


namespace PlayFlow
{
    public class PlayFlowManager : MonoBehaviour
    {

        private static TicketStatusResponse ExtractStatus(string jsonResponse)
        {
            TicketStatusResponse response = JsonUtility.FromJson<TicketStatusResponse>(jsonResponse);
            return response;
        }
        
        [Serializable]
        public class TicketStatusResponse
        {
            public string ticket_id;
            public string status;
            public string match_id;
            public string matchmaker_id;
            public string player_id;
            public string group_id; // Use string type even for 'None' value, as JsonUtility doesn't support nullable types
        }
        
        private static string ExtractTicketId(string jsonResponse)
        {
            TicketCreationResponse response = JsonUtility.FromJson<TicketCreationResponse>(jsonResponse);
            return response.ticket_id;
        }
        
        [Serializable]
        public class TicketCreationResponse
        {
            public string ticket_id;
            public string status;
        }
        
        public static  void GetActiveServers(string clientToken)
        {
            PlayFlowBackend.GetActiveServersList(clientToken, true, (response) =>
            {
                ActiveServers activeServers = response;
            });
        }
        
        public static void GetActiveServers(string clientToken, Action<ActiveServers> callback)
        {
            PlayFlowBackend.GetActiveServersList(clientToken, true, (response) =>
            {
                ActiveServers activeServers = response;
                callback?.Invoke(activeServers);
            });
        }
        
        public static void GetServerStatus(string clientToken, string matchId, Action<Server> callback)
        {
            PlayFlowBackend.GetServerStatus(clientToken, matchId, (response) =>
            {
                Server server = response;
                callback?.Invoke(server);
            });
        }


        public static IEnumerator FindMatch(string clientToken, PlayerData playerData, Action<Server> callback)
        {
            // Call FindMatch with default expiration time and max retries
            yield return FindMatch(clientToken, playerData, callback, 300, 3);
        }

        public static IEnumerator FindMatch(string clientToken, PlayerData playerData, Action<Server> callback, int expirationTime, int maxRetries = 3)
    {
        string playerDataJson = JsonUtility.ToJson(playerData);
        Debug.Log($"Player Data: {playerDataJson}");
        int attempts = 0;
        bool matchFound = false;
        string ticketId = string.Empty;

        while (attempts < maxRetries && !matchFound)
        {
            if (attempts > 0)
            {
                Debug.Log($"Retry attempt {attempts}");
            }

            // Create matchmaking ticket
            yield return PlayFlowBackend.Instance.StartCoroutine(
                PlayFlowBackend.CreateMatchmakingTicket(clientToken, playerDataJson, ticketResponse =>
                {
                    if (ticketResponse == null)
                    {
                        Debug.LogError("Failed to create a matchmaking ticket.");
                    } else {
                        ticketId = ExtractTicketId(ticketResponse);
                        Debug.Log($"Ticket ID: {ticketId}");
                    }
                   
                }));

            if (string.IsNullOrEmpty(ticketId))
            {
                Debug.LogError("Failed to create a matchmaking ticket.");
                yield break;
            }

            float startTime = Time.time;
            bool ticketExpired = false;

            // Check match status periodically until expiration time is reached
            while (Time.time - startTime < expirationTime && !matchFound && !ticketExpired)
            {
                yield return new WaitForSeconds(5); // Check status every 5 seconds

                yield return PlayFlowBackend.Instance.StartCoroutine(
                    CheckMatchStatus(clientToken, ticketId, server =>
                    {
                        if (server != null) // Assuming server != null means match is found
                        {
                            Debug.Log($"Running Server Status from Find Match: {server}");
                            matchFound = true;
                            callback?.Invoke(server);
                        }
                    }));

                ticketExpired = Time.time - startTime >= expirationTime;
            }

            if (ticketExpired)
            {
                Debug.Log($"Matchmaking attempt expired after {expirationTime} seconds.");
                DeleteTicketIfExpired(clientToken, ticketId); // Clean up expired ticket
            }

            attempts++;
        }

        if (!matchFound)
        {
            Debug.LogError("Failed to find a match after all attempts.");
        }
    }

    private static void DeleteTicketIfExpired(string clientToken, string ticketId)
    {
        Debug.Log($"Ticket ID: {ticketId} is expired. Attempting to cancel...");

        PlayFlowBackend.Instance.StartCoroutine(
            PlayFlowBackend.CancelMatchmakingTicket(clientToken, ticketId, cancelResponse =>
            {
                Debug.Log(cancelResponse);
            }));
    }

        
        public static IEnumerator CheckMatchStatus(string clientToken, string ticketId, Action<Server> callback)
        {
            bool isInMatch = false;
            
            while (!isInMatch)
            {
                yield return PlayFlowBackend.Instance.StartCoroutine(PlayFlowBackend.GetTicketStatus(clientToken, ticketId, statusResponse =>
                {
                    Debug.Log($"Get Status: {statusResponse}");
                    TicketStatusResponse ticket = ExtractStatus(statusResponse); // Implement ExtractStatus method
                    if (ticket == null)
                    {
                        //Throw an error
                        throw new Exception("Ticket not found.");
                    }
                    
                    
                    if (ticket.status == "In Match")
                    {
                        isInMatch = true;
                        Debug.Log("Status is 'In Match'.");
                        // Further actions when in match
                        PlayFlowBackend.Instance.StartCoroutine(CheckServerStatus(clientToken, ticket.match_id, server =>
                        {
                            Debug.Log($"Running Server Status from Check Match Status: {server}");
                            callback?.Invoke(server);
                            
                        }));
                        
                    }
                    else
                    {
                        Debug.Log($"Current status: {ticket.status}. Waiting for match...");
                    }
                }));

                yield return new WaitForSeconds(5); // Wait time between status checks
            }
        }
        
        //Create a method that will check the status of the server
        public static IEnumerator CheckServerStatus(string clientToken, string match_id, Action<Server> callback)
        {
            bool isServerOnline = false;
            while (!isServerOnline)
            {
                yield return PlayFlowBackend.Instance.StartCoroutine(PlayFlowBackend.CheckServerStatus(clientToken, match_id, statusResponse =>
                {
                    
                    Debug.Log($"Get Status: {statusResponse}");
                    Server server = JsonUtility.FromJson<Server>(statusResponse);
                    Debug.Log($"Server Status: {server}");
                    if (server.status == "running")
                    {
                        string portsJson = PlayFlowBackend.ExtractPortsJson(statusResponse, server.match_id);
                        server.ports = PlayFlowBackend.ParsePorts(portsJson);
                        isServerOnline = true;
                        callback?.Invoke(server);
                    }
                    else
                    {
                        Debug.Log($"Current status: {server.status}. Waiting for server...");
                    }
                }));

                yield return new WaitForSeconds(5); // Wait time between status checks
            }
        }
        
        
        
        [Serializable]
        public class CustomParameter
        {
            public string key;
            public string value;
        }
        
        [Serializable]
        public class PlayerData
        {
            public string player_id;
            public List<string> region;
            public int elo;
            //Custom data can be added here
            public string game_type = "default";
            public List<CustomParameter> custom_parameters = new List<CustomParameter>();
            
            public string GetCustomParameter(string key)
            {
                foreach (CustomParameter param in custom_parameters)
                {
                    if (param.key == key)
                    {
                        return param.value;
                    }
                }
                return null; // or return a default value if key not found
            }
        }
        
        public static void EndMatch()
        {
            //Kill this process
            Application.Quit();
        }
    }
}