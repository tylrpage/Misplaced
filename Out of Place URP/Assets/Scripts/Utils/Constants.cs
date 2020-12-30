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
    public static readonly float SECONDS_WAITING_IN_BEGIN = 5f;
    public static readonly float SECONDS_WAITING_IN_BUILD = 15f;
    public static readonly float SECONDS_WAITING_IN_SEARCH = 30f;
    public static readonly float SECONDS_WAITING_IN_SCORING = 3f;
    
    // CLIENT
    public static readonly int GRID_WIDTH = 14;
    public static readonly int GRID_HEIGHT = 11;
    public static readonly int GRID_UNITS = 1;
    public static readonly BoundedRange[] WORLD_BOUNDS = new BoundedRange[3]
    {
        new BoundedRange(-19f, 15f, 0.05f),
        new BoundedRange(-2f, 12f, 0.05f),
        new BoundedRange(0f, 50f, 0.05f),
    };

    public static readonly int CLIENT_TICKRATE = 25;
    public static readonly int[] CAMERA_REF_RESOLUTION = new[] {800, 480};
    public static readonly int CAMERA_ASSET_PPU = 64;
}
