using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Mighty_MazeGen;

[CreateAssetMenu]
public class Mighty_MazeGen : ScriptableObject
{
    //editable fields in Inspector
    public int columns;
    public int rows;
    public bool removeDeadEnds;
    public Vector3 defaultWorldPosition;
    public Generic GenericRooms;
    public Special specialRooms;
    public List<Anchor> anchorRooms;

    //"private" fields not shown in Inspector
    public enum Types { Anchor, Special, Generic }
    [HideInInspector]
    public List<List<layoutBone>> layoutSkeleton = new List<List<layoutBone>>();
    private List<GameObject> layoutObjects = new List<GameObject>();
    private Dictionary<Vector2Int, int> dirHistory = new Dictionary<Vector2Int, int>();

    /* These are the main interface functions that user will be using. They are
     * as follows: makeSkeleton makes the layoutSkeleton and nothing else, but 
     * makeFull will make a skeleton and build it. Print() is a great debugging 
     * tool, so use it.*/
    public void makeSkeleton()
    {
        if (!SetUp()) return;
        Generate();
    }
    public void makeSkeleton(int seed)
    {
        UnityEngine.Random.InitState(seed);
        if (!SetUp()) return;
        Generate();
    }
    public void makeFull()
    {
        Destroy();
        if (!SetUp()) return;
        if (!Generate()) return;
        Build();
    }
    public void makeFull(int seed)
    {
        UnityEngine.Random.InitState(seed);
        Destroy();
        if (!SetUp()) return;
        if (!Generate()) return;
        Build();
    }
    public void makeFull(Vector3 buildPos)
    {
        Destroy();
        if (!SetUp()) return;
        if (!Generate()) return;
        Build(buildPos);
    }
    public void makeFull(int seed, Vector3 buildPos)
    {
        UnityEngine.Random.InitState(seed);
        Destroy();
        if (!SetUp()) return;
        if (!Generate()) return;
        Build(buildPos);
    }
    public void Print()
    {
        string printMe = "";
        printMe += this.name + "'s layoutSkeleton:";
        char roomChar;
        string line1 = "\n";
        string line2 = "\n";
        string line3 = "\n";
        for (int i = rows - 1; i > -1; i--)
        {
            for (int j = 0; j < columns; j++)
            {
                if (layoutSkeleton[i][j].group != null)
                {
                    if ((layoutSkeleton[i][j].boneExits & 0b0001) != 0) line1 += "*|*"; else line1 += "***";
                    switch (layoutSkeleton[i][j].group.TypeOfGroup)
                    {
                        default: roomChar = 'o'; break;
                        case Types.Anchor: roomChar = 'a'; break;
                        case Types.Special: roomChar = 's'; break;
                        case Types.Generic: roomChar = 'g'; break;
                    }
                    switch (layoutSkeleton[i][j].boneExits & 0b1010)
                    {
                        default: line2 += "*" + roomChar + "*"; break;
                        case 0b1010: line2 += "-" + roomChar + "-"; break;
                        case 0b1000: line2 += "-" + roomChar + "*"; break;
                        case 0b0010: line2 += "*" + roomChar + "-"; break;
                    }
                    if ((layoutSkeleton[i][j].boneExits & 0b0100) != 0) line3 += "*|*"; else line3 += "***";
                }
                else
                {
                    line1 += "***";
                    line2 += "***";
                    line3 += "***";
                }
            }
            printMe += line1 + line2 + line3;
            line1 = "\n";
            line2 = "\n";
            line3 = "\n";
        }
        Debug.Log(printMe);
    }

