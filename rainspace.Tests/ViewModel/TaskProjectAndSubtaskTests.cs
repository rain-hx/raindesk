using rainspace.ViewModel;

namespace rainspace.Tests.ViewModel;

[TestClass]
public class TaskProjectAndSubtaskTests
{
    [TestMethod]
    public void TaskProject_SettingSameName_DoesNotRaisePropertyChanged()
    {
        var project = new TaskProject { Name = "Work" };
        int changeCount = 0;
        project.PropertyChanged += (_, _) => changeCount++;

        project.Name = "Work";

        Assert.AreEqual(0, changeCount);
    }

    [TestMethod]
    public void TaskProject_SettingDifferentColor_RaisesColorAndBrushChanges()
    {
        var project = new TaskProject();
        var propertyNames = new List<string?>();
        project.PropertyChanged += (_, e) => propertyNames.Add(e.PropertyName);

        project.Color = "#112233";

        CollectionAssert.AreEqual(
            new[] { nameof(TaskProject.Color), nameof(TaskProject.ColorBrush) },
            propertyNames);
    }

    [TestMethod]
    public void Subtask_SettingSameCompletionState_DoesNotRaisePropertyChanged()
    {
        var subtask = new Subtask { IsCompleted = true };
        int changeCount = 0;
        subtask.PropertyChanged += (_, _) => changeCount++;

        subtask.IsCompleted = true;

        Assert.AreEqual(0, changeCount);
    }

    [TestMethod]
    public void Subtask_SettingDifferentTitle_RaisesPropertyChangedOnce()
    {
        var subtask = new Subtask();
        int changeCount = 0;
        string? propertyName = null;
        subtask.PropertyChanged += (_, e) =>
        {
            changeCount++;
            propertyName = e.PropertyName;
        };

        subtask.Title = "Draft notes";

        Assert.AreEqual(1, changeCount);
        Assert.AreEqual(nameof(Subtask.Title), propertyName);
    }
}
