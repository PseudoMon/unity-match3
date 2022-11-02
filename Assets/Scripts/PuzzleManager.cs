using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PuzzleManager : MonoBehaviour
{
    public GameObject[] blockPrefabs;
    public GameObject hovererPrefab;
    public GameObject selectorPrefab;

    private Grid grid;
    private GridSlotMachine gridSlotMachine;

    private BlockBehavior selectedBlock;
    
    // Start is called before the first frame update
    void Start()
    {
        grid = GetComponent<Grid>();
        gridSlotMachine = new GridSlotMachine(10, 10, -5, -5);

        for (int y = 4; y < 4 + 10; y++)
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

        var newBlockBehavior = newBlock.AddComponent<BlockBehavior>();
        newBlockBehavior.hovererPrefab = hovererPrefab;
        newBlockBehavior.selectorPrefab = selectorPrefab;

        targetGridSlot.MakeObjectElsewhereFallHere(newBlock);

        newBlock.GetComponent<Rigidbody2D>().WakeUp();
    }

    void Update()
    {
        ResetOnR();

        bool allSlotsAreStill =
            gridSlotMachine.slots.TrueForAll(slot => 
                slot.objectInside == null || slot.objectInside.GetComponent<Rigidbody2D>().isKinematic
            );

        // if (allSlotsAreStill)
        // {
            // Interaction is only possible when no movement is happening
            if (Input.GetButtonDown("Fire2"))
            {
                DestroyBlockAtMousePos();
            }

            if (Input.GetButtonDown("Fire1"))
            {
                SelectBlockIfClicked();
            }       
        // }
         
        if (!(gridSlotMachine.slots.Exists(slot => slot.markedForDeletion == true)))
        {
            // gridSlotMachine.CheckForScorers();
        }

        gridSlotMachine.CheckForDeletion();

        // Check for empty grid slots and set those above it to fall
        gridSlotMachine.CheckForFallers();

        // If there's an empty slot on the top row, spawn a new block there
        List<GridSlot> emptyTopSlots = gridSlotMachine.FindEmptySlotsAtTop();
        Debug.Log($"WEE {emptyTopSlots.Count}");
        foreach (var slot in emptyTopSlots)
        {
            SpawnBlock(
                slot, 
                slot.coordinate + new Vector3Int(0,4,0)
            );
        }
    }

    void SelectBlockIfClicked()
    {
        RaycastHit2D hitInfo = Physics2D.Raycast(
            Camera.main.ScreenToWorldPoint(Input.mousePosition), 
            Vector2.zero
        );

        if (hitInfo.collider)
        {
            var clickedObject = hitInfo.collider.gameObject;
            var clickedBlock = 
                clickedObject.GetComponent<BlockBehavior>();

            if (BlockIsNextToSelected(clickedBlock))
            {
                // If there's a previously selected block
                // Swap their position
                selectedBlock.UnselectBlock();
                selectedBlock.currentSlot.SwapObjectInsideWithObjectInSlot(clickedBlock.currentSlot);
                selectedBlock = null;
            }
            else {
                clickedBlock.SelectBlock();
                if (selectedBlock) selectedBlock.UnselectBlock();
                selectedBlock = clickedBlock;
            }
        }
    }

    bool BlockIsNextToSelected(BlockBehavior clickedBlock)
    {
        return selectedBlock &&
            selectedBlock != clickedBlock &&
            selectedBlock.currentSlot.IsAdjacentWith(
                clickedBlock.currentSlot
            );
    }

    // Debug function
    void ResetOnR()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    // Debug function
    void DestroyBlockAtMousePos()
    {
        var worldMousePosition = 
            Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hitInfo = Physics2D.Raycast(
            worldMousePosition, 
            Vector2.zero
        );
        if (hitInfo.collider)
        {
            var clickedObject = hitInfo.collider.gameObject;
            Debug.Log($"HIT SOMETHING {clickedObject}", clickedObject);
            //clickedObject.GetComponent<BlockBehavior>().DestroyBlock();

            var augh = grid.WorldToCell(worldMousePosition);
            gridSlotMachine.GetSlotAtPosition(
                augh.x, augh.y
            ).MarkForDeletion();
        }
    }
}

