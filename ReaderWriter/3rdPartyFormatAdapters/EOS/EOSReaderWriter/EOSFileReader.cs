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

using Google.Protobuf;
using OpenVectorFormat;
using OpenVectorFormat.AbstractReaderWriter;
using OpenVectorFormat.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;

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

        // --- Inherited abstract methods ---
        // Need to / must be implemented.
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
                    throw new InvalidDataException("No data loaded yet! Call OpenJob first!");
                }
            }
        }

        public override Job CacheJobToMemory()
        {
            if (_cacheState == CacheState.CompleteJobCached)
            {
                return CompleteJob;
            }
            else if (File.Exists(_filename))
            {
                return CompleteJob;
            }
            else
            {
                throw new InvalidDataException("No data loaded yet! Call OpenJob first!");
            }
        }

        public override void Dispose()
        {
            UnloadJobFromMemory();
        }

        public override VectorBlock GetVectorBlock(int i_workPlane, int i_vectorblock)
        {
            if (_cacheState == CacheState.CompleteJobCached)
            {
                return CompleteJob.WorkPlanes[i_workPlane].VectorBlocks[i_vectorblock];
            }
            else
            {
                throw new InvalidDataException("No data loaded yet! Call OpenJob first!");
            }
        }

        public override WorkPlane GetWorkPlane(int i_workPlane)
        {
            if (CompleteJob.NumWorkPlanes < i_workPlane)
            {
                throw new ArgumentOutOfRangeException("i_workPlane " + i_workPlane.ToString() + " out of range for jobfile with " + CompleteJob.NumWorkPlanes.ToString() + " workPlanes!");
            }

            if (_cacheState == CacheState.CompleteJobCached)
            {
                return CompleteJob.WorkPlanes[i_workPlane];
            }
            else
            {
                throw new InvalidDataException("No data loaded yet! Call OpenJob first!");
            }
        }

        public override WorkPlane GetWorkPlaneShell(int i_workPlane)
        {
            if (CompleteJob.NumWorkPlanes < i_workPlane)
            {
                throw new ArgumentOutOfRangeException("i_workPlane " + i_workPlane.ToString() + " out of range for jobfile with " + CompleteJob.NumWorkPlanes.ToString() + " workPlanes!");
            }

            if (CacheState == CacheState.CompleteJobCached)
            {
                return CompleteJob.WorkPlanes[i_workPlane].CloneWithoutVectorData();
            }
            else
            {
                throw new InvalidDataException("No data loaded yet! Call OpenJob first!");
            }
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
                MarkingParams markParams = new MarkingParams();
                VectorData firstVector = vectors.First().Value.First();
                int? exposureType = firstVector.ExposureType;
                if (firstVector.LaserPowerW == null || firstVector.LaserScannerIndex == 255 || firstVector.LaserScannerIndex == null || firstVector.ExposureType == -1 || firstVector.ExposureType == null)
                    throw new InvalidDataException("The first Vector cannot have placeholder values");

                int?[] hatchExposureTypes = { 1, 2, 3, 7, 8 };
                int?[] contourExposureTypes = { 4, 5, 6, 10 };

                // go through each layer
                foreach (var paar in vectors)
                {
                    List<VectorData> vectorList = paar.Value;

                    _workPlane = new WorkPlane();
                    _currentVectorBlock = new VectorBlock();

                    // set up the first vectorblock of each layer
                    double? tmp = vectorList.First().LaserPowerW;
                    if (tmp != null) markParams.LaserPowerInW = (float)tmp;
                    tmp = vectorList.First().ScannerSpeedMmPerS;
                    if (tmp != null) markParams.LaserSpeedInMmPerS = (float)tmp;

                    if (!CompleteJob.MarkingParamsMap.ContainsKey((int)paar.Key))
                        CompleteJob.MarkingParamsMap.Add((int)paar.Key, markParams);
                    
                    // go through each Vector
                    for (int i = 0; i < vectorList.Count; i++)
                    {
                        VectorData v = vectorList[i];

                        if (vectorList.First().ExposureType != -1 && vectorList.First().ExposureType != null)
                            exposureType = v.ExposureType;

                        if (v.LaserScannerIndex != _currentVectorBlock.LaserIndex || v.LaserPowerW != markParams.LaserPowerInW || v.ExposureType == 11) // ExposureType 11 = jump vector
                        {   // start a new Vectorblock
                            _workPlane.VectorBlocks.Add(_currentVectorBlock);
                            int? b = v.LaserScannerIndex;
                            if (b == null || b == 255) b = _currentVectorBlock.LaserIndex;
                            _currentVectorBlock = new VectorBlock();
                            _currentVectorBlock.LaserIndex = b.Value;
                        }

                        if (hatchExposureTypes.Contains(exposureType))
                        {
                            if (_currentVectorBlock.Hatches == null) _currentVectorBlock.Hatches = new VectorBlock.Types.Hatches();
                            _currentVectorBlock.Hatches.Points.Add((float)v.StartX);
                            _currentVectorBlock.Hatches.Points.Add((float)v.StartY);
                            _currentVectorBlock.Hatches.Points.Add((float)v.EndX);
                            _currentVectorBlock.Hatches.Points.Add((float)v.EndY);
                        }
                        if (contourExposureTypes.Contains(exposureType))
                        {
                            if (_currentVectorBlock.LineSequence == null) _currentVectorBlock.LineSequence = new VectorBlock.Types.LineSequence();
                            _currentVectorBlock.LineSequence.Points.Add((float)v.StartX);
                            _currentVectorBlock.LineSequence.Points.Add((float)v.StartY);
                            _currentVectorBlock.LineSequence.Points.Add((float)v.EndX);
                            _currentVectorBlock.LineSequence.Points.Add((float)v.EndY);
                        }
                    }
                    CompleteJob.WorkPlanes.Add(_workPlane);
                }
            }
            else if (fileExtension == ".openjz")
            {
                try
                {
                    string dir = Directory.GetCurrentDirectory()+"\\temp";
                    Wrap.EosError[] error = new Wrap.EosError[8];
                    error[0] = Wrap.Eos_InitializeApi("V:\\Transfer\\DAP_TRANSFER\\Erb\\EOS\\EOSPRINT 2_11 SDK\\bin\\x64\\release\\EosprintApi.log.config");
                    error[1] = Wrap.EosTaskGen_LoadMachineConfiguration("V:\\Transfer\\DAP_TRANSFER\\Erb\\EOS\\EOSPRINT 2_11 SDK\\DefaultMachineConfigs\\M291");
                    error[2] = Wrap.EosTaskGen_LoadOpenJz(Wrap.DllName, null);
                    error[3] = Wrap.EosTaskGen_BeginPreviewCreation(1/*, EOS_VECTORTYPES v*/);
                    error[4] = Wrap.EosTaskGen_WaitForPreviewCreation(/*EOS_INFINITE_DURATION*/);
                    error[5] = Wrap.EosTaskGen_GetPreviewCreationResult();
                    bool debugBool = false;
                    error[6] = Wrap.EosTaskGen_GetPreviewData2(ref debugBool, ref debugBool); //* pointers
                    error[7] = Wrap.Eos_DeinitializeApi();
                    // delete the temporary directory
                    if (error[2] == Wrap.EosError.EOS_ERR_NO_ERROR) Directory.Delete(dir, true);
                    //* debug
                    for (int i = 0; i < error.Length; i++)
                        if (error[i] != Wrap.EosError.EOS_ERR_NO_ERROR) 
                            throw new Exception("Die Funktion an Stelle " + i + " hat den Fehlercode " + error[i] + " ausgegeben");
                }
                catch (Exception e)
                {
                    Wrap.Eos_DeinitializeApi();
                    throw e;
                }
                /*using(ZipArchive archive = new ZipArchive(new FileStream(filename, FileMode.Open, FileAccess.Read)))
                {
                    var test = archive.Entries.Where(file => file.Name.Length > 8);
                    var test2 = test.Where(file => file.Name.Substring((int)file.Length - 8).Equals(".openjob"));
                    ZipArchiveEntry openjobFile = archive.Entries.Where(file => file.Name.Length > 8).Where(file => file.Name.Substring((int)file.Length - 8).Equals(".openjob")).First();
                }*/
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

        public static class Wrap
        {
            internal const string DllName = "V:\\Transfer\\DAP_TRANSFER\\Erb\\EOS\\EOSPRINT 2_11 SDK\\bin\\x64\\release\\EosprintApi.dll";

            public enum EosError
            {
                /* Everything went well */
                EOS_ERR_NO_ERROR = 1,

                /* An invalid argument was passed, e.g., a null pointer where not allowed */
                EOS_ERR_ARGUMENT_INVALID = 2,

                /* A string could not be converted to another encoding */
                EOS_ERR_ENCODING_CONVERSION_ERROR = 3,

                /* A string contains illegal characters. E.g., characters not in codepage Windows-1252 are illegal for task creation in general. In addition, non-ASCII characters are illegal for M100/P120 tasks. */
                EOS_ERR_STRING_CONTAINS_ILLEGAL_CHARS = 4,

                /* A file path contains illegal characters. Generally all characters not in codepage Windows-1252 are illegal in file paths. For M100/P120 systems all non-ASCII characters are illegal. */
                EOS_ERR_PATH_CONTAINS_ILLEGAL_CHARS = 5,

                /* A required license is missing */
                EOS_ERR_MISSING_LICENSE = 6,

                /* Empty, nothing to get */
                EOS_ERR_EMPTY = 7,

                /* Nothing found, no match */
                EOS_ERR_NO_MATCH = 8,

                /* The given path is not a directory (e.g., because it does not exist or because it's a file) */
                EOS_ERR_NOT_A_DIRECTORY = 9,

                /* The specified file was not found */
                EOS_ERR_FILE_NOT_FOUND = 10,

                /* The extension is invalid; e.g. extension does not fit to the requirements (not: file not found) */
                EOS_ERR_ILLEGAL_FILE_EXTENSION = 11,

                /* A file system level error occurred, e.g., because of missing access rights (not: file not found), disk out of space  */
                EOS_ERR_FILESYSTEM_IO_ERROR = 12,

                /* A network level error occurred, e.g., because missing network, missing server access rights, wrong network address */
                EOS_ERR_NETWORK_IO_ERROR = 13,

                /* An operation could not be performed because memory allocating failed */
                EOS_ERR_OUT_OF_MEMORY = 14,

                /* A machine type is not supported */
                EOS_ERR_MACHINE_NOT_SUPPORTED = 15,

                /* An operation could not be performed, because the subject is (already) in idle state; e.g. aborting something which is not running */
                EOS_ERR_IDLE = 16,

                /* An operation could not be performed, because a competing operation is already running */
                EOS_ERR_NOT_IDLE = 17,

                /* An invalid file transfer handle was passed */
                EOS_ERR_FILETRANSFER_INVALID = 18,

                /* A file transfer is running or pending (queued) */
                EOS_ERR_FILETRANSFER_RUNNING = 19,

                /* No running or queued file transfer (nothing to abort)*/
                EOS_ERR_FILETRANSFER_NOT_RUNNING = 20,

                /* The file transfer was aborted by the user */
                EOS_ERR_FILETRANSFER_ABORTED = 21,

                /* The file transfer failed, e.g., caused by transmission failure, protocol failure, non-gracefully disconnection */
                EOS_ERR_FILETRANSFER_FAILED = 22,

                /* An invalid session handle was passed */
                EOS_ERR_SESSION_INVALID = 23,

                /* The maximum of simultaneously allowed sessions was already reached */
                EOS_ERR_TOO_MANY_SESSIONS = 24,

                /* A build time estimation is running or pending */
                EOS_ERR_BUILDTIME_ESTIMATION_RUNNING = 25,

                /* No running build time estimation (nothing to abort)*/
                EOS_ERR_BUILDTIME_ESTIMATION_NOT_RUNNING = 26,

                /* The build time estimation was aborted by the user */
                EOS_ERR_BUILDTIME_ESTIMATION_ABORTED = 27,

                /* A preview is running or pending */
                EOS_ERR_PREVIEW_RUNNING = 28,

                /* No running preview (nothing to abort)*/
                EOS_ERR_PREVIEW_NOT_RUNNING = 29,

                /* The preview was aborted by the user */
                EOS_ERR_PREVIEW_ABORTED = 30,

                /* A non-existent layer index was specified */
                EOS_ERR_LAYER_INVALID = 31,

                // Deprecated: A Non-existent layer index was specified. Now replaced by EOS_ERR_LAYER_INVALID. */
                EOS_ERR_PREVIEW_LAYER_INVALID = EOS_ERR_LAYER_INVALID,

                /* An invalid z-height was specified */
                EOS_ERR_PREVIEW_ZHEIGHT_INVALID = 32,

                /* A task creation is running or pending (queued) */
                EOS_ERR_TASK_CREATION_RUNNING = 33,

                /* No running task creation (nothing to abort)*/
                EOS_ERR_TASK_CREATION_NOT_RUNNING = 34,

                /* The task creation was aborted by the user */
                EOS_ERR_TASK_CREATION_ABORTED = 35,

                /* Some information could not be written to the task */
                EOS_ERR_TASK_WRITE_FAILED = 36,

                /* The task is invalid, e.g., because it contains invalid/inconsistent data */
                EOS_ERR_TASK_INVALID = 37,

                /* The task is corrupt, i.e. it cannot be read, because its format is completely unrecognized */
                EOS_ERR_TASK_CORRUPT = 38,

                /* No job loaded; operations which need a loaded job could not be performed */
                EOS_ERR_JOB_NOT_LOADED = 39,

                /* No machine configuration loaded; operations which need a loaded machine configuration could not be performed */
                EOS_ERR_CONFIG_NOT_LOADED = 40,

                /* An operation could not be performed, e.g., because machine configuration is invalid: serial number differs from machine or outdated or contains inconsistent data */
                EOS_ERR_CONFIG_INVALID = 41,

                /* The machine configuration is corrupt, e.g., because files cannot be opened or because their format is not recognized */
                EOS_ERR_CONFIG_CORRUPT = 42,

                /* One of the required files in the configuration directory is missing */
                EOS_ERR_ILLEGAL_CONFIG_DIR = 43,

                /* The OpenJob was not found */
                EOS_ERR_OPENJOB_NOT_FOUND = 44,

                /* The OpenJob is of a not supported version */
                EOS_ERR_OPENJOB_NOT_SUPPORTED = 45,

                /* The OpenJob contains invalid data, e.g., a reference to an undefined positioning point */
                EOS_ERR_OPENJOB_INVALID = 46,

                /* The OpenJob is corrupt, i.e. its format is completely unrecognized (e.g., not an XML or not well-formed) */
                EOS_ERR_OPENJOB_CORRUPT = 47,

                /* The OpenJz is invalid, e.g., because it can be opened and extracted, but its internal file structure is unrecognized */
                EOS_ERR_OPENJZ_INVALID = 48,

                /* The OpenJz is corrupt, i.e. it cannot be extracted */
                EOS_ERR_OPENJZ_CORRUPT = 49,

                /* The .eospar file was not found */
                EOS_ERR_EOSPAR_NOT_FOUND = 50,

                /* The .eospar file is of a not supported version */
                EOS_ERR_EOSPAR_UNSUPPORTED = 51,

                /* The .eospar file contains invalid/inconsistent data */
                EOS_ERR_EOSPAR_INVALID = 52,

                /* The .eospar file cannot be read, because its format is completely unrecognized */
                EOS_ERR_EOSPAR_CORRUPT = 53,

                /* A directory that was expected to be empty was not */
                EOS_ERR_DIRECTORY_NOT_EMPTY = 54,

                /* Waiting timed out */
                EOS_ERR_WAIT_TIMED_OUT = 55,

                /* The provided code page is invalid */
                EOS_ERR_INVALID_CODE_PAGE = 56,

                /* A string was not correctly encoded as unicode */
                EOS_ERR_INCORRECT_ENCODING = 57,

                /* A specified part file was not found; e.g., a referenced part file in an openjob */
                EOS_ERR_PART_FILE_NOT_FOUND = 58,

                /* The machine software version does either not fit the minimum requirements for a given machine or is too high */
                EOS_ERR_MACHINE_VERSION_NOT_SUPPORTED = 59,

                /* An invalid file transfer handle was passed */
                EOS_ERR_FILECREATION_INVALID = 60,

                /* No running or queued file creation (nothing to abort)*/
                EOS_ERR_FILECREATION_NOT_RUNNING = 61,

                /* The file creation was aborted by the user */
                EOS_ERR_FILECREATION_ABORTED = 62,

                /* The given stl file cannot be loaded */
                EOS_ERR_STL_INVALID = 63,

                /* The requested deformation operation on a mesh could not be performed */
                EOS_ERR_DEFORMATION_ERROR = 64,

                /* A numerical input value was outside the domain of an operation */
                EOS_ERR_DOMAIN_ERROR = 65,

                /* An overflow occured while processing the result of an operation */
                EOS_ERR_OVERFLOW_ERROR = 66,

                /* The given stl file is (partially) below the platform, i.e. it contains negative z coordinates */
                EOS_ERR_STL_BELOW_PLATFORM = 67,

                /* The given stl file is (partially) outside the build volume of the machine */
                EOS_ERR_STL_OUTSIDE_BUILD_VOLUME = 68,

                /* The given stl file is (partially) above the given job height */
                EOS_ERR_STL_ABOVE_JOB_HEIGHT = 69,

                /* The given stl file contains negative coordinates, which is not supported by the requested action */
                EOS_ERR_STL_CONTAINS_NEGATIVE_COORDINATES = 70,

                /* An invalid part id was supplied */
                EOS_ERR_INVALID_PART_ID = 71,

                /* An invalid Exposure Set name was supplied */
                EOS_ERR_INVALID_EXPOSURE_SET_NAME = 72,

                /* A specific feature is used by the job, but the target machine does not support it */
                EOS_ERR_FEATURE_NOT_SUPPORTED = 73,

                /* The version of the task or one of its dependencies is not supported for this feature */
                EOS_ERR_TASK_UNSUPPORTED_VERSION = 74,

                /* After applying one or several adjustments (job- or machine-specific) to an adjustable building process parameter
                    its value now violates the specified min/max threshold */
                EOS_ERR_ADJUSTED_VALUE_VIOLATES_THRESHOLD = 75,

                /* The currently loaded material set does not contain an adjustable process parameter with the specified id */
                EOS_ERR_ADJUSTMENT_ID_INVALID = 76,

                /* The specified adjustment type is not enabled for the specified adjustable process parameter.
                    (e.g., when trying to get a job specific adjustment for an adjustable process parameter that only allows machine specific adjustments) */
                EOS_ERR_ADJUSTMENT_TYPE_NOT_ENABLED = 77,

                /* The specified adjustment was valid, but it is not supported by the machine to which it applies, because the machine
                    is running an old version of EOSYSTEM. */
                EOS_ERR_ADJUSTMENT_NOT_SUPPORTED_BY_MACHINE = 78,

                /* The specified adjustment cannot be applied because the EOSPRINT API only allows adjustment of process parameters that have an
                    effect on exposure. */
                EOS_ERR_ADJUSTMENT_HAS_NO_EFFECT_ON_EXPOSURE = 79,

                /* The requested operation is not (yet) implemented */
                EOS_ERR_NOT_IMPLEMENTED = 80,

                /* A necessary DLL could not be loaded, because it was not found or because necessary licenses are missing */
                EOS_ERR_LOADING_DLL_FAILED = 81,

                /* A constraint has been violated */
                EOS_ERR_CONSTRAINT_VIOLATED = 82,

                /* Defocus has been requested but is not supported by the machine */
                EOS_ERR_DEFOCUS_NOT_SUPPORTED = 83,

                /* A protocol required to run smoothly did not complete successfully. */
                EOS_ERR_PROTOCOL_ERROR = 84,

                /* A file creation is running or pending (queued) */
                EOS_ERR_FILECREATION_RUNNING = 85,

                /* A file handle was supplied but was found to be invalid */
                EOS_ERR_FILE_HANDLE_INVALID = 86,

                /* Requested layer does not exist */
                EOS_ERR_INVALID_LAYER = 87,

                /* A faulty mesh caused an unfixable error during slicing */
                EOS_ERR_FAULTY_TRIANGLE_MESH = 88,

                /* An internal error state was reached, please consider reporting this to EOS support */
                EOS_ERR_INTERNAL_ERROR = 32766,

                /* An unknown exception was caught, please consider reporting this to EOS support */
                EOS_ERR_UNEXPECTED_ERROR = 32767
            }

            [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
            public static extern EosError Eos_InitializeApi(string logPath = null);

            [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
            public static extern EosError Eos_DeinitializeApi();

            [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
            public static extern EosError EosTaskGen_LoadOpenJz(string pathToOpenJz, string tempExtractPath);

            [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
            public static extern EosError EosTaskGen_LoadMachineConfiguration(string configPath);

            [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
            public static extern EosError EosTaskGen_BeginPreviewCreation(uint layerIndex/*, Vectortypes vectorTypes*/);
            //* layerIndex fängt bei 1 an 

            [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
            public static extern EosError EosTaskGen_WaitForPreviewCreation(double millisecWaitTime = double.PositiveInfinity);

            [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
            public static extern EosError EosTaskGen_GetPreviewCreationResult();

            [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
            public static extern EosError EosTaskGen_GetPreviewData2(ref bool previewData, ref bool exposureData);
            //* in C: GetPreviewData(EOS_EXPOSURE_VECTOR_ARRAY const **previewData, EOS_EXPOSURE_DATA_ARRAY const **exposureData)
        }
    }
}