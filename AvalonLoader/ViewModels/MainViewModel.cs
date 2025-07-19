using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia;
using ReactiveUI;
using System.Collections.Generic;
using System.Reactive;
using System.Threading.Tasks;
using System;
using System.IO;
using Avalonia.Threading;
using AvalonLoader.Loader;
using System.Diagnostics;
using System.Linq;
using Avalonia.Controls;
using System.Collections.ObjectModel;

namespace AvalonLoader.ViewModels;

public class MainViewModel : ViewModelBase
{
    public class ProcessInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Bitmap Icon { get; set; }
    }

    private string _statusMessage = "Ready";
    private bool _isBusy;
    private ProcessInfo _selectedProcess;
    private string _dllPath;
    private string _rootPath = Path.Combine(Environment.CurrentDirectory, "Data");
    private string _rootScripts = Path.Combine(Environment.CurrentDirectory, "Scripts");

    public ObservableCollection<ProcessInfo> Processes { get; private set; } = new ObservableCollection<ProcessInfo>();
    public ReactiveCommand<Unit, Unit> RefreshProcessesCommand { get; }
    public ReactiveCommand<Unit, Unit> BrowseDllCommand { get; }
    public ReactiveCommand<Unit, Unit> InjectCommand { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }

    public ProcessInfo SelectedProcess
    {
        get => _selectedProcess;
        set => this.RaiseAndSetIfChanged(ref _selectedProcess, value);
    }

    public string DllPath
    {
        get => _dllPath;
        set => this.RaiseAndSetIfChanged(ref _dllPath, value);
    }

    public string RootPath
    {
        get => _rootPath;
        set => this.RaiseAndSetIfChanged(ref _rootPath, value);
    }

    public string RootScripts
    {
        get => _rootScripts;
        set => this.RaiseAndSetIfChanged(ref _rootScripts, value);
    }

    public MainViewModel()
    {
        RefreshProcessesCommand = ReactiveCommand.Create(RefreshProcesses);
        BrowseDllCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select DLL to inject",
                Filters = { new FileDialogFilter { Name = "DLL Files", Extensions = { "dll" } } },
                AllowMultiple = false
            };

            var result = await dialog.ShowAsync(new Window());
            if (result != null && result.Length > 0)
            {
                DllPath = result[0];
            }
        });

        InjectCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            if (SelectedProcess == null)
            {
                StatusMessage = "Please select a target process";
                return;
            }

            if (string.IsNullOrWhiteSpace(DllPath) || !File.Exists(DllPath))
            {
                StatusMessage = "Please select a valid DLL file";
                return;
            }

            IsBusy = true;
            StatusMessage = "Injecting DLL...";

            try
            {
                await Task.Run(() =>
                {
                    var parameters = new Injection.InjectionParameters
                    {
                        RootPath = RootPath,
                        RootScripts = RootScripts
                    };

                    bool success = Injection.Inject(
                        SelectedProcess.Id,
                        DllPath,
                        parameters);

                    Dispatcher.UIThread.Post(() =>
                    {
                        StatusMessage = success
                            ? $"Successfully injected into {SelectedProcess.Name}"
                            : $"Failed to inject into {SelectedProcess.Name}";
                    });
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        });

        RefreshProcesses();
    }

    private async Task RefreshProcesses()
    {
        IsBusy = true;
        StatusMessage = "Loading processes...";

        var processes = Process.GetProcesses()
                   .Where(p => p.Id != Process.GetCurrentProcess().Id)
                   .OrderBy(p => p.ProcessName)
                   .Select(p =>
                   {
                       try
                       {
                           string processName = Path.GetFileNameWithoutExtension(p.ProcessName);
                           string iconPath = p.MainModule?.FileName;

                           var icon = iconPath != null && File.Exists(iconPath)
                               ? GetProcessIcon(iconPath)
                               : GetDefaultIcon();

                           return new ProcessInfo
                           {
                               Id = p.Id,
                               Name = $"{processName}",
                               Icon = icon
                           };
                       }
                       catch
                       {
                           return new ProcessInfo
                           {
                               Id = p.Id,
                               Name = p.ProcessName,
                               Icon = GetDefaultIcon()
                           };
                       }
                   });
    }
    

    private Bitmap GetProcessIcon(string path)
    {
        using var stream = File.OpenRead(path);
        return new Bitmap(stream);
    }

    private Bitmap GetDefaultIcon()
    {
        return new Bitmap(AssetLoader.Open(new Uri("avares://AvalonLoader/Assets/default-process-icon.png")));
    }
}

