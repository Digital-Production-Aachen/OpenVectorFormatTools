using OpenVectorFormat.ILTFileReader;


namespace IltCliWriterAdapter.CLI
{
    public class Hatch : Vector, IHatch
    {
        public Hatch(EuclidKoordinates initialPoint, EuclidKoordinates terminalPoint) : base(initialPoint, terminalPoint)
        {

        }
        public IPoint2D End => TerminalPoint;

        public IPoint2D Start => InitialPoint;
    }
}
