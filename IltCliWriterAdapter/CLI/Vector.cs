
using IltCliWriterAdapter.CLI;

namespace IltCliWriterAdapter
{
    public class Vector
    {
        public Vector(EuclidKoordinates initialPoint, EuclidKoordinates terminalPoint)
        {
            InitialPoint = initialPoint;
            TerminalPoint = terminalPoint;
        }

        public EuclidKoordinates InitialPoint { get; set; }
        public EuclidKoordinates TerminalPoint { get; set; }
    }
}
