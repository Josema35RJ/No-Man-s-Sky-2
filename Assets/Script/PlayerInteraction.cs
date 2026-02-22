using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Configuración de Edición")]
    public Camera mainCamera;
    public float editRadius = 2f;
    public float editStrength = 0.3f;
    public float maxDistance = 50f; // Distancia máxima a la que se puede editar

    void Update()
    {
        // Click Izquierdo para elevar (crear montaña)
        if (Input.GetMouseButton(0))
        {
            TryEditTerrain(true);
        }
        // Click Derecho para hundir (excavar)
        else if (Input.GetMouseButton(1))
        {
            TryEditTerrain(false);
        }
    }

    private void TryEditTerrain(bool addHeight)
    {
        if (mainCamera == null) return;

        // Lanzamos un rayo desde el centro de la pantalla (o ratón)
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            // Intentamos obtener el componente del planeta en el objeto golpeado
            ProceduralPlanet planet = hit.collider.GetComponent<ProceduralPlanet>();
            
            if (planet != null)
            {
                // Pasamos el punto de impacto global y los parámetros
                planet.EditTerrain(hit.point, addHeight, editRadius, editStrength);
            }
        }
    }
}