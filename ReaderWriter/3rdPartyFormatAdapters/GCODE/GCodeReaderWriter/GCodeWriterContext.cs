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

namespace OpenVectorFormat.GCodeReaderWriter
{
    /// <summary>
    /// Read-only cross-block writer state passed to the command factory.
    /// </summary>
    public class GCodeWriterContext
    {
        public float CurrentZ { get; }

        /// <summary>
        /// When true, the factory injects the workplane Z coordinate into the first point
        /// of VectorBlocks.
        /// </summary>
        public bool InjectZ { get; }

        public GCodeWriterContext(float currentZ, bool injectZ)
        {
            CurrentZ = currentZ;
            InjectZ = injectZ;
        }
    }
}
