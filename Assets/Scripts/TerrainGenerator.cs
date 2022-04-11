using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour {
    public Texture2D biomeColourMap;

    private Vector2[] heightOffsets;
    private Vector2[] humidityOffsets;
    private Vector2[] temperatureOffsets;

    public static int nHeightLayers = 4;

    // the higher, the more 'zoomed in'.
    // Needs to be likely to result in non-integer
    public float scale = 361.4f;

    // the higher the octave, the less of an effect
    public float persistance = 0.5f;

    // value that decreases scale each octave
    public float lacunarity = 2.5f;

    // seed to help with world generation
    public int SEED = 130;

    // pseudo random number generator will help generate the same random numbers each run
    private System.Random PRNG;

    void Awake() {
        PRNG = new System.Random(SEED);
        heightOffsets = generateNRandomVectors(nHeightLayers);
        humidityOffsets = generateNRandomVectors(1);
        temperatureOffsets = generateNRandomVectors(1);
    }

    void Start() {
        Debug.Log("Generating images...");
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
        float humidityScale = scale / 2;
        float perlinValue = Mathf.PerlinNoise((x / humidityScale) + humidityOffsets[0].x, (y / humidityScale) + humidityOffsets[0].y);
        float humidity = perlinValue;

        float height = Mathf.InverseLerp(-1f, 1f, heightVal);

        // slightly influence humidity by height
        humidity -= height / 3;
        humidity += 0.2f;
        return Mathf.Clamp01(humidity);
    }

    private float getTemperature(int x, int y, float heightVal) {
        int lat = Mathf.Abs(y - (Consts.MAP_DIMENSION / 2)); // positive latitude at position given
        float temp;

        // get noise based on seed
        float perlinValue = Mathf.PerlinNoise((x / scale) + temperatureOffsets[0].x, (y / scale) + temperatureOffsets[0].y) * 2 - 1;
        perlinValue = perlinValue * 20; // make value larger so can directly subtract it

        // choose value based on latitude and height
        temp = 60 - perlinValue - (lat / 20f);
        temp -= 20 * (1 - heightVal);
        return temp;
    }

    IEnumerator GenerateTerrainImage() {
        Texture2D tex = new Texture2D(Consts.MAP_DIMENSION, Consts.MAP_DIMENSION);
        for (int i = 0; i < tex.width; i++) {
            for (int j = 0; j < tex.width; j++) {
                float height = getHeightValue(i, j);

                float temp = getTemperature(i, j, height);
                temp = Mathf.InverseLerp(-80f, 80f, temp);
                temp *= biomeColourMap.width;

                float humidity = getHumidity(i, j, height);
                humidity = 1 - humidity;
                humidity *= biomeColourMap.width;
                Color color = biomeColourMap.GetPixel((int)temp, (int)humidity);

                if (height < -0.29f) {
                    color = Consts.BIOME_COLOUR_DICT[BiomeType.Beach];
                    // could add water color here for minimap?
                }

                color.a = Mathf.Clamp01(Mathf.InverseLerp(-1f, 1f, height)); // lower alpha is deeper
                tex.SetPixel(i, j, color);
            }
            yield return null;
        }

        Debug.Log("Finished generating colourmap. Saving...");
        SavePNG(tex);
        Debug.Log("Saved!");
    }

    void SavePNG(Texture2D tex) {
        byte[] bytes = tex.EncodeToPNG();
        var dirPath = Application.dataPath + "/Images";
        if (!Directory.Exists(dirPath)) {
            Directory.CreateDirectory(dirPath);
        }
        File.WriteAllBytes(dirPath + "image.png", bytes);
    }
}
