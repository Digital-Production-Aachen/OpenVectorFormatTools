using OpenVectorFormat.ILTFileReader;


namespace IltCliWriterAdapter.CLI
{
    public class Header : IHeader
    {
        public Header(int date, IDimension dimension, int numLayers, float units, IUserData userData, int version)
        {
            Date = date;
            Dimension = dimension;
            NumLayers = numLayers;
            Units = units;
            UserData = userData;
            Version = version;
        }
        private DataFormatType dataFormat = DataFormatType.ASCII;
        public DataFormatType DataFormat
        {
            get { return dataFormat; }
            set { dataFormat = value; }
        }

        public int Date { get; set; }

        public IDimension Dimension { get; set; }

        public int NumLayers { get; set; }

        public float Units { get; set; }

        public IUserData UserData { get; set; }

        public int Version { get; set; }
    }
}
