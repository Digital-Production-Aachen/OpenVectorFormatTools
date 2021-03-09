/*
---- Copyright Start ----

This file is part of the OpenVectorFormatTools collection. This collection provides tools to facilitate the usage of the OpenVectorFormat.

Copyright (C) 2021 Digital-Production-Aachen

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

﻿using OpenVectorFormat.ILTFileReader.Controller;
using OpenVectorFormat.ILTFileReader.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenVectorFormat.ILTFileReader.Controller
{
    public class IltFileAccess : IFileAccess, IBuildJob
    {
        private Regex fileNamesRegEx;
        private StreamReader file;
        private String iltFilePath;
        private DirectoryInfo unpackPath;
        private static List<string> fileFormats = new List<string>() { ".ilt" };

        public IltFileAccess()
        {

        }

        public void OpenFile(String iltFilePath)
        {
            this.fileNamesRegEx = new Regex(@"\[(.*)\]");
            this.ModelSections = new List<IModelSection>();
            this.iltFilePath = iltFilePath;
            string tempFile = Path.GetTempFileName();
            File.Delete(tempFile);
            unpackPath = Directory.CreateDirectory(tempFile);
            ZipFile.ExtractToDirectory(iltFilePath, unpackPath.FullName);
            this.ReadIltFile(Path.Combine(unpackPath.FullName, "modelsection_param.txt"));
        }


        public void CloseFile()
        {
            file?.Close();
            if (ModelSections != null)
            {
                foreach (ModelSection modelSection in ModelSections)
                {
                    modelSection?.CloseFile();
                }
            }
        }

        /// <summary>
        /// Writes the given information from the interface to a new file.
        /// </summary>
        /// <param name="filePath">path to the file to write</param>
        /// <param name="fileToWrite">interface providing the data to write</param>
        public void WriteFile(string filePath, IBuildJob fileToWrite)
        {

        }

        public IList<IModelSection> ModelSections
        {
            get;
            set;
        }

        public static List<string> SupportedFileFormats => fileFormats;
        ~IltFileAccess()
        {
            CloseFile();
            if (this.unpackPath != null)
            {
                unpackPath.Delete(true);
            }
            this.unpackPath = null;
        }

        /// <summary>
        ///  Uses CultureInfo.InvariantCulture which is roughly the same as english. Needed to make the conversion of String to Double independend from 
        ///  current location / machine locale
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private void ReadIltFile(String filePath)
        {
            string line;
            string cliFileName;
            Match match;
            String unit;
            this.file = new StreamReader(filePath, System.Text.Encoding.Default);
            while ((line = this.file.ReadLine()) != null)
            {
                match = this.fileNamesRegEx.Match(line);
                if (match.Success)
                {
                    ModelSectionParams mParams = new ModelSectionParams();
                    cliFileName = match.Groups[1].Value;
                    for (int i = 0; i < 5; i++) // That's not nice...we maybe need to jump newlines or are some of the parameters optional?!, maybe less than 5
                    {
                        line = this.file.ReadLine();
                        match = Regex.Match(line, @"LaserSpeed\s*=\s*(\d*.?\d*)");
                        if (match.Success)
                        {
                            unit = Regex.Match(line, @"[^\s]+$").Value;
                            if (unit.Equals("mm/s"))
                            {
                                mParams.LaserSpeed = double.Parse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture);
                            }
                            else
                            {
                                throw new FormatException("LaserSpeed has to be in mm/s, but is instead in: " + unit);
                            }
                        }
                        match = Regex.Match(line, @"LaserPower\s*=\s*(\d*.?\d*)");
                        if (match.Success)
                        {
                            unit = Regex.Match(line, @"[^\s]+$").Value;
                            if (unit.Equals("watt"))
                            {
                                mParams.LaserPower = double.Parse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture);
                            }
                            else
                            {
                                throw new FormatException("LaserPower has to be in watt, but is instead in: " + unit);
                            }
                        }

                        match = Regex.Match(line, @"FocusShift\s*=\s*(\d*.?\d*)");
                        if (match.Success)
                        {
                            unit = Regex.Match(line, @"[^\s]+$").Value;
                            if (unit.Equals("mm"))
                            {
                                mParams.FocusShift = double.Parse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture);
                            }
                            else
                            {
                                throw new FormatException("FocusShift has to be in mm, but is instead in: " + unit);
                            }
                        }

                        match = Regex.Match(line, @"PointDistance\s*=\s*(\d*.?\d*)");
                        if (match.Success)
                        {
                            unit = Regex.Match(line, @"[^\s]+$").Value;
                            if (unit.Equals("µm") | unit.Equals("�m") | unit.Equals("Âµm"))
                            {
                                mParams.PointDistance = double.Parse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture);
                            }
                            else
                            {
                                throw new FormatException("PointDistance has to be in µm, but is instead in: " + unit);
                            }
                        }
                        match = Regex.Match(line, @"ExposureTime\s*=\s*(\d*.?\d*)");
                        if (match.Success)
                        {
                            unit = Regex.Match(line, @"[^\s]+$").Value;
                            if (unit.Equals("µs") | unit.Equals("�s") | unit.Equals("Âµs"))
                            {
                                mParams.ExposureTime = double.Parse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture);
                            }
                            else
                            {
                                throw new FormatException("ExposureTime has to be in µs, but is instead in: " + unit);
                            }
                        }
                    }

                    string cliFilePath = Path.Combine(unpackPath.FullName, cliFileName);
                    ModelSection modelSection = new ModelSection(cliFilePath, mParams);
                    modelSection.OpenFile(cliFilePath);
                    this.ModelSections.Add(modelSection);
                }
            }
            file.Close();
        }

    }
}
