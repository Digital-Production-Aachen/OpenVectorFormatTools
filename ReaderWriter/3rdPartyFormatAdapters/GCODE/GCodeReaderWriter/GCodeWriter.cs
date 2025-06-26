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

using OpenVectorFormat;
using OpenVectorFormat.AbstractReaderWriter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using GCodeReaderWriter;
using OpenVectorFormat.OVFReaderWriter;

namespace GCodeReaderWriter
{
    public class GCodeWriter : FileWriter
    {
        private IFileReaderWriterProgress _progress;
        private string _filename;
        private StreamWriter _fs;


        public override Job JobShell { get { return _jobShell; } }
        private Job _jobShell;
        float[] _lastPt = null;
        float _currentZ;



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


        public void ProcessOVFtoGCode(OVFFileReader ovfReader, string gcodeOutputPath)
        {
            Job job = ovfReader.CacheJobToMemory();

            if (job == null)
                throw new InvalidOperationException("Failed to load job from OVF file.");
            if (job.NumWorkPlanes <= 0)
                throw new InvalidOperationException("No WorkPlanes found in job.");

            this.SimpleJobWrite(job, gcodeOutputPath);
        }



        public override void AppendWorkPlane(WorkPlane workPlane)
        {
            _currentZ = workPlane.ZPosInMm;

            for (uint repeatIndex = 0; repeatIndex < workPlane.Repeats + 1; repeatIndex++)
            {
                for (int blockIndex = 0; blockIndex < workPlane.VectorBlocks.Count; blockIndex++)
                {
                    VectorBlock block = workPlane.VectorBlocks[blockIndex];

                    bool isFirstBlockInPlane = (repeatIndex == 0 && blockIndex == 0);

                    bool injectZ = isFirstBlockInPlane &&
                           (block.VectorDataCase == VectorBlock.VectorDataOneofCase.PointSequence ||
                            block.VectorDataCase == VectorBlock.VectorDataOneofCase.Hatches ||
                            block.VectorDataCase == VectorBlock.VectorDataOneofCase.Arcs ||
                            block.VectorDataCase == VectorBlock.VectorDataOneofCase.LineSequence);

                    AddVectorBlock(block, injectZ);
                }
            }
        }

        /// <inheritdoc/>
        public override void AppendVectorBlock(VectorBlock block)
        {
            AddVectorBlock(block, false);
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            if (_fileOperationInProgress != FileWriteOperation.None)
            {
                _fs.Close();
            }
        }

