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
using OpenVectorFormat.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;

namespace OpenVectorFormat.EOSReaderWriter
{
    public class EOSFileReader : FileReader
    {
        // --- Class variables for parsing ---
        // Adjust as needed for final converter.
        private WorkPlane _workPlane;
        private VectorBlock _currentVectorBlock;
        private CacheState _cacheState = CacheState.NotCached;
        private string _filename;

        /// <inheritdoc/>
        public new static List<string> SupportedFileFormats { get; } = new List<string>() { ".openjz", ".evb" };

        /// <inheritdoc/>
        public override CacheState CacheState => _cacheState;

        public Job CompleteJob { get; private set; }

        // --- EVB specific class variables ---
        // Adjust for openjz later and delete the ones below in final converter.
        private BinaryReader _evbReader;
        private readonly Dictionary<uint, (ulong Offset, uint Count)> _evbTable = new Dictionary<uint, (ulong Offset, uint Count)>();
        public IReadOnlyList<uint> EVBLayerIndices => _evbTable.Keys.OrderBy(k => k).ToList();
        public int EVBNumLayers => _evbTable.Count;
        public uint EVBVectorCount(uint layer) => _evbTable[layer].Count;
        public sealed class VectorData
        {
            public uint LayerIndex;
            public double StartX, StartY, EndX, EndY;
            public ulong ExposureDataIndex;
            public double TimestampUs;
            public int? PartId;
            public int? ExposureType;
            public double? LaserPowerW;
            public byte? LaserScannerIndex;
            public double? ScannerSpeedMmPerS;
            public double? PulsedWavePeriodTimeUs;
            public double? PulsedWavePowerOnTimeUs;
            public double? PulsedWavePowerOnDelayUs;
        }

        // --- Inerited abstract methods ---
        // Need to be implemented.
        public override Job JobShell
        {
            get
            {
                if (_cacheState == CacheState.CompleteJobCached)
                {
                    Job jobShell = new Job();
                    ProtoUtils.CopyWithExclude(CompleteJob, jobShell, new List<int> { Job.WorkPlanesFieldNumber });
                    return jobShell;
                }
                else
                {
                    throw new InvalidDataException("No data loaded yet! Call OpenJobAsync first!");
                }
            }
        }

        public override Job CacheJobToMemory()
        {
            throw new NotImplementedException();
        }

        public override void Dispose()
        {
            UnloadJobFromMemory();
        }

        public override VectorBlock GetVectorBlock(int i_workPlane, int i_vectorblock)
        {
            throw new NotImplementedException();
        }

        public override WorkPlane GetWorkPlane(int i_workPlane)
        {
            throw new NotImplementedException();
        }

        public override WorkPlane GetWorkPlaneShell(int i_workPlane)
        {
            throw new NotImplementedException();
        }

        public override void OpenJob(string filename, IFileReaderWriterProgress progress = null)
        {
            string fileExtension = Path.GetExtension(filename);
            if (!SupportedFileFormats.Contains(fileExtension))
            {
                throw new Exception(fileExtension + " is not supported by ASPFileReader. Supported formats are: " + string.Join(";", SupportedFileFormats));
            }
            CompleteJob = new Job();
            DateTime fileCreationTime = File.GetCreationTime(filename);
            CompleteJob.JobMetaData = new Job.Types.JobMetaData
            {
                JobCreationTime = 0,
                JobName = Path.GetFileNameWithoutExtension(filename)
            };

            _filename = filename;

            if (fileExtension == ".evb")
            {
                _evbReader = new BinaryReader(File.Open(filename, FileMode.Open, FileAccess.Read));
                ReadEVBHeader();

                Dictionary<uint, List<VectorData>> vectors = ReadAllEVBLayers();
            }
            else if (fileExtension == ".openjz")
            {
                throw new NotImplementedException();
            }
        }

        public override void UnloadJobFromMemory()
        {
            CompleteJob = null;
            _cacheState = CacheState.NotCached;
        }

        // --- EVB specific methods ---
        // Implement openjz reader (using EOS SDK C#-wrappers) instead and delete below in final converter.
        private void ReadEVBHeader()
        {
            var magic = _evbReader.ReadBytes(8);
            if (!magic.SequenceEqual(new byte[] { 0x45, 0x4F, 0x53, 0x56, 0x45, 0x43, 0x01, 0x00 }))
                throw new InvalidDataException("Not an .evb file (bad header bytes).");

            ushort version = _evbReader.ReadUInt16();
            if (version != 1)
                throw new InvalidDataException($"Unsupported format version {version}.");

            uint numLayers = _evbReader.ReadUInt32();
            _evbReader.ReadBytes(2);

            for (int i = 0; i < numLayers; i++)
            {
                uint layerIndex = _evbReader.ReadUInt32();
                uint vectorCount = _evbReader.ReadUInt32();
                ulong byteOffset = _evbReader.ReadUInt64();
                _evbTable[layerIndex] = (byteOffset, vectorCount);
            }
        }

        public List<VectorData> ReadEVBLayer(uint layer)
        {
            if (!_evbTable.TryGetValue(layer, out var entry))
                throw new KeyNotFoundException($"Layer {layer} not found.");

            var result = new List<VectorData>((int)entry.Count);
            _evbReader.BaseStream.Seek((long)entry.Offset, SeekOrigin.Begin);
            for (uint i = 0; i < entry.Count; i++)
            {
                var v = new VectorData
                {
                    LayerIndex = _evbReader.ReadUInt32(),
                    StartX = _evbReader.ReadDouble(),
                    StartY = _evbReader.ReadDouble(),
                    EndX = _evbReader.ReadDouble(),
                    EndY = _evbReader.ReadDouble(),
                    ExposureDataIndex = _evbReader.ReadUInt64(),
                    TimestampUs = _evbReader.ReadDouble(),
                };

                int partId = _evbReader.ReadInt32();
                int exposureType = _evbReader.ReadInt32();
                double laserPower = _evbReader.ReadDouble();
                byte scannerIndex = _evbReader.ReadByte();
                _evbReader.ReadBytes(7);
                double scanSpeed = _evbReader.ReadDouble();
                double pwPeriod = _evbReader.ReadDouble();
                double pwOnTime = _evbReader.ReadDouble();
                double pwOnDelay = _evbReader.ReadDouble();

                v.PartId = partId == -1 ? (int?)null : partId;
                v.ExposureType = exposureType == -1 ? (int?)null : exposureType;
                v.LaserPowerW = double.IsNaN(laserPower) ? (double?)null : laserPower;
                v.LaserScannerIndex = scannerIndex == 255 ? (byte?)null : scannerIndex;
                v.ScannerSpeedMmPerS = double.IsNaN(scanSpeed) ? (double?)null : scanSpeed;
                v.PulsedWavePeriodTimeUs = double.IsNaN(pwPeriod) ? (double?)null : pwPeriod;
                v.PulsedWavePowerOnTimeUs = double.IsNaN(pwOnTime) ? (double?)null : pwOnTime;
                v.PulsedWavePowerOnDelayUs = double.IsNaN(pwOnDelay) ? (double?)null : pwOnDelay;

                result.Add(v);
            }
            return result;
        }

        public Dictionary<uint, List<VectorData>> ReadAllEVBLayers()
        => _evbTable.Keys.ToDictionary(li => li, ReadEVBLayer);
    }
}
