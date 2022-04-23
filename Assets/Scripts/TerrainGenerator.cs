using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour {
    public Texture2D biomeColourMap;
    public Texture2D waterColourMap;
    public Texture2D temperatureColourMap;
    public Texture2D humidityColourMap;

    private Vector2[] heightOffsets;
    private Vector2[] humidityOffsets;
    private Vector2[] temperatureOffsets;

    public static int nHeightLayers = 5;
    public static int nHumidityLayers = 3;

    // the higher, the more 'zoomed in'.
    // Needs to be likely to result in non-integer
    private float scale = 101.7f;

    // value used to lessen the effect each octave layer has
    private float persistance = 0.5f;

    // value that decreases scale each octave
    private float lacunarity = 2.1f;

    // pseudo random number generator will help generate the same random numbers each run
    private System.Random PRNG;

    void Start() {
        // initliase pseudo-random number generator with a seed
        PRNG = new System.Random(Consts.SEED);

        // Unity's perlin noise is not initialised with a seed, so instead we pick random spots in that perlin noise to sample from
        heightOffsets = generateNRandomVectors(nHeightLayers);
        humidityOffsets = generateNRandomVectors(nHumidityLayers);
        temperatureOffsets = generateNRandomVectors(1);

        // generate the image using coroutine so Unity doesn't freeze
        StartCoroutine("GenerateTerrainImage");
        // StartCoroutine("GenerateTemperatureImage");
        // StartCoroutine("GenerateHumidityImage");
        // StartCoroutine("GenerateHeightImage");
    }

    // function to generate an array of coordinates, remember that PRNG is dependent on SEED
    private Vector2[] generateNRandomVectors(int n) {
        Vector2[] numbers = new Vector2[n];
        for (int i = 0; i < n; i++) {
            float offsetX = PRNG.Next(-1000, 1000);
            float offsetY = PRNG.Next(-1000, 1000);
            numbers[i] = new Vector2(offsetX, offsetY);
        }
        return numbers;
    }

    // function to get the height at some specific x and y coordinates
    private float getHeightValue(int x, int y) {
        float height = 0;
        float amplitude = 1;
        float frequency = 1;

        for (int i = 0; i < nHeightLayers; i++) {
            // calculate sample coordinates
            float sampleX = x / (scale / frequency) + heightOffsets[i].x + 0.1f;
            float sampleY = y / (scale / frequency) + heightOffsets[i].y + 0.1f;

            // sample the perlin noise, making in range -1 to 1 (in range 0 - 1 by default)
            float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;

            // add this new noise to existing layers
            height += perlinValue * amplitude;

            // amplitude decreases each octave if persistance < 1
            amplitude *= persistance;
            // frequency increases each octave if lacunarity > 1
            frequency *= lacunarity;
        }
        return height;
    }

    // function to get the humidity at some specific x and y coordinates
    private float getHumidity(int x, int y, float heightVal) {
        float humidityScale = scale / 3f;
        float humidity = 0;
        float amplitude = 1;
        float frequency = 1;

        for (int i = 0; i < nHumidityLayers; i++) {
            // calculate sample coordinates
            float sampleX = x / (humidityScale / frequency) + humidityOffsets[i].x + 0.1f;
            float sampleY = y / (humidityScale / frequency) + humidityOffsets[i].y + 0.1f;

            // sample perlin noise
            float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
            humidity += perlinValue * amplitude;

            // amplitude decreases each octave if persistance < 1
            amplitude *= persistance;
            // frequency increases each octave if lacunarity > 1
            frequency *= lacunarity;
        }

        // convert height value to be in range 0 - 1
        float height = Mathf.InverseLerp(-1f, 1f, heightVal);

        // slightly influence humidity by height
        humidity -= height;

        // compensate for adjustment
        humidity += 0.4f;
        return Mathf.Clamp01(humidity);
    }

    // get the temperate at some specific x and y coordinates
    private float getTemperature(int x, int y, float heightVal) {
        // temperature is hotter at equator and cooler at poles, so it is deoendant on y value
        float lat = Mathf.Abs(y - (Consts.MAP_DIMENSION / 2));

        // make lat value in range 0 - 1
        lat = Mathf.InverseLerp(Consts.MAP_DIMENSION / 2, 0, lat);

        // convert to degrees celcius (stretch in range -50 to 50 degrees)
        float latTemperature = Mathf.Lerp(-50, 50, lat);

        // sample the perlin noise
        float perlinValue = Mathf.PerlinNoise((x / scale) + temperatureOffsets[0].x, (y / scale) + temperatureOffsets[0].y) * 2 - 1;

        // make perlin value larger (convert it to degrees)
        perlinValue = perlinValue * 20;

        // modifiy previous temperature value by the perlin noise
        return latTemperature - perlinValue;
    }

    // function that calculates the colour of each pixel to form the final image by using the height, temperature and humidity values
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

                if (height < Consts.BEACH_HEIGHT) {
                    color = Consts.BEACH_COLOUR;
                }

                if (height < Consts.WATER_HEIGHT) {
                    float heightPos = Mathf.InverseLerp(-1, Consts.WATER_HEIGHT, height);
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
    }

    IEnumerator GenerateHumidityImage() {
        Debug.Log("Generating humidity map...");

        Texture2D humidityMap = new Texture2D(Consts.MAP_DIMENSION, Consts.MAP_DIMENSION);
        for (int i = 0; i < humidityMap.width; i++) {
            for (int j = 0; j < humidityMap.width; j++) {
                float height = getHeightValue(i, j);
                float humidityPos = getHumidity(i, j, height);
                humidityPos *= humidityColourMap.height;
                if (height < Consts.WATER_HEIGHT) {
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

    IEnumerator GenerateHeightImage() {
        Debug.Log("Generating height map...");

        Texture2D heightMap = new Texture2D(Consts.MAP_DIMENSION, Consts.MAP_DIMENSION);
        for (int i = 0; i < heightMap.width; i++) {
            for (int j = 0; j < heightMap.width; j++) {
                float height = getHeightValue(i, j);
                float lerpedHeight = Mathf.InverseLerp(-1, 1, height);
                Color colour = new Color(lerpedHeight, lerpedHeight, lerpedHeight, 1);
                if (height < Consts.WATER_HEIGHT) {
                    colour = Color.black;
                }
                heightMap.SetPixel(i, j, colour);
            }
            yield return null;
        }

        Debug.Log("Finished generating height map. Saving...");
        SavePNG(heightMap, "HeightMap");
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
