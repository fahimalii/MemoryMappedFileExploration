using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace MemoryMappedFileDemo.MemoryMappedUtils
{
    public class MemoryMap<T> : IDisposable where T : class
    {
        private MemoryMappedFile _memoryMappedFile;
        private long _fileLength;

        public string MemoryMapName { get; private set; }
        public string MemoryMapFileDirectory { get; set; }
        public string MemoryMapFilePath { get; set; }
        public string MutexName { get; private set; }
        public bool LoadedFromFile = false;

        public MemoryMap(string memoryMapName, string directoryPath)
        {
            Validate(memoryMapName);

            MemoryMapName = memoryMapName;
            MutexName = $"{MemoryMapName}-Mutex"; // Note: This MUST BE Unique throughout the server, to avoid accidentally blocking some other resource

            MemoryMapFileDirectory = directoryPath;
            MemoryMapFilePath = Path.Combine(MemoryMapFileDirectory, "info.json"); // TODO
        }

        private MemoryMappedFile GetMemoryMappedFile()
        {
            var mutex = GetMutex();

            try
            {
                _memoryMappedFile = MemoryMappedFile.OpenExisting(mapName: MemoryMapName);
                LoadedFromFile = false;
            }
            catch (FileNotFoundException ex)
            {
                _memoryMappedFile = MemoryMappedFile.CreateFromFile(path: MemoryMapFilePath, FileMode.Open, mapName: MemoryMapName, capacity: 0, access: MemoryMappedFileAccess.ReadWrite);
                LoadedFromFile = true;
            }
            finally
            {
                var fileInfo = new FileInfo(MemoryMapFilePath);
                _fileLength = fileInfo.Length;
            }

            mutex.ReleaseMutex();

            return _memoryMappedFile;
        }

        public void WriteData(T obj)
        {
            if (_memoryMappedFile == null)
            {
                GetMemoryMappedFile();
            }

            var mutex = GetMutex();
            var temp2 = Serialize(obj);

            using (var view = _memoryMappedFile.CreateViewAccessor())
            {
                view.WriteArray(position: 0, array: temp2, offset: 0, count: temp2.Length);
            }

            mutex.ReleaseMutex();
        }

        public T ReadData()
        {
            if (_memoryMappedFile == null)
            {
                GetMemoryMappedFile();
            }

            var mutex = GetMutex();

            using (var viewStream = _memoryMappedFile.CreateViewStream(offset: 0, size: _fileLength, access: MemoryMappedFileAccess.ReadWrite))
            {
                using (BinaryReader binaryReader = new BinaryReader(viewStream))
                {
                    var result = binaryReader.ReadBytes((int)viewStream.Length);

                    mutex.ReleaseMutex();
                    
                    var temp2 = Deserialize(result);

                    return temp2;
                }
            }
        }

        private Mutex GetMutex()
        {
            if(Mutex.TryOpenExisting(MutexName, out Mutex mutex))
            {
                mutex.WaitOne();
            }
            else
            {
                mutex = new Mutex(initiallyOwned: true, name: MutexName, createdNew: out bool mutexCreated);

                if(!mutexCreated)
                {
                    mutex.WaitOne();
                }
            }

            return mutex;
        }

        private byte[] Serialize(T obj)
        {
            var data = JsonSerializer.Serialize(
                value: obj,
                inputType: typeof(T),
                options: new JsonSerializerOptions()
                {
                    WriteIndented = true
                }
            );

            var byteData = Encoding.UTF8.GetBytes(data);

            return byteData;
        }

        private T Deserialize(byte[] data)
        {
            var resultData = Encoding.UTF8.GetString(data);
            var output = JsonSerializer.Deserialize(resultData, typeof(FileStatus), new JsonSerializerOptions() { WriteIndented = true }) as T;

            return output;
        }

        private void Validate(string memoryMapName)
        {
            if (string.IsNullOrWhiteSpace(memoryMapName))
            {
                throw new ArgumentNullException(nameof(memoryMapName));
            }

            if (memoryMapName.IndexOfAny(Path.GetInvalidPathChars()) > 0)
            {
                throw new ArgumentException($"{memoryMapName} contains invalid characters.");
            }       
        }

        public void Dispose()
        {
            _memoryMappedFile?.Dispose();
        }
    }
}
