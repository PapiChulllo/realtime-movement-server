using System.Collections.Generic;
using UnityEngine;

static public class NetworkServerProcessing
{
    static NetworkServer networkServer;
    static GameLogic gameLogic;

    static public void ReceivedMessageFromClient(string msg, int clientConnectionID, TransportPipeline pipeline)
    {
        string[] csv = msg.Split(',');

        if (csv[0] == "PlayerMoved")
        {
            int playerId = clientConnectionID;
            float x = float.Parse(csv[1]);
            float y = float.Parse(csv[2]);
            gameLogic.UpdatePlayerPosition(playerId, new Vector2(x, y));
        }
    }

    static public List<int> GetAllClientIDs()
    {
        return new List<int>(networkServer.GetAllClientIDs());
    }

    static public void SendMessageToClient(string msg, int clientConnectionID, TransportPipeline pipeline)
    {
        networkServer.SendMessageToClient(msg, clientConnectionID, pipeline);
    }

    static public void ConnectionEvent(int clientConnectionID)
    {
        UnityEngine.Debug.Log($"Client with ID {clientConnectionID} connected.");
        gameLogic.SendAllPlayerPositions(clientConnectionID);
    }

    static public void DisconnectionEvent(int clientConnectionID)
    {
        UnityEngine.Debug.Log($"Client with ID {clientConnectionID} disconnected.");
    }

    static public void SetNetworkServer(NetworkServer NetworkServer)
    {
        networkServer = NetworkServer;
    }

    static public void SetGameLogic(GameLogic GameLogic)
    {
        gameLogic = GameLogic;
    }
}
