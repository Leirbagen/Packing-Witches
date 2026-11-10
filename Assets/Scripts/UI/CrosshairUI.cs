using UnityEngine;

public class CrosshairUI : MonoBehaviour
{
    [SerializeField] private GameObject visual;
    private void Update()
    {
        visual.SetActive(!ShopTerminal.IsOpen);
    }
}
