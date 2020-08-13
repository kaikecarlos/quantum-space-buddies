﻿using System;
using System.Linq;
using OWML.ModHelper.Events;
using QSB.Animation;
using QSB.DeathSync;
using QSB.Events;
using QSB.GeyserSync;
using QSB.TimeSync;
using QSB.TransformSync;
using QSB.Utility;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace QSB
{
    public class QSBNetworkManager : NetworkManager
    {
        public static UnityEvent OnNetworkManagerReady = new UnityEvent();
        public static bool IsReady;

        private const int MaxConnections = 128;

        private AssetBundle _assetBundle;
        private GameObject _shipPrefab;
        private GameObject _cameraPrefab;
        private GameObject _probePrefab;

        private readonly string[] _defaultNames = {
            "Arkose",
            "Chert",
            "Esker",
            "Hal",
            "Hornfels",
            "Feldspar",
            "Gabbro",
            "Galena",
            "Gneiss",
            "Gossan",
            "Marl",
            "Mica",
            "Moraine",
            "Porphy",
            "Riebeck",
            "Rutile",
            "Slate",
            "Spinel",
            "Tektite",
            "Tephra",
            "Tuff"
        };
        private string _playerName;
        private bool _canEditName;

        private void Awake()
        {
            _assetBundle = QSB.Helper.Assets.LoadBundle("assets/network");

            playerPrefab = _assetBundle.LoadAsset<GameObject>("assets/networkplayer.prefab");
            playerPrefab.AddComponent<PlayerTransformSync>();
            playerPrefab.AddComponent<AnimationSync>();
            playerPrefab.AddComponent<WakeUpSync>();

            _shipPrefab = _assetBundle.LoadAsset<GameObject>("assets/networkship.prefab");
            _shipPrefab.AddComponent<ShipTransformSync>();
            spawnPrefabs.Add(_shipPrefab);

            _cameraPrefab = _assetBundle.LoadAsset<GameObject>("assets/networkcameraroot.prefab");
            _cameraPrefab.AddComponent<PlayerCameraSync>();
            spawnPrefabs.Add(_cameraPrefab);

            _probePrefab = _assetBundle.LoadAsset<GameObject>("assets/networkprobe.prefab");
            _probePrefab.AddComponent<PlayerProbeSync>();
            spawnPrefabs.Add(_probePrefab);

            ConfigureNetworkManager();

            _playerName = GetPlayerName();
            _canEditName = true;
        }

        private string GetPlayerName()
        {
            var profileManager = StandaloneProfileManager.SharedInstance;
            profileManager.Initialize();
            var profile = profileManager.GetValue<StandaloneProfileManager.ProfileData>("_currentProfile");
            var profileName = profile?.profileName;
            return !string.IsNullOrEmpty(profileName)
                ? profileName
                : _defaultNames.OrderBy(x => Guid.NewGuid()).First();
        }

        private void ConfigureNetworkManager()
        {
            networkAddress = QSB.DefaultServerIP;
            maxConnections = MaxConnections;
            customConfig = true;
            connectionConfig.AddChannel(QosType.Reliable);
            connectionConfig.AddChannel(QosType.Unreliable);
            channels.Add(QosType.Reliable);
            channels.Add(QosType.Unreliable);

            QSB.Helper.HarmonyHelper.EmptyMethod<NetworkManagerHUD>("Update");
        }

        public override void OnServerAddPlayer(NetworkConnection connection, short playerControllerId) // Called on the server when a client joins
        {
            base.OnServerAddPlayer(connection, playerControllerId);

            // These have to be in a constant order (for now, until I get a better netId getting system...)
            NetworkServer.SpawnWithClientAuthority(Instantiate(_shipPrefab), connection);
            NetworkServer.SpawnWithClientAuthority(Instantiate(_cameraPrefab), connection);
            NetworkServer.SpawnWithClientAuthority(Instantiate(_probePrefab), connection);

            gameObject.AddComponent<Events.PlayerState>();
        }

        public override void OnClientConnect(NetworkConnection connection) // Called on the client when connecting to a server
        {
            base.OnClientConnect(connection);

            gameObject.AddComponent<SectorSync>();
            gameObject.AddComponent<RespawnOnDeath>();
            gameObject.AddComponent<PreventShipDestruction>();

            if (NetworkClient.active && !NetworkServer.active)
            {
                gameObject.AddComponent<Events.PlayerState>();
                GeyserManager.Instance.EmptyUpdate();
            }

            _canEditName = false;

            OnNetworkManagerReady.Invoke();
            IsReady = true;

            UnityHelper.Instance.RunWhen(() => PlayerTransformSync.LocalInstance != null, EventList.Init);

            UnityHelper.Instance.RunWhen(() => EventList.Ready,
                () => GlobalMessenger<string>.FireEvent(EventNames.QSBPlayerJoin, _playerName));
        }

        public override void OnStopClient() // Called on the client when closing connection
        {
            DebugLog.ToScreen("OnStopClient");
            Destroy(GetComponent<SectorSync>());
            Destroy(GetComponent<RespawnOnDeath>());
            Destroy(GetComponent<PreventShipDestruction>());
            if (IsClientConnected())
            {
                PlayerTransformSync.LocalInstance.gameObject.GetComponent<AnimationSync>().Reset();
            }
            _canEditName = true;
        }

        public override void OnServerDisconnect(NetworkConnection connection) // Called on the server when any client disconnects
        {
            DebugLog.ToScreen("OnServerDisconnect");

            var playerId = connection.playerControllers[0].gameObject.GetComponent<PlayerTransformSync>().netId.Value;
            var objectIds = connection.clientOwnedObjects.Select(x => x.Value).ToArray();
            GlobalMessenger<uint, uint[]>.FireEvent(EventNames.QSBPlayerLeave, playerId, objectIds);

            base.OnServerDisconnect(connection);
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(10, 10, 200f, 20f), "Name:");
            if (_canEditName)
            {
                _playerName = GUI.TextField(new Rect(60, 10, 145, 20f), _playerName);
            }
            else
            {
                GUI.Label(new Rect(60, 10, 145, 20f), _playerName);
            }
        }

    }
}