        private void AddVectorBlock(VectorBlock block, bool injectZ)
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
                            for (int pointIndex = 0; pointIndex < block.PointSequence3D.Points.Count; pointIndex += 3)
                            {
                                newPt = new float[3] { block.PointSequence3D.Points[pointIndex], block.PointSequence3D.Points[pointIndex + 1], block.PointSequence3D.Points[pointIndex + 2] };

                                Console.WriteLine($"PointSequence3D: X={newPt[0]}, Y={newPt[1]}, Z={newPt[2]}");

                                if (_lastPt == null || !newPt.SequenceEqual(_lastPt))
                                {
                                    WriteGoPoint(newPt);
                                }
                            }
                            break;
                        }
                    case VectorBlock.VectorDataOneofCase.PointSequence:
                        {
                            float[] newPt;
                            for (int pointIndex = 0; pointIndex < block.PointSequence.Points.Count; pointIndex += 2)
                            {
                                if (injectZ && pointIndex == 0)
                                {
                                    newPt = new float[3] { block.PointSequence.Points[pointIndex], block.PointSequence.Points[pointIndex + 1], _currentZ };
                                    Console.WriteLine($"[PointSequence with Z] X={newPt[0]} Y={newPt[1]} Z={newPt[2]}");
                                }
                                else
                                {
                                    newPt = new float[2] { block.PointSequence.Points[pointIndex], block.PointSequence.Points[pointIndex + 1] };
                                    Console.WriteLine($"[PointSequence] X={newPt[0]} Y={newPt[1]}");
                                }

                                if (_lastPt == null || !newPt.SequenceEqual(_lastPt))
                                {
                                    WriteGoPoint(newPt);
                                }
                            }
                            break;
                        }
                    case VectorBlock.VectorDataOneofCase.Hatches3D:
                        {
                            float[] startPt;
                            float[] endPt;

                            for (int pointIndex = 0; pointIndex < block.Hatches3D.Points.Count; pointIndex += 6)
                            {
                                startPt = new float[3] { block.Hatches3D.Points[pointIndex], block.Hatches3D.Points[pointIndex + 1], block.Hatches3D.Points[pointIndex + 2] };
                                endPt = new float[3] { block.Hatches3D.Points[pointIndex + 3], block.Hatches3D.Points[pointIndex + 4], block.Hatches3D.Points[pointIndex + 5] };

                                Console.WriteLine($"[Hatches3D] Start: X={startPt[0]} Y={startPt[1]} Z={startPt[2]} | End: X={endPt[0]} Y={endPt[1]} Z={endPt[2]}");

                                WriteGoPoint(startPt);
                                WriteGoLine(endPt);
                            }
                            break;
                        }
                    case VectorBlock.VectorDataOneofCase.Hatches:
                        {
                            float[] startPt;
                            float[] endPt;

                            for (int pointIndex = 0; pointIndex < block.Hatches.Points.Count; pointIndex += 4)
                            {
                                if (injectZ && pointIndex == 0)
                                {
                                    startPt = new float[3] { block.Hatches.Points[pointIndex], block.Hatches.Points[pointIndex + 1], _currentZ };
                                }
                                else
                                {
                                    startPt = new float[2] { block.Hatches.Points[pointIndex], block.Hatches.Points[pointIndex + 1] };
                                }

                                endPt = new float[2] { block.Hatches.Points[pointIndex + 2], block.Hatches.Points[pointIndex + 3] };

                                // Console.WriteLine($"[Hatches] Start: X={startPt[0]} Y={startPt[1]} | End: X={endPt[0]} Y={endPt[1]}");
                                WriteGoPoint(startPt);
                                WriteGoLine(endPt);
                            }
                            break;
                        }
                    case VectorBlock.VectorDataOneofCase.Arcs:
                        {
                            double angle = block.Arcs.Angle;
                            float startPointX = block.Arcs.StartDx;
                            float startPointY = block.Arcs.StartDy;

                            float[] arcCenters;

                            for (int centerIndex = 0; centerIndex < block.Arcs.Centers.Count; centerIndex += 2)
                            {
                                arcCenters = new float[2] { block.Arcs.Centers[centerIndex], block.Arcs.Centers[centerIndex + 1] };

                                Console.WriteLine($"[Arcs] Center: X={arcCenters[0]} Y={arcCenters[1]} Angle={angle} StartOffset: ({startPointX}, {startPointY})");

                                WriteGoArc(arcCenters, startPointX, startPointY, angle);
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
                                WriteGoPoint(newPt);
                            }

                            for (int lineIndex = 3; lineIndex < block.LineSequence3D.Points.Count; lineIndex += 3)
                            {
                                newPt = new float[3] { block.LineSequence3D.Points[lineIndex], block.LineSequence3D.Points[lineIndex + 1], block.LineSequence3D.Points[lineIndex + 2] };

                                Console.WriteLine($"[LineSequence3D] LineTo: X={newPt[0]} Y={newPt[1]} Z={newPt[2]}");


                                WriteGoLine(newPt);
                            }
                            break;
                        }
                    case VectorBlock.VectorDataOneofCase.LineSequence:
                        {
                            float[] newPt;

                            if (injectZ)
                            {
                                newPt = new float[3] { block.LineSequence.Points[0], block.LineSequence.Points[1], _currentZ };
                                Console.WriteLine($"[LineSequence] Start: X={newPt[0]} Y={newPt[1]} Z={newPt[2]}");
                            }
                            else
                            {
                                newPt = new float[2] { block.LineSequence.Points[0], block.LineSequence.Points[1], };
                                Console.WriteLine($"[LineSequence] Start: X={newPt[0]} Y={newPt[1]}");
                            }

                            if (_lastPt == null || !newPt.SequenceEqual(_lastPt))
                            {
                                WriteGoPoint(newPt);
                            }

                            for (int lineIndex = 2; lineIndex < block.LineSequence.Points.Count; lineIndex += 2)
                            {
                                newPt = new float[2] { block.LineSequence.Points[lineIndex], block.LineSequence.Points[lineIndex + 1] };

                                Console.WriteLine($"[LineSequence] LineTo: X={newPt[0]} Y={newPt[1]}");


                                WriteGoLine(newPt);
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




        private void WriteGoArc(float[] arcCenters, float startPointX, float startPointY, double angle)
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
        private void WriteGoPlaneZ(float[] pt)
        {
            Console.WriteLine($"[GCodeWriter] Writing Z postion of Work Plane : Z={pt[0]}");
            _fs.WriteLine("G0 Z{0}", pt[0].ToString(_nfi));
            _lastPt = pt;
        }

        private void WriteGoPoint(float[] pt)
        {
            if (pt.Length == 3)
            {
                //Console.WriteLine($"[GCodeWriter] Writing 3D point: X={pt[0]} Y={pt[1]} Z={pt[2]}");
                _fs.WriteLine("G0 X{0} Y{1} Z{2}", pt[0].ToString(_nfi), pt[1].ToString(_nfi), pt[2].ToString(_nfi));
                _lastPt = pt;
            }
            else if (pt.Length == 2)
            {
                //Console.WriteLine($"[GCodeWriter] Writing 2D point: X={pt[0]} Y={pt[1]}");
                _fs.WriteLine("G0 X{0} Y{1}", pt[0].ToString(_nfi), pt[1].ToString(_nfi));
                _lastPt = pt;
            }
            else
            {
                throw new InvalidDataException("Point needs to contain 2 or 3 values");
            }
        }

        private void WriteGoLine(float[] pt)
        {
            if (pt.Length == 3)
            {
                //Console.WriteLine($"[GCodeWriter] Writing 3D point: X={pt[0]} Y={pt[1]} Z={pt[2]}");
                _fs.WriteLine("G1 X{0} Y{1} Z{2}", pt[0].ToString(_nfi), pt[1].ToString(_nfi), pt[2].ToString(_nfi));
                _lastPt = pt;
            }
            else if (pt.Length == 2)
            {
                //Console.WriteLine($"[GCodeWriter] Writing 2D point: X={pt[0]} Y={pt[1]}");
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
                int expected = job.WorkPlanes[i].NumBlocks;
                int actual = job.WorkPlanes[i].VectorBlocks.Count;

                if (expected != actual)
                {
                    //CheckConsistence(job.WorkPlanes[i].NumBlocks, job.WorkPlanes[i].VectorBlocks.Count);
                    Console.WriteLine($"[DEBUG] Inconsistency detected in WorkPlane {i}: Expected {expected}, Actual {actual}");
                    throw new IOException("inconsistence in file detected");
                }
            }
            _jobShell = job;
            _fileOperationInProgress = FileWriteOperation.CompleteWrite;
            _fs = new StreamWriter(filename);
            this._filename = filename;
            this._progress = progress;
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
            this._filename = filename;
            this._progress = progress;
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