    /* These construction and deconstruction functions are kept public for when
     * people need to add more customization to their maze generation process.
     * (By customization, I mean pre-setting custom bones before generation)*/
    public bool SetUp()
    {
        if (rows <= 0 || columns <= 0) { Debug.LogError(this.name + " Setup Failed:\nColumns and Rows must be greater than 0"); return false; }
        if (rows == 1 && columns == 1) { Debug.LogError(this.name + " Setup Failed:\nGeneration Area must be bigger than 1 by 1"); return false; }
        if (rows > 15 || columns > 15) { Debug.LogWarning(this.name + " Setup Warning:\nColumns and Rows greater than 15 may introduce lag when generting.\nIf you don't care, ignore this warning."); return false; }

        //empty skeleton for generation
        layoutSkeleton.Clear();
        for (int i = 0; i < rows; i++)
        {
            List<layoutBone> col = new List<layoutBone>();
            for (int j = 0; j < columns; j++)
            {
                col.Add(new layoutBone(j, i));
            }
            layoutSkeleton.Add(col);
        }
        //set the type for each room collection
        GenericRooms.TypeOfGroup = Types.Generic;
        specialRooms.TypeOfGroup = Types.Special;
        foreach (Anchor g in anchorRooms)
        {
            g.TypeOfGroup = Types.Anchor;
        }
        return true;
    }
    public bool Generate()
    {
        if (!placeAnchor()) 
        { 
            Debug.LogError(this.name + " Generation Failed:\nTwo Anchors tried to generate on the same location." +
                "\nAdd more possible locations or remove some anchors to prevent this."); 
            return false; 
        }
        //place special rooms
        if (!placeSpecial()) 
        { 
            Debug.LogError(this.name + " Generation Failed:\nThe layout is too small and not all Special rooms can " +
                "be generated."); 
            return false; 
        }

        //change the code to connect either specials, anchors, complexes, a mix, or two generics depending on input
        //connect two specials
        List<layoutBone> allSpecial = findBones(Types.Special);
        List<layoutBone> allAnchors = findBones(Types.Anchor);
        List <layoutBone> noLifeBones = new List<layoutBone>();
        foreach (layoutBone b in allAnchors) { if (b.boneExits == 240) { noLifeBones.Add(b); } }
        foreach (layoutBone b in noLifeBones) { if (b.boneExits == 240) { allAnchors.Remove(b); } }
        int specialCount = allSpecial.Count();
        int anchorCount = allAnchors.Count();
        layoutBone bn1;
        layoutBone bn2;
        bool fullMaze = false;
        if (specialCount > 1)
        {
            bn1 = allSpecial[UnityEngine.Random.Range(0, allSpecial.Count())];
            allSpecial.Remove(bn1);
            bn2 = allSpecial[UnityEngine.Random.Range(0, allSpecial.Count())];
            allSpecial.Remove(bn2);
        } else if (specialCount == 1 && anchorCount > 0)
        {
            bn1 = allSpecial[UnityEngine.Random.Range(0, allSpecial.Count())];
            allSpecial.Remove(bn1);
            bn2 = allAnchors[UnityEngine.Random.Range(0, allSpecial.Count())];
            allAnchors.Remove(bn2);
        } else if (anchorCount > 1)
        {
            bn1 = allAnchors[UnityEngine.Random.Range(0, allSpecial.Count())];
            allAnchors.Remove(bn1);
            bn2 = allAnchors[UnityEngine.Random.Range(0, allSpecial.Count())];
            allAnchors.Remove(bn2);
        } else if (specialCount > 0)
        {
            bn1 = allSpecial[UnityEngine.Random.Range(0, allSpecial.Count())];
            allSpecial.Remove(bn1);
            do
            {
                bn2 = layoutSkeleton[UnityEngine.Random.Range(0, rows)][UnityEngine.Random.Range(0, columns)];
            } while (bn2.group is null);
            bn2.group = GenericRooms;
        } else if (anchorCount > 0)
        {
            bn1 = allAnchors[UnityEngine.Random.Range(0, allSpecial.Count())];
            allAnchors.Remove(bn1);
            do
            {
                bn2 = layoutSkeleton[UnityEngine.Random.Range(0, rows)][UnityEngine.Random.Range(0, columns)];
            } while (bn2.group is null);
            bn2.group = GenericRooms;
        } else
        {
            bn1 = layoutSkeleton[UnityEngine.Random.Range(0, rows)][UnityEngine.Random.Range(0, columns)];
            do
            {
                bn2 = layoutSkeleton[UnityEngine.Random.Range(0, rows)][UnityEngine.Random.Range(0, columns)];
            } while (bn1 == bn2);
            bn1.group = GenericRooms;
            bn2.group = GenericRooms;
            fullMaze = true;
        }
        if (!connectBones(bn1, bn2))
        {
            //Debug Error is already returned in connectbone() function
            return false;
        }
        //add an exit conecting anchors to adjacent bones they share an exit with, make null bones into generic one
        if (!addAnchorDeadEnds()) 
        { 
            Debug.LogError(this.name + " Generation Failed:\nAt least one anchor has a fixedExit going outside the bounds " +
                "of the layout, or\nat least one anchor has an exit that faces into the non-exit side of another fixedExit bone."); 
            return false; 
        }
        //connect bones to maze (only if it doesn't already have a connection)
        if (!connectBonesOfType(Types.Special))
        {
            //Debug Error is already returned in connectbone() function
            return false;
        }
        if (!fullMaze && !noMoreDeadEnds(Types.Generic))
        {
            //Debug Error is already returned in noMoreDeadEnds() function
            return false;
        } else if (fullMaze)
        {
            layoutBone impossible = new layoutBone(-1, -1);
            foreach (List<layoutBone> lb in layoutSkeleton)
            {
                foreach(layoutBone b in lb)
                {
                    if (b.group is null && !connectBones(b, impossible))
                    {
                        return false;
                    }
                }
            }
        }
        foreach (layoutBone an in allAnchors)
        {
            if (UnityEngine.Random.Range(0, 2) == 0)
            {
                if (!connectBones(an, bn2))
                {
                    //Debug Error is already returned in connectbone() function
                    return false;
                }
            } else
            {
                if (!connectBones(bn1, an))
                {
                    //Debug Error is already returned in connectbone() function
                    return false;
                }
            }
        }

        if (removeDeadEnds && !noMoreDeadEnds())
        {
            //Debug Error is already returned in noMoreDeadEnds() function
            return false;
        }
        if (removeDeadEnds && !reallyKillDeadEndsForTheLastTimeHopefully())
        {
            //Debug Error is already returned in noMoreDeadEnds() function
            return false;
        }
        return true;
    }
    public void Build()
    {
        foreach (List<layoutBone> lb in layoutSkeleton)
        {
            foreach (layoutBone b in lb)
            {
                if ((b.boneExits & 0b1111) != 0)
                {
                    b.setRoom();
                    if (b.roomPrefab != null)
                    {
                        GameObject boneRoom = Instantiate(b.roomPrefab);
                        Bounds boneSize = boneRoom.GetComponent<Renderer>().bounds;
                        boneRoom.transform.position = new Vector3((boneSize.size.x * (b.position.x + 1)) + defaultWorldPosition.x, (boneSize.size.y * (b.position.y + 1)) + defaultWorldPosition.y, defaultWorldPosition.z);
                        layoutObjects.Add(boneRoom);
                    } else
                    {
                        Debug.LogWarningFormat(this.name + " Build Warning:\n" + b.group.Name + " is missing the prefab for a room with exit code: " + (b.boneExits & 0b1111));
                    }
                }
            }
        }
    }
    public void Build(Vector3 buildPos)
    {
        foreach (List<layoutBone> lb in layoutSkeleton)
        {
            foreach (layoutBone b in lb)
            {
                if ((b.boneExits & 0b1111) != 0)
                {
                    b.setRoom();
                    if (b.roomPrefab != null)
                    {
                        GameObject boneRoom = Instantiate(b.roomPrefab);
                        Bounds boneSize = boneRoom.GetComponent<Renderer>().bounds;
                        boneRoom.transform.position = new Vector3((boneSize.size.x * (b.position.x + 1)) + buildPos.x, (boneSize.size.y * (b.position.y + 1)) + buildPos.y, buildPos.z);
                        layoutObjects.Add(boneRoom);
                    }
                    else
                    {
                        Debug.LogWarningFormat(this.name + " Build Warning:\n" + b.group.Name + " is missing the prefab for a room with exit code: " + (b.boneExits & 0b1111));
                    }
                }
            }
        }
    }
    public void Destroy()
    {
        foreach (GameObject obj in layoutObjects)
        {
            Destroy(obj);
        }
        layoutObjects.Clear();
    }

