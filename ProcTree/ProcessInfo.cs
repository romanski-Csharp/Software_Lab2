namespace ProcTree;

/// <summary>
/// Інформація про один процес: ідентифікатор, ідентифікатор батька та назва.
/// </summary>
public sealed record ProcessInfo(int Pid, int ParentPid, string Name);
