using Fusion;
using UnityEngine;

public class PackingController : NetworkBehaviour
{
    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private PlayerGrab playerGrab;

    private Transform preview;
    private Renderer previewRenderer;
    private int rotation;

    public override void Spawned()
    {
        if (!HasInputAuthority)
        {
            return;
        } 

        //preview
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(quad.GetComponent<Collider>());
        previewRenderer = quad.GetComponent<Renderer>();
        previewRenderer.material = new Material(Shader.Find("Sprites/Default"));
        preview = quad.transform;
        preview.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!HasInputAuthority || preview == null)
        {
            return;
        } 
        preview.gameObject.SetActive(false);
        if (ShopTerminal.IsOpen)
        {
            return;
        } 
        bool rotatePressed = InputController.Instance.GetButtonDown(InputController.Input.ROTATE);
        if (playerGrab == null || !playerGrab.IsHolding || playerGrab.HeldObject == null)
        {
            rotation = 0;
            return;
        }

        PackableItem item = playerGrab.HeldObject.GetComponent<PackableItem>();
        if (item == null || item.IsPacked || item.productData == null)
        {
            return;
        } 

        BoxContainer box = interactor.AimedBox;
        if (box == null || box.grid == null)
        {
            return;
        } 

        BoxGrid grid = box.grid;

        if (rotatePressed)
        {
            rotation = (rotation + 1) % 4;
        }

        
        //Aimed cell
        Vector3 local = grid.gridOrigin.InverseTransformPoint(interactor.AimedPoint);
        int cellX = Mathf.FloorToInt(local.x / grid.cellSize);
        int cellY = Mathf.FloorToInt(local.z / grid.cellSize);


        Vector2Int size = grid.GetSize(item.productData.footprint, rotation);
        int x = Mathf.Clamp(cellX - size.x / 2, 0, Mathf.Max(0, grid.width - size.x));
        int y = Mathf.Clamp(cellY - size.y / 2, 0, Mathf.Max(0, grid.height - size.y));

        bool valid = grid.CanPlace(size, x, y);
        ShowPreview(grid, size, x, y, valid);

        if (valid && InputController.Instance != null && InputController.Instance.GetButtonDown(InputController.Input.PACK))
        {
            box.RequestPack(x, y, rotation);
            rotation = 0;
        }
    }

    private void ShowPreview(BoxGrid grid, Vector2Int size, int x, int y, bool valid)
    {
        Vector3 center = grid.FootprintCenterWorld(size, x, y);
        preview.position = center + grid.gridOrigin.up * 0.005f;
        preview.rotation = grid.gridOrigin.rotation * Quaternion.Euler(90f, 0f, 0f);
        preview.localScale = new Vector3(size.x * grid.cellSize, size.y * grid.cellSize, 1f);
        previewRenderer.material.color = valid
            ? new Color(0.2f, 0.9f, 0.2f, 0.5f)
            : new Color(0.9f, 0.2f, 0.2f, 0.5f);
        preview.gameObject.SetActive(true);
    }
}