    /* All of these are used in the bone placement and correction process.
     * the last fuction with a long ass name is because dead ends are a pain.*/
    private bool placeSpecial()
    {
        List<layoutBone> possibleRooms = findBones(null);
        possibleRooms.AddRange(findBones(Types.Generic));
        for (int i = 0; i < specialRooms.Amount; i++)
        {
            if (possibleRooms.Count <= 0) return false;
            int temp = UnityEngine.Random.Range(0, possibleRooms.Count);
            possibleRooms[temp].group = specialRooms;
            possibleRooms.RemoveAt(temp);
        }
        return true;
    }
    private bool placeAnchor()
    {
        foreach (Anchor a in anchorRooms)
        {
            List<Vector2Int> possibleLocations = new List<Vector2Int>(a.possibleLocations);
            foreach(Vector2Int location in a.possibleLocations)
            {
                if (location.x < 0 || location.y < 0 || location.x >= columns || location.y >= rows || layoutSkeleton[location.y][location.x].group is not null) possibleLocations.Remove(location);
            }
            for (int i = 0; i < a.Amount; i++)
            {
                if (possibleLocations.Count <= 0) return false;
                Vector2Int chosenLocation = possibleLocations[UnityEngine.Random.Range(0, possibleLocations.Count)];
                layoutSkeleton[chosenLocation.y][chosenLocation.x].group = a;
                uint exits = 0b11110000;
                foreach (Room.Exits e in a.exits)
                {
                    exits |= (uint)e;
                }
                layoutSkeleton[chosenLocation.y][chosenLocation.x].boneExits = exits;
                possibleLocations.Remove(chosenLocation);
            }
        }
        return true;
    }
    private bool addAnchorDeadEnds()
    {
        List<layoutBone> anchors = findBones(Types.Anchor);
        foreach (layoutBone bone in anchors)
        {
            Anchor a = (Anchor)bone.group;
            layoutBone deadEnd;
            if ((bone.boneExits & 0b0001) > 0 && bone.position.y < rows - 1)
            {
                deadEnd = layoutSkeleton[bone.position.y + 1][bone.position.x];
                if (deadEnd.addExit(0b0100) ||
                    (deadEnd.boneExits & 0b0100) != 0)
                {
                    if (deadEnd.group is null)
                    {
                        deadEnd.group = GenericRooms;
                    }
                }
                else
                {
                    return false;
                }
            }
            if ((bone.boneExits & 0b0010) > 0 && bone.position.x < columns - 1)
            {
                deadEnd = layoutSkeleton[bone.position.y][bone.position.x + 1];
                if (deadEnd.addExit(0b1000) ||
                    (deadEnd.boneExits & 0b1000) != 0)
                {
                    if (deadEnd.group is null)
                    {
                        deadEnd.group = GenericRooms;
                    }
                }
                else
                {
                    return false;
                }
            }
            if ((bone.boneExits & 0b0100) > 0 && bone.position.y > 0)
            {
                deadEnd = layoutSkeleton[bone.position.y - 1][bone.position.x];
                if (deadEnd.addExit(0b0001) ||
                    (deadEnd.boneExits & 0b0001) != 0)
                {
                    if (deadEnd.group is null)
                    {
                        deadEnd.group = GenericRooms;
                    }
                }
                else
                {
                    return false;
                }
            }
            if ((bone.boneExits & 0b1000) > 0 && bone.position.x > 0)
            {
                deadEnd = layoutSkeleton[bone.position.y][bone.position.x - 1];
                if (deadEnd.addExit(0b0010) ||
                    (deadEnd.boneExits & 0b0010) != 0)
                {
                    if (deadEnd.group is null)
                    {
                        deadEnd.group = GenericRooms;
                    }
                }
                else
                {
                    return false;
                }
            }
        }
        return true;
    }
    private bool noMoreDeadEnds()
    {
        Vector2Int impossibleEnd = new Vector2Int(-1, -1);
        foreach (List<layoutBone> lb in layoutSkeleton)
        {
            foreach (layoutBone b in lb)
            {
                if (b.boneExits != 0 && b.group.TypeOfGroup != Types.Anchor && (b.boneExits & (b.boneExits - 1)) == 0)
                {
                    if (!prepareWilson(b.position, impossibleEnd, (int)b.boneExits))
                    {
                        Debug.LogError(this.name + "Generation Error!!!\nBone at "
                    + b.position + " failed to connect to other bones and remove it's dead end.\n" +
                    "Please check placement of rooms with fixed exits, as they may be blocking " +
                    "off certain rooms from connecting\nto the rest of the skeleton.");
                        return false;
                    }
                    enactWilson(b);
                    unlockNonAnchorWalls();
                }
            }
        }
        return true;
    }
    private bool noMoreDeadEnds(Types type)
    {
        Vector2Int impossibleEnd = new Vector2Int(-1, -1);
        foreach (List<layoutBone> lb in layoutSkeleton)
        {
            foreach (layoutBone b in lb)
            {
                if ((b.boneExits & 0b1111) != 0 && b.group.TypeOfGroup == type && (b.boneExits & (b.boneExits - 1)) == 0)
                {
                    uint copyExits = b.boneExits + 0; //makes it a copy by value rather than a copy by reference
                    b.boneExits |= (copyExits & 0b1111) << 4;
                    b.boneExits &= ~(copyExits & 0b1111);

                    if (!prepareWilson(b.position, impossibleEnd, (int)copyExits))
                    {
                        Debug.LogError(this.name + "Generation Error!!!\nBone at "
                    + b.position + " failed to connect to other bones and remove it's dead end.\n" +
                    "Please check placement of rooms with fixed exits, as they may be blocking " +
                    "off certain rooms from connecting\nto the rest of the skeleton.");
                        return false;
                    }
                    b.boneExits = copyExits;
                    enactWilson(b);
                    unlockNonAnchorWalls();
                }
            }
        }
        return true;
    }
    private bool reallyKillDeadEndsForTheLastTimeHopefully()
    {
        GenericRooms.TypeOfGroup = Types.Special;
        Vector2Int impossibleEnd = new Vector2Int(-1, -1);
        foreach (List<layoutBone> lb in layoutSkeleton)
        {
            foreach (layoutBone b in lb)
            {
                if (b.boneExits != 0 && b.group.TypeOfGroup != Types.Anchor && (b.boneExits & (b.boneExits - 1)) == 0)
                {
                    if (!prepareWilson(b.position, impossibleEnd, (int)b.boneExits))
                    {
                        Debug.LogError(this.name + "Generation Error!!!\nBone at "
                    + b.position + " failed to connect to other bones and remove it's dead end.\n" +
                    "Please check placement of rooms with fixed exits, as they may be blocking " +
                    "off certain rooms from connecting\nto the rest of the skeleton.");
                        return false;
                    }
                    enactWilson(b);
                    unlockNonAnchorWalls();
                }
            }
        }
        GenericRooms.TypeOfGroup = Types.Generic;
        return true;
    }

