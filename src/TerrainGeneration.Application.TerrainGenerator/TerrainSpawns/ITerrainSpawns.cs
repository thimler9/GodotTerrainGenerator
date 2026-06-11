using System;

namespace TerrainGeneration.Application.TerrainGenerator.TerrainSpawns;

public interface ITerrainSpawns : IDisposable
{
    void Render();
}
