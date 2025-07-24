using System.Diagnostics;

namespace BlockGameTesting;

public class Loops {
    
    
    /**
     * Loops until 1 million iteractions has passed, prints the elapsed time in ms.
     */
    [Test]
    public void loop() {
        int i = 0;
        var s = new Stopwatch();
        s.Start();
        
        while (i < 1000000) {
            i++;
        }
        s.Stop();
        var end = s.Elapsed.TotalMicroseconds;
        Console.WriteLine($"Elapsed time: {end}, {i}");
    }
    
    [Test]
    public void loop2() {
        // call loop 40 times first
        for (int j = 0; j < 40; j++) {
            loop();
        }
        // time it for real
        var s = new Stopwatch();
        s.Start();
        loop();
        s.Stop();
        var end = s.Elapsed.TotalMicroseconds;
        Console.WriteLine($"Elapsed time (final): {end}");
    }
    
}