    /* These functions all are used in the bone connecting process. PrepareWilson 
     * and enactWilson both use a modified version of Wilson's algorithm to find
     * suitable connections.*/
    private bool connectBones(layoutBone a, layoutBone b)
    {
        if (a == b) return true;
        if (!prepareWilson(a.position, b.position, 0)) 
        {
            //Debug.Log(a.position + ", " + b.position);
            Debug.LogError( this.name + "Generation Error!!!\nBone at "+ a.position + " failed to connect " +
                "to bone at " + b.position + "\nPlease check placement of rooms with fixed exits, as " +
                "they may be blocking off certain rooms from connecting\nto the rest of the skeleton."); 
            return false; 
        }
        enactWilson(a);
        unlockNonAnchorWalls();
        return true;
    }
    private bool connectBonesOfType(Types type)
    {
        List<layoutBone> bones = findBones(type);
        Vector2Int impossibleEnd = new Vector2Int(-1, -1);
        foreach (layoutBone bone in bones)
        {
            if (!(bone.group.TypeOfGroup == Types.Anchor && bone.boneExits == 0) && bone.boneExits == 0)
            {
                if (!prepareWilson(bone.position, impossibleEnd, 0))
                {
                    Debug.LogError(this.name + "Generation Error!!!\nBone at "
                    + bone.position + " failed to connect to another bone.\n" +
                    "Please check placement of rooms with fixed exits, as they may be blocking " +
                    "off certain rooms from connecting\nto the rest of the skeleton."); 
                    return false;
                }
                enactWilson(bone);
                unlockNonAnchorWalls();
            }
        }
        return true;
    }
    private bool prepareWilson(Vector2Int start, Vector2Int end, int lastDir)
    {
        layoutBone nextRoom;
        uint otherExits;
        Vector2Int beginningBone = start;
        uint count = 0;
        do {
            count++;
            if (getRandDirection(start, 0) == 0) { return false; }
            int randDir = getRandDirection(start, lastDir);
            if (randDir == lastDir)
            {
                uint wall = (uint)randDir;
                layoutSkeleton[start.y][start.x].boneExits |= (wall << 4);
            }
            dirHistory[start] = randDir;
            switch (randDir)
            {
                case 0b0001: start.y += 1; lastDir = 0b0100; break;
                case 0b0010: start.x += 1; lastDir = 0b1000; break;
                case 0b0100: start.y -= 1; lastDir = 0b0001; break;
                case 0b1000: start.x -= 1; lastDir = 0b0010; break;
                default: break;
            }
            nextRoom = layoutSkeleton[start.y][start.x];
            otherExits = nextRoom.boneExits & 0b1111;
            if (count > 1000000) { Debug.LogWarning("Generation is taking too long, so it will end here.\nTry making a smaller area, " +
                "reducing the number of anchor rooms, or reducing the number of anchors with a fixed exit.\nThis is a safety feature " +
                "to prevent endless loops. Disable at your own risk."); return false; }
        } while (!(start == end ||
                (end.x == -1 && nextRoom.group is not null &&
                    ((layoutSkeleton[beginningBone.y][beginningBone.x].group is null &&
                    nextRoom.group.TypeOfGroup == Types.Generic) ||
                    (otherExits != 0 &&
                    nextRoom.group.TypeOfGroup == Types.Special) ||
                    ((otherExits & (otherExits - 1)) != 0 &&
                    nextRoom.group.TypeOfGroup == Types.Generic)))));
        dirHistory[new Vector2Int(-1, -1)] = dirHistory[beginningBone];
        dirHistory[start] = 0;
        return true;

    }
    private void enactWilson(layoutBone a)
    {
        int dir;
        Vector2Int beginningBone = a.position;
        if (dirHistory[a.position] == 0)
        {
            dirHistory[a.position] = dirHistory[new Vector2Int(-1, -1)];
        }
        do
        {
            dir = dirHistory[a.position];
            a.addExit((uint)dir);
            Vector2Int nextPos = a.position;
            switch (dir)
            {
                case 0b0001: nextPos.y += 1; layoutSkeleton[nextPos.y][nextPos.x].addExit(0b0100); break;
                case 0b0010: nextPos.x += 1; layoutSkeleton[nextPos.y][nextPos.x].addExit(0b1000); break;
                case 0b0100: nextPos.y -= 1; layoutSkeleton[nextPos.y][nextPos.x].addExit(0b0001); break;
                case 0b1000: nextPos.x -= 1; layoutSkeleton[nextPos.y][nextPos.x].addExit(0b0010); break;
                default: break;
            }
            if (a.group is null) { a.group = GenericRooms; }
            a = layoutSkeleton[nextPos.y][nextPos.x];
            if (a.position == beginningBone)
            {
                break;
            }
        } while (dir != 0);
        return;
    }

