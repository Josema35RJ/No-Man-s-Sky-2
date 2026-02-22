using UnityEngine;

public class RockInteraction : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private GameObject lootToSpawn;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private float spawnHeightOffset = 1.0f;

    public GameObject LootToSpawn
    {
        get => lootToSpawn;
        set => lootToSpawn = value;
    }

    private Color originalColor;
    private Renderer rend;
    
    // Cambiamos GravityAttractor por ProceduralPlanet
    private ProceduralPlanet myPlanetAttractor;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend != null && rend.material != null)
        {
            originalColor = rend.material.color;
        }

        GravityBody myGravity = GetComponent<GravityBody>();
        if (myGravity != null)
        {
            // Ahora busca la variable 'planet' que actualizamos en GravityBody
            myPlanetAttractor = myGravity.planet;
        }
        else
        {
            // Busca ProceduralPlanet en lugar de GravityAttractor
            var attractors = FindObjectsByType<ProceduralPlanet>(FindObjectsSortMode.None);
            if (attractors.Length > 0) myPlanetAttractor = attractors[0];
        }
    }

    public void ToggleHighlight(bool active)
    {
        if (rend != null) rend.material.color = active ? highlightColor : originalColor;
    }

    public void Interact()
    {
        if (lootToSpawn != null)
        {
            Vector3 spawnPos = transform.position + (transform.up * spawnHeightOffset);

            GameObject loot = Instantiate(lootToSpawn, spawnPos, transform.rotation);

            GravityBody lootGravity = loot.GetComponent<GravityBody>();
            if (lootGravity != null && myPlanetAttractor != null)
            {
                // Asignamos el planeta al objeto que cae (Loot)
                lootGravity.planet = myPlanetAttractor;
            }
        }

        Destroy(gameObject);
    }
}