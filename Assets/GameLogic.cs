using System.Collections.Generic;
using UnityEngine;

public class GameLogic : MonoBehaviour
{
    private Dictionary<int, Vector2> balloonPositions = new Dictionary<int, Vector2>();
    private int nextBalloonID = 0;

    void Start()
    {
        NetworkServerProcessing.SetGameLogic(this);
    }

    void Update()
    {
        if (Time.time % 1 < Time.deltaTime) // Spawn a balloon every second
        {
            SpawnBalloon();
        }
    }

    private void SpawnBalloon()
    {
        float screenPositionXPercent = UnityEngine.Random.Range(0.0f, 1.0f);
        float screenPositionYPercent = UnityEngine.Random.Range(0.0f, 1.0f);
        Vector2 screenPosition = new Vector2(screenPositionXPercent * Screen.width, screenPositionYPercent * Screen.height);

        int balloonID = nextBalloonID++;
        balloonPositions[balloonID] = screenPosition;

        string message = $"{(int)ServerToClientSignifiers.SpawnBalloon},{balloonID},{screenPosition.x},{screenPosition.y}";
        foreach (var connectionId in NetworkServerProcessing.GetAllClientIDs())
        {
            NetworkServerProcessing.SendMessageToClient(message, connectionId, TransportPipeline.ReliableAndInOrder);
        }
    }

    public void BalloonPopped(int balloonID, int clientID)
    {
        if (!balloonPositions.ContainsKey(balloonID)) return;

        balloonPositions.Remove(balloonID);

        string message = $"{(int)ServerToClientSignifiers.BalloonPopped},{balloonID}";
        foreach (var connectionId in NetworkServerProcessing.GetAllClientIDs())
        {
            NetworkServerProcessing.SendMessageToClient(message, connectionId, TransportPipeline.ReliableAndInOrder);
        }
    }

    public void SendUnpoppedBalloons(int clientID)
    {
        foreach (var balloon in balloonPositions)
        {
            string message = $"{(int)ServerToClientSignifiers.SpawnBalloon},{balloon.Key},{balloon.Value.x},{balloon.Value.y}";
            NetworkServerProcessing.SendMessageToClient(message, clientID, TransportPipeline.ReliableAndInOrder);
        }
    }
}
