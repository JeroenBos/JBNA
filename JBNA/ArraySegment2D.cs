//public struct TwoDimensionalScalingFunction<T>
//{
//    private readonly ArraySegment<byte> data;
//    private readonly Func<byte, T> getValue;
//    private readonly float stepsPerRow;
//    private readonly float stepsPerCell;
//    public TwoDimensionalScalingFunction(
//        ArraySegment<byte> data,
//        int projectedWidth,
//        int projectedHeight,
//        Func<byte, T> getValue)
//    {
//        this.data = data;
//        this.getValue = getValue;
//        this.stepsPerRow = data.Count / projectedHeight;
//        this.stepsPerCell = this.stepsPerRow / projectedWidth;
//    }
//    public T GetValue(Point p, int width, int height)
//    {
//        float f = p.Y * stepsPerRow + p.X * stepsPerCell;
//        int i = (int)f;
//        var result = getValue(data[i]);
//        return result;

//    }
//}
