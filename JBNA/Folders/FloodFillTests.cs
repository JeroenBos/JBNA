using JBSnorro.Diagnostics;
public class FloodFillTests
{
    public void TestFloodfillA()
    {
        var rects = Floodfill.DivideMapInAreas(new int[,] { { 0 } });

        Contract.AssertSequenceEqual(rects, Array.Empty<Rectangle>());
    }
    public void TestFloodfillB()
    {
        var rects = Floodfill.DivideMapInAreas(new int[,] { { 1 } });

        if (!rects.SequenceEqual(new[] { new Rectangle(0, 0, 1, 1) }))
            throw new Exception();
    }
    public void TestFloodfillC()
    {
        var rects = Floodfill.DivideMapInAreas(
            new int[,] {
                { 0, 0, 0, 0, 0, },
                { 1, 1, 0, 0, 0, },
                { 0, 1, 1, 0, 0, },
                { 0, 0, 0, 0, 0, }
            });

        if (!rects.SequenceEqual(new[] { new Rectangle(0, 1, 3, 2) }))
            throw new Exception();
    }

    public void TestFloodfillD()
    {
        var rects = Floodfill.DivideMapInAreas(
            new int[,] {
                { 0, 0, 1, 0, 0, },
                { 1, 1, 1, 1, 1, },
                { 0, 1, 1, 0, 0, },
                { 0, 1, 0, 0, 1, }
            });

        if (!rects.SequenceEqual(new[] { new Rectangle(0, 0, 5, 4), new Rectangle(4, 3, 1, 1) }))
            throw new Exception();
    }
}
