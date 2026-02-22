using UnityEngine;
using UnityEditor;
using System.IO;

public class BiomeTextureGenerator
{
    // Esto crea un botón nuevo en el menú superior de Unity
    [MenuItem("Tools/Generar Textura de Biomas")]
    public static void GenerateBiomeLUT()
    {
        int width = 256;
        int height = 256;
        Texture2D tex = new Texture2D(width, height);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // X = Temperatura (0 Frío, 1 Calor)
                // Y = Humedad (0 Seco, 1 Húmedo)
                float temp = (float)x / width;     
                float humidity = (float)y / height; 

                Color biomeColor = Color.white;

                // --- LÓGICA DE COLORES DE BIOMAS ---
                if (temp < 0.3f) 
                {
                    // ZONAS FRÍAS
                    if (humidity < 0.4f) biomeColor = new Color(0.5f, 0.5f, 0.5f); // Tundra (Gris)
                    else biomeColor = new Color(0.9f, 0.95f, 1f); // Nieve (Blanco azulado)
                } 
                else if (temp < 0.65f) 
                {
                    // ZONAS TEMPLADAS
                    if (humidity < 0.3f) biomeColor = new Color(0.7f, 0.6f, 0.4f); // Estepa seca (Marrón claro)
                    else biomeColor = new Color(0.2f, 0.5f, 0.2f); // Bosque templado (Verde)
                } 
                else 
                {
                    // ZONAS CALIENTES
                    if (humidity < 0.35f) biomeColor = new Color(0.9f, 0.8f, 0.3f); // Desierto (Arena)
                    else if (humidity < 0.6f) biomeColor = new Color(0.4f, 0.7f, 0.2f); // Sabana (Verde amarillento)
                    else biomeColor = new Color(0.1f, 0.4f, 0.1f); // Selva húmeda (Verde muy oscuro)
                }

                tex.SetPixel(x, y, biomeColor);
            }
        }

        tex.Apply();
        
        // Guardar la imagen en la carpeta Assets
        byte[] bytes = tex.EncodeToPNG();
        string path = Application.dataPath + "/MapaDeBiomas.png";
        File.WriteAllBytes(path, bytes);
        
        // Refrescar Unity para que vea el nuevo archivo
        AssetDatabase.Refresh();
        
        Debug.Log("¡Textura creada con éxito! Búscala en tu carpeta principal de Assets.");
    }
}