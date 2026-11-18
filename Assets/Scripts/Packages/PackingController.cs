using Fusion;
using UnityEngine;

public class PackingController : NetworkBehaviour
{
    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private PlayerGrab playerGrab;
    private Transform preview;
    private Renderer previewRenderer;
    private Transform anchorMarker;
    private Renderer anchorRenderer;
    private int rotation;
    private Color validColor = new Color(0.2f, 0.9f, 0.2f, 0.5f);
    private Color invalidColor = new Color(0.9f, 0.2f, 0.2f, 0.5f);

    public override void Spawned()
    {
        if (!HasInputAuthority)
        {
            return;
        }
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(quad.GetComponent<Collider>());
        previewRenderer = quad.GetComponent<Renderer>();
        previewRenderer.material = new Material(Shader.Find("Sprites/Default"));
        preview = quad.transform;
        preview.gameObject.SetActive(false);

        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(marker.GetComponent<Collider>());
        anchorRenderer = marker.GetComponent<Renderer>();
        anchorRenderer.material = new Material(Shader.Find("Sprites/Default"));
        anchorRenderer.material.color = new Color(1f, 1f, 1f, 0.9f);
        anchorMarker = marker.transform;
        anchorMarker.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!HasInputAuthority || preview == null)
        {
            return;
        }

        preview.gameObject.SetActive(false);
        anchorMarker.gameObject.SetActive(false);

        if (ShopTerminal.IsOpen)
        {
            return;
        }

        bool rotatePressed = InputController.Instance.GetButtonDown(InputController.Input.ROTATE);
        bool packPressed = InputController.Instance.GetButtonDown(InputController.Input.PACK);

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

        Vector3 local = grid.gridOrigin.InverseTransformPoint(interactor.AimedPoint);
        int anchorX = Mathf.FloorToInt(local.x / grid.cellSize);
        int anchorY = Mathf.FloorToInt(local.z / grid.cellSize);
        Vector2Int size;
        int x;
        int y;
        GetAnchoredRect(item.productData.footprint, rotation, anchorX, anchorY, out size, out x, out y);

        bool valid = grid.CanPlace(size, x, y);
        ShowPreview(grid, size, x, y, anchorX, anchorY, valid);

        if (valid && packPressed)
        {
            box.RequestPack(x, y, rotation, size);
            rotation = 0;
        }
    }

    private void GetAnchoredRect(Vector2Int footprint, int rot, int anchorX, int anchorY,out Vector2Int size, out int x, out int y)
    {
        int minX = 0;
        int maxX = footprint.x - 1;
        int minY = 0;
        int maxY = footprint.y - 1;

        for (int i = 0; i < rot; i++)
        {
            int newMinX = minY;
            int newMaxX = maxY;
            int newMinY = -maxX;
            int newMaxY = -minX;

            minX = newMinX;
            maxX = newMaxX;
            minY = newMinY;
            maxY = newMaxY;
        }

        size = new Vector2Int(maxX - minX + 1, maxY - minY + 1);
        x = anchorX + minX;
        y = anchorY + minY;
    }

    private void ShowPreview(BoxGrid grid, Vector2Int size, int x, int y, int anchorX, int anchorY, bool valid)
    {
        Quaternion flat = grid.gridOrigin.rotation * Quaternion.Euler(90f, 0f, 0f);
        Vector3 center = grid.FootprintCenterWorld(size, x, y);
        preview.position = center + grid.gridOrigin.up * 0.005f;
        preview.rotation = flat;
        preview.localScale = new Vector3(size.x * grid.cellSize, size.y * grid.cellSize, 1f);

        if (valid)
        {
            previewRenderer.material.color = validColor;
        }
        else
        {
            previewRenderer.material.color = invalidColor;
        }
        preview.gameObject.SetActive(true);
        Vector3 anchorCenter = grid.FootprintCenterWorld(Vector2Int.one, anchorX, anchorY);
        anchorMarker.position = anchorCenter + grid.gridOrigin.up * 0.01f;
        anchorMarker.rotation = flat;
        anchorMarker.localScale = new Vector3(grid.cellSize * 0.4f, grid.cellSize * 0.4f, 1f);
        anchorMarker.gameObject.SetActive(true);
    }
}