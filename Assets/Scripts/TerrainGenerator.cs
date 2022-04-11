using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour {
    public Texture2D biomeColourMap;
    public Texture2D waterColourMap;
    public Texture2D temperatureColourMap;
    public Texture2D humidityColourMap;

    float BEACH_HEIGHT = -0.17f;
    float WATER_HEIGHT = -0.2f;

    private Vector2[] heightOffsets;
    private Vector2[] humidityOffsets;
    private Vector2[] temperatureOffsets;

    public static int nHeightLayers = 5;
    public static int nHumidityLayers = 2;

    // the higher, the more 'zoomed in'.
    // Needs to be likely to result in non-integer
    float scale = 201.7f;

    // the higher the octave, the less of an effect
    float persistance = 0.5f;

    // value that decreases scale each octave
    float lacunarity = 2.1f;

    // seed to help with world generation
    public int SEED = 130;

    // pseudo random number generator will help generate the same random numbers each run
    private System.Random PRNG;

    void Awake() {
        PRNG = new System.Random(SEED);
        heightOffsets = generateNRandomVectors(nHeightLayers);
        humidityOffsets = generateNRandomVectors(nHumidityLayers);
        temperatureOffsets = generateNRandomVectors(1);
    }

    void Start() {
        StartCoroutine("GenerateTerrainImage");
    }
    private Vector2[] generateNRandomVectors(int n) {
        Vector2[] numbers = new Vector2[n];
        for (int i = 0; i < n; i++) {
            float offsetX = PRNG.Next(-1000, 1000);
            float offsetY = PRNG.Next(-1000, 1000);
            numbers[i] = new Vector2(offsetX, offsetY);
        }
        return numbers;
    }

    private float getHeightValue(int x, int y) {
        float height = 0;
        float amplitude = 1;
        float frequency = 1;

        for (int i = 0; i < nHeightLayers; i++) {
            // scale results in non-integer value
            float sampleX = x / (scale / frequency) + heightOffsets[i].x + 0.1f;
            float sampleY = y / (scale / frequency) + heightOffsets[i].y + 0.1f;

            float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1; // make in range -1 to 1
            height += perlinValue * amplitude;

            // amplitude decreases each octave if persistance < 1
            amplitude *= persistance;
            // frequency increases each octave if lacunarity > 1
            frequency *= lacunarity;
        }
        return height;
    }

    // note: there is currently no noise layers for humidity
    private float getHumidity(int x, int y, float heightVal) {
        float humidityScale = scale / 3f;
        float humidity = 0;
        float amplitude = 1;
        float frequency = 1;

        for (int i = 0; i < nHumidityLayers; i++) {
            // scale results in non-integer value
            float sampleX = x / (humidityScale / frequency) + humidityOffsets[i].x + 0.1f;
            float sampleY = y / (humidityScale / frequency) + humidityOffsets[i].y + 0.1f;

            float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
            humidity += perlinValue * amplitude;

            // amplitude decreases each octave if persistance < 1
            amplitude *= persistance;
            // frequency increases each octave if lacunarity > 1
            frequency *= lacunarity;
        }

        float height = Mathf.InverseLerp(-1f, 1f, heightVal);

        // slightly influence humidity by height
        humidity -= height;
        humidity += 0.5f;
        return Mathf.Clamp01(humidity);
    }

    private float getTemperature(int x, int y, float heightVal) {
        float lat = Mathf.Abs(y - (Consts.MAP_DIMENSION / 2));
        lat = Mathf.InverseLerp(Consts.MAP_DIMENSION / 2, 0, lat);
        float latTemperature = Mathf.Lerp(-50, 50, lat);

        float perlinValue = Mathf.PerlinNoise((x / scale) + temperatureOffsets[0].x, (y / scale) + temperatureOffsets[0].y) * 2 - 1;
        perlinValue = perlinValue * 20; // make perlin value larger

        return latTemperature - perlinValue;
    }

    IEnumerator GenerateTerrainImage() {
        Debug.Log("Generating colour map...");

        Texture2D colourMap = new Texture2D(Consts.MAP_DIMENSION, Consts.MAP_DIMENSION);
        for (int i = 0; i < colourMap.width; i++) {
            for (int j = 0; j < colourMap.width; j++) {
                float height = getHeightValue(i, j);

                float colourMapPos1 = getTemperature(i, j, height);
                colourMapPos1 = Mathf.InverseLerp(-60f, 60f, colourMapPos1);
                colourMapPos1 *= biomeColourMap.width;

                float colourMapPos2 = getHumidity(i, j, height);
                colourMapPos2 = 1 - colourMapPos2;
                colourMapPos2 *= biomeColourMap.width;
                Color color = biomeColourMap.GetPixel((int)colourMapPos1, (int)colourMapPos2);

                if (height < BEACH_HEIGHT) {
                    color = Consts.BIOME_COLOUR_DICT[BiomeType.Beach];
                }
                if (height < WATER_HEIGHT) {
                    float heightPos = Mathf.InverseLerp(-1, WATER_HEIGHT, height);
                    heightPos *= waterColourMap.height;
                    color = waterColourMap.GetPixel(0, (int)heightPos);
                }
                colourMap.SetPixel(i, j, color);
            }
            yield return null;
        }

        Debug.Log("Finished generating colour map. Saving...");
        SavePNG(colourMap, "ColourMap");
        Debug.Log("Saved!");
        StartCoroutine("GenerateTemperatureImage");
    }
    IEnumerator GenerateTemperatureImage() {
        Debug.Log("Generating temperature map...");

        Texture2D temperatureMap = new Texture2D(Consts.MAP_DIMENSION, Consts.MAP_DIMENSION);
        for (int i = 0; i < temperatureMap.width; i++) {
            for (int j = 0; j < temperatureMap.width; j++) {
                float height = getHeightValue(i, j);
                float temperaturePos = getTemperature(i, j, height);
                temperaturePos = Mathf.InverseLerp(-60f, 60f, temperaturePos);
                temperaturePos *= temperatureColourMap.height;
                temperatureMap.SetPixel(i, j, temperatureColourMap.GetPixel(0, (int)temperaturePos));
            }
            yield return null;
        }

        Debug.Log("Finished generating temperature map. Saving...");
        SavePNG(temperatureMap, "TemperatureMap");
        Debug.Log("Saved!");
        StartCoroutine("GenerateHumidityImage");
    }

    IEnumerator GenerateHumidityImage() {
        Debug.Log("Generating humidity map...");

        Texture2D humidityMap = new Texture2D(Consts.MAP_DIMENSION, Consts.MAP_DIMENSION);
        for (int i = 0; i < humidityMap.width; i++) {
            for (int j = 0; j < humidityMap.width; j++) {
                float height = getHeightValue(i, j);
                float humidityPos = getHumidity(i, j, height);
                humidityPos *= humidityColourMap.height;
                if (height < WATER_HEIGHT) {
                    humidityMap.SetPixel(i, j, humidityColourMap.GetPixel(0, humidityColourMap.height - 1));
                } else {
                    humidityMap.SetPixel(i, j, humidityColourMap.GetPixel(0, (int)humidityPos));
                }
            }
            yield return null;
        }

        Debug.Log("Finished generating humidity map. Saving...");
        SavePNG(humidityMap, "HumidityMap");
        Debug.Log("Saved!");
    }

    void SavePNG(Texture2D tex, string name) {
        byte[] bytes = tex.EncodeToPNG();
        var dirPath = Application.dataPath + "/Images/";
        if (!Directory.Exists(dirPath)) {
            Directory.CreateDirectory(dirPath);
        }
        File.WriteAllBytes(dirPath + name + ".png", bytes);
    }
}
