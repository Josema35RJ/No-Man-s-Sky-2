using UnityEngine;
using UnityEditor;
using System.IO;

public class BiomeTextureGenerator
{
    // Tipos de mapas que vamos a generar
    public enum PlanetLUTType { Terrestre, Helado, Desierto, Volcanico, Alien }

    // Este botón generará todos los mapas de golpe
    [MenuItem("Tools/Generar TODAS las Texturas de Biomas")]
    public static void GenerateAllLUTs()
    {
        GenerateLUT("MapaBiomas_Terrestre", PlanetLUTType.Terrestre);
        GenerateLUT("MapaBiomas_Helado", PlanetLUTType.Helado);
        GenerateLUT("MapaBiomas_Desierto", PlanetLUTType.Desierto);
        GenerateLUT("MapaBiomas_Volcanico", PlanetLUTType.Volcanico);
        GenerateLUT("MapaBiomas_Alien", PlanetLUTType.Alien); // Para planetas tóxicos o extraños

        // Refrescar Unity una sola vez al terminar
        AssetDatabase.Refresh();
        Debug.Log("¡Éxito! Se han generado 5 texturas de biomas nuevas en la carpeta Assets.");
    }

    private static void GenerateLUT(string fileName, PlanetLUTType type)
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

                // --- LÓGICAS DE COLORES SEGÚN EL TIPO DE PLANETA ---
                switch (type)
                {
                    case PlanetLUTType.Terrestre:
                        if (temp < 0.3f) {
                            biomeColor = (humidity < 0.4f) ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.9f, 0.95f, 1f); // Tundra / Nieve
                        } else if (temp < 0.65f) {
                            biomeColor = (humidity < 0.3f) ? new Color(0.7f, 0.6f, 0.4f) : new Color(0.2f, 0.5f, 0.2f); // Estepa / Bosque
                        } else {
                            if (humidity < 0.35f) biomeColor = new Color(0.9f, 0.8f, 0.3f); // Desierto
                            else if (humidity < 0.6f) biomeColor = new Color(0.4f, 0.7f, 0.2f); // Sabana
                            else biomeColor = new Color(0.1f, 0.4f, 0.1f); // Selva
                        }
                        break;

                    case PlanetLUTType.Helado:
                        if (temp < 0.4f) {
                            biomeColor = (humidity < 0.5f) ? new Color(0.6f, 0.65f, 0.7f) : new Color(0.95f, 0.98f, 1f); // Roca congelada / Nieve pura
                        } else {
                            biomeColor = (humidity < 0.5f) ? new Color(0.3f, 0.5f, 0.7f) : new Color(0.5f, 0.7f, 0.8f); // Hielo azulado / Aguanieve
                        }
                        break;

                    case PlanetLUTType.Desierto:
                        if (temp < 0.5f) {
                            biomeColor = (humidity < 0.5f) ? new Color(0.8f, 0.75f, 0.6f) : new Color(0.6f, 0.4f, 0.3f); // Arena pálida / Roca rojiza seca
                        } else {
                            biomeColor = (humidity < 0.5f) ? new Color(0.9f, 0.5f, 0.2f) : new Color(0.7f, 0.3f, 0.1f); // Dunas naranjas / Arcilla cocida
                        }
                        break;

                    case PlanetLUTType.Volcanico:
                        if (temp < 0.6f) {
                            biomeColor = (humidity < 0.5f) ? new Color(0.2f, 0.2f, 0.2f) : new Color(0.3f, 0.25f, 0.25f); // Ceniza negra / Escoria oscura
                        } else {
                            // En los planetas volcánicos, alta "temperatura/humedad" representa zonas geotérmicas activas
                            biomeColor = (humidity < 0.6f) ? new Color(0.1f, 0.1f, 0.1f) : new Color(0.9f, 0.2f, 0.0f); // Basalto obsidiana / Magma brillante
                        }
                        break;

                    case PlanetLUTType.Alien:
                        if (temp < 0.5f) {
                            biomeColor = (humidity < 0.5f) ? new Color(0.3f, 0.1f, 0.4f) : new Color(0.5f, 0.2f, 0.6f); // Suelo morado oscuro / Tierra fucsia
                        } else {
                            biomeColor = (humidity < 0.5f) ? new Color(0.8f, 0.2f, 0.4f) : new Color(0.2f, 0.9f, 0.3f); // Arena roja alienígena / Pantano ácido verde neón
                        }
                        break;
                }

                tex.SetPixel(x, y, biomeColor);
            }
        }

        tex.Apply();
        
        // Guardamos cada imagen con su nombre único
        byte[] bytes = tex.EncodeToPNG();
        string path = Application.dataPath + "/" + fileName + ".png";
        File.WriteAllBytes(path, bytes);
    }
}