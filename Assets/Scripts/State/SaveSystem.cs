using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class SaveSystem
{
    public static void Savegame(RoomManager manager)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        string path = Path.Combine(Application.persistentDataPath, "threadsave.gt");
        FileStream stream = new FileStream(path, FileMode.Create);

        GameData data = new GameData(manager);

        formatter.Serialize(stream, data);
        stream.Close();
    }

    public static GameData Loadgame()
    {
        string path = Path.Combine(Application.persistentDataPath, "threadsave.gt");
        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);

            GameData data  =  (GameData)formatter.Deserialize(stream);
            stream.Close();

            return data;
        }
        else
        {
            Debug.Log("savefile not found in " + path);
            return null;
        }
    }

    public static void DestroyData()
    {
        string path = Path.Combine(Application.persistentDataPath, "threadsave.gt");
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
