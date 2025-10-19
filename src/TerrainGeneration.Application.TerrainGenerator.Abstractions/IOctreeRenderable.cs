using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrainGeneration.Application.TerrainGenerator.Abstractions
{
    public interface IOctreeRenderable
    {
        public void Render(Vector3 playerPosition);

        public void Dispose();
    }
}
