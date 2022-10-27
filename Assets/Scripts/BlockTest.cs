using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* This block will fall to the designated grid (in this case, 0, -5)
   and then snap to position
*/

public class BlockTest : MonoBehaviour
{
    Grid grid;
    Rigidbody2D rb;

    // Start is called before the first frame update
    void Start()
    {
        grid = transform.parent.GetComponent<Grid>();
        rb = GetComponent<Rigidbody2D>();

        transform.localPosition =  grid.GetCellCenterLocal(Vector3Int.up * 4);
    }

    // Update is called once per frame
    void Update()
    {
        float bottomCellPos = grid.GetCellCenterLocal(Vector3Int.down * 5).y;
        if (Vector2.Distance(rb.position, grid.GetCellCenterLocal(Vector3Int.down * 5)) <= 0.1)
        {
            rb.isKinematic = true;
            rb.velocity = Vector2.zero;

            Vector3Int cellPosition = grid.LocalToCell(transform.localPosition);
            rb.MovePosition(grid.GetCellCenterLocal(cellPosition));
        }
    }
}
