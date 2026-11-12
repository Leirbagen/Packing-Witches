using UnityEngine;

public class GridPreview : MonoBehaviour
{
     [SerializeField] private BoxGrid grid;
    [SerializeField] private Renderer quadRenderer;
    [SerializeField] private Color validColor = new Color(0.2f, 0.9f, 0.2f, 0.5f);
    [SerializeField] private Color invalidColor = new Color(0.9f, 0.2f, 0.2f, 0.5f);
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Show(Vector2Int size, int x, int y)
    {
        quadRenderer.gameObject.SetActive(true);

        Vector3 center = grid.FootprintCenterWorld(size, x, y);
        center.y = grid.gridOrigin.position.y + 0.005f;
        quadRenderer.transform.position = center;
        quadRenderer.transform.rotation = grid.gridOrigin.rotation * Quaternion.Euler(90f, 0f, 0f);
        quadRenderer.transform.localScale = new Vector3(size.x * grid.cellSize, size.y * grid.cellSize, 1f);
        bool valid = grid.CanPlace(size, x, y);
        quadRenderer.material.color = valid ? validColor : invalidColor;
    }

    public void Hide()
    {
        quadRenderer.gameObject.SetActive(false);
    }
}
