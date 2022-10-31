using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockBehavior : MonoBehaviour
{
    private Grid grid;
    private Rigidbody2D rb;
    private Collider2D thisCollider;

    private bool isHovered;
    private GameObject hoverCursor;
    private bool isSelected;
    private GameObject selectCursor;

    public GameObject hovererPrefab;
    public GameObject selectorPrefab;

    private bool isMovingKinematically;

    public GridSlot currentSlot { get; private set; }

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
            // is not moving and therefore can be selected
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

        Vector2 currentPos = rb.position;
        Vector2 stopPosition = (Vector2)grid.GetCellCenterWorld(targetSlot.GetVector3Int());
        Vector2 movementDirection = 
            (stopPosition - currentPos).normalized;

        gameObject.layer = LayerMask.NameToLayer("Free Movement");
        rb.isKinematic = false;
        rb.gravityScale = 0f;
        rb.AddForce(movementDirection * 500);

        currentSlot = targetSlot; 
        
        IEnumerator StopAtTargetPos()
        {
            while (Vector2.Distance(rb.position, stopPosition) > 0.1)
            {                
                yield return null;
            }

            // When we're veeery close to the target position 
            // (difference <= 0.1), stop moving and just snap to position
            rb.isKinematic = true;
            rb.gravityScale = 1f;
            rb.velocity = Vector2.zero;
            rb.MovePosition(stopPosition);
            gameObject.layer = LayerMask.NameToLayer("Default");
            yield break;
        }

        StartCoroutine(StopAtTargetPos());
    }
}