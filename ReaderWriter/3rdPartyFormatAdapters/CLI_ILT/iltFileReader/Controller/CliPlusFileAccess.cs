using OpenVectorFormat.ILTFileReader.Controller;
using OpenVectorFormat.ILTFileReader.Model;
using OpenVectorFormat.ILTFileReader;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenVectorFormat.ILTFileReader.Controller
{
    public class CliPlusFileAccess : IFileAccess, ICLIFile
    {
        private const ushort layerStartLong = 127; // Hex 7F
        private const ushort layerStartShort = 128; // Hex 80
        private const ushort polylineStartShort = 129; // Hex 81
        private const ushort polylineStartLong = 130; // Hex 82
        private const ushort hatchStartShort = 131; //Hex 83
        private const ushort hatchStartLong = 132; //Hex 84

        private static int NumOfEolChars = 1; //Number of chars that indicate a new line (have to be added to the length of a line)
        private IList<IPart> parts;
        private IList<IUserData> userData;
        private Header header;
        private StreamReader sR;
        private BinaryReader bR;
        private long index; // index where we are at the moment, different from StreamReader.BaseStream.Position because this is actuelle a buffered reader
        private static List<string> fileFormats = new List<string>() { ".cli" };

        public float units { get; set; } = 1;

        Dictionary<int, Tuple<float, float>> MarkingParameterMap;

        public CliPlusFileAccess()
        {
            this.parts = new List<IPart>();
            this.userData = new List<IUserData>();
            this.header = new Header();
        }

        public void OpenFile(string filePath)
        {
            sR = new StreamReader(filePath);
            Header = ReadHeader();
            IList<ILayer> layers;
            switch (header.DataFormat)
            {
                case DataFormatType.binary:
                    layers = ReadBinaryContent();
                    break;
                case DataFormatType.ASCII:
                    layers = ReadASCIIContent();
                    break;
                default:
                    throw new FileLoadException("unknown format type");
            }
            Geometry = new Geometry(layers);
        }

        public enum BinaryWriteStyle
        {
            LONG,
            SHORT
        }

        /// <summary>
        /// Writes the given information from the interface to a new file.
        /// </summary>
        /// <param name="filePath">path to the file to write</param>
        /// <param name="fileToWrite">interface providing the data to write</param>
        /// <param name="layerStyle">write the layer height int 16 or float 32 bit</param>
        /// <param name="hatchesStyle">write the hatches coordinates int16 or float32 bit</param>
        /// <param name="polylineStyle">write the polyline coordinates int16 or float32 bit</param>
        /// <param name="convertUnits">if true, the coordiantes passed into the interface will be converted based on the unit given in the header. if false, no conversion is appplied but the units are still written to the header</param>
        public void WriteFile(string filePath, ICLIFile fileToWrite, Dictionary<int, Tuple<float, float>> markingParameterMap)
        {
            this.MarkingParameterMap = markingParameterMap;

            using (var sW = new StreamWriter(filePath, false, Encoding.ASCII))
            {
                WriteHeader(sW, fileToWrite);
            }
            //using (var bW = new BinaryWriter(new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.None)))
            using (var sW = new StreamWriter(new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.None), Encoding.ASCII))
            {
                var units = fileToWrite.Header.Units;
                // only commands used: long hatches/polylines (32bit number of points), long layer (z is float)
                sW.WriteLine("$$GEOMETRYSTART");
                foreach (var layer in fileToWrite.Geometry.Layers)
                {
                    AppendLayer(sW, layer);
                }

                sW.WriteLine();
                sW.WriteLine("$$GEOMETRYEND");
                sW.WriteLine();
                sW.WriteLine();
                sW.WriteLine();
                sW.WriteLine();
            }
        }

        public void CloseFile()
        {
            sR?.Close();
        }

        public static List<string> SupportedFileFormats => fileFormats;


        public IGeometry Geometry
        {
            get;
            set;
        }

        public IHeader Header
        {
            get;
            set;
        }

        public IList<IPart> Parts
        {
            get { return (IList<IPart>)this.parts; }
        }



        /// <summary>
        /// The Header is always ASCII
        /// </summary>
        /// <returns></returns>
        private IHeader ReadHeader()
        {
            String line;
            Match match;
            while ((line = this.sR.ReadLine()) != null)
            {
                if (!line.Equals("$$HEADERSTART"))
                    index += line.Length + NumOfEolChars; //if one of the first lines isn't 'headerstart' it is ignored but we still need to add the offset
                if (line.Equals("$$HEADERSTART"))
                {//Indicates Start of Header, data before will and should be ignored
                    index += line.Length + NumOfEolChars;
                    while ((line = this.sR.ReadLine()) != null)
                    {
                        if (Regex.Match(line, @"\/\/.*\/\/").Success) // ignore comments
                        {
                            index += line.Length + NumOfEolChars;
                            continue;
                        }
                        if (line.Equals("$$BINARY"))
                        {
                            this.header.DataFormat = DataFormatType.binary;
                        }
                        if (line.Equals("$$ASCII"))
                        {
                            this.header.DataFormat = DataFormatType.ASCII;
                        }

                        if ((match = Regex.Match(line, @"\$\$UNITS\/(.*)")).Success)
                        {
                            this.header.Units = float.Parse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture);
                        }

                        if ((match = Regex.Match(line, @"\$\$VERSION\/(\d*)")).Success)
                        {
                            this.header.Version = int.Parse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture) / 100;
                        }

                        if ((match = Regex.Match(line, @"\$\$LABEL\/(.*)")).Success)
                        {
                            string name;
                            int id;
                            name = match.Groups[1].Value.Split(',')[1];
                            id = int.Parse(match.Groups[1].Value.Split(',')[0], NumberStyles.Any, CultureInfo.InvariantCulture);
                            this.parts.Add(new Part(id, name));
                        }

                        if ((match = Regex.Match(line, @"\$\$USERDATA\/(.*)")).Success)
                        {
                            string uid;
                            long len;
                            byte[] data;
                            uid = match.Groups[1].Value.Split(',')[0];
                            len = long.Parse(match.Groups[1].Value.Split(',')[1], NumberStyles.Any, CultureInfo.InvariantCulture);
                            data = Encoding.ASCII.GetBytes(match.Groups[1].Value.Split(',')[2]); // Not shure whether we can/should do this conversion :S
                            this.userData.Add(new UserData(data, len, uid));
                        }

                        if ((match = Regex.Match(line, @"\$\$DATE\/(\d*)")).Success)
                        {
                            this.header.Date = int.Parse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture);
                        }

                        if ((match = Regex.Match(line, @"\$\$DIMENSION\/(.*)")).Success)
                        {
                            //order:  x1, y1, z1, x2, y2, z2;
                            string[] sDimensions = match.Groups[1].Value.Split(',');

                            float[] fDimensions = new float[6];
                            for (int i = 0; i < sDimensions.Length && i < fDimensions.Length; i++)
                            {
                                string dim = sDimensions[i];
                                if (dim != "")
                                    fDimensions[i] = float.Parse(dim, NumberStyles.Any, CultureInfo.InvariantCulture);
                            }
                            this.header.Dimension = new Dimension(fDimensions[0], fDimensions[1], fDimensions[2], fDimensions[3], fDimensions[4], fDimensions[5]);
                        }

                        if ((match = Regex.Match(line, @"\$\$LAYERS\/(\d*)")).Success)
                        {
                            this.header.NumLayers = int.Parse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture);
                        }

                        if (line.Contains("$$HEADEREND"))
                        {//Contains because last line of the header isn't terminated by new line anymore
                            this.index += "$$HEADEREND".Length; //After "HEADEREND" follows direct Binary so we add the length to index, to get the exact start position of the binary parsing
                            return this.header;
                        }
                        else
                        {
                            this.index += line.Length + NumOfEolChars; //to Calculate position...stream reader is buffered...so it is easiest make it this way
                        }
                    }
                }
                throw new FileLoadException("Headerend not found");
            }
            throw new FileLoadException("Headerstart not found");
        }

        private IList<ILayer> ReadBinaryContent()
        {
            //this.BaseStream.Seek(index,SeekOrigin.Begin); //Sets the FileStream to the Begging of the Binary Part
            ushort ci;
            this.bR = new BinaryReader(new BufferedStream(this.sR.BaseStream));
            this.bR.BaseStream.Seek(index, SeekOrigin.Begin);
            IList<ILayer> layers = new List<ILayer>();
            while (this.bR.BaseStream.Position < this.bR.BaseStream.Length)
            {
                ci = this.bR.ReadUInt16();
                switch (ci)
                {
                    case layerStartLong:
                        {
                            // 127 Hex: 7F => Start Layer Long -> next Byte is Param and is of type 'real' -> 4bytes long
                            float z = this.bR.ReadSingle();//readHelper.readReal(this, ref index);
                            layers.Add(new Layer(index, z, true));
                            break;
                        }
                    case layerStartShort:
                        {
                            // 128 Hex: 80 => Start Layer Short -> next Byte is Param and is of type 'unit16' -> 2bytes long
                            UInt16 zShort = this.bR.ReadUInt16();//readHelper.readUint16(this, ref index);
                            layers.Add(new Layer(index, Convert.ToSingle(zShort), false));
                            break;
                        }
                    case polylineStartShort:
                        {
                            // 129 Hex: 81 => Start Polyline Short -> next Byte is Param and is of type 'real' -> 4bytes long
                            ushort id = this.bR.ReadUInt16();//readHelper.readUint16(this, ref index); //identifier to allow more than one model information in one file. id refers to the parameter id of command $$LABEL (HEADER-section). 
                            /*dir : orientation of the line when viewing in the negative z-direction 
                                    0 : clockwise 
                                    1 : counter clockwise 
                                    2 : open line 
                             */
                            ushort dir = this.bR.ReadUInt16();// readHelper.readUint16(this, ref index);
                            ushort n = this.bR.ReadUInt16(); //readHelper.readUint16(this, ref index); //number of points 

                            layers[layers.Count - 1].VectorBlocks.Add(new Polyline(this.bR.BaseStream.Position, id, (Direction)dir, n, false, this.bR)); //using the last layer in the list, since the format gives: Layer, it's Vectors, next Layer
                            this.bR.BaseStream.Seek(this.bR.BaseStream.Position + n * 2 * 2, SeekOrigin.Begin);
                            //For all non Layer Commands there is the second parameter, giving the number of VectorBlocks following
                            //at least that value has to parsed to see how far we have to jump to get to the next Layer/CI
                            break;
                        }
                    case polylineStartLong:
                        {
                            // 130 Hex: 82 => Start Polyline Long -> next Byte is Param and is of type 'int' -> 4bytes long, the Coords are here of type float!

                            int id = this.bR.ReadInt32(); // readHelper.readInt(this, ref index); //identifier to allow more than one model information in one file. id refers to the parameter id of command $$LABEL (HEADER-section). 
                            /*dir : orientation of the line when viewing in the negative z-direction 
                                    0 : clockwise 
                                    1 : counter clockwise 
                                    2 : open line 
                             */
                            int dir = this.bR.ReadInt32();//readHelper.readInt(this, ref index);
                            int n = this.bR.ReadInt32(); //readHelper.readInt(this, ref index); //number of points 
                            layers[layers.Count - 1].VectorBlocks.Add(new Polyline(this.bR.BaseStream.Position, id, (Direction)dir, n, true, this.bR)); //using the last layer in the list, since the format gives: Layer, it's Vectors, next Layer
                            this.bR.BaseStream.Seek(this.bR.BaseStream.Position + n * 2 * 4, SeekOrigin.Begin);
                            //index += n * 2 * 4; //for each vector value jump four bytes ahead
                            //For all non Layer Commands there is the second parameter, giving the number of VectorBlocks following
                            //at least that value has to parsed to see how far we have to jump to get to the next Layer/CI
                            break;
                        }
                    case hatchStartShort:
                        {
                            //Hex: 83 start Hatch Short
                            ushort id = this.bR.ReadUInt16();//readHelper.readUint16(this, ref index); //identifier to allow more than one model information in one file. id refers to the parameter id of command $$LABEL (HEADER-section). 
                            ushort n = this.bR.ReadUInt16();//readHelper.readUint16(this, ref index); //number of points 
                            layers[layers.Count - 1].VectorBlocks.Add(new Hatches(this.bR.BaseStream.Position, id, n, false, this.bR)); //using the last layer in the list, since the format gives: Layer, it's Vectors, next Layer

                            this.bR.BaseStream.Seek(this.bR.BaseStream.Position + n * 4 * 2, SeekOrigin.Begin);
                            //index += n * 4 * 2;
                            break;
                        }
                    case hatchStartLong:
                        {
                            // Hex: 84 'start hatch long
                            int id = this.bR.ReadInt32();//readHelper.readInt(this, ref index); //identifier to allow more than one model information in one file. id refers to the parameter id of command $$LABEL (HEADER-section). 
                            int n = this.bR.ReadInt32();//readHelper.readInt(this, ref index); //number of points 
                            layers[layers.Count - 1].VectorBlocks.Add(new Hatches(this.bR.BaseStream.Position, id, n, true, this.bR)); //using the last layer in the list, since the format gives: Layer, it's Vectors, next Layer
                            this.bR.BaseStream.Seek(this.bR.BaseStream.Position + n * 4 * 4, SeekOrigin.Begin);
                            break;
                        }
                    default:
                        throw new FormatException("invalid command code detected: " + ci.ToString());
                }
            }

            return layers;
        }

        private IList<ILayer> ReadASCIIContent()
        {
            String line;
            IList<ILayer> layers = new List<ILayer>();
            Layer currentLayer = null;
            //read first line
            if (this.sR.ReadLine() != "$$GEOMETRYSTART") throw new FileLoadException("no geometry found");
            while ((line = this.sR.ReadLine()) != null)
            {
                //optimization: hatches are most common, polylines 2nd
                if (line.StartsWith(@"$$HATCHES/"))
                {
                    var numberStrings = line.Substring(10).Split(',');
                    if (numberStrings.Length < 4)
                    {
                        throw new FileLoadException("invalid hatches: " + line);
                    }
                    int id = int.Parse(numberStrings[0], NumberStyles.Any, CultureInfo.InvariantCulture);
                    int n = int.Parse(numberStrings[1], NumberStyles.Any, CultureInfo.InvariantCulture);
                    float[] coordinates = new float[numberStrings.Length - 2];
                    for (int i = 0; i < coordinates.Length; i++)
                    {
                        coordinates[i] = float.Parse(numberStrings[i + 2], NumberStyles.Any, CultureInfo.InvariantCulture);
                    }
                    currentLayer.VectorBlocks.Add(new Hatches(id, n, coordinates));
                }
                if (line.StartsWith(@"$$POLYLINE/"))
                {
                    var numberStrings = line.Substring(11).Split(',');
                    if (numberStrings.Length < 5)
                    {
                        throw new FileLoadException("invalid hatches: " + line);
                    }
                    int id = int.Parse(numberStrings[0], NumberStyles.Any, CultureInfo.InvariantCulture);
                    Direction dir = (Direction)int.Parse(numberStrings[1], NumberStyles.Any, CultureInfo.InvariantCulture);
                    int n = int.Parse(numberStrings[2], NumberStyles.Any, CultureInfo.InvariantCulture);
                    float[] coordinates = new float[numberStrings.Length - 3];
                    for (int i = 0; i < coordinates.Length; i++)
                    {
                        coordinates[i] = float.Parse(numberStrings[i + 3], NumberStyles.Any, CultureInfo.InvariantCulture);
                    }
                    currentLayer.VectorBlocks.Add(new Polyline(id, dir, n, coordinates));
                }
                if (line.StartsWith(@"$$LAYER/"))
                {
                    float height = float.Parse(line.Substring(8), NumberStyles.Any, CultureInfo.InvariantCulture);
                    currentLayer = new Layer(height);
                    layers.Add(currentLayer);
                }
            }
            return layers;
        }

        /// <summary>
        /// write the given header
        /// </summary>
        /// <returns></returns>
        public static void WriteHeader(StreamWriter sW, ICLIFile file)
        {
            sW.NewLine = "\n"; //sets the newline format to unix lf
            var header = file.Header;
            sW.WriteLine("$$HEADERSTART");
            sW.WriteLine("$$ASCII");
            sW.WriteLine("$$UNITS/" + header.Units.ToString(CultureInfo.InvariantCulture));
            sW.WriteLine("$$VERSION/" + header.Version * 100); //Parameter v : integer, v divided by 100 gives the version number.For example 200-- > Version 2.00
            //and we interpret v alreay as 2 e.g, therefore it has to be written as 200
            foreach (var part in file.Parts)
            {
                sW.WriteLine("$$LABEL/" + part.id + "," + part.name);
            }
            sW.WriteLine("$$DATE/" + header.Date.ToString("D6"));
            sW.WriteLine("$$DIMENSION/" +
                header.Dimension.X1.ToString("0.000", CultureInfo.InvariantCulture) + "," +
                header.Dimension.Y1.ToString("0.000", CultureInfo.InvariantCulture) + "," +
                header.Dimension.Z1.ToString("0.000", CultureInfo.InvariantCulture) + "," +
                header.Dimension.X2.ToString("0.000", CultureInfo.InvariantCulture) + "," +
                header.Dimension.Y2.ToString("0.000", CultureInfo.InvariantCulture) + "," +
                header.Dimension.Z2.ToString("0.000", CultureInfo.InvariantCulture));
            sW.WriteLine("$$LAYERS/" + file.Geometry.Layers.Count.ToString("D6"));//ignore number in the header interface
            sW.WriteLine("$$HEADEREND");//no new line at header end
        }

        public void AppendLayer(StreamWriter sW, ILayer layer)
        {
            
            AppendLayerHeader(sW, layer);
            foreach (var block in layer.VectorBlocks)
            {
                AppendVectorBlock(sW, block);
            }
        }

        public void AppendLayerHeader(StreamWriter sW, ILayer layer)
        {
            sW.WriteLine("$$LAYER/" + layer.Height.ToString(CultureInfo.InvariantCulture));
        }


        public void AppendVectorBlock(StreamWriter sW, IVectorBlock block)
        {
            sW.WriteLine("$$POWER/" + this.MarkingParameterMap[block.Id].Item1);
            sW.WriteLine("$$SPEED/" + this.MarkingParameterMap[block.Id].Item2);
            if (block is IHatches)
            {
                if (block.Coordinates.Length % 4 != 0)
                {
                    throw new ArgumentException("Number of Points of Hatch is not a multiple of four.");
                }

                sW.Write("$$HATCHES/");
                sW.Write(block.Id+ ",");
                sW.Write((block.Coordinates.Length / 4) + ",");//ignore number in the layer interface for hatches and polylines
                var first = true;
                foreach (var coord in block.Coordinates)
                {
                    if (!first)
                    {
                        sW.Write(",");
                    }
                    else
                        first = false;
                    sW.Write(coord.ToString(CultureInfo.InvariantCulture));
                }
                sW.Write("");
            }
            else if (block is IPolyline)
            {
                if (block.Coordinates.Length % 2 != 0)
                {
                    throw new ArgumentException("Number of Points of Polylines is not even.");
                }
                Int32 dir;
                switch ((block as IPolyline).Dir)
                {
                    case Direction.clockwise:
                        dir = 0;
                        break;
                    case Direction.counterClockwise:
                        dir = 1;
                        break;
                    case Direction.openLine:
                        dir = 2;
                        break;
                    default:
                        throw new ArgumentException("direction (clockwise, counterClockwise, openLine) of polyline in block " + block.Id + " not set");
                }

                sW.Write("$$POLYLINE/");
                sW.Write(block.Id + ",");
                sW.Write(dir + ",");
                sW.Write((block.Coordinates.Length / 2) + ",");//ignore number in the layer interface// for hatches and polylines

                var first = true;
                foreach (var coord in block.Coordinates)
                {
                    if (!first)
                    {
                        sW.Write(",");
                    }
                    else
                        first = false;
                    sW.Write(coord.ToString(CultureInfo.InvariantCulture));
                }
                sW.Write("");
            }
            else
            {
                throw new ArgumentException("Invalid block detected. CLI Format only supports hatches and polylines. All blocks have to be either IHatches or IPolyline.");
            }
            sW.WriteLine("");
        }
    }
}
