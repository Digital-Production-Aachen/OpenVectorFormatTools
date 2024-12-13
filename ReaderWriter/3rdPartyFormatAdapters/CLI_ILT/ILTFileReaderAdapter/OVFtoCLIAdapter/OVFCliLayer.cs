/*
---- Copyright Start ----

This file is part of the OpenVectorFormatTools collection. This collection provides tools to facilitate the usage of the OpenVectorFormat.

Copyright (C) 2024 Digital-Production-Aachen

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA

---- Copyright End ----
*/
using ILTFileReader.OVFToCLIAdapter;
using ILTFileReaderAdapter.OVFtoCLIAdapter;
using OpenVectorFormat;
using OpenVectorFormat.ILTFileReader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ILTFileReaderAdapter.OVFToCLIAdapter
{
    internal class OVFCliLayer : ILayer
    {
        private WorkPlane wp;
        private readonly List<ILayerCommand> layerCommands = new List<ILayerCommand>();
        private readonly List<IVectorBlock> vectorBlocks = new List<IVectorBlock>();

        public OVFCliLayer(WorkPlane wp)
        {
            this.wp = wp ?? throw new ArgumentNullException(nameof(wp));
        }

        public float Height => wp.ZPosInMm;

        public IList<IVectorBlock> VectorBlocks
        {
            get
            {
                var result = new List<IVectorBlock>(wp.NumBlocks);
                foreach (var block in wp.VectorBlocks)
                {
                    if (block.VectorDataCase == VectorBlock.VectorDataOneofCase.LineSequence)
                    {
                        result.Add(new OVFCliPolyline(block));
                    }
                    else if (block.VectorDataCase == VectorBlock.VectorDataOneofCase.Hatches)
                    {
                        result.Add(new OVFCliHatches(block));
                    }
                    else if (block.VectorDataCase == VectorBlock.VectorDataOneofCase.LineSequence3D)
                    {
                        result.Add(new OVFCliLineSequence3D(block));
                    }
                    else if (block.VectorDataCase == VectorBlock.VectorDataOneofCase.Hatches3D)
                    {
                        result.Add(new OVFCliHatches3D(block));
                    }
                    else if (block.VectorDataCase == VectorBlock.VectorDataOneofCase.PointSequence3D)
                    {
                        result.Add(new OVFCliPointSequence3D(block));
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
                return result;
            }
        }

        public IList<ILayerCommand> LayerCommands
        {
            get => layerCommands;
        }

        public void AddLayerCommand(ILayerCommand command)
        {
            layerCommands.Add(command);
            if (command is IVectorBlock vectorBlock) { vectorBlocks.Add(vectorBlock); }
        }


    }
}
