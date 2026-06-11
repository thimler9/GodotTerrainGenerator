namespace TerrainGeneration.Application.TerrainGenerator.TerrainSpawns;

public interface ITerrainSpawnFactory
{
    ITerrainSpawns? CreateTerrainSpawns(TerrainChunkDescriptor descriptor);
}
