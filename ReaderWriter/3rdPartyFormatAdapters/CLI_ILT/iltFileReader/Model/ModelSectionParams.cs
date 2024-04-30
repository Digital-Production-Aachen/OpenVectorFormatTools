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

﻿using OpenVectorFormat.ILTFileReader;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenVectorFormat.ILTFileReader.Model
{
    class ModelSectionParams:IModelSectionParams
    {
        private double exposureTime;
        private double focusShift;
        private double laserPower;
        private double laserSpeed;
        private double pointDistance;

        public ModelSectionParams(double expTime, double focusShift, double laserPow, double laserSpeed, double pointDist) {
            this.exposureTime = expTime;
            this.focusShift = focusShift;
            this.laserPower = laserPow;
            this.laserSpeed = laserSpeed;
            this.pointDistance = pointDist;

        }
        public ModelSectionParams() { }
        /// <summary>
        /// ExposureTime in µs
        /// </summary>
        public double ExposureTime
        {
            get { return this.exposureTime; }
            set { this.exposureTime = value; }
        }
        /// <summary>
        /// FocusShift in mm
        /// </summary>
        public double FocusShift
        {
            get { return this.focusShift; }
            set { this.focusShift = value; }
        }
        /// <summary>
        /// LaserPower in watt
        /// </summary>
        public double LaserPower
        {
            get { return this.laserPower; }
            set { this.laserPower = value; }
        }
        /// <summary>
        /// LaserSpeed in mm/s
        /// </summary>
        public double LaserSpeed
        {
            get { return this.laserSpeed; }
            set { this.laserSpeed = value; }
        }
        /// <summary>
        /// PointDistance in µm
        /// </summary>
        public double PointDistance
        {
            get { return this.pointDistance; }
            set { this.pointDistance = value; }
        }
    }
}
