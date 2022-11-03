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

    public GameObject gameplayUI;
    private GameplayUI UIComponent;
    
    // Start is called before the first frame update
    void Start()
    {
        UIComponent = gameplayUI.GetComponent<GameplayUI>();
        grid = GetComponent<Grid>();
        gridSlotMachine = new GridSlotMachine(10, 10, -5, -5, UIComponent);

        for (int y = 4; y < 4 + 10; y++)
        {
            SpawnRow();
        }

        do {
            ReplaceDeletedBlockWithAnother();
            gridSlotMachine.CheckForScorers(true);
        } while (gridSlotMachine.slots.Exists(slot => slot.markedForDeletion == true));
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
    }

    GameObject SpawnBlock()
    {
        // Spawning without any specified slot or position
        // Will return the spawned block
        GameObject blockPrefab = blockPrefabs[Random.Range(0, blockPrefabs.Length)];
        GameObject newBlock = Instantiate(blockPrefab, transform);

        var newBlockBehavior = newBlock.AddComponent<BlockBehavior>();
        newBlockBehavior.hovererPrefab = hovererPrefab;
        newBlockBehavior.selectorPrefab = selectorPrefab;

        return newBlock;
    }

    void ReplaceDeletedBlockWithAnother()
    {
        // Should only be used on initialiation (e.g. removing
        // matches that happen before the game even start).
        // For all slots marked for deletion,
        // quickly replace its content with a new block, spawned
        // at the same position, with the same velocity.
        // Doing this should unmark it for deletion too.

        List<GridSlot> slotsToDelete = gridSlotMachine.slots.FindAll(
            slot => slot.markedForDeletion == true
        );

        foreach (GridSlot slot in slotsToDelete)
        {
            var newBlock = SpawnBlock();
            slot.StraightUpReplace(newBlock);
        }
    }

    void Update()
    {
        ResetOnR();

        if (gridSlotMachine.allSlotsAreStill)
        {
            // Interaction is only possible when no movement is happening
            // There's no technical problem with it, it's just
            // better user experience.
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
         
        // Check for 3 colour matches in a row/column
        if (!(gridSlotMachine.slots.Exists(slot => slot.markedForDeletion == true)))
        {
            gridSlotMachine.CheckForScorers();
        }

        // Check for slots marked for deletions and destroy their content
        gridSlotMachine.CheckForDeletion();

        // If there's an empty slot on the top row, spawn a new block there
        List<GridSlot> emptyTopSlots = gridSlotMachine.FindEmptySlotsAtTop();
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

            if (!clickedBlock) return; // not a block!

            if (BlockIsNextToSelected(clickedBlock))
            {
                // If there's a previously selected block
                // Swap their position
                selectedBlock.UnselectBlock();
                selectedBlock.currentSlot.SwapObjectInsideWithObjectInSlot(clickedBlock.currentSlot);
                selectedBlock = null;
                UIComponent.ReduceScore();
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
        numberOfSlots = gridSlotMachine.slots?.Count ?? 0;
    }

    public void OnAfterDeserialize() {}
}