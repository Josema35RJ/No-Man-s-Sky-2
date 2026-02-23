using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Configuración de Edición")]
    public Camera mainCamera;
    public float editRadius = 2f;
    public float editStrength = 0.3f;
    public float maxDistance = 50f; 

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            TryEditTerrain(true);
        }
        else if (Input.GetMouseButton(1))
        {
            TryEditTerrain(false);
        }
    }

    private void TryEditTerrain(bool addHeight)
    {
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            ProceduralPlanet planet = hit.collider.GetComponent<ProceduralPlanet>();
            
            if (planet != null)
            {
                planet.EditTerrain(hit.point, addHeight, editRadius, editStrength);
            }
        }
    }
}