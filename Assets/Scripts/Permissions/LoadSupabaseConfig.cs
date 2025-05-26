using System.IO;
using UnityEngine;

public class LoadSupabaseConfig : MonoBehaviour
{
    [SerializeField] private TextAsset jsonData;
    public static SupabaseConfig Config;

    private void Awake()
    {
        if ( jsonData != null )
        {
            Config = JsonUtility.FromJson<SupabaseConfig>(jsonData.text);
        }
        else
        {
            Debug.LogError("Supabase config file not found. ");
        }
    }
}