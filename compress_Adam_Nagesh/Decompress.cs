namespace compress;

public class Decompress
{
    public void Execute()
    {
        var filenames = Directory
            .GetFiles(Compress.DestinationFolder,"*.sif");

        Directory.Delete(Compress.DestinationFolderDecompressed, true);
        Directory.CreateDirectory(Compress.DestinationFolderDecompressed);

        foreach (var filename in filenames)
        {
            var destinationFilename = Path.Combine(Compress.DestinationFolderDecompressed, Path.GetFileName(filename));
            File.Copy(filename, destinationFilename);
        }

        var decompressionDetailsLines = File.ReadAllLines(Path.Combine(Compress.DestinationFolder, Compress.CompressDetailsFilename));

        foreach (var compressionLine in decompressionDetailsLines)
        {
            var parts = compressionLine.Split(" ");
            var sourceFilename = Path.Combine(Compress.DestinationFolderDecompressed, parts[0]);
            var destinationFilename = Path.Combine(Compress.DestinationFolderDecompressed, parts[2]);

            File.Copy(sourceFilename, destinationFilename);
        }
    } 
}