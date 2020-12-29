using System.Collections;
using System.Collections.Generic;
using NetStack.Quantization;
using UnityEngine;

public static class Constants
{
    // SERVER + CLIENT
    public static readonly ushort GAME_PORT = 9001;
    public static readonly ushort SERVER_TICKRATE = 25;
    public static readonly int MAX_SHIFTED_OBJECTS = 3;
    
    // CLIENT
    public static readonly int GRID_WIDTH = 10;
    public static readonly int GRID_HEIGHT = 10;
    public static readonly int GRID_UNITS = 1;
    public static readonly BoundedRange[] WORLD_BOUNDS = new BoundedRange[3]
    {
        new BoundedRange(0f, 50f, 0.05f),
        new BoundedRange(0f, 50f, 0.05f),
        new BoundedRange(0f, 50f, 0.05f),
    };

    public static readonly int CLIENT_TICKRATE = 25;
    public static readonly int[] CAMERA_REF_RESOLUTION = new[] {192, 120};
}
