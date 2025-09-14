using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using Client.MirControls;
using C = ClientPackets;


namespace Client.MirNetwork
{
    static class Network
    {
        private static TcpClient _client;
        public static int ConnectAttempt = 0;
        public static bool Connected;
        public static long TimeOutTime, TimeConnected;

        private static ConcurrentQueue<Packet> _receiveList;
        private static ConcurrentQueue<Packet> _sendList;

        static byte[] _rawData = new byte[0];


        public static void Connect()
        {
            if (_client != null)
                Disconnect();

            ConnectAttempt++;

            _client = new TcpClient {NoDelay = true};
            _client.BeginConnect(Settings.IPAddress, Settings.Port, Connection, null);

        }

        private static void Connection(IAsyncResult result)
        {
            try
            {
                _client.EndConnect(result);

                if (!_client.Connected)
                {
                    Connect();
                    return;
                }

                _receiveList = new ConcurrentQueue<Packet>();
                _sendList = new ConcurrentQueue<Packet>();
                _rawData = new byte[0];

                TimeOutTime = CMain.Time + Settings.TimeOut;
                TimeConnected = CMain.Time;


                BeginReceive();
            }
            catch (SocketException)
            {
                Connect();
            }
            catch (Exception ex)
            {
                if (Settings.LogErrors) CMain.SaveError(ex.ToString());
                Disconnect();
            }
        }

        private static void BeginReceive()
        {
            if (_client == null || !_client.Connected) return;

            byte[] rawBytes = new byte[8 * 1024];

            try
            {
                _client.Client.BeginReceive(rawBytes, 0, rawBytes.Length, SocketFlags.None, ReceiveData, rawBytes);
            }
            catch
            {
                Disconnect();
            }
        }
        private static void ReceiveData(IAsyncResult result)
        {
            if (_client == null || !_client.Connected) return;

            int dataRead;

            try
            {
                dataRead = _client.Client.EndReceive(result);
            }
            catch
            {
                Disconnect();
                return;
            }

            if (dataRead == 0)
            {
                Disconnect();
            }

            byte[] rawBytes = result.AsyncState as byte[];
            // 这里的 rawData 用于 TCP 粘包，可能多个 packet 会被合并成一个 data 到达
            // 这种情况下 Packet.ReceivePacket 只会处理部分数据，而剩下的数据就会被
            // 存储在 rawData 中，下次处理 data 时，需要将 rawData 拼接在 rawBytes
            // 前面进行处理
            byte[] temp = _rawData;
            _rawData = new byte[dataRead + temp.Length];
            Buffer.BlockCopy(temp, 0, _rawData, 0, temp.Length);
            Buffer.BlockCopy(rawBytes, 0, _rawData, temp.Length, dataRead);

            Packet p;
            while ((p = Packet.ReceivePacket(_rawData, out _rawData)) != null)
                _receiveList.Enqueue(p);

            BeginReceive();
        }

        private static void BeginSend(List<byte> data)
        {
            if (_client == null || !_client.Connected || data.Count == 0) return;
            
            try
            {
                _client.Client.BeginSend(data.ToArray(), 0, data.Count, SocketFlags.None, SendData, null);
            }
            catch
            {
                Disconnect();
            }
        }
        private static void SendData(IAsyncResult result)
        {
            try
            {
                _client.Client.EndSend(result);
            }
            catch
            { }
        }


        public static void Disconnect()
        {
            if (_client == null) return;

            _client.Close();

            TimeConnected = 0;
            Connected = false;
            _sendList = null;
            _client = null;

            _receiveList = null;
        }

        /// <summary>
        /// 处理包事务（服务端解包，发送客户端心跳包）
        /// </summary>
        public static void Process()
        {
            //客户端为空，及客户端未连接时
            if (_client == null || !_client.Connected)
            {
                if (Connected) //连接状态未刷新
                {
                    while (_receiveList != null && !_receiveList.IsEmpty)  //把未处理完的包先处理完
                    {
                        //从接收队列取包
                        if (!_receiveList.TryDequeue(out Packet p) || p == null) 
                            continue;
                        if (!(p is ServerPackets.Disconnect) && !(p is ServerPackets.ClientVersion)) 
                            continue;

                        //场景处理器处理包
                        MirScene.ActiveScene.ProcessPacket(p);
                        _receiveList = null;
                        return;
                    }

                    //报错，刷新连接状态为关闭
                    MirMessageBox.Show("Lost connection with the server.", true);
                    Disconnect();
                    return;
                }
                return;
            }

            //需要重新建立连接的情况
            if (!Connected && TimeConnected > 0 && CMain.Time > TimeConnected + 5000)
            {
                Disconnect();
                Connect();
                return;
            }

            //正常连接情况下的从接收队列取包及处理包请求
            while (_receiveList != null && !_receiveList.IsEmpty)
            {
                if (!_receiveList.TryDequeue(out Packet p) || p == null) continue;
                MirScene.ActiveScene.ProcessPacket(p);
            }

            //新建一个心跳包压入待发送队列
            if (CMain.Time > TimeOutTime && _sendList != null && _sendList.IsEmpty)
                _sendList.Enqueue(new C.KeepAlive());

            if (_sendList == null || _sendList.IsEmpty) 
                return;

            TimeOutTime = CMain.Time + Settings.TimeOut;

            //从待发送队列取包，压入字节数组，发送
            List<byte> data = new List<byte>();
            while (!_sendList.IsEmpty)
            {
                if (!_sendList.TryDequeue(out Packet p)) 
                    continue;
                data.AddRange(p.GetPacketBytes());
            }
            BeginSend(data);
        }
        
        public static void Enqueue(Packet p)
        {
            if (_sendList != null && p != null)
                _sendList.Enqueue(p);
        }
    }
}
