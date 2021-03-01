using MemoryMappedFileDemo.Controllers;
using MemoryMappedFileDemo.MemoryMappedUtils;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MemoryMappedFileDemo
{
    internal class MemoryMappedFileWorker : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            Debug.Write("Sleeping for 5 second");
            Thread.Sleep(5000);
            Debug.Write("Reading Data");

            var memoryMap = new MemoryMap<FileStatus>(memoryMapName: "demo", "TODO");

            var data = memoryMap.ReadData();

            Debug.Write("Data Read Done");

            HomeController._fileStatus = data;
            
            Debug.Write("Data Update Done");

            Debug.Write("Sleeping for 5 second");

            Thread.Sleep(5000);

            Debug.Write("Writing Data");

            var fileStatus = new FileStatus()
            {
                FirstFileChanged = 1,
                SecondFileChanged = 0,
                ThirdFileChanged = 0,
                FourthFileChanged = 0,
                FifthFileChanged = 1
            };

            memoryMap.WriteData(fileStatus);

            Debug.Write("Data Write Done");

            var data2 = memoryMap.ReadData();

            HomeController._fileStatus = data;

            return Task.CompletedTask;
        }

        private void RandomTest1Success()
        {
            var t = new FileStatus().LoadJson();
            var p = t.LoadJson();
            var data = MemoryMapUtility.ReadMemoryMappedFile();
            var data2 = Encoding.UTF8.GetString(data);
            var p2 = JsonSerializer.Deserialize(data2, typeof(FileStatus), new JsonSerializerOptions() { WriteIndented = true }) as FileStatus;

            var fileStatus = new FileStatus()
            {
                FirstFileChanged = 0,
                SecondFileChanged = 1,
                ThirdFileChanged = 0,
                FourthFileChanged = 0,
                FifthFileChanged = 1
            };

            var d2 = JsonSerializer.Serialize(
                value: fileStatus,
                inputType: typeof(FileStatus),
                options: new JsonSerializerOptions()
                {
                    WriteIndented = true
                }
                );

            var byteData = Encoding.UTF8.GetBytes(d2);
            MemoryMapUtility.WriteData(byteData);

            data = MemoryMapUtility.ReadMemoryMappedFile();
            data2 = Encoding.UTF8.GetString(data);
            p2 = JsonSerializer.Deserialize(data2, typeof(FileStatus)) as FileStatus;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}