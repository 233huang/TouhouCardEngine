﻿using System;
using System.Collections.Generic;
using UnityEngine;
using TouhouCardEngine.Interfaces;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.IO;
using System.Runtime.Serialization.Json;

namespace TouhouCardEngine
{
    public class HostManager : MonoBehaviour, IHostManager, INetEventListener
    {
        [SerializeField]
        int _port = 9050;
        public int port
        {
            get { return _port; }
        }
        public string address
        {
            get { return Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString() + ":" + port; }
        }
        [SerializeField]
        bool _autoStart = false;
        public bool autoStart
        {
            get { return _autoStart; }
            set { _autoStart = value; }
        }
        NetManager net { get; set; }
        Dictionary<int, NetPeer> clientDic { get; } = new Dictionary<int, NetPeer>();
        public Interfaces.ILogger logger { get; set; } = null;
        protected void Awake()
        {
            net = new NetManager(this)
            {
                AutoRecycle = true,
                DiscoveryEnabled = true
            };

        }
        protected void Start()
        {
            if (autoStart)
            {
                if (port > 0)
                    start(port);
                else
                    start();
            }
        }
        protected void Update()
        {
            net.PollEvents();
        }
        public void start()
        {
            if (!net.IsRunning)
            {
                net.Start();
                _port = net.LocalPort;
                logger?.log("主机初始化，本地端口：" + net.LocalPort);
            }
            else
                logger?.log("Warning", "主机已经初始化，本地端口：" + net.LocalPort);
        }
        public void start(int port)
        {
            if (!net.IsRunning)
            {
                net.Start(port);
                _port = net.LocalPort;
                logger?.log("主机初始化，本地端口：" + net.LocalPort);
            }
            else
                logger?.log("Warning", "主机已经初始化，本地端口：" + net.LocalPort);
        }
        public void OnConnectionRequest(ConnectionRequest request)
        {
            NetPeer peer = request.Accept();
            clientDic.Add(peer.Id, peer);
            NetDataWriter writer = new NetDataWriter();
            writer.Put((int)PacketType.connectResponse);
            writer.Put(peer.Id);
            peer.Send(writer, DeliveryMethod.ReliableOrdered);
            logger?.log("主机同意" + request.RemoteEndPoint + "的连接请求");
        }
        public void OnPeerConnected(NetPeer peer)
        {
            logger?.log("主机被客户端" + peer.Id + "连接");
            onClientConnected?.Invoke(peer.Id);
        }
        public event Action<int> onClientConnected;
        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            PacketType type = (PacketType)reader.GetInt();
            switch (type)
            {
                case PacketType.sendRequest:
                    int id = reader.GetInt();
                    string typeName = reader.GetString();
                    string json = reader.GetString();
                    NetDataWriter writer = new NetDataWriter();
                    writer.Put((int)PacketType.sendResponse);
                    writer.Put(id);
                    writer.Put(typeName);
                    writer.Put(json);
                    logger?.log("主机收到来自客户端" + id + "的数据：（" + typeName + "）" + json);
                    foreach (var client in clientDic.Values)
                    {
                        client.Send(writer, DeliveryMethod.ReliableOrdered);
                    }
                    break;
                default:
                    logger?.log("Warning", "服务端未处理的数据包类型：" + type);
                    break;
            }
        }
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
        }
        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            logger?.log("客主机与客户端" + peer.Id + "断开连接，原因：" + disconnectInfo.Reason + "，SocketErrorCode：" + disconnectInfo.SocketErrorCode);
        }
        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            logger?.log("Error", "主机与" + endPoint + "发生网络异常：" + socketError);
        }
        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            throw new NotImplementedException();
        }
        public void stop()
        {
            net.Stop();
        }
        #region Room
        public void openRoom(RoomInfo roomInfo)
        {
            throw new NotImplementedException();
        }
        public event Action<RoomPlayerInfo> onPlayerJoin;
        public event Action<RoomPlayerInfo> onPlayerQuit;
        public void closeRoom()
        {

        }
        #endregion
    }
    [Serializable]
    public class RoomInfo
    {
        public string ip;
        public int port;
        public List<RoomPlayerInfo> playerList = new List<RoomPlayerInfo>();
    }
    [Serializable]
    public class RoomPlayerInfo
    {
        public int id = 0;
        public string name = null;
    }
    enum PacketType
    {
        connectResponse,
        sendRequest,
        sendResponse,
    }
}
