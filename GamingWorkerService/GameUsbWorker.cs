using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace GamingWorkerService
{
    public class GameUsbWorker : BackgroundService
    {
        private readonly ILogger<GameUsbWorker> _logger;
        private ManagementEventWatcher? _usbWatcher;

        public GameUsbWorker(ILogger<GameUsbWorker> logger) => this._logger = logger;

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this._logger.LogInformation("Serviço de jogos iniciado.");
            this._usbWatcher = new ManagementEventWatcher((EventQuery)new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2"));
            this._usbWatcher.EventArrived += new EventArrivedEventHandler(this.OnUsbArrived);
            this._usbWatcher.Start();
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            this._logger.LogInformation("Serviço de jogos parado.");
            if (this._usbWatcher != null)
            {
                this._usbWatcher.Stop();
                this._usbWatcher.Dispose();
            }
            return base.StopAsync(cancellationToken);
        }

        private void OnUsbArrived(object sender, EventArrivedEventArgs e)
        {
            try
            {
                string driveName = e.NewEvent.Properties["DriveName"].Value?.ToString();
                if (string.IsNullOrEmpty(driveName))
                    return;
                string drivePath = driveName + "\\";
                DriveInfo drive = new DriveInfo(drivePath);
                if (drive.DriveType != DriveType.Removable || !drive.IsReady)
                    return;

                string runFilePath = Path.Combine(drive.RootDirectory.FullName, "run.txt");
                string argsFilePath = Path.Combine(drive.RootDirectory.FullName, "args.txt");

                if (File.Exists(runFilePath))
                {
                    try
                    {
                        string[] executables = File.ReadAllLines(runFilePath);

                        if (executables.Length > 1)
                        {
                            new Thread(() =>
                            {
                                Application.EnableVisualStyles();
                                Application.Run(new GameLauncherForm(drivePath));
                            })
                            { ApartmentState = ApartmentState.STA }.Start();
                        } else
                        {
                            string exePath = File.ReadAllText(runFilePath).Trim();
                            string execPath = drivePath + exePath;

                            string args = File.Exists(argsFilePath) ? File.ReadAllText(argsFilePath).Trim() : "";

                            if (!string.IsNullOrWhiteSpace(exePath) && File.Exists(execPath))
                            {
                                TaskManager.RunProcess(execPath, args);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        this._logger.LogError(ex, "Erro ao processar 'run.txt' em {DrivePath}", (object)drivePath);
                    }
                }
            }
            catch (IOException ex)
            {
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Ocorreu um erro geral no monitoramento do USB.");
            }
        }
    }
}
