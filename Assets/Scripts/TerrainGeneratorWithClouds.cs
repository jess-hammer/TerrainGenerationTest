using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TerrainGeneratorWithClouds : MonoBehaviour {
    public Texture2D biomeColourMap;
    public Texture2D waterColourMap;
    public Texture2D temperatureColourMap;
    public Texture2D humidityColourMap;
    public Texture2D cloudColourMap;

    private Vector2[] heightOffsets;
    private Vector2[] humidityOffsets;
    private Vector2[] temperatureOffsets;

    public static int nHeightLayers = 5;
    public static int nHumidityLayers = 3;

    float scale = 101.7f;
    float persistance = 0.5f;
    float lacunarity = 2.1f;

    private System.Random PRNG;

    void Awake() {
        PRNG = new System.Random(Consts.SEED);
        heightOffsets = generateNRandomVectors(nHeightLayers);
        humidityOffsets = generateNRandomVectors(nHumidityLayers);
        temperatureOffsets = generateNRandomVectors(1);
    }

    //generates 30 images
    void Start() {
        int offsetX = 0;
        for (float i = 0; i < 3; i += 0.1f) {
            StartCoroutine(GenerateTerrainImageWithClouds(i, offsetX));
            offsetX += 5;
        }
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
        humidity += 0.4f;
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

    private float getCloudNoise(int x, int y, float slice) {
        float height = 0;
        float amplitude = 1;
        float frequency = 1;
        float cloudScale = scale / 1.5f;

        for (int i = 0; i < nHeightLayers; i++) {
            // scale results in non-integer value
            float sampleX = x / (cloudScale / frequency) + heightOffsets[i].x + 0.1f;
            float sampleY = y / (cloudScale / frequency) + heightOffsets[i].y + 0.1f;

            float perlinValue = Perlin.Noise(sampleX, sampleY, slice);
            height += perlinValue * amplitude;

            // amplitude decreases each octave if persistance < 1
            amplitude *= persistance;
            // frequency increases each octave if lacunarity > 1
            frequency *= lacunarity;
        }
        return Mathf.InverseLerp(-1, 1, height);
    }

    IEnumerator GenerateTerrainImageWithClouds(float slice, int offsetX) {
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

                float cloudNoise = getCloudNoise(i + offsetX, j, slice);

                if (height < Consts.BEACH_HEIGHT) {
                    color = Consts.BEACH_COLOUR;
                }
                if (height < Consts.WATER_HEIGHT) {
                    float heightPos = Mathf.InverseLerp(-1, Consts.WATER_HEIGHT, height);
                    heightPos *= waterColourMap.height;
                    color = waterColourMap.GetPixel(0, (int)heightPos);
                }

                if (cloudNoise > 0.57) {
                    // a very light grey colour
                    Color cloudColor = new Color(0.96f, 0.95f, 0.95f, 1);

                    float cloudColorMapPos = cloudColourMap.height * Mathf.Clamp01(Mathf.InverseLerp(0.57f, 1f, cloudNoise));

                    cloudColor.a = cloudColourMap.GetPixel(0, (int)cloudColorMapPos).r;
                    color = blendColours(color, cloudColor);
                }

                // thought it looked nicer with a very transparent border around each cloud
                else if (cloudNoise > 0.56) {
                    // a very transparent light grey colour
                    Color cloudColor = new Color(0.96f, 0.95f, 0.95f, 0.2f);
                    color = blendColours(color, cloudColor);
                }
                colourMap.SetPixel(i, j, color);
            }
            yield return null;
        }
        SavePNG(colourMap, "ColourMap" + slice);
        Debug.Log("Saved!");
    }

    // blend between background and foreground colours
    Color blendColours(Color bg, Color fg) {
        Color result = new Color();
        result.a = 1 - (1 - fg.a) * (1 - bg.a);
        result.r = fg.r * fg.a / result.a + bg.r * bg.a * (1 - fg.a) / result.a;
        result.g = fg.g * fg.a / result.a + bg.g * bg.a * (1 - fg.a) / result.a;
        result.b = fg.b * fg.a / result.a + bg.b * bg.a * (1 - fg.a) / result.a;
        return result;
    }

    void SavePNG(Texture2D tex, string name) {
        byte[] bytes = tex.EncodeToPNG();
        var dirPath = Application.dataPath + "/Images/CloudSlices/";
        if (!Directory.Exists(dirPath)) {
            Directory.CreateDirectory(dirPath);
        }
        File.WriteAllBytes(dirPath + name + ".png", bytes);
    }
}