    /* These helper functions serve many purposes, but are kept private to prevent 
     * people from using them.*/
    private void unlockNonAnchorWalls()
    {
        foreach (List<layoutBone> lb in layoutSkeleton)
        {
            foreach (layoutBone b in lb)
            {
                if (b.group is null)
                {
                    b.boneExits = 0;
                } else if (b.group.TypeOfGroup != Types.Anchor)
                {
                    b.boneExits &= 0b1111;
                }
            }
        }
    }
    private List<layoutBone> findBones(Types type)
    {
        List<layoutBone> layoutBones = new List<layoutBone>();
        foreach (List<layoutBone> Llb in layoutSkeleton)
        {
            foreach (layoutBone bone in Llb)
            {
                if (bone.group != null && bone.group.TypeOfGroup == type)
                {
                    layoutBones.Add(bone);
                }
            }
        }
        return layoutBones;
    }
    private List<layoutBone> findBones(Group g)
    {
        List<layoutBone> layoutBones = new List<layoutBone>();
        foreach (List<layoutBone> Llb in layoutSkeleton)
        {
            foreach (layoutBone bone in Llb) 
            {
                if (bone.group == g)
                {
                    layoutBones.Add(bone);
                }
            }
        }
        return layoutBones;
    }
    private int getRandDirection(Vector2Int position, int exclusion)
    {
        List<int> dirs = new List<int> { 1, 2, 4, 8 };
        if ((position.x == 0) ||
            ((((layoutSkeleton[position.y][position.x - 1].boneExits >> 4) & 0b0010) != 0) &&
            ((layoutSkeleton[position.y][position.x - 1].boneExits & 0b0010) == 0)) ||
            ((((layoutSkeleton[position.y][position.x].boneExits >> 4) & 0b1000) != 0) &&
            ((layoutSkeleton[position.y][position.x].boneExits & 0b1000) == 0))) { dirs.Remove(8); }
        if (position.y == 0 ||
            ((((layoutSkeleton[position.y - 1][position.x].boneExits >> 4) & 0b0001) != 0) &&
            ((layoutSkeleton[position.y - 1][position.x].boneExits & 0b0001) == 0)) ||
            ((((layoutSkeleton[position.y][position.x].boneExits >> 4) & 0b0100) != 0) &&
            ((layoutSkeleton[position.y][position.x].boneExits & 0b0100) == 0))) { dirs.Remove(4); }
        if (position.x == columns - 1 ||
            ((((layoutSkeleton[position.y][position.x + 1].boneExits >> 4) & 0b1000) != 0) &&
            ((layoutSkeleton[position.y][position.x + 1].boneExits & 0b1000) == 0)) ||
            ((((layoutSkeleton[position.y][position.x].boneExits >> 4) & 0b0010) != 0) &&
            ((layoutSkeleton[position.y][position.x].boneExits & 0b0010) == 0))) { dirs.Remove(2); }
        if (position.y == rows - 1 ||
            ((((layoutSkeleton[position.y + 1][position.x].boneExits >> 4) & 0b0100) != 0) &&
            ((layoutSkeleton[position.y + 1][position.x].boneExits & 0b0100) == 0)) ||
            ((((layoutSkeleton[position.y][position.x].boneExits >> 4) & 0b0001) != 0) &&
            ((layoutSkeleton[position.y][position.x].boneExits & 0b0001) == 0))) { dirs.Remove(1); }
        dirs.Remove(exclusion);
        int t = exclusion;
        if (dirs.Count > 0) t = dirs[UnityEngine.Random.Range(0, dirs.Count)];
        return t;
    }


