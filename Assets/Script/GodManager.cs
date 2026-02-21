using UnityEngine;

public class GodManager : MonoBehaviour
{
    public ProceduralPlanet planetPrefab;
    public int numberOfPlanets = 5;
    public float universeSize = 80f;

    void Start()
    {
        CreateUniverse();
    }

    void CreateUniverse()
    {
        for (int i = 0; i < numberOfPlanets; i++)
        {
            // Posición aleatoria
            Vector3 randomPosition = Random.insideUnitSphere * universeSize;
            ProceduralPlanet newPlanet = Instantiate(planetPrefab, randomPosition, Quaternion.identity);
            
            // Semilla única y tamaño
            newPlanet.seedOffset = new Vector3(Random.Range(-1000f, 1000f), Random.Range(-1000f, 1000f), Random.Range(-1000f, 1000f));
            newPlanet.planetRadius = Random.Range(8f, 15f);
            
            // Lógica de biomas planetarios
            float tipoPlaneta = Random.value; 

            if (tipoPlaneta < 0.3f) 
            {
                // Planeta Oceánico / Archipiélagos
                newPlanet.oceanLevel = Random.Range(0.2f, 0.6f); 
                newPlanet.heightMultiplier = Random.Range(1f, 3f);
                newPlanet.name = "Oceanico_" + i;
            }
            else if (tipoPlaneta < 0.6f) 
            {
                // Planeta Árido / Desértico
                newPlanet.oceanLevel = Random.Range(-0.8f, -0.5f);
                newPlanet.heightMultiplier = Random.Range(0.5f, 1.5f);
                newPlanet.noiseScale = Random.Range(8f, 12f);
                newPlanet.name = "Arido_" + i;
            }
            else 
            {
                // Planeta Terrestre
                newPlanet.oceanLevel = Random.Range(-0.1f, 0.1f);
                newPlanet.heightMultiplier = Random.Range(2f, 4f);
                newPlanet.name = "Terrestre_" + i;
            }
        }
    }
}