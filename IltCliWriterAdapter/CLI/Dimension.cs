using OpenVectorFormat.ILTFileReader;


namespace IltCliWriterAdapter.CLI
{
    public class Dimension : IDimension
    {
        public Dimension(float zmin, float zmax, float xmin, float xmax, float ymin, float ymax)
        {
            Z1 = zmin;
            Z2 = zmax;
            X1 = xmin;
            X2 = xmax;
            Y1 = ymin;
            Y2 = ymax;
        }
        public float X1 { get; set; }
        public float X2 { get; set; }
        public float Y1 { get; set; }
        public float Y2 { get; set; }
        public float Z1 { get; set; }
        public float Z2 { get; set; }
    }
}
