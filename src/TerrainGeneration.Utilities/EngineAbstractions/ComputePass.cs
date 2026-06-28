using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrainGeneration.Utilities.EngineAbstractions
{
    public class ComputePass
    {
        private RenderingDevice Rd;
        private long ComputeList;
        private Rid Pipeline;

        public ComputePass(RenderingDevice rd, long computeList, Rid pipeline)
        {
            if (rd == null)
            {
                throw new ArgumentNullException($"{nameof(rd)} cannot be null.");
            }

            if(!pipeline.IsValid)
            {
                throw new ArgumentNullException($"{nameof(Pipeline)} is not valid");
            }

            ComputeList = computeList;
            Pipeline = pipeline;
            Rd = rd;
        }

        public void BindComputeBuffer(ComputeBuffer buffer, int setIndex)
        {

        }

        public void Dispatch()
        {

        }
    }
}
