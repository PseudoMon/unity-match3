using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

        Debug.Log(slots.FindAll(slot => slot.markedForDeletion == true).Count);
        Debug.Log("NAUR");
    }

    public void CheckForScorersOneLine(int staticAxis, string rowOrColumn)
    {
        if (rowOrColumn != "row" && rowOrColumn != "column")
        {
            throw new System.ArgumentException(
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