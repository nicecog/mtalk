#if UNITY_EDITOR
using UnityEditor;

[InitializeOnLoad]
static class AndroidCleartextBootstrap
{
    static AndroidCleartextBootstrap()
    {
#if UNITY_2022_2_OR_NEWER
        if (PlayerSettings.insecureHttpOption != InsecureHttpOption.AlwaysAllowed)
            PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
#endif
    }
}
#endif
