const string originalFolder = "/Users/davidbetteridge/OutOfSpace/Images";
const string compressedFolder = "/Users/davidbetteridge/OutOfSpace/Compressed";
const string decompressedFolder = "/Users/davidbetteridge/OutOfSpace/Decompressed";

// CompressFolder();
DecompressFolder();
Console.WriteLine("Press a key");
Console.ReadKey();

    
return;

void DecompressFolder()
{
    if (Directory.Exists(decompressedFolder)) Directory.Delete(decompressedFolder, true);
    Directory.CreateDirectory(decompressedFolder);
    
    //Copy over all sif files
    var listOfFiles = Directory.GetFiles(compressedFolder, "*.sif");
    foreach (var filename in listOfFiles)
        File.Copy(filename, Path.Combine(decompressedFolder, Path.GetFileName(filename)));
    
    //Process each different file
    var commands = File.ReadAllLines(Path.Combine(compressedFolder, "index.txt"));
    foreach (var command in commands)
    {
        var parts = command.Split(','); //Target, DIFF, Source
        RebuildFileFromDiff(Path.Combine(decompressedFolder, parts[0]), 
                            Path.Combine(decompressedFolder, parts[2]), 
                            Path.Combine(compressedFolder, Path.ChangeExtension(parts[0], "diff")));
    }
}

void CompressFolder()
{
    if (Directory.Exists(compressedFolder)) Directory.Delete(compressedFolder, true);
    Directory.CreateDirectory(compressedFolder);

    var listOfFiles = Directory.GetFiles(originalFolder, "*.sif");

    var fileContents = listOfFiles
        .Select(filename => new { filename = Path.GetFileName(filename), content = ReadFileAsPixels(filename) })
        .ToArray();

    var written = new Dictionary<string, (string, int)>(); //Source => Base, Size

    for (var baseIndex = 0; baseIndex < listOfFiles.Length - 1; baseIndex++)
    {
        for (var toCompareIndex = baseIndex + 1; toCompareIndex < listOfFiles.Length; toCompareIndex++)
        {
            var differences = FindDifferences(fileContents[baseIndex].content, fileContents[toCompareIndex].content);

            // To write out a difference needs 3 bytes for the index, and another 3 for the colour.
            // So to make it worth our while, we need n*6 < 1024*1024*3 
            if (differences.Count * 6 < (1024 * 1024 * 3))
            {
                // Have we already written a better version?
                var needToWrite = true;
                if (written.TryGetValue(fileContents[toCompareIndex].filename, out var details))
                    needToWrite = differences.Count < details.Item2;

                if (needToWrite)
                {
                    Console.WriteLine(
                        $"{fileContents[baseIndex].filename} to {fileContents[toCompareIndex].filename} is {differences.Count}");
                    
                    var differenceFilename =
                        Path.Combine(compressedFolder,
                            Path.ChangeExtension(fileContents[toCompareIndex].filename, "diff"));

                    WriteDifferenceFile(differenceFilename, differences);
                    written[fileContents[toCompareIndex].filename] =
                        (fileContents[baseIndex].filename, differences.Count);
                }
            }
        }
    }

    // Write index file
    var indexFilename = Path.Combine(compressedFolder, "index.txt");
    using var indexFile = File.CreateText(indexFilename);
    foreach (var wrote in written)
        indexFile.WriteLine($"{wrote.Key},DIFF,{wrote.Value.Item1}");

    // Copy over remaining files
    foreach (var filename in listOfFiles)
    {
        if (!written.ContainsKey(Path.GetFileName(filename)))
            File.Copy(filename, Path.Combine(compressedFolder, Path.GetFileName(filename)));
    }
}

void RebuildFileFromDiff(string targetFilename, string sourceFilename, string differencesFilename)
{
    var source = ReadFileAsPixels(sourceFilename);
    
    var diffs = File.ReadAllBytes(differencesFilename);
    var replacements = new Dictionary<int, Pixel>();
    var i = 0;
    while (i < diffs.Length)
    {
        var index = (diffs[i] << 0) | (diffs[i+1] << 8) | (diffs[i+2] << 16);
        var pixel = new Pixel(diffs[i + 3], diffs[i + 4], diffs[i + 5]);
        replacements[index] = pixel;
        i += 6;
    }

    using var target = File.Create(targetFilename);
    foreach (var (index, basePixel) in source.Index())
    {
        if (replacements.TryGetValue(index, out var rePixel))
        {
            target.WriteByte(rePixel.Red);
            target.WriteByte(rePixel.Green);
            target.WriteByte(rePixel.Blue);
        }
        else
        {
            target.WriteByte(basePixel.Red);
            target.WriteByte(basePixel.Green);
            target.WriteByte(basePixel.Blue);
        }
    }
}

void WriteDifferenceFile(string filename, List<PixelDifference> differences)
{
    using var file = File.Create(filename);
    foreach (var difference in differences)
    {
        file.WriteByte((byte)(difference.Index & 0xFF));
        file.WriteByte((byte)((difference.Index >> 8) & 0xFF));
        file.WriteByte((byte)((difference.Index >> 16) & 0xFF));
        
        var pixel = difference.Pixel;
        file.WriteByte(pixel.Red);
        file.WriteByte(pixel.Green);
        file.WriteByte(pixel.Blue);
    }
}

List<PixelDifference> FindDifferences(Pixel[] baseFile, Pixel[] fileToCompare)
{
    var differences = new List<PixelDifference>();

    foreach (var (index, basePixel) in baseFile.Index())
    {
        if (basePixel != fileToCompare[index])
        {
            differences.Add(new PixelDifference
            {
                Pixel = fileToCompare[index],
                Index = index
            });
        }
    }

    return differences;
}


Pixel[] ReadFileAsPixels(string filename)
{
    var source = File.ReadAllBytes(filename);
    var i = 0;
    var j = 0;
    var target = new Pixel[source.Length / 3];
    while (j < target.Length)
    {
        target[j] = new Pixel(source[i], source[i + 1], source[i + 2]);
        i += 3;
        j++;
    }

    return target;
}

record PixelDifference
{
    public required Pixel Pixel { get; init; }
    public required int Index { get; init; }
}

record Pixel(byte Red, byte Green, byte Blue);