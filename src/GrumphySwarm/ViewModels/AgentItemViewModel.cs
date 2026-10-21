using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GrumphySwarm.Core.Models;

namespace GrumphySwarm.ViewModels;

public sealed partial class AgentItemViewModel : ObservableObject
{
    private readonly AgentProfile _profile;
    private readonly Action<AgentItemViewModel> _onEdit;
    private readonly Action<AgentItemViewModel> _onDelete;

    public AgentProfile Profile => _profile;

    public string Id => _profile.Id;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _description;

    [ObservableProperty]
    private AgentTaskType _taskType;

    [ObservableProperty]
    private string _statusNote;

    public string TaskTypeLabel => TaskType switch
    {
        AgentTaskType.DesktopAssistant => "Desktop Assistant",
        AgentTaskType.TaskMonitor => "Task Monitor",
        AgentTaskType.ScreenUnderstander => "Screen Understander",
        AgentTaskType.FileOrganizer => "File Organizer",
        _ => "Custom Task"
    };

    public IRelayCommand EditCommand { get; }
    public IRelayCommand DeleteCommand { get; }

    public AgentItemViewModel(
        AgentProfile profile,
        Action<AgentItemViewModel> onEdit,
        Action<AgentItemViewModel> onDelete)
    {
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _onEdit = onEdit ?? throw new ArgumentNullException(nameof(onEdit));
        _onDelete = onDelete ?? throw new ArgumentNullException(nameof(onDelete));

        _name = profile.Name;
        _description = profile.Description;
        _taskType = profile.TaskType;
        _statusNote = profile.StatusNote;

        EditCommand = new RelayCommand(() => _onEdit(this));
        DeleteCommand = new RelayCommand(() => _onDelete(this));
    }

    public void UpdateFrom(string name, string description, AgentTaskType taskType)
    {
        Name = name;
        Description = description;
        TaskType = taskType;

        _profile.Name = name;
        _profile.Description = description;
        _profile.TaskType = taskType;

        OnPropertyChanged(nameof(TaskTypeLabel));
    }
}
