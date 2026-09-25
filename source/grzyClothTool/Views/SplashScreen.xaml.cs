using grzyClothTool.Helpers;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace grzyClothTool.Views;

public interface ISplashScreen
{
    void AddMessage(string message);
    Task LoadComplete();
    int MessageQueueCount { get; }
    void Shutdown();
}

public partial class SplashScreen : Window, ISplashScreen
{
    private readonly ConcurrentQueue<string> _messageQueue = new();
    private readonly DispatcherTimer _messageTimer;

    public int MessageQueueCount => _messageQueue.Count;

    public SplashScreen()
    {
        InitializeComponent();
        LocalizationHelper.ApplyTo(this);

        _messageTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(350)
        };
        _messageTimer.Tick += (_, _) => ProcessMessageQueue();
        _messageTimer.Start();
    }

    public void AddMessage(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            _messageQueue.Enqueue(message.Trim());
        }
    }

    private void ProcessMessageQueue()
    {
        if (_messageQueue.TryDequeue(out var message))
        {
            updateTextBox.Text = message;
        }
    }

    public async Task LoadComplete()
    {
        if (!Dispatcher.CheckAccess())
        {
            await Dispatcher.InvokeAsync(() =>
            {
                _messageTimer.Stop();
                updateTextBox.Text = LocalizationHelper.Translate("Редактор готов");
            });
        }
        else
        {
            _messageTimer.Stop();
            updateTextBox.Text = LocalizationHelper.Translate("Редактор готов");
        }

        var shutdownSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = Dispatcher.BeginInvoke(DispatcherPriority.Send, () =>
        {
            shutdownSignal.TrySetResult(true);
            Dispatcher.InvokeShutdown();
        });
        await shutdownSignal.Task;

        var mainWindow = Application.Current?.MainWindow;
        if (mainWindow != null)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                mainWindow.Visibility = Visibility.Visible;
                mainWindow.Activate();
            });
            await Task.Delay(180);
        }
    }

    public void Shutdown()
    {
        if (Dispatcher.CheckAccess())
        {
            _messageTimer.Stop();
            Dispatcher.InvokeShutdown();
            return;
        }

        Dispatcher.Invoke(() =>
        {
            _messageTimer.Stop();
            Dispatcher.InvokeShutdown();
        });
    }
}
