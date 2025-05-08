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

﻿using OpenVectorFormat;
using OpenVectorFormat.AbstractReaderWriter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GCodeReaderWriter
{
    public class GCodeWriter : FileWriter
    {
        private IFileReaderWriterProgress progress;
        private string filename;
        private StreamWriter _fs;


        public override Job JobShell { get { return _jobShell; } }
        private Job _jobShell;
        float[] _lastPt = null;
        float [] _currentZ = null;


        private NumberFormatInfo _nfi = new NumberFormatInfo();

        public GCodeWriter()
        {
            _nfi.NumberDecimalSeparator = ".";
        }

        /// <inheritdoc/>
        public override FileWriteOperation FileOperationInProgress { get { return _fileOperationInProgress; } }
        private FileWriteOperation _fileOperationInProgress = FileWriteOperation.None;

        private MarkingParams _lastWrittenParams = null;

        public new static List<string> SupportedFileFormats { get; } = new List<string>() { ".gcode" };

        public override void AppendWorkPlane(WorkPlane workPlane)
        {
            _currentZ = new float[] { workPlane.ZPosInMm };
            _writeGoPlaneZ(_currentZ);

            for (uint i = 0; i < workPlane.Repeats + 1; i++)
            {
                for (int i_vectorblock = 0; i_vectorblock < workPlane.VectorBlocks.Count; i_vectorblock++)
                {
                    _addVectorBlock(workPlane.VectorBlocks[i_vectorblock]);
                }
            }
        }

        /// <inheritdoc/>
        public override void AppendVectorBlock(VectorBlock block)
        {
            _addVectorBlock(block);
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            if (_fileOperationInProgress != FileWriteOperation.None)
            {
                _fs.Close();
            }
        }

        public void _addVectorBlock(VectorBlock block)
        {
            for (ulong i = 0; i < block.Repeats + 1; i++)
            {
                MarkingParams newParams = new MarkingParams();
                //if (block.MarkingParamsKey != 0 || _jobShell.MarkingParamsMap.Count != 0)
                //{
                //    newParams = _jobShell.MarkingParamsMap[block.MarkingParamsKey];
                //    if (_lastWrittenParams == null ||
                //        _lastWrittenParams.JumpDelayInUs != newParams.JumpDelayInUs)
                //    {
                //        _fs.WriteLine("M"
                //            + newParams.JumpDelayInUs.ToString(_nfi)); // change for M
                //    }
                //    if (_lastWrittenParams == null ||
                //        _lastWrittenParams.JumpDelayInUs != newParams.JumpDelayInUs)
                //    {
                //        _fs.WriteLine("T" 
                //            + newParams.JumpDelayInUs.ToString(_nfi)); // change for T
                //    }

                //    _lastWrittenParams = newParams;
                //}

                switch (block.VectorDataCase)
                {
                    case VectorBlock.VectorDataOneofCase.PointSequence3D:
                        {
                            float[] newPt;
                            for (int i_point = 0; i_point < block.PointSequence3D.Points.Count; i_point += 3)
                            {
                                newPt = new float[3] { block.PointSequence3D.Points[i_point], block.PointSequence3D.Points[i_point + 1], block.PointSequence3D.Points[i_point + 2] };

                                Console.WriteLine($"Point3D: X={newPt[0]}, Y={newPt[1]}, Z={newPt[2]}");

                                if (_lastPt == null || !newPt.SequenceEqual(_lastPt))
                                {
                                    _writeGoPoint(newPt);
                                } 
                            }
                            break;
                        }
                    case VectorBlock.VectorDataOneofCase.PointSequence:
                        {
                            float[] newPt;
                            for (int i_point = 0; i_point < block.PointSequence.Points.Count; i_point += 2)
                            {
                                newPt = new float[2] { block.PointSequence.Points[i_point], block.PointSequence.Points[i_point + 1] };

                                Console.WriteLine($"[PointSequence] X={newPt[0]} Y={newPt[1]}");

                                if (_lastPt == null || !newPt.SequenceEqual(_lastPt))
                                {
                                    _writeGoPoint(newPt);
                                }
                            }
                            break;
                        }





                    case VectorBlock.VectorDataOneofCase.Hatches3D:
                        {
                            float[] startPt;
                            float[] endPt;

                            for (int i_hatch_point = 0; i_hatch_point < block.Hatches3D.Points.Count; i_hatch_point += 6)
                            {
                                startPt = new float[3] { block.Hatches3D.Points[i_hatch_point], block.Hatches3D.Points[i_hatch_point + 1], block.Hatches3D.Points[i_hatch_point + 2] };
                                endPt = new float[3] { block.Hatches3D.Points[i_hatch_point + 3], block.Hatches3D.Points[i_hatch_point + 4], block.Hatches3D.Points[i_hatch_point + 5] };

                                Console.WriteLine($"[Hatches3D] Start: X={startPt[0]} Y={startPt[1]} Z={startPt[2]} | End: X={endPt[0]} Y={endPt[1]} Z={endPt[2]}");

                                _writeGoPoint(startPt);
                                _writeGoLine(endPt);
                            }
                            break;

                        }

                    case VectorBlock.VectorDataOneofCase.Hatches:
                        {
                            float[] startPt;
                            float[] endPt;

                            for (int i_hatch_point = 0; i_hatch_point < block.Hatches.Points.Count; i_hatch_point += 4)
                            {
                                startPt = new float[2] { block.Hatches.Points[i_hatch_point], block.Hatches.Points[i_hatch_point + 1] };
                                endPt = new float[2] { block.Hatches.Points[i_hatch_point + 2], block.Hatches.Points[i_hatch_point + 3] };

                                Console.WriteLine($"[Hatches] Start: X={startPt[0]} Y={startPt[1]} | End: X={endPt[0]} Y={endPt[1]}");

                                _writeGoPoint(startPt);
                                _writeGoLine(endPt);
                            }
                            break;

                        }






                    case VectorBlock.VectorDataOneofCase.Arcs:
                        {
                            double angle = block.Arcs.Angle;
                            float startPointX = block.Arcs.StartDx;
                            float startPointY = block.Arcs.StartDy;

                            float[] arcCenters;
                            
                            for (int i_arc_center = 0; i_arc_center < block.Arcs.Centers.Count; i_arc_center += 2)
                            {
                                arcCenters = new float[2] { block.Arcs.Centers[i_arc_center], block.Arcs.Centers[i_arc_center + 1] };

                                Console.WriteLine($"[Arcs] Center: X={arcCenters[0]} Y={arcCenters[1]} Angle={angle} StartOffset: ({startPointX}, {startPointY})");

                                _writeGoArc(arcCenters, startPointX, startPointY, angle);
                            }
                            break;
                        }

                    case VectorBlock.VectorDataOneofCase.ExposurePause:
                        {
                            ulong newPause = block.ExposurePause.PauseInUs;
                            _fs.WriteLine("G4 P" + newPause.ToString(_nfi)); 
                            break;
                        }



                            





                    case VectorBlock.VectorDataOneofCase.LineSequence3D:
                        {
                            float[] newPt;
                            newPt = new float[3] { block.LineSequence3D.Points[0], block.LineSequence3D.Points[1], block.LineSequence3D.Points[2] };

                            Console.WriteLine($"[LineSequence3D] Start: X={newPt[0]} Y={newPt[1]} Z={newPt[2]}");


                            if (_lastPt == null || !newPt.SequenceEqual(_lastPt))
                            {
                                _writeGoPoint(newPt);
                            }

                            for (int i_lineSeq = 3; i_lineSeq < block.LineSequence3D.Points.Count; i_lineSeq += 3)
                            {
                                newPt = new float[3] { block.LineSequence3D.Points[i_lineSeq], block.LineSequence3D.Points[i_lineSeq + 1], block.LineSequence3D.Points[i_lineSeq + 2] };

                                Console.WriteLine($"[LineSequence3D] LineTo: X={newPt[0]} Y={newPt[1]} Z={newPt[2]}");


                                _writeGoLine(newPt);
                            }
                            break;
                        }
                    case VectorBlock.VectorDataOneofCase.LineSequence:
                        {
                            float[] newPt;
                            newPt = new float[2] { block.LineSequence.Points[0], block.LineSequence.Points[1] };

                            Console.WriteLine($"[LineSequence] Start: X={newPt[0]} Y={newPt[1]}");


                            if (_lastPt == null || !newPt.SequenceEqual(_lastPt))
                            {
                                _writeGoPoint(newPt);
                            }

                            for (int i_lineSeq = 2; i_lineSeq < block.LineSequence.Points.Count; i_lineSeq += 2)
                            {
                                newPt = new float[2] { block.LineSequence.Points[i_lineSeq], block.LineSequence.Points[i_lineSeq + 1] };

                                Console.WriteLine($"[LineSequence] LineTo: X={newPt[0]} Y={newPt[1]}");


                                _writeGoLine(newPt);
                            }
                            break;
                        }




                    default:
                        {
                            throw new NotImplementedException("DataType " + block.VectorDataCase.ToString() + " is invalid.");
                        }
                }
            }
        }




        private void _writeGoArc(float[] arcCenters, float startPointX, float startPointY, double angle)
        {
            double I = arcCenters[0] - startPointX;
            double J = arcCenters[1] - startPointY;

            double deltaY = (double)startPointY - (double)arcCenters[1];
            double deltaX = (double)startPointX - (double)arcCenters[0];
            double radius = Math.Sqrt(Math.Pow(deltaX, 2) + Math.Pow(deltaY, 2));

            double angleStart = Math.Atan2(deltaY, deltaX);
            double angleFinal = angleStart - angle;


            angleFinal = NormalizeAngleDegrees(angleFinal);


            double endX = arcCenters[0] + radius * Math.Cos(angleFinal);
            double endY = arcCenters[1] + radius * Math.Sin(angleFinal);


            


            if (angle > 0)
            {
                _fs.WriteLine("G2 X{0} Y{1} I{2} J{3}", endX.ToString(_nfi), endY.ToString(_nfi), I.ToString(_nfi), J.ToString(_nfi));
            }
            else if (angle < 0)
            {
                _fs.WriteLine("G3 X{0} Y{1} I{2} J{3}", endX.ToString(_nfi), endY.ToString(_nfi), I.ToString(_nfi), J.ToString(_nfi));
            }
            else
            {
                throw new InvalidDataException("Point needs to contain 2 or 3 values");
            }
        }

        double NormalizeAngleDegrees(double angle)
        {
            angle = angle % (2 * Math.PI); 

            if (angle > Math.PI)
                angle -= 2 * Math.PI;
            else if (angle <= -Math.PI)
                angle += 2 * Math.PI;

            return angle;
        }
        private void _writeGoPlaneZ(float[] pt)
        {
            Console.WriteLine($"[GCodeWriter] Writing Z postion of Work Plane : Z={pt[0]}");
            _fs.WriteLine("G0 Z{0}",  pt[0].ToString(_nfi));
            _lastPt = pt;
        }

        private void _writeGoPoint(float[] pt)
        {
            if (pt.Length == 3)
            {
                Console.WriteLine($"[GCodeWriter] Writing 3D point: X={pt[0]} Y={pt[1]} Z={pt[2]}");
                _fs.WriteLine("G0 X{0} Y{1} Z{2}", pt[0].ToString(_nfi), pt[1].ToString(_nfi), pt[2].ToString(_nfi));
                _lastPt = pt;
            }
            else if (pt.Length == 2)
            {
                Console.WriteLine($"[GCodeWriter] Writing 2D point: X={pt[0]} Y={pt[1]}");
                _fs.WriteLine("G0 X{0} Y{1}", pt[0].ToString(_nfi), pt[1].ToString(_nfi));
                _lastPt = pt;
            }
            else
            {
                throw new InvalidDataException("Point needs to contain 2 or 3 values");
            }
        }

        private void _writeGoLine(float[] pt)
        {
            if (pt.Length == 3)
            {
                Console.WriteLine($"[GCodeWriter] Writing 3D point: X={pt[0]} Y={pt[1]} Z={pt[2]}");
                _fs.WriteLine("G1 X{0} Y{1} Z{2}", pt[0].ToString(_nfi), pt[1].ToString(_nfi), pt[2].ToString(_nfi));
                _lastPt = pt;
            }
            else if (pt.Length == 2)
            {
                Console.WriteLine($"[GCodeWriter] Writing 2D point: X={pt[0]} Y={pt[1]}");
                _fs.WriteLine("G1 X{0} Y{1}", pt[0].ToString(_nfi), pt[1].ToString(_nfi));
                _lastPt = pt;
            }
            else
            {
                throw new InvalidDataException("Point needs to contain 2 or 3 values");
            }
        }



        public override void SimpleJobWrite(Job job, string filename, IFileReaderWriterProgress progress = null)
        {
            CheckConsistence(job.NumWorkPlanes, job.WorkPlanes.Count);
            for (int i = 0; i < job.NumWorkPlanes; i++)
            {
                CheckConsistence(job.WorkPlanes[i].NumBlocks, job.WorkPlanes[i].VectorBlocks.Count);
            }
            _jobShell = job;
            _fileOperationInProgress = FileWriteOperation.CompleteWrite;
            _fs = new StreamWriter(filename);
            this.filename = filename;
            this.progress = progress;
            foreach (WorkPlane wp in job.WorkPlanes)
            {
                AppendWorkPlane(wp);
            }
            _fs.Close();
            _fileOperationInProgress = FileWriteOperation.None;
        }

        

        /// <inheritdoc/>
        public override void StartWritePartial(Job jobShell, string filename, IFileReaderWriterProgress progress = null)
        {
            _jobShell = jobShell;
            _fileOperationInProgress = FileWriteOperation.PartialWrite;
            _fs = new StreamWriter(filename);
            this.filename = filename;
            this.progress = progress;
        }

        private void CheckConsistence(int number1, int number2)
        {
            if (number1 != number2)
            {
                Dispose();
                throw new IOException("inconsistence in file detected");
            }
        }
    }
}

