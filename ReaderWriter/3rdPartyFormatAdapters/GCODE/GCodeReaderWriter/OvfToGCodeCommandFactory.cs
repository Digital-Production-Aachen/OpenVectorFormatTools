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

using GCodeReaderWriter.Commands;
using System;
using System.Collections.Generic;

namespace OpenVectorFormat.GCodeReaderWriter
{
    /// <summary>
    /// Translates OVF VectorBlocks into sequences of GCodeCommands.
    /// Subclass and override <see cref="CreateCommands"/> to support custom GCode flavors.
    /// </summary>
    public class OvfToGCodeCommandFactory
    {
        public virtual IEnumerable<GCodeCommand> CreateCommands(VectorBlock block, GCodeWriterContext ctx)
        {
            switch (block.VectorDataCase)
            {
                case VectorBlock.VectorDataOneofCase.PointSequence:
                    return PointSequenceCommands(block, ctx);
                case VectorBlock.VectorDataOneofCase.Hatches:
                    return HatchesCommands(block, ctx);
                case VectorBlock.VectorDataOneofCase.Arcs:
                    return ArcsCommands(block, ctx);
                case VectorBlock.VectorDataOneofCase.ExposurePause:
                    return ExposurePauseCommands(block, ctx);
                case VectorBlock.VectorDataOneofCase.LineSequence:
                    return LineSequenceCommands(block, ctx);
                default:
                    throw new NotImplementedException($"VectorDataCase {block.VectorDataCase} is not supported.");
            }
        }

        private IEnumerable<GCodeCommand> PointSequenceCommands(VectorBlock block, GCodeWriterContext context)
        {
            var pts = block.PointSequence.Points;
            for (int i = 0; i < pts.Count; i += 2)
            {
                if (context.InjectZ && i == 0)
                    yield return TravelMove(pts[i], pts[i + 1], context.CurrentZ);
                else
                    yield return TravelMove(pts[i], pts[i + 1]);
            }
        }

        private IEnumerable<GCodeCommand> HatchesCommands(VectorBlock block, GCodeWriterContext context)
        {
            var pts = block.Hatches.Points;
            for (int i = 0; i < pts.Count; i += 4)
            {
                if (context.InjectZ && i == 0)
                    yield return TravelMove(pts[i], pts[i + 1], context.CurrentZ);
                else
                    yield return TravelMove(pts[i], pts[i + 1]);

                yield return FeedMove(pts[i + 2], pts[i + 3]);
            }
        }

        private IEnumerable<GCodeCommand> ArcsCommands(VectorBlock block, GCodeWriterContext context)
        {
            double angle = block.Arcs.Angle;
            float startX = block.Arcs.StartDx;
            float startY = block.Arcs.StartDy;
            var centers = block.Arcs.Centers;

            for (int i = 0; i < centers.Count; i += 2)
            {
                float cx = centers[i], cy = centers[i + 1];
                double iOffset = cx - startX;
                double jOffset = cy - startY;
                double deltaX = startX - cx, deltaY = startY - cy;
                double radius = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
                double angleStart = Math.Atan2(deltaY, deltaX);
                double angleFinal = NormalizeAngle(angleStart - angle);
                double endX = cx + radius * Math.Cos(angleFinal);
                double endY = cy + radius * Math.Sin(angleFinal);

                if (angle > 0)
                    yield return new CircularInterpolationCmd(PrepCode.G, 2, true,
                        (float)endX, (float)endY, (float)iOffset, (float)jOffset, null, null);
                else if (angle < 0)
                    yield return new CircularInterpolationCmd(PrepCode.G, 3, false,
                        (float)endX, (float)endY, (float)iOffset, (float)jOffset, null, null);
                else
                    throw new ArgumentException("Arc angle must be non-zero.");
            }
        }

        private IEnumerable<GCodeCommand> ExposurePauseCommands(VectorBlock block, GCodeWriterContext context)
        {
            // PauseInUs is written as-is to G4 P — matching the existing writer behavior.
            yield return new PauseCommand(PrepCode.G, 4, (int)block.ExposurePause.PauseInUs);
        }

        private IEnumerable<GCodeCommand> LineSequenceCommands(VectorBlock block, GCodeWriterContext context)
        {
            var pts = block.LineSequence.Points;
            if (context.InjectZ)
                yield return TravelMove(pts[0], pts[1], context.CurrentZ);
            else
                yield return TravelMove(pts[0], pts[1]);

            for (int i = 2; i < pts.Count; i += 2)
                yield return FeedMove(pts[i], pts[i + 1]);
        }

        protected static LinearInterpolationCmd TravelMove(float x, float y, float? z = null)
            => new LinearInterpolationCmd(PrepCode.G, 0, false, x, y, z);

        protected static LinearInterpolationCmd FeedMove(float x, float y, float? z = null)
            => new LinearInterpolationCmd(PrepCode.G, 1, true, x, y, z);

        private static double NormalizeAngle(double angle)
        {
            angle %= 2 * Math.PI;
            if (angle > Math.PI) angle -= 2 * Math.PI;
            else if (angle <= -Math.PI) angle += 2 * Math.PI;
            return angle;
        }
    }
}
