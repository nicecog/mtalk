using UnityEngine;

[CreateAssetMenu(fileName = "MSocialStageCatalog", menuName = "M-Social/Stage Catalog")]
public class MSocialStageCatalog : ScriptableObject
{
    public Sprite[] firstFrames;
    public string[] titles;
    public int[] videoCodes;
    public int[] prv;

    static MSocialStageCatalog _instance;

    public static MSocialStageCatalog Instance
    {
        get
        {
            if (_instance != null)
                return _instance;
            _instance = Resources.Load<MSocialStageCatalog>("MSocialStageCatalog");
            return _instance;
        }
    }
}
