using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GameManager : MonoBehaviour
{
    public static GameManager inst = null;
    public Tilemap groundTilemap;
    public Tilemap traversableTilemap;

    public GameObject testMarkerPrefab;

    public GameObject player;
    public Transform pathTarget;

    Pathfinding pathfinding;

    void Awake() {
        if (inst == null) {
            inst = this;
        } else {
            Destroy(gameObject);
        }
    }

    void Start() {
        pathfinding = new Pathfinding();
        pathfinding.GenerateCollisionMasks(groundTilemap, traversableTilemap);
        pathfinding.InitPathFind(player.transform.position, pathTarget.position, 32);
        StartCoroutine(DelayedStart());
    }

    IEnumerator DelayedStart() {
        yield return new WaitForSeconds(5f);
        List<Vector3> solution = pathfinding.PathFind(new float[]{16, 16}, testMarkerPrefab);
        /*foreach(Vector3 point in solution) {
            GameObject.Instantiate(testMarkerPrefab, point, Quaternion.identity);
        }*/
    }
}
