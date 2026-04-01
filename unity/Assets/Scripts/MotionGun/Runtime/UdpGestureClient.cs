using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace MotionGun.Runtime
{
    public class UdpGestureClient : MonoBehaviour, IGesturePacketSource
    {
        [SerializeField] private string bindAddress = "127.0.0.1";
        [SerializeField] private int port = 5053;
        [SerializeField] private bool startOnAwake = true;

        private readonly ConcurrentQueue<string> _messages = new ConcurrentQueue<string>();
        private UdpClient _udpClient;
        private Thread _receiveThread;
        private volatile bool _isRunning;
        private IMotionGunTimeSource _timeSource = UnityMotionGunTimeSource.Instance;

        public GesturePacket LatestPacket { get; private set; } = new GesturePacket();
        public bool HasPacket { get; private set; }
        public float LastPacketReceivedRealtime { get; private set; } = float.NegativeInfinity;
        public event Action<GesturePacket> PacketUpdated;

        private void Awake()
        {
            if (startOnAwake)
            {
                StartListening();
            }
        }

        private void OnDisable()
        {
            StopListening();
        }

        private void Update()
        {
            string json;
            while (_messages.TryDequeue(out json))
            {
                try
                {
                    GesturePacket packet = JsonUtility.FromJson<GesturePacket>(json);
                    if (packet == null)
                    {
                        continue;
                    }

                    LatestPacket = packet;
                    HasPacket = true;
                    LastPacketReceivedRealtime = _timeSource.RealtimeSinceStartup;
                    if (PacketUpdated != null)
                    {
                        PacketUpdated(packet);
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("Gesture packet parse failed: " + exception.Message);
                }
            }
        }

        public void StartListening()
        {
            if (_isRunning)
            {
                return;
            }

            IPAddress address = IPAddress.Parse(bindAddress);
            _udpClient = new UdpClient(new IPEndPoint(address, port));
            _isRunning = true;
            _receiveThread = new Thread(ReceiveLoop);
            _receiveThread.IsBackground = true;
            _receiveThread.Name = "MotionGunUdpClient";
            _receiveThread.Start();
        }

        public void StopListening()
        {
            if (!_isRunning)
            {
                return;
            }

            _isRunning = false;
            try
            {
                if (_udpClient != null)
                {
                    _udpClient.Close();
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Gesture socket close failed: " + exception.Message);
            }

            if (_receiveThread != null && _receiveThread.IsAlive)
            {
                _receiveThread.Join(200);
            }
        }

        private void ReceiveLoop()
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

            while (_isRunning)
            {
                try
                {
                    byte[] payload = _udpClient.Receive(ref remoteEndPoint);
                    _messages.Enqueue(Encoding.UTF8.GetString(payload));
                }
                catch (SocketException)
                {
                    if (_isRunning)
                    {
                        Debug.LogWarning("Gesture UDP receive failed.");
                    }
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
            }
        }

        public bool HasFreshPacket(float maxAgeSeconds)
        {
            return HasPacket
                && (_timeSource.RealtimeSinceStartup - LastPacketReceivedRealtime) <= maxAgeSeconds;
        }

        public void SetTimeSource(IMotionGunTimeSource timeSource)
        {
            _timeSource = timeSource ?? UnityMotionGunTimeSource.Instance;
        }
    }
}
