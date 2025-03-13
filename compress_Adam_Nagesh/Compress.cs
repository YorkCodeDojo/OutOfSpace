using System.Security.Cryptography;
using System.Text;

namespace compress;

internal class Compress
{
  public const string ImagesFolder = "/Users/adampridmore/work/Dev/york-code-dojo/OutOfSpace/Images";
  public const string DestinationFolder = "/Users/adampridmore/work/Dev/york-code-dojo/OutOfSpace/ImagesCompressed";
  public const string DestinationFolderDecompressed = "/Users/adampridmore/work/Dev/york-code-dojo/OutOfSpace/ImagesDecompressed";

  public const string CompressDetailsFilename = "compress.txt";

  private static string ComputeHash(byte[] image)
  {
    var data = SHA256.HashData(image);
    var sBuilder = new StringBuilder();
    foreach (var t in data)
      sBuilder.Append(t.ToString("x2"));
    return sBuilder.ToString();
  }

  public void Execute()
  {

    var fileNames = Directory.GetFiles(ImagesFolder);
    
    Directory.Delete(DestinationFolder, true);
    Directory.CreateDirectory(DestinationFolder);
    
    using var compressStreamWriter = File.CreateText(Path.Combine(DestinationFolder, CompressDetailsFilename));

    var fileHashes = new Dictionary<string, List<string>>();

    foreach (var filename in (fileNames))
    {
      var fileHash = ComputeHash(File.ReadAllBytes(filename));
      if (fileHashes.TryGetValue(fileHash, out var matchingFileNames))
      {
        matchingFileNames.Add(filename);
      }
      else
      {
        fileHashes.Add(fileHash, new List<string>(new[] { filename }));
      }
    }

    foreach (var keyValue in fileHashes)
    {
      var fileNamesToCopy = keyValue.Value;

      if (fileNamesToCopy.Count == 1)
      {
        var filename = fileNamesToCopy.Single();
        var destinationFilename = Path.Combine(DestinationFolder, Path.GetFileName(filename));

        File.Copy(filename, destinationFilename);
      }
      else
      {
        var filename = fileNamesToCopy.First();
        var destinationFilename = Path.Combine(DestinationFolder, Path.GetFileName(filename));

        File.Copy(filename, destinationFilename);
        
        foreach (var fileNameThatIsACopy in fileNamesToCopy.Skip(1))
        {
          var compressionLine = $"{Path.GetFileName(filename)} COPY {Path.GetFileName(fileNameThatIsACopy)} ";
          compressStreamWriter.WriteLine(compressionLine);
        }
      }
    }

    // Write out hashes and number of duplicates
    // foreach (var keyValue in fileHashes)
    // {
    //   Console.WriteLine($"hash: {keyValue.Key} filename {keyValue.Value.Count}");
    // }
  }
}