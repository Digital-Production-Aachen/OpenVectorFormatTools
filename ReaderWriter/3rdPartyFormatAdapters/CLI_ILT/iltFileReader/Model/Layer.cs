/*
---- Copyright Start ----

This file is part of the OpenVectorFormatTools collection. This collection provides tools to facilitate the usage of the OpenVectorFormat.

Copyright (C) 2025 Digital-Production-Aachen

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


using System.Collections.Generic;

namespace OpenVectorFormat.ILTFileReader.Model
{
    class Layer:ILTFileReader.ILayer
    {
        private readonly bool isLong;
        private readonly List<ILayerCommand> layerCommands = new List<ILayerCommand>();
        private readonly List<IVectorBlock> vectorBlocks = new List<IVectorBlock>();

        public Layer(long offsetInFile, float height, bool isLong) {
            OffsetInFile = offsetInFile;
            Height = height;
            this.isLong = isLong;
        }

        public Layer(float height)
        {
            Height = height;
        }

        public long OffsetInFile
        {
            get;
            set;
        }

        public float Height
        {
            get;
            set;
        }

        public IList<ILayerCommand> LayerCommands
        {
            get => layerCommands;
        }

        public IList<IVectorBlock> VectorBlocks
        {
            get => vectorBlocks;
        }

        public bool IsLong => isLong;

        public void AddLayerCommand(ILayerCommand command)
        {
            layerCommands.Add(command);
            if (command is IVectorBlock vectorBlock) { vectorBlocks.Add(vectorBlock); }
        }
    }
}
