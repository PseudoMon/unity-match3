// Just keeping this working code for safekeeping in case I fuck up 
// Yeah yeah I should just use version control at this point

/*

public class PuzzleManager : MonoBehaviour
{
    public GameObject[] blockPrefabs;
    private Grid grid;
    
    // Grid goes from -5 to 5

    public List<Vector3Int> bottomGrids = new List<Vector3Int>();

    // Start is called before the first frame update
    void Start()
    {
        grid = GetComponent<Grid>();
        SetBottomGrids();


        for (int y = 8; y < 18; y++)
        {
            SpawnRow(y);
        }
    }

    void SpawnRow(int y)
    {
        // Spawn a whole row of blocks with randomized colours
        for (int x = -5; x < 5; x++)
        {
            GameObject blockPrefab = blockPrefabs[Random.Range(0, blockPrefabs.Length)];
            GameObject newBlock = Instantiate(blockPrefab, transform);

            Vector3Int gridOrigin = new Vector3Int(x, y, 0);
            Vector3 gridOriginPos = grid.GetCellCenterLocal(gridOrigin);
            Debug.Log(gridOriginPos);

            newBlock.AddComponent<BlockBehavior>();
            newBlock.GetComponent<BlockBehavior>().bottomGrids = bottomGrids;
            
            newBlock.transform.localPosition = gridOriginPos;
            newBlock.GetComponent<Rigidbody2D>().WakeUp();
        }
    }

    void SetBottomGrids()   
    {
        for (int x = -5; x < 5; x++)
        {
            bottomGrids.Add(new Vector3Int(x, -5, 0));
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}

public class BlockBehavior : MonoBehaviour
{
    private Grid grid;
    private Rigidbody2D rb;
    
    public List<Vector3Int> bottomGrids;

    private Vector3 stopPosition;
    private bool stopPositionFound;

    void Start()
    {
        grid = transform.parent.GetComponent<Grid>();
        rb = transform.GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (!rb.isKinematic) {
            // If the block is still affected by gravity i.e. falling
            // Fall to place
            if (!stopPositionFound)
            {
                CheckIfFallPosition();
            }
            
            if (stopPositionFound)
            {
                StopAtFallPosition();
            } 
        }

    }

    void CheckIfFallPosition()
    {
        // Check if the cell we're at is where we're supposed to stop
        Vector3Int currentCell = grid.LocalToCell(transform.localPosition);
        int stopCellId = bottomGrids.FindIndex(
            cell => Vector3Int.Equals(currentCell, cell)
        );
        bool stopHere = stopCellId != -1;

        if (stopHere)
        {
            stopPosition = grid.GetCellCenterWorld(currentCell);
            bottomGrids[stopCellId] += Vector3Int.up;
            stopPositionFound = true;
        }
    }

    void StopAtFallPosition()
    {
        if (Vector2.Distance(rb.position, stopPosition) <= 1)
        {
            rb.isKinematic = true;
            rb.velocity = Vector2.zero;
            rb.MovePosition(stopPosition);
        }
    }
}
*/