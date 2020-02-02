using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// Monobehaviour for controlling which room is active at any given time. Will translate
// the grid of objects and make sure to move the player to the correct door in the new room.
public class RoomManager : MonoBehaviour
{
    public GameObject[] Rooms;
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
    public bool deletesave;

    public Dictionary<int, bool> SceneBools;
    public List<string> flags;
    // Start is called before the first frame update
    void Start()
    {
        roomTransitionHandlers = new List<OnRoomTransition>();
        if (flagChangeHandlers == null) flagChangeHandlers = new List<OnFlagsChanged>();
        if (deletesave)
        {
            SaveSystem.DestroyData();
        }
        RoomMap = new Dictionary<string, GameObject>();
        foreach (GameObject entry in Rooms) {
            RoomMap[entry.name] = entry;
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
        ActivePlayer.GetComponent<PlayerController>().manager = this;

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
            SaveSystem.Savegame(this);
        } else {
            print("No Room Named {" + newRoom + "} has been mapped!");
        }
    }

    void Update()
    { 
        if (Input.GetKeyDown(KeyCode.G))
        {
            BroadcastRoomSave();
            SaveSystem.Savegame(this);
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
        this.flags.Clear();
        this.flags.AddRange(data.flags);
        Camera.SetWorldBounds(GetBoundsOfActiveRoom());
        Camera.Track(ActivePlayer);
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

    private List<OnFlagsChanged> flagChangeHandlers;
    public void ChangeFlags(string[] newFlags, string[] removeFlags) {
        if (this.flags == null) this.flags = new List<string>();
        HashSet<string> lFlags = new HashSet<string>();
        lFlags.UnionWith(removeFlags);
        foreach (var flag in newFlags) {
            this.flags.Add(flag);
        }
        this.flags.RemoveAll((x) => lFlags.Contains(x));
        BroadcastFlagsChanged();
    }
    public void RegisterFlagsChangedHandler(OnFlagsChanged handler) {
        if (flagChangeHandlers == null) flagChangeHandlers = new List<OnFlagsChanged>();
        flagChangeHandlers.Add(handler);
    }
    void BroadcastFlagsChanged() {
        List<OnFlagsChanged> toRemove = new List<OnFlagsChanged>();
        flagChangeHandlers.RemoveAll((x) => x == null);
        foreach (var handler in flagChangeHandlers) {
            if (handler == null) continue;
            handler.FlagsChanged(this);
        }
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

public interface OnFlagsChanged {
    void FlagsChanged(RoomManager manager);
}