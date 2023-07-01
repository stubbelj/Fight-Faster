using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoulderSpawn : MonoBehaviour
{
    public GameObject boulderPrefab;
    public GameObject boulderSpawn;

    public void Init () {
        Vector3 boulderSpawnLocation = boulderSpawn.transform.position;
        GameObject boulder = Instantiate(boulderPrefab, boulderSpawnLocation, Quaternion.identity);
    }
}
