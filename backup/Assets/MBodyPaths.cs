using UnityEngine;

public static class MBodyPaths
{
    public static string DataRoot
    {
        get
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return Application.persistentDataPath;
#else
            return Application.dataPath;
#endif
        }
    }
}