[System.Serializable]
public class GridSlot
{
    public GridSlot(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public int x { get; private set; }
    public int y { get; private set; }
    public Vector3Int coordinate { 
        get
        {
            return new Vector3Int(x, y, 0);
        }
    }

    private GameObject _objectInside;
    public GameObject objectInside { 
        get { return _objectInside; } 
        private set { _objectInside = value; }
    }

    public bool isFilled {
        get { return _objectInside != null; }
    }

    public bool markedForDeletion { get; set; }

    // Note:
    // At the moment, gridslot can contain an object whose .currentSlot
    // doesn't correspond with the gridslot. We should probably find a way
    // to make it have one consistent source of truth.

    public Vector3Int GetVector3Int() {
        return new Vector3Int(x, y, 0);
    }

    public void FillWith(GameObject thing)
    {
        objectInside = thing;
    }

    public void Clear()
    {
        objectInside = null;
    }

    public bool IsAdjacentWith(GridSlot otherSlot)
    {
        return Vector3Int.Distance(
                coordinate, 
                otherSlot.GetVector3Int()
            ) == 1;
    }

    public void SwapObjectInsideWithObjectInSlot(GridSlot targetSlot)
    {
        var targetSlotObject = targetSlot.objectInside;
        objectInside.GetComponent<BlockBehavior>().MoveTowards(targetSlot);
        targetSlotObject.GetComponent<BlockBehavior>().MoveTowards(this);

        targetSlot.objectInside = objectInside;
        objectInside = targetSlotObject;  
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

    public void DestroyObjectInside()
    {
        GameObject.Destroy(objectInside);
        Clear();
        markedForDeletion = false;
    }

    public void MarkForDeletion()
    {
        markedForDeletion = true;
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

    public bool allSlotsAreStill {
        get
        {
            return slots.TrueForAll(slot => 
                slot.objectInside == null || slot.objectInside.GetComponent<Rigidbody2D>().isKinematic
            );
        }
    }

    public void CheckForFallers()
    {
        // Check every slot aside from those 
        // at the bottom row and those that are empty.
        // Then check the slot below it. If the slot below is empty,
        // make the object inside this slot fall.
        foreach (GridSlot slot in slots)
        {
            if (slot.y == bottomY) continue;
            if (!slot.isFilled) continue;

            var slotBelow = GetSlotAtPosition(slot.x, slot.y - 1);
            if (slotBelow.isFilled) continue;
            
            Debug.Log($"{slotBelow.x} and {slotBelow.y}");
            slot.MakeObjectInsideFallTo(slotBelow);
        }
    }

    public void CheckForScorers()
    {
        // Only check for scorers when no block is moving
        if (!allSlotsAreStill) return;

        for (int y = bottomY; y <= topY; y++)
        {
            CheckForScorersOneLine(y, "row");
        }

        for (int x = leftmostX; x <= rightmostX; x++)
        {
            CheckForScorersOneLine(x, "column");
        }

        Debug.Log("NAUR");
    }

    public void CheckForScorersOneLine(int staticAxis, string rowOrColumn)
    {
        if (rowOrColumn != "row" && rowOrColumn != "column")
        {
            throw new ArgumentException(
                "Parameter rowOrColumn can only be either row or column!"
            );
        }

        int mobileAxis = rowOrColumn == "row" ? leftmostX : bottomY;
        int axisEnd = rowOrColumn == "row" ? rightmostX : topY;

        while (mobileAxis <= axisEnd)
        {
            GridSlot thisSlot = rowOrColumn == "row" ? 
                GetSlotAtPosition(mobileAxis, staticAxis) :
                GetSlotAtPosition(staticAxis, mobileAxis);

            GameObject blockAtThisSlot = thisSlot.objectInside;
            if (blockAtThisSlot == null)
            {
                mobileAxis += 1;
                continue;
            }

            string thisColor = 
                blockAtThisSlot.GetComponent<BlockInfo>().color;
            
            List<GridSlot> slotsWithThisColor = new List<GridSlot>();
            slotsWithThisColor.Add(thisSlot);

            int nextAxis = mobileAxis + 1;

            while (nextAxis <= axisEnd)
            {
                GridSlot nextSlot = rowOrColumn == "row" ? 
                    GetSlotAtPosition(nextAxis, staticAxis) :
                    GetSlotAtPosition(staticAxis, nextAxis);
                GameObject blockAtNextSlot = nextSlot.objectInside;
                
                if (blockAtNextSlot == null)
                {
                    nextAxis += 1;
                    break;
                }

                if (blockAtNextSlot.GetComponent<BlockInfo>().color == thisColor)
                {
                    slotsWithThisColor.Add(nextSlot);
                    nextAxis += 1;
                    continue;
                }

                break;
            }

            mobileAxis = nextAxis;
            if (slotsWithThisColor.Count >= 3)
            {
                //Debug.Log("SCORE");
                foreach (GridSlot slot in slotsWithThisColor)
                {
                    // TODO SOME BULLSHIT HERE
                    // 1. Add to score
                    // 2. Mark slots for deletion
                    //Debug.Log(slot.coordinate, slot.objectInside);

                    slot.MarkForDeletion();
                }
            }
        }

        Debug.Log($"SCORER CHECK DONE FOR {rowOrColumn} {staticAxis}");
    }

    public void CheckForDeletion()
    {
        // Only delete *one* slot a frame, to limit things fucking up
        GridSlot slotToDelete = slots.Find(
            slot => slot.markedForDeletion == true
        );

        // No slot marked for deletion? Slot is empty?
        if (slotToDelete == null || !slotToDelete.isFilled) return;
        
        Debug.Log(slotToDelete.objectInside, slotToDelete.objectInside);
        slotToDelete.DestroyObjectInside();
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

    public List<GridSlot> FindEmptySlotsAtTop()
    {
        return slots.FindAll(slot => slot.y == topY && !slot.isFilled);
    }
}