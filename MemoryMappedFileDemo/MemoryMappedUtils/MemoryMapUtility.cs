using System.IO;
using System.IO.MemoryMappedFiles;

namespace MemoryMappedFileDemo.MemoryMappedUtils
{
    public static class MemoryMapUtility
    {
        private static string FilePath = @"D:\dotnet\MemoryMappedFileDemo\MemoryMappedFileDemo\wwwroot\data\info.json";
        private static string FileMapName = "demo";
        private static long _fileLength;
        private static MemoryMappedFile memoryMappedFile;

        private static void Initialize()
        {
            try
            {
                memoryMappedFile = MemoryMappedFile.OpenExisting(mapName: FileMapName);
            }
            catch (FileNotFoundException ex)
            {
                memoryMappedFile = MemoryMappedFile.CreateFromFile(path: FilePath, FileMode.Open, mapName: FileMapName, capacity: 0, access: MemoryMappedFileAccess.ReadWrite);
                var fileInfo = new FileInfo(FilePath);
                _fileLength = fileInfo.Length;
            }
        }

        private static void Dispose()
        {
            memoryMappedFile?.Dispose();
        }
        
        public static byte[] ReadMemoryMappedFile()
        {
            if(memoryMappedFile == null)
            {
                Initialize();
            }

            using (var viewStream = memoryMappedFile.CreateViewStream(offset: 0, size: _fileLength, access: MemoryMappedFileAccess.ReadWrite))
            {
                using (BinaryReader binaryReader = new BinaryReader(viewStream))
                {
                    var result = binaryReader.ReadBytes((int)viewStream.Length);
                    return result;
                }
            }
        }

        public static void WriteData(byte[] data)
        {
            if(memoryMappedFile == null)
            {
                Initialize();
            }

            using (var view = memoryMappedFile.CreateViewAccessor())
            {
                view.WriteArray(position: 0, array: data, offset: 0, count: data.Length);
            }
        }
    }
}
