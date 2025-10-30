using Microsoft.Win32.TaskScheduler;

public class TaskManager
{
    public static void RunProcess(string filepath, string args)
    {
        string taskName = "RunGameFromUsbStick";
        string workingDir = Path.GetDirectoryName(filepath);

        using (TaskService ts = new TaskService())
        {
            var existing = ts.FindTask(taskName);
            if (existing != null)
            {
                ts.RootFolder.DeleteTask(taskName);
            }

            TaskDefinition td = ts.NewTask();
            td.RegistrationInfo.Description = "Executa o jogo do pendrive";

            td.Actions.Add(new ExecAction(filepath, args, workingDir));

            td.Principal.LogonType = TaskLogonType.InteractiveToken;

            ts.RootFolder.RegisterTaskDefinition(taskName, td);
        }

        using (TaskService ts = new TaskService())
        {
            ts.FindTask(taskName).Run();
        }
    }
}
