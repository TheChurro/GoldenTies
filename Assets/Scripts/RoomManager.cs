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
    public Dictionary<string, GameObject> RoomMap;
    public string InitialRoom;
    public GameObject ActiveRoom;
    public GameObject PlayerPrefab;
    private GameObject ActivePlayer;
    public GameObject Camera;
    public RoomDoor[] ActiveDoors;
    public Transform SpawnTrasform;
    // Start is called before the first frame update
    void Start()
    {
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
            }
            SpawnTrasform = roomToActivate.transform.Find("Player Spawn");
        } else {
            print("No Room Named {" + newRoom + "} has been mapped!");
        }
    }
}
