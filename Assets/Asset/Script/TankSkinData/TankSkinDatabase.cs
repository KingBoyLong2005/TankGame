// TankSkinDatabase.cs (create as ScriptableObject)
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Tank/TankSkinDatabase")]
public class TankSkinDatabase : ScriptableObject
{
    public List<TankSkin> skins;

    public TankSkin GetSkinById(string id)
    {
        return skins.Find(s => s.id == id);
    }

    public TankSkin GetSkinByIndex(int idx)
    {
        if (idx < 0 || idx >= skins.Count) return null;
        return skins[idx];
    }
}
