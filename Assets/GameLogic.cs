using System.Collections.Generic;
using UnityEngine;

public class GameLogic : MonoBehaviour
{
    private Dictionary<int, Vector2> playerPositions = new Dictionary<int, Vector2>();

    void Start()
    {
        NetworkServerProcessing.SetGameLogic(this);
    }

    public void UpdatePlayerPosition(int playerId, Vector2 newPosition)
    {
        playerPositions[playerId] = newPosition;

        // Notify all clients about the player's new position
        string message = $"PlayerMoved,{playerId},{newPosition.x},{newPosition.y}";
        foreach (var connectionId in NetworkServerProcessing.GetAllClientIDs())
        {
            NetworkServerProcessing.SendMessageToClient(message, connectionId, TransportPipeline.ReliableAndInOrder);
        }
    }

    public void SendAllPlayerPositions(int clientId)
    {
        foreach (var player in playerPositions)
        {
            string message = $"PlayerMoved,{player.Key},{player.Value.x},{player.Value.y}";
            NetworkServerProcessing.SendMessageToClient(message, clientId, TransportPipeline.ReliableAndInOrder);
        }
    }
}