    /* These are all the differnet classes used to help organize and structure the
     * maze generation. They are public for access in the Inspector and for people
     * who want to make their own adjustments to the maze generation outside of the
     * give functions. They are also very helpful for editing a layout's settings in
     * runtime.*/

    //different types of Groups
    [System.Serializable]
    public class Group
    {
        public string Name;
        [HideInInspector]
        public Mighty_MazeGen.Types TypeOfGroup;
        //figures out if this group is of a certain type
        public bool isType(Mighty_MazeGen.Types type)
        {
            if (type != TypeOfGroup) return false;
            return true;
        }
    }
    [System.Serializable]
    public class Generic : Group
    {
        [System.Serializable]
        public struct roomList
        {
            public List<__RU> __RU;
            public List<_DR_> _DR_;
            public List<LD__> LD__;
            public List<L__U> L__U;
            public List<L_R_> L_R_;
            public List<_D_U> _D_U;
            public List<LDR_> LDR_;
            public List<LD_U> LD_U;
            public List<L_RU> L_RU;
            public List<_DRU> _DRU;
            public List<LDRU> LDRU;
        }
        public roomList Rooms;
    }
    [System.Serializable]
    public class Special : Group
    {
        public int Amount;
        [System.Serializable]
        public struct roomList
        {
            public List<__RU> __RU;
            public List<_DR_> _DR_;
            public List<LD__> LD__;
            public List<L__U> L__U;
            public List<L_R_> L_R_;
            public List<_D_U> _D_U;
            public List<LDR_> LDR_;
            public List<LD_U> LD_U;
            public List<L_RU> L_RU;
            public List<_DRU> _DRU;
            public List<LDRU> LDRU;
        }
        public roomList Rooms;
    }
    [System.Serializable]
    public class Anchor : Group
    {
        public GameObject room;
        public int Amount;
        public List<Room.Exits> exits;
        public List<Vector2Int> possibleLocations;
    }
    //all the various room types
    public class Room
    {
        public GameObject prefab;
        public enum Exits { U = 1, R = 2, D = 4, L = 8 };
    }
    [System.Serializable]
    public class __RU : Room
    {
        [HideInInspector]
        public int exits = 0b0011;
    }
    [System.Serializable]
    public class _DR_ : Room
    {
        [HideInInspector]
        public int exits = 0b0110;
    }
    [System.Serializable]
    public class LD__ : Room
    {
        [HideInInspector]
        public int exits = 0b1100;
    }
    [System.Serializable]
    public class L__U : Room
    {
        [HideInInspector]
        public int exits = 0b1001;
    }
    [System.Serializable]
    public class L_R_ : Room
    {
        [HideInInspector]
        public int exits = 0b1010;
    }
    [System.Serializable]
    public class _D_U : Room
    {
        [HideInInspector]
        public int exits = 0b0101;
    }
    [System.Serializable]
    public class LDR_ : Room
    {
        [HideInInspector]
        public int exits = 0b1110;
    }
    [System.Serializable]
    public class LD_U : Room
    {
        [HideInInspector]
        public int exits = 0b1101;
    }
    [System.Serializable]
    public class L_RU : Room
    {
        [HideInInspector]
        public int exits = 0b1011;
    }
    [System.Serializable]
    public class _DRU : Room
    {
        [HideInInspector]
        public int exits = 0b0111;
    }
    [System.Serializable]
    public class LDRU : Room
    {
        [HideInInspector]
        public int exits = 0b1111;
    }
    //bones that make up the skeleton of the layout
    public class layoutBone
    {
        public Group group;
        public GameObject roomPrefab;
        public uint boneExits = 0;
        public Vector2Int position;
        public layoutBone(int x, int y)
        {
            position.x = x; position.y = y;
        }
        public bool addExit(uint newExit)
        {
            uint possible = newExit & ~(boneExits >> 4);
            if (possible != 0)
            {
                boneExits |= possible;
                return true;
            }
            return false;
        }
        public bool removeExit(uint oldExit)
        {
            boneExits &= ~oldExit;
            return true;
        }
        //returns the rotation needed to align room
        public int setRoom()
        {
            uint unlockedBoneExits = boneExits & 0b1111;
            if (group.TypeOfGroup == Types.Anchor)
            {
                Anchor groupA = (Anchor)group;
                roomPrefab = groupA.room;
            } else if (group.TypeOfGroup == Types.Special)
            {
                Special groupS = (Special)group;
                switch (unlockedBoneExits)
                {
                    default:
                        roomPrefab = null;
                        break;
                    case 0b0011:
                        roomPrefab = groupS.Rooms.__RU[UnityEngine.Random.Range(0, groupS.Rooms.__RU.Count)].prefab;
                        break;
                    case 0b0110:
                        roomPrefab = groupS.Rooms._DR_[UnityEngine.Random.Range(0, groupS.Rooms._DR_.Count)].prefab;
                        break;
                    case 0b1100:
                        roomPrefab = groupS.Rooms.LD__[UnityEngine.Random.Range(0, groupS.Rooms.LD__.Count)].prefab;
                        break;
                    case 0b1001:
                        roomPrefab = groupS.Rooms.L__U[UnityEngine.Random.Range(0, groupS.Rooms.L__U.Count)].prefab;
                        break;
                    case 0b1010:
                        roomPrefab = groupS.Rooms.L_R_[UnityEngine.Random.Range(0, groupS.Rooms.L_R_.Count)].prefab;
                        break;
                    case 0b0101:
                        roomPrefab = groupS.Rooms._D_U[UnityEngine.Random.Range(0, groupS.Rooms._D_U.Count)].prefab;
                        break;
                    case 0b1110:
                        roomPrefab = groupS.Rooms.LDR_[UnityEngine.Random.Range(0, groupS.Rooms.LDR_.Count)].prefab;
                        break;
                    case 0b1101:
                        roomPrefab = groupS.Rooms.LD_U[UnityEngine.Random.Range(0, groupS.Rooms.LD_U.Count)].prefab;
                        break;
                    case 0b1011:
                        roomPrefab = groupS.Rooms.L_RU[UnityEngine.Random.Range(0, groupS.Rooms.L_RU.Count)].prefab;
                        break;
                    case 0b0111:
                        roomPrefab = groupS.Rooms._DRU[UnityEngine.Random.Range(0, groupS.Rooms._DRU.Count)].prefab;
                        break;
                    case 0b1111:
                        roomPrefab = groupS.Rooms.LDRU[UnityEngine.Random.Range(0, groupS.Rooms.LDRU.Count)].prefab;
                        break;
                }
            } else
            {
                Generic groupG = (Generic)group;
                switch (unlockedBoneExits)
                {
                    default:
                        roomPrefab = null;
                        break;
                    case 0b0011:
                        roomPrefab = groupG.Rooms.__RU[UnityEngine.Random.Range(0, groupG.Rooms.__RU.Count)].prefab;
                        break;
                    case 0b0110:
                        roomPrefab = groupG.Rooms._DR_[UnityEngine.Random.Range(0, groupG.Rooms._DR_.Count)].prefab;
                        break;
                    case 0b1100:
                        roomPrefab = groupG.Rooms.LD__[UnityEngine.Random.Range(0, groupG.Rooms.LD__.Count)].prefab;
                        break;
                    case 0b1001:
                        roomPrefab = groupG.Rooms.L__U[UnityEngine.Random.Range(0, groupG.Rooms.L__U.Count)].prefab;
                        break;
                    case 0b1010:
                        roomPrefab = groupG.Rooms.L_R_[UnityEngine.Random.Range(0, groupG.Rooms.L_R_.Count)].prefab;
                        break;
                    case 0b0101:
                        roomPrefab = groupG.Rooms._D_U[UnityEngine.Random.Range(0, groupG.Rooms._D_U.Count)].prefab;
                        break;
                    case 0b1110:
                        roomPrefab = groupG.Rooms.LDR_[UnityEngine.Random.Range(0, groupG.Rooms.LDR_.Count)].prefab;
                        break;
                    case 0b1101:
                        roomPrefab = groupG.Rooms.LD_U[UnityEngine.Random.Range(0, groupG.Rooms.LD_U.Count)].prefab;
                        break;
                    case 0b1011:
                        roomPrefab = groupG.Rooms.L_RU[UnityEngine.Random.Range(0, groupG.Rooms.L_RU.Count)].prefab;
                        break;
                    case 0b0111:
                        roomPrefab = groupG.Rooms._DRU[UnityEngine.Random.Range(0, groupG.Rooms._DRU.Count)].prefab;
                        break;
                    case 0b1111:
                        roomPrefab = groupG.Rooms.LDRU[UnityEngine.Random.Range(0, groupG.Rooms.LDRU.Count)].prefab;
                        break;
                }
            }
            return (int)unlockedBoneExits;
        }
    }
}
