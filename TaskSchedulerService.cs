namespace StrømAPI;

public class TaskSchedulerService
{
    private readonly Func<Task> _taskToExecuteAsync;
    private readonly TimeSpan _executionTime;
    private Timer? _timer;

    public TaskSchedulerService(Func<Task> taskToExecuteAsync, TimeSpan executionTime)
    {
        _taskToExecuteAsync = taskToExecuteAsync;
        _executionTime = executionTime;
    }

    public void ScheduleNextExecution()
    {
        DateTime now = DateTime.Now;
        DateTime nextExecutionTime = now.Date.Add(_executionTime);

        if (nextExecutionTime < now)
        {
            nextExecutionTime = nextExecutionTime.AddDays(1);
        }

        double interval = (nextExecutionTime - now).TotalMilliseconds;
        _timer = new Timer(ExecuteTaskAsync, null, (int)interval, Timeout.Infinite);
    }

    private async void ExecuteTaskAsync(object state)
    {
        await _taskToExecuteAsync();
        ScheduleNextExecution();
    }
}
