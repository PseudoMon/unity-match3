using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PuzzleManager : MonoBehaviour
{
    public GameObject[] blockPrefabs;
    private Grid grid;
    private GridSlotMachine gridSlotMachine;
    
    // Start is called before the first frame update
    void Start()
    {
        grid = GetComponent<Grid>();
        gridSlotMachine = new GridSlotMachine(10, 10, -5, -5);

        for (int y = 8; y < 18; y++)
        {
            SpawnRow();
        }
    }

    void SpawnRow()
    {
        // Spawn a whole row of blocks with randomized colours
        for (int x = -5; x < 5; x++)
        {
            GridSlot slotToFill = gridSlotMachine.GetBottommostEmptySlot(x);
            SpawnBlock(
                slotToFill, 
                slotToFill.GetVector3Int() + new Vector3Int(0, 10, 0)
            );
        }
    }

    void SpawnBlock(GridSlot targetGridSlot, Vector3Int spawnOrigin)
    {
        GameObject blockPrefab = blockPrefabs[Random.Range(0, blockPrefabs.Length)];
        // transform here is the Grid's transform, set as new block's parent
        GameObject newBlock = Instantiate(blockPrefab, transform);

        Vector3 spawnOriginPos = grid.GetCellCenterLocal(spawnOrigin);
        newBlock.transform.localPosition = spawnOriginPos;

        newBlock.AddComponent<BlockBehavior>();
        targetGridSlot.MakeObjectElsewhereFallHere(newBlock);

        newBlock.GetComponent<Rigidbody2D>().WakeUp();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        gridSlotMachine.CheckForFallers();
    }
}

public class BlockBehavior : MonoBehaviour
{
    private Grid grid;
    private Rigidbody2D rb;

    private GridSlot currentSlot;
    private Vector3 stopPosition;
    private bool stopPositionFound;

    void Awake()
    {
        grid = transform.parent.GetComponent<Grid>();
        rb = transform.GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (!rb.isKinematic) {
            // If the block is still affected by gravity i.e. falling

            if (stopPositionFound)
            {
                StopAtFallPosition();
            } 
        }
    }

    void OnMouseDown()
    {
        currentSlot.Clear();
        Destroy(gameObject);
    }

    void StopAtFallPosition()
    {
        if (Vector2.Distance(rb.position, stopPosition) <= 0.1)
        {
            rb.isKinematic = true;
            rb.velocity = Vector2.zero;
            rb.MovePosition(stopPosition);
        }
    }

    public void StartFallingTo(GridSlot targetSlot)
    {
        currentSlot = targetSlot;
        stopPositionFound = true;
        stopPosition = grid.GetCellCenterWorld(targetSlot.GetVector3Int()); 
        rb.isKinematic = false;
    }
}

[System.Serializable]
public class GridSlot
{
    public GridSlot(int x, int y)
    {
        this.x = x;
        this.y = y;
        isFilled = false;
    }

    public int x { get; private set; }
    public int y { get; private set; }

    public bool isFilled { get; private set; }
    private GameObject _objectInside;
    public GameObject objectInside { 
        get 
        { 
            if (isFilled) return _objectInside;
            else return null; 
        } 

        private set { _objectInside = value; }
    }

    public Vector3Int GetVector3Int() {
        return new Vector3Int(x, y, 0);
    }

    public void FillWith(GameObject thing)
    {
        isFilled = true;
        objectInside = thing;
    }

    public void Clear()
    {
        isFilled = false;
    }

    public void MakeObjectElsewhereFallHere(GameObject thing)
    {
        FillWith(thing);
        var blockBehavior = thing.GetComponent<BlockBehavior>();
        blockBehavior.StartFallingTo(this);
    }

    public void MakeObjectInsideFallTo(GridSlot targetSlot)
    {

        if (!isFilled || targetSlot.isFilled) return;
        
        targetSlot.MakeObjectElsewhereFallHere(objectInside);
        Clear();
    }
}

[System.Serializable]
public class GridSlotMachine
{
    public GridSlotMachine(int xlength, int ylength, int xstart, int ystart)
    {
        slots = new List<GridSlot>();
        leftmostX = xstart;
        bottomY = ystart;
        rightmostX = xstart + xlength - 1;
        topY = ystart + ylength - 1;

        for (int x = xstart; x < xstart + xlength; x++)
        {
            for (int y = ystart; y < ystart + ylength; y++)
            {
                slots.Add(new GridSlot(x, y));
            }
        }
    }

    public List<GridSlot> slots { get; private set; }
    public int leftmostX { get; set; }
    public int bottomY { get; set; }
    public int rightmostX { get; set; }
    public int topY { get; set; }

    public void CheckForFallers()
    {
        var emptySlots = slots.Where(slot => !slot.isFilled);

        foreach (GridSlot thisEmptySlot in emptySlots)
        {
            if (thisEmptySlot.y == topY) continue;
            var slotAbove = slots.Single(slot => slot.x == thisEmptySlot.x && slot.y == thisEmptySlot.y + 1);
            slotAbove.MakeObjectInsideFallTo(thisEmptySlot);
        }
    }

    public GridSlot GetBottommostEmptySlot(int x)
    {
        var emptySlotsAtX = slots
            .Where(slot => slot.x == x)
            .Where(slot => !slot.isFilled)
            .OrderBy(slot => slot.y);

        return emptySlotsAtX.Last();
    }

    public GridSlot GetSlotAtPosition(int x, int y)
    {
        return slots.Find(slot => slot.x == x && slot.y == y);
    }
}