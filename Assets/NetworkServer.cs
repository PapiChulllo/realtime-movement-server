using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;
using System.Collections.Generic;
using System.Text;

public enum TransportPipeline
{
    NotIdentified,
    ReliableAndInOrder,
    FireAndForget
}

public class NetworkServer : MonoBehaviour
{
    public NetworkDriver networkDriver;
    private NativeList<NetworkConnection> networkConnections;
    NetworkPipeline reliableAndInOrderPipeline;
    NetworkPipeline nonReliableNotInOrderedPipeline;
    const ushort NetworkPort = 9001;
    const int MaxNumberOfClientConnections = 1000;
    Dictionary<int, NetworkConnection> idToConnectionLookup;
    Dictionary<NetworkConnection, int> connectionToIDLookup;

    void Start()
    {
        NetworkServerProcessing.SetNetworkServer(this);
        DontDestroyOnLoad(this.gameObject);

        #region Bind and Listen

        idToConnectionLookup = new Dictionary<int, NetworkConnection>();
        connectionToIDLookup = new Dictionary<NetworkConnection, int>();

        networkDriver = NetworkDriver.Create();
        reliableAndInOrderPipeline = networkDriver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(ReliableSequencedPipelineStage));
        nonReliableNotInOrderedPipeline = networkDriver.CreatePipeline(typeof(FragmentationPipelineStage));
        NetworkEndPoint endpoint = NetworkEndPoint.AnyIpv4;
        endpoint.Port = NetworkPort;

        int error = networkDriver.Bind(endpoint);
        if (error != 0)
        {
            UnityEngine.Debug.LogError($"Failed to bind to port {NetworkPort}. Error: {error}");
        }
        else
        {
            networkDriver.Listen();
            UnityEngine.Debug.Log($"Server is listening on port {NetworkPort}");
        }

        networkConnections = new NativeList<NetworkConnection>(MaxNumberOfClientConnections, Allocator.Persistent);

        #endregion
    }

    void OnDestroy()
    {
        networkDriver.Dispose();
        networkConnections.Dispose();
    }

    void Update()
    {
        networkDriver.ScheduleUpdate().Complete();

        #region Manage Connections

        // Remove stale connections
        for (int i = 0; i < networkConnections.Length; i++)
        {
            if (!networkConnections[i].IsCreated)
            {
                networkConnections.RemoveAtSwapBack(i);
                i--;
            }
        }

        // Accept new connections
        while (AcceptIncomingConnection()) { }

        #endregion

        #region Handle Events

        DataStreamReader streamReader;
        NetworkPipeline pipelineUsedToSendEvent;
        NetworkEvent.Type networkEventType;

        for (int i = 0; i < networkConnections.Length; i++)
        {
            if (!networkConnections[i].IsCreated) continue;

            while (PopNetworkEventAndCheckForData(networkConnections[i], out networkEventType, out streamReader, out pipelineUsedToSendEvent))
            {
                TransportPipeline pipelineUsed = TransportPipeline.NotIdentified;
                if (pipelineUsedToSendEvent == reliableAndInOrderPipeline)
                    pipelineUsed = TransportPipeline.ReliableAndInOrder;
                else if (pipelineUsedToSendEvent == nonReliableNotInOrderedPipeline)
                    pipelineUsed = TransportPipeline.FireAndForget;

                switch (networkEventType)
                {
                    case NetworkEvent.Type.Data:
                        int sizeOfDataBuffer = streamReader.ReadInt();
                        NativeArray<byte> buffer = new NativeArray<byte>(sizeOfDataBuffer, Allocator.Persistent);
                        streamReader.ReadBytes(buffer);
                        byte[] byteBuffer = buffer.ToArray();
                        string msg = Encoding.Unicode.GetString(byteBuffer);
                        NetworkServerProcessing.ReceivedMessageFromClient(msg, connectionToIDLookup[networkConnections[i]], pipelineUsed);
                        buffer.Dispose();
                        break;
                    case NetworkEvent.Type.Disconnect:
                        NetworkConnection nc = networkConnections[i];
                        int id = connectionToIDLookup[nc];
                        NetworkServerProcessing.DisconnectionEvent(id);
                        idToConnectionLookup.Remove(id);
                        connectionToIDLookup.Remove(nc);
                        networkConnections[i] = default(NetworkConnection);
                        break;
                }
            }
        }

        #endregion
    }

    private bool AcceptIncomingConnection()
    {
        NetworkConnection connection = networkDriver.Accept();
        if (connection == default(NetworkConnection))
            return false;

        networkConnections.Add(connection);

        int id = 0;
        while (idToConnectionLookup.ContainsKey(id)) id++;
        idToConnectionLookup.Add(id, connection);
        connectionToIDLookup.Add(connection, id);

        NetworkServerProcessing.ConnectionEvent(id);

        return true;
    }

    private bool PopNetworkEventAndCheckForData(NetworkConnection networkConnection, out NetworkEvent.Type networkEventType, out DataStreamReader streamReader, out NetworkPipeline pipelineUsedToSendEvent)
    {
        networkEventType = networkConnection.PopEvent(networkDriver, out streamReader, out pipelineUsedToSendEvent);

        if (networkEventType == NetworkEvent.Type.Empty)
            return false;
        return true;
    }

    public void SendMessageToClient(string msg, int connectionID, TransportPipeline pipeline)
    {
        NetworkPipeline networkPipeline = reliableAndInOrderPipeline;
        if (pipeline == TransportPipeline.FireAndForget)
            networkPipeline = nonReliableNotInOrderedPipeline;

        byte[] msgAsByteArray = Encoding.Unicode.GetBytes(msg);
        NativeArray<byte> buffer = new NativeArray<byte>(msgAsByteArray, Allocator.Persistent);
        DataStreamWriter streamWriter;

        networkDriver.BeginSend(networkPipeline, idToConnectionLookup[connectionID], out streamWriter);
        streamWriter.WriteInt(buffer.Length);
        streamWriter.WriteBytes(buffer);
        networkDriver.EndSend(streamWriter);

        buffer.Dispose();
    }

    public List<int> GetAllClientIDs()
    {
        return new List<int>(idToConnectionLookup.Keys);
    }
}
