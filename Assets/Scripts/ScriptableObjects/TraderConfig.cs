using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TraderConfig", menuName = "Scriptable Objects/TraderConfig")]
public class TraderConfig : ScriptableObject {

    public List<Item> persistentItems;
    public ItemPool itemPool;

}
