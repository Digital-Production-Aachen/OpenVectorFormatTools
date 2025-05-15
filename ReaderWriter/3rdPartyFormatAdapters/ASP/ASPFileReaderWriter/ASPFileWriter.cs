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



using OpenVectorFormat.AbstractReaderWriter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Globalization;
using System.Linq;

namespace OpenVectorFormat.ASPFileReaderWriter
{
    public class ASPFileWriter : FileWriter
    {
        private IFileReaderWriterProgress progress;
        private string filename;
        private StreamWriter _fs;

        /// <inheritdoc/>
        public override Job JobShell { get { return _jobShell; } }
        private Job _jobShell;
        float[] _lastPt = null;


        private NumberFormatInfo _nfi = new NumberFormatInfo();

        public ASPFileWriter()
        {
            _nfi.NumberDecimalSeparator = ".";
        }

        /// <inheritdoc/>
        public override FileWriteOperation FileOperationInProgress { get { return _fileOperationInProgress; } }
        private FileWriteOperation _fileOperationInProgress = FileWriteOperation.None;

        private MarkingParams _lastWrittenParams = null;

        public new static List<string> SupportedFileFormats { get; } = new List<string>() { ".asp" };

        /// <inheritdoc/>
        public override void AppendWorkPlane(WorkPlane workPlane)
        {
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

        private void _addVectorBlock(VectorBlock vectorBlock)
        {
            for (ulong i = 0; i < vectorBlock.Repeats + 1; i++)
            {
                MarkingParams newParams = new MarkingParams();
                if (vectorBlock.MarkingParamsKey != 0 || _jobShell.MarkingParamsMap.Count != 0)
                {
                    // When no params are provided (Map.Count == 0), ParamsKey is still 0 because it's the default value.
                    // Only process params map if one of the values is not default.
                    newParams = _jobShell.MarkingParamsMap[vectorBlock.MarkingParamsKey];
                    if (_lastWrittenParams == null || _lastWrittenParams.LaserPowerInW != newParams.LaserPowerInW)
                    {
                        _fs.WriteLine("LP" + newParams.LaserPowerInW.ToString(_nfi));
                    }
                    if (_lastWrittenParams == null ||
                        _lastWrittenParams.JumpDelayInUs != newParams.JumpDelayInUs ||
                        _lastWrittenParams.MarkDelayInUs != newParams.MarkDelayInUs ||
                        _lastWrittenParams.PolygonDelayInUs != newParams.PolygonDelayInUs)
                    {
                        _fs.WriteLine("DS"
                            + newParams.JumpDelayInUs.ToString(_nfi) + ","
                            + newParams.MarkDelayInUs.ToString(_nfi) + ","
                            + newParams.PolygonDelayInUs.ToString(_nfi));
                    }
                    if (_lastWrittenParams == null ||
                        _lastWrittenParams.LaserOnDelayInUs != newParams.LaserOnDelayInUs ||
                        _lastWrittenParams.LaserOffDelayInUs != newParams.LaserOffDelayInUs)
                    {
                        _fs.WriteLine("DL"
                            + newParams.LaserOnDelayInUs.ToString(_nfi) + ","
                            + newParams.LaserOffDelayInUs.ToString(_nfi));
                    }

                    if (_lastWrittenParams == null ||
                        _lastWrittenParams.TimeLagInUs != newParams.TimeLagInUs ||
                        _lastWrittenParams.LaserOnShiftInUs != newParams.LaserOnShiftInUs ||
                        _lastWrittenParams.NPrevInUs != newParams.NPrevInUs ||
                        _lastWrittenParams.NPostInUs != newParams.NPostInUs)
                    {
                        _fs.WriteLine("PA"
                            + newParams.TimeLagInUs.ToString(_nfi) + ","
                            + newParams.LaserOnShiftInUs.ToString(_nfi) + ","
                            + newParams.NPrevInUs.ToString(_nfi) + ","
                            + newParams.NPostInUs.ToString(_nfi));
                    }

                    if (_lastWrittenParams == null || _lastWrittenParams.Limit != newParams.Limit)
                    {
                        _fs.WriteLine("LI" + newParams.Limit.ToString(_nfi));
                    }
                    if (_lastWrittenParams == null || _lastWrittenParams.MarkingMode != newParams.MarkingMode)
                    {
                        switch (newParams.MarkingMode)
                        {
                            case MarkingParams.Types.MarkingMode.NoSky:
                                {
                                    _fs.WriteLine("MO0");
                                    break;
                                }
                            case MarkingParams.Types.MarkingMode.Sky1:
                                {
                                    _fs.WriteLine("MO1");
                                    break;
                                }
                            case MarkingParams.Types.MarkingMode.Sky2:
                                {
                                    _fs.WriteLine("MO2");
                                    break;
                                }
                            case MarkingParams.Types.MarkingMode.Sky3:
                                {
                                    _fs.WriteLine("MO3");
                                    break;
                                }
                        }
                    }
                    if (_lastWrittenParams == null || _lastWrittenParams.LaserSpeedInMmPerS != newParams.LaserSpeedInMmPerS)
                    {
                        _fs.WriteLine("VG" + newParams.LaserSpeedInMmPerS.ToString(_nfi));
                    }
                    if (_lastWrittenParams == null || _lastWrittenParams.JumpSpeedInMmS != newParams.JumpSpeedInMmS)
                    {
                        _fs.WriteLine("VJ" + newParams.JumpSpeedInMmS.ToString(_nfi));
                    }

                    _lastWrittenParams = newParams;
                }

                switch (vectorBlock.VectorDataCase)
                {
                    case VectorBlock.VectorDataOneofCase.PointSequence3D:
                        {
                            float[] newPt;
                            for (int i_point = 0; i_point < vectorBlock.PointSequence3D.Points.Count; i_point += 3)
                            {
                                newPt = new float[3] { vectorBlock.PointSequence3D.Points[i_point], vectorBlock.PointSequence3D.Points[i_point + 1], vectorBlock.PointSequence3D.Points[i_point + 2] };

                                if (_lastPt == null || !newPt.SequenceEqual(_lastPt))
                                {
                                    _writeJump(newPt);
                                }
                                _fs.WriteLine("ON" + newParams.PointExposureTimeInUs.ToString(_nfi));
                            }
                            break;
                        }
                    case VectorBlock.VectorDataOneofCase.PointSequence:
                        {
                            float[] newPt;
                            for (int i_point = 0; i_point < vectorBlock.PointSequence.Points.Count; i_point += 2)
                            {
                                newPt = new float[2] { vectorBlock.PointSequence.Points[i_point], vectorBlock.PointSequence.Points[i_point + 1] };
                                if (_lastPt == null || !newPt.SequenceEqual(_lastPt))
                                {
                                    _writeJump(newPt);
                                }
                                _fs.WriteLine("ON" + newParams.PointExposureTimeInUs.ToString(_nfi));
                            }
                            break;
                        }
                    case VectorBlock.VectorDataOneofCase.LineSequence3D:
                        {
                            float[] newPt;
                            newPt = new float[3] { vectorBlock.LineSequence3D.Points[0], vectorBlock.LineSequence3D.Points[1], vectorBlock.LineSequence3D.Points[2] };
                            if (_lastPt == null || !newPt.SequenceEqual(_lastPt))
                            {
                                _writeJump(newPt);
                            }

                            for (int i_lineSeq = 3; i_lineSeq < vectorBlock.LineSequence3D.Points.Count; i_lineSeq += 3)
                            {
                                newPt = new float[3] { vectorBlock.LineSequence3D.Points[i_lineSeq], vectorBlock.LineSequence3D.Points[i_lineSeq + 1], vectorBlock.LineSequence3D.Points[i_lineSeq + 2] };
                                _writeGo(newPt);
                            }
                            break;
                        }
                    case VectorBlock.VectorDataOneofCase.LineSequence:
                        {
                            float[] newPt;
                            newPt = new float[2] { vectorBlock.LineSequence.Points[0], vectorBlock.LineSequence.Points[1] };
                            if (_lastPt == null || !newPt.SequenceEqual(_lastPt))
                            {
                                _writeJump(newPt);
                            }

                            for (int i_lineSeq = 2; i_lineSeq < vectorBlock.LineSequence.Points.Count; i_lineSeq += 2)
                            {
                                newPt = new float[2] { vectorBlock.LineSequence.Points[i_lineSeq], vectorBlock.LineSequence.Points[i_lineSeq + 1] };
                                _writeGo(newPt);
                            }
                            break;
                        }
                    case VectorBlock.VectorDataOneofCase.Hatches3D:
                        {
                            float[] startPt;
                            float[] endPt;

                            for (int i_hatch_point = 0; i_hatch_point < vectorBlock.Hatches3D.Points.Count; i_hatch_point += 6)
                            {
                                startPt = new float[3] { vectorBlock.Hatches3D.Points[i_hatch_point], vectorBlock.Hatches3D.Points[i_hatch_point + 1], vectorBlock.Hatches3D.Points[i_hatch_point + 2] };
                                endPt = new float[3] { vectorBlock.Hatches3D.Points[i_hatch_point + 3], vectorBlock.Hatches3D.Points[i_hatch_point + 4], vectorBlock.Hatches3D.Points[i_hatch_point + 5] };
                                _writeJump(startPt);
                                _writeGo(endPt);
                            }
                            break;
                        }
                    case VectorBlock.VectorDataOneofCase.Hatches:
                        {
                            float[] startPt;
                            float[] endPt;

                            for (int i_hatch_point = 0; i_hatch_point < vectorBlock.Hatches.Points.Count; i_hatch_point += 4)
                            {
                                startPt = new float[2] { vectorBlock.Hatches.Points[i_hatch_point], vectorBlock.Hatches.Points[i_hatch_point + 1] };
                                endPt = new float[2] { vectorBlock.Hatches.Points[i_hatch_point + 2], vectorBlock.Hatches.Points[i_hatch_point + 3] };
                                _writeJump(startPt);
                                _writeGo(endPt);
                            }
                            break;
                        }
                    default:
                        {
                            throw new NotImplementedException("DataType " + vectorBlock.VectorDataCase.ToString() + " is invalid. ASP can only handle 3D Points & lines");
                        }
                }
            }
        }

        private void _writeJump(float[] pt)
        {
            if (pt.Length == 3)
            {
                _fs.WriteLine("JP{0},{1},{2}", pt[0].ToString(_nfi), pt[1].ToString(_nfi), pt[2].ToString(_nfi));
                _lastPt = pt;
            }
            else if (pt.Length == 2)
            {
                _fs.WriteLine("JP{0},{1},{2}", pt[0].ToString(_nfi), pt[1].ToString(_nfi), 0.ToString(_nfi));
                _lastPt = pt;
            }
            else
            {
                throw new InvalidDataException("Point needs to contain 2 or 3 values");
            }
        }

        private void _writeGo(float[] pt)
        {
            if (pt.Length == 3)
            {
                _fs.WriteLine("GO{0},{1},{2}", pt[0].ToString(_nfi), pt[1].ToString(_nfi), pt[2].ToString(_nfi));
                _lastPt = pt;
            }
            else if (pt.Length == 2)
            {
                _fs.WriteLine("GO{0},{1},{2}", pt[0].ToString(_nfi), pt[1].ToString(_nfi), 0.ToString(_nfi));
                _lastPt = pt;
            }
            else
            {
                throw new InvalidDataException("Point needs to contain 2 or 3 values");
            }
        }

        /// <inheritdoc/>
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