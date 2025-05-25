using System.IO;
using UnityEngine;

public class LoadSupabaseConfig : MonoBehaviour
{
    public static SupabaseConfig Config;

    private void Awake()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "SBkeys.json");
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            Config = JsonUtility.FromJson<SupabaseConfig>(json);
        }
        else
        {
            Debug.LogError("Supabase config file not found. ");
        }
    }
}