using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// Monobehaviour for controlling which room is active at any given time. Will translate
// the grid of objects and make sure to move the player to the correct door in the new room.
public class RoomManager : MonoBehaviour
{
    [System.Serializable]
    public struct RoomEntry {
        public string name;
        public GameObject RoomPrefab;
    }
    public RoomEntry[] Rooms;
    public bool[] collected;

    public Dictionary<string, GameObject> RoomMap;
    public string InitialRoom;
    public GameObject ActiveRoom;
    public string ActiveRoomID;
    public GameObject PlayerPrefab;
    public GameObject ActivePlayer;
    public RoomCamera Camera;
    public RoomDoor[] ActiveDoors;
    public Transform SpawnTrasform;

    public Dictionary<int, bool> SceneBools;
    // Start is called before the first frame update
    void Start()
    {
        roomTransitionHandlers = new List<OnRoomTransition>();
        // SaveSystem.DestroyData();
        RoomMap = new Dictionary<string, GameObject>();
        foreach (RoomEntry entry in Rooms) {
            RoomMap[entry.name] = entry.RoomPrefab;
        }
        ActiveRoom = Instantiate(RoomMap[InitialRoom]);
        ActiveDoors = ActiveRoom.GetComponentsInChildren<RoomDoor>();
        foreach (RoomDoor door in ActiveDoors) {
            door.Manager = this;
        }
        SpawnTrasform = ActiveRoom.transform.Find("Player Spawn");
        ActivePlayer = Instantiate(PlayerPrefab, SpawnTrasform.position, Quaternion.identity);
        Camera.TrackingObject = ActivePlayer;
        Camera.SetWorldBounds(GetBoundsOfActiveRoom());
        ActiveRoomID = InitialRoom;

        collected = new bool[1];
        collected[0] = false;
        SceneBools = new Dictionary<int, bool>();
        LoadGame();
    }

    private Bounds GetBoundsOfActiveRoom() {
        var tiles = ActiveRoom.GetComponentInChildren<Tilemap>();
        var min = tiles.transform.TransformPoint(tiles.localBounds.min);
        var max = tiles.transform.TransformPoint(tiles.localBounds.max);
        var bounds = new Bounds();
        bounds.SetMinMax(min, max);
        return bounds;
    }

    public void ChangeRoom(string newRoom, string targetDoor) {
        if (RoomMap.TryGetValue(newRoom, out var newRoomPrefab)) {
            var roomToActivate = Instantiate(newRoomPrefab);
            var newDoors = roomToActivate.GetComponentsInChildren<RoomDoor>();
            int doorToMoveTo = -1;
            for (int i = 0; i < newDoors.Length; i++) {
                newDoors[i].Manager = this;
                if (newDoors[i].name.Equals(targetDoor)) {
                    doorToMoveTo = i;
                }
            }
            if (doorToMoveTo == -1) {
                print("No Door Named [" + targetDoor + "] in Room {" + newRoom + "}");
                Destroy(roomToActivate);
                return;
            }
            BroadcastRoomTransition(true);
            Destroy(ActiveRoom);
            ActiveRoom = roomToActivate;
            ActiveDoors = newDoors;
            var DoorTransition = ActiveDoors[doorToMoveTo];
            Grid roomGrid = roomToActivate.GetComponent<Grid>();
            Vector3 gridSize = roomGrid.cellSize + roomGrid.cellGap;
            var offsetDir = DoorTransition.GetEntryWorldOffset();
            var offset = new Vector3(gridSize.x * offsetDir.x, gridSize.y * offsetDir.y, 0);
            if (ActivePlayer != null) {
                ActivePlayer.transform.position = DoorTransition.transform.position + offset;
            } else {
                ActivePlayer = Instantiate(
                    PlayerPrefab,
                    DoorTransition.transform.position + offset,
                    Quaternion.identity
                );
                Camera.TrackingObject = ActivePlayer;
            }
            SpawnTrasform = roomToActivate.transform.Find("Player Spawn");
            Camera.SetWorldBounds(GetBoundsOfActiveRoom());
            Camera.Track(ActivePlayer);
            ActiveRoomID = newRoom;
            SaveSystem.Savegame(GameObject.Find("NumCeramics").GetComponent<CeramicIndicator>(), this);
        } else {
            print("No Room Named {" + newRoom + "} has been mapped!");
        }
    }

    void Update()
    { 
        if (Input.GetKeyDown(KeyCode.G))
        {
            BroadcastRoomSave();
            SaveSystem.Savegame(GameObject.Find("NumCeramics").GetComponent<CeramicIndicator>(), this);
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            LoadGame();
        }
    }

    void LoadGame() {
        GameData data = SaveSystem.Loadgame();
        if (data == null) return;
        BroadcastRoomTransition(false);
        Destroy(ActiveRoom);
        ActiveRoom = Instantiate(RoomMap[data.activeroom]);
        ActiveDoors = ActiveRoom.GetComponentsInChildren<RoomDoor>();
        foreach (RoomDoor door in ActiveDoors)
        {
            door.Manager = this;
        }
        ActivePlayer.transform.position = new Vector3(data.playerposition[0], data.playerposition[1], data.playerposition[2]);
        var controller = ActivePlayer.GetComponent<PlayerController>();
        controller.flags.Clear();
        controller.flags.AddRange(data.flags);
        Camera.SetWorldBounds(GetBoundsOfActiveRoom());
        Camera.Track(ActivePlayer);
        var ceram = GameObject.Find("NumCeramics").GetComponent<CeramicIndicator>();
        ceram.ceramicnumber = data.ceramics;
        SceneBools = new Dictionary<int, bool>();
        for (int i = 0; i < data.boolStorageUID.Length; i++) {
            SceneBools[data.boolStorageUID[i]] = data.boolStorageVals[i];
        }
    }

    private List<OnRoomTransition> roomTransitionHandlers;
    public void RegisterTransitionHandler(OnRoomTransition handler) {
        roomTransitionHandlers.Add(handler);
    }
    void BroadcastRoomSave() {
        foreach (OnRoomTransition handler in roomTransitionHandlers) {
            if (handler == null) continue;
            handler.OnRoomSave(this);
        }
    }
    void BroadcastRoomTransition(bool willSave) {
        foreach (OnRoomTransition handler in roomTransitionHandlers) {
            if (handler == null) continue;
            handler.OnRoomTransition(this, willSave);
        }
        roomTransitionHandlers.Clear();
    }

    public bool GetBool(int objectUID) {
        if (SceneBools.TryGetValue(objectUID, out bool val)) {
            return val;
        }
        SceneBools[objectUID] = false;
        return false;
    }

    public void SetBool(int objectUID, bool val) {
        SceneBools[objectUID] = val;
    }
}

public interface OnRoomTransition {
    void OnRoomTransition(RoomManager manager, bool willSave);
    void OnRoomSave(RoomManager manager);
}
