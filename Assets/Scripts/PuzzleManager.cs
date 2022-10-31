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

        if (allSlotsAreStill)
        {
            // Interaction is only possible when no movement is happening
            if (Input.GetButtonDown("Fire2"))
            {
                DestroyBlockAtMousePos();
            }

            if (Input.GetButtonDown("Fire1"))
            {
                SelectBlockIfClicked();
            }       
        }
         

        // Check for empty grid slots and set those above it to fall
        gridSlotMachine.CheckForFallers();

        gridSlotMachine.CheckForScorers();
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
        RaycastHit2D hitInfo = Physics2D.Raycast(
            Camera.main.ScreenToWorldPoint(Input.mousePosition), 
            Vector2.zero
        );
        if (hitInfo.collider)
        {
            var clickedObject = hitInfo.collider.gameObject;
            Debug.Log($"HIT SOMETHING {clickedObject}", clickedObject);
            clickedObject.GetComponent<BlockBehavior>().DestroyBlock();
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
        isFilled = false;
    }

    public int x { get; private set; }
    public int y { get; private set; }
    public Vector3Int coordinate { 
        get
        {
            return new Vector3Int(x, y, 0);
        }
    }

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

    // Note:
    // At the moment, gridslot can contain an object whose .currentSlot
    // doesn't correspond with the gridslot. We should probably find a way
    // to make it have one consistent source of truth.

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
            if (!slotBelow.isFilled)
            {
                slot.MakeObjectInsideFallTo(slotBelow);
            }
        }
    }

    public void CheckForScorers()
    {
        // Only check for scorers when no block is moving
        if (!allSlotsAreStill) return;

        for (int y = bottomY; y <= topY; y++)
        {
            CheckForScorersAtRow(y);
        }

        for (int x = leftmostX; x <= rightmostX; x++)
        {
            CheckForScorersAtColumn(x);
        }
    }

    public void CheckForScorersAtRow(int thisRow)
    {
        // TODO: This and the column version are almost identical
        // just make them one function please

        int x = leftmostX;
        while (x <= rightmostX)
        {
            GridSlot thisSlot = GetSlotAtPosition(x, thisRow);
            GameObject blockAtThisSlot = thisSlot.objectInside;
            if (blockAtThisSlot == null)
            {
                x += 1;
                continue;
            }
            
            string colorToCheck = 
                blockAtThisSlot.GetComponent<BlockInfo>().color;
            
            List<GridSlot> slotsWithThisColor = new List<GridSlot>();
            slotsWithThisColor.Add(thisSlot);

            int xAtRight = x + 1;

            while (xAtRight <= rightmostX)
            {
                GridSlot nextSlot = GetSlotAtPosition(xAtRight, thisRow);
                GameObject blockAtNextSlot = nextSlot.objectInside;
                if (blockAtNextSlot == null)
                {
                    xAtRight += 1;
                    break;
                }

                Debug.Log(colorToCheck);
                Debug.Log(blockAtNextSlot.GetComponent<BlockInfo>().color, blockAtThisSlot);

                if (blockAtNextSlot.GetComponent<BlockInfo>().color == colorToCheck)
                {
                    slotsWithThisColor.Add(nextSlot);
                    xAtRight += 1;
                    continue;
                }

                break;
            }   

            x = xAtRight;
            if (slotsWithThisColor.Count >= 3)
            {
                Debug.Log("SCORE");
                foreach (GridSlot slot in slotsWithThisColor)
                {
                    // TODO SOME BULLSHIT HERE
                    Debug.Log(slot.coordinate, slot.objectInside);
                    slot.objectInside.GetComponent<BlockBehavior>().DestroyBlock();
                }
            }

        }

        Debug.Log($"DONE FOR ROW {thisRow}");
    }

    public void CheckForScorersAtColumn(int thisColumn)
    {
        int y = bottomY;
        while (y <= topY)
        {
            GridSlot thisSlot = GetSlotAtPosition(thisColumn, y);
            GameObject blockAtThisSlot = thisSlot.objectInside;
            if (blockAtThisSlot == null)
            {
                y += 1;
                continue;
            }
            
            string colorToCheck = 
                blockAtThisSlot.GetComponent<BlockInfo>().color;
            
            List<GridSlot> slotsWithThisColor = new List<GridSlot>();
            slotsWithThisColor.Add(thisSlot);

            int yAbove = y + 1;

            while (yAbove <= topY)
            {
                GridSlot nextSlot = GetSlotAtPosition(thisColumn, yAbove);
                GameObject blockAtNextSlot = nextSlot.objectInside;
                if (blockAtNextSlot == null)
                {
                    yAbove += 1;
                    break;
                }

                if (blockAtNextSlot.GetComponent<BlockInfo>().color == colorToCheck)
                {
                    slotsWithThisColor.Add(nextSlot);
                    yAbove += 1;
                    continue;
                }

                break;
            }   

            y = yAbove;
            if (slotsWithThisColor.Count >= 3)
            {
                Debug.Log("SCORE");
                foreach (GridSlot slot in slotsWithThisColor)
                {
                    Debug.Log(slot.coordinate, slot.objectInside);
                    // TODO SOME BULLSHIT HERE
                    slot.objectInside.GetComponent<BlockBehavior>().DestroyBlock();
                }
            }
        }

        Debug.Log($"DONE FOR COLUMN {thisColumn}");
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