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

    public bool markedForDeletion { get; private set; }

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

    public void StraightUpReplace(GameObject newObject)
    {
        // Destroy the object inside and replace it with
        // another object with identical position and speed
        // and sets its fall position to this slot

        newObject.transform.position = objectInside.transform.position;
        newObject.GetComponent<Rigidbody2D>().velocity = objectInside.GetComponent<Rigidbody2D>().velocity;

        DestroyObjectInside();
        MakeObjectElsewhereFallHere(newObject);
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
            if (slot.markedForDeletion) continue;

            var slotBelow = GetSlotAtPosition(slot.x, slot.y - 1);
            if (slotBelow.isFilled) continue;
            
            slot.MakeObjectInsideFallTo(slotBelow);
        }
    }

    public void CheckForScorers(bool initMode = false)
    {
        // Only check for scorers when no block is moving
        if (!allSlotsAreStill && !initMode) return;

        // Check if the slot's object has the same color as provided
        bool HasSameColor(GridSlot nextSlot, string thisColor)
        {
            if (!nextSlot.isFilled || nextSlot.markedForDeletion)
                return false;

            string nextColor = 
                nextSlot.objectInside.GetComponent<BlockInfo>().color;

            if (nextColor != thisColor)
            {
                return false;
            }

            return true;
        }

        // In all provided slots, check if they have the same color
        // Add to the list if they do.
        // If they don't, break loop i.e. don't bother looking at the rest
        void CheckForSameColorInAllThese(
            string thisColor,
            List<GridSlot> slotsToAdd,
            IOrderedEnumerable<GridSlot> slotsToCheck
        ) {
            foreach (var slotToCheck in slotsToCheck)
            {
                if (!HasSameColor(slotToCheck, thisColor)) break;
                slotsToAdd.Add(slotToCheck);
            }
        }

        foreach (GridSlot thisSlot in slots)
        {
            if (!thisSlot.isFilled) continue;
            if (thisSlot.markedForDeletion) continue;
            // Note that blocks already marked for deletion i.e.

            string thisColor = 
                thisSlot.objectInside.GetComponent<BlockInfo>().color;

            List<GridSlot> slotsWithThisColor = new List<GridSlot>();
            slotsWithThisColor.Add(thisSlot);

            // Check for all slots on the right
            CheckForSameColorInAllThese(
                thisColor,
                slotsWithThisColor,
                slots.Where(
                    slot => slot.y == thisSlot.y && slot.x > thisSlot.x
                ).OrderBy(slot => slot.x)
            );

            if (slotsWithThisColor.Count >= 3)
            {
                foreach (var slot in slotsWithThisColor)
                {
                    slot.MarkForDeletion();
                }
            }

            slotsWithThisColor.Clear();
            slotsWithThisColor.Add(thisSlot);

            // Check for all slots above
            CheckForSameColorInAllThese(
                thisColor,
                slotsWithThisColor,
                slots.Where(
                    slot => slot.x == thisSlot.x && slot.y > thisSlot.y
                ).OrderBy(slot => slot.y)
            );

            if (slotsWithThisColor.Count >= 3)
            {
                foreach (var slot in slotsWithThisColor)
                {
                    slot.MarkForDeletion();
                }
            }
        }
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