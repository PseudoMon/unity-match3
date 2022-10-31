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

        RaycastHit2D hitInfo = Physics2D.Raycast(
            Camera.main.ScreenToWorldPoint(Input.mousePosition), 
            Vector2.zero
        );

        if (Input.GetButtonDown("Fire2"))
        {
            DestroyBlockAtMousePos();
        }

        if (Input.GetButtonDown("Fire1"))
        {
            SelectBlock();
        }        

        // Check for empty grid slots and set those above it to fall
        gridSlotMachine.CheckForFallers();
    }

    void SelectBlock()
    {
        RaycastHit2D hitInfo = Physics2D.Raycast(
            Camera.main.ScreenToWorldPoint(Input.mousePosition), 
            Vector2.zero
        );

        if (hitInfo.collider)
        {
            var clickedObject = hitInfo.collider.gameObject;
            var blockBehavior = clickedObject.GetComponent<BlockBehavior>();
            blockBehavior.SelectBlock();

            if (selectedBlock && selectedBlock != blockBehavior)
            {
                selectedBlock.UnselectBlock();
            }

            selectedBlock = blockBehavior;
        }
    }

    // void SwapBlock(BlockBehavior block1, BlockBehavior block2)
    // {

    // }



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

public class BlockBehavior : MonoBehaviour
{
    private Grid grid;
    private Rigidbody2D rb;
    private Collider2D thisCollider;

    private GridSlot currentSlot;

    private bool isHovered;
    private GameObject hoverCursor;
    private bool isSelected;
    private GameObject selectCursor;

    public GameObject hovererPrefab;
    public GameObject selectorPrefab;

    private bool isMovingKinematically;

    void Awake()
    {
        grid = transform.parent.GetComponent<Grid>();
        rb = transform.GetComponent<Rigidbody2D>();
        thisCollider = GetComponent<Collider2D>();
    }

    void Update()
    {
        if (rb.isKinematic) 
        {
            // is not affected by gravity i.e. not falling and therefore can be moved
            var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            bool mouseIsOverBlock = thisCollider.OverlapPoint(mousePos);
            if (mouseIsOverBlock && !isHovered)
            {
                HoverBlock();
            }
            if (!mouseIsOverBlock && isHovered)
            {
                UnhoverBlock();
            }
        }
    }

    // void FixedUpdate()
    // {
    //     if (isMovingKinematically)
    //     {
    //         // TODO
    //         // check what direction the target is moving in
    //         //rb.MovePosition(rb.position + 1 * Time.deltaTime);
    //     }

    //     if (isMovingKinematically && stopPositionFound)
    //     {
    //         StopAtMovementPosition();
    //     }
    // }

    public void SelectBlock()
    {
        if (isSelected) return;
        selectCursor = Instantiate(selectorPrefab, transform);
        isSelected = true;
    }

    public void UnselectBlock()
    {
        if (!isSelected) return;
        Destroy(selectCursor);
        isSelected = false;
    }

    void HoverBlock()
    {
        hoverCursor = Instantiate(hovererPrefab, transform);
        isHovered = true;
    }

    void UnhoverBlock()
    {
        Destroy(hoverCursor); 
        isHovered = false;
    }

    public void DestroyBlock()
    {
        currentSlot.Clear();
        Destroy(gameObject);
    }

    public void StartFallingTo(GridSlot targetSlot)
    {
        // Set where to fall towards and disable isKinematic
        // so this block is affectecd by gravity

        currentSlot = targetSlot;
        rb.isKinematic = false;
        var stopPosition = grid.GetCellCenterWorld(targetSlot.GetVector3Int()); 
        
        IEnumerator StopAtFallPosition()
        {
            while (Vector2.Distance(rb.position, stopPosition) > 0.1)
            {
                yield return null;
            }

            // When we're veeery close to the target position 
            // (difference <= 0.1), stop falling, and just snap to position
            rb.isKinematic = true;
            rb.velocity = Vector2.zero;
            rb.MovePosition(stopPosition);
            yield break;
        }

        StartCoroutine(StopAtFallPosition());
    }

    public void MoveTowards(GridSlot targetSlot)
    {
        // Move towards the specific target slot
        // MASSIVE TODO
        // currentSlot = targetSlot;
        // stopPosition = grid.GetCellCenterWorld(targetSlot.GetVector3Int());
        // isMovingKinematically = true;
    }

    // void StopAtMovementPosition()
    // {
    //     if (Vector2.Distance(rb.position, stopPosition) <= 0.1)
    //     {
    //         isMovingKinematically = false;
    //         rb.velocity = Vector2.zero;
    //         rb.MovePosition(stopPosition); // snap to position
    //     } 
    // }
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