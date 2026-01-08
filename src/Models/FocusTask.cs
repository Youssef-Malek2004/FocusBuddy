namespace Focus.AI.Models;

public class FocusTask
{
    public string Task { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }

    public FocusTask(string task)
    {
        Task = task;
        StartTime = DateTime.Now;
    }
}
