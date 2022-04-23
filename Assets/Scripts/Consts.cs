using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// this class is for storing constant variables
public static class Consts {
    // length of the map in number of tiles
    public static int MAP_DIMENSION = 500;

    public static int SEED = 130;

    // table used to convert temp and humidity into biomes
    public static BiomeType[,] BIOME_TYPE_TABLE = {   
    //                                               <--Colder      Hotter -->            
    { BiomeType.Ice, BiomeType.Ice, BiomeType.Tundra, BiomeType.Grassland,      BiomeType.Grassland,          BiomeType.Savanna,    BiomeType.Desert,     BiomeType.Desert},   // Dryest
    { BiomeType.Ice, BiomeType.Ice, BiomeType.Tundra, BiomeType.Grassland,      BiomeType.Grassland,          BiomeType.Savanna,    BiomeType.Desert,     BiomeType.Desert},
    { BiomeType.Ice, BiomeType.Ice, BiomeType.Tundra, BiomeType.Grassland,      BiomeType.Grassland,          BiomeType.Savanna,    BiomeType.Desert,     BiomeType.Desert },
    { BiomeType.Ice, BiomeType.Ice, BiomeType.Tundra, BiomeType.Grassland,      BiomeType.Grassland,          BiomeType.Savanna,    BiomeType.Savanna,    BiomeType.Desert },
    { BiomeType.Ice, BiomeType.Ice, BiomeType.Taiga,  BiomeType.Taiga,          BiomeType.SeasonalForest,     BiomeType.Grassland,  BiomeType.Savanna,    BiomeType.Desert },
    { BiomeType.Ice, BiomeType.Ice, BiomeType.Taiga,  BiomeType.Taiga,          BiomeType.SeasonalForest,     BiomeType.Rainforest, BiomeType.Savanna,    BiomeType.Desert },  // Wettest
	{ BiomeType.Ice, BiomeType.Ice, BiomeType.Taiga,  BiomeType.Taiga,          BiomeType.SeasonalForest,     BiomeType.Rainforest, BiomeType.Savanna,    BiomeType.Desert },
    { BiomeType.Ice, BiomeType.Ice, BiomeType.Taiga,  BiomeType.Taiga,          BiomeType.SeasonalForest,     BiomeType.Rainforest, BiomeType.Savanna,    BiomeType.Desert }
    };

    public static Color32 BEACH_COLOUR = new Color32(229, 209, 168, 255);

    public static float BEACH_HEIGHT = -0.17f;
    public static float WATER_HEIGHT = -0.2f;

}

public enum BiomeType {
    Desert,
    Savanna,
    Rainforest,
    Grassland,
    SeasonalForest,
    Taiga,
    Tundra,
    Ice,
    Water,
    DeepWater,
    Beach
}
