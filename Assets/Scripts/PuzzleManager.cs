using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PuzzleManager : MonoBehaviour, ISerializationCallbackReceiver
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
            gridSlotMachine.CheckForScorers();
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

    // The following are for informational display on the editor

    [SerializeField] int numberOfSlots;

    public void OnBeforeSerialize() 
    {
        if (gridSlotMachine == null) return;
        numberOfSlots = gridSlotMachine.slots.Count;
    }

    public void OnAfterDeserialize() {}
}