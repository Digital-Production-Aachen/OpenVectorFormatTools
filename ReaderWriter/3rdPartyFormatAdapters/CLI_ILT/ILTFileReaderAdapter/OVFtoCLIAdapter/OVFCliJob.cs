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
using OpenVectorFormat;
using OpenVectorFormat.ILTFileReader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ILTFileReaderAdapter.OVFToCLIAdapter
{
    internal class OVFCliJob : IGeometry, IHeader, ICLIFile
    {
        private Job job;

        public OVFCliJob(Job job)
        {
            this.job = job ?? throw new ArgumentNullException(nameof(job));
        }

        public IList<ILayer> Layers
        {
            get
            {
                var ret = new List<ILayer>(job.WorkPlanes.Count);
                foreach (var layer in job.WorkPlanes)
                {
                    ret.Add(new OVFCliLayer(layer));
                }
                return ret;
            }
        }

        public DataFormatType DataFormat => DataFormatType.binary;

        public int Date {
            get
            {
                var dateTime = job.JobMetaData != null ? DateTimeOffset.FromUnixTimeSeconds(job.JobMetaData.JobCreationTime) : DateTimeOffset.Now;
                int result = dateTime.Day * 10000 + dateTime.Month * 100 + (dateTime.Year - 2000);
                return result;
            }
        }

        public IDimension Dimension => CalcBounds();

        public int NumLayers => job.WorkPlanes?.Count > 0 ? job.WorkPlanes.Count : job.NumWorkPlanes;

        public float Units { get; set; }

        public UserData[] UserData { 
            get
            {
                var userData = new UserData();
                userData.Data = job.JobMetaData.JobName;
                userData.UID = "JobName";

                return new UserData[1]{userData};
            }
        }

        public int Version => 0;

        public IGeometry Geometry => this;

        public IHeader Header => this;

        public IList<IPart> Parts
        {
            get
            {
                var ret = new List<IPart>();
                foreach(var part in job.PartsMap)
                {
                    ret.Add(new ILTFileReader.OVFToCLIAdapter.Part() { id = part.Key, name = part.Value.Name });
                }
                return ret;
            }
        }

        private IDimension CalcBounds()
        {
            if (job.JobMetaData == null) job.JobMetaData = new Job.Types.JobMetaData();
            if (job.JobMetaData.Bounds == null) job.JobMetaData.Bounds = job.Bounds2D();
            var aabb = job.JobMetaData.Bounds;

            var zPos = 0.0f;
            if (job.WorkPlanes.Count > 0)
            {
                zPos = job.WorkPlanes.Last().ZPosInMm;
            }

            return new Dimension(0, zPos, aabb.XMin, aabb.XMax, aabb.YMin, aabb.YMax);
        }

    }